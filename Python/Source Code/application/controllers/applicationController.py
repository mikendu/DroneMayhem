import time
import openvr
import cflib.crtp
from PyQt5.QtCore import QObject, QTimer, QThreadPool, pyqtSignal

from application.util import dialogUtil, threadUtil, exceptionUtil
from .sequenceController import SequenceController
from .baseStationController import BaseStationController
from .swarmController import SwarmController


class ApplicationController(QObject):
    
    sequenceLoaded = pyqtSignal()    
    scanStarted = pyqtSignal()
    scanFinished = pyqtSignal()
    sequenceSelected = pyqtSignal()
    startTimer = pyqtSignal()
    sequenceStarted = pyqtSignal()
    sequenceUpdated = pyqtSignal()
    sequenceFinished = pyqtSignal()
    droneDisconnected = pyqtSignal(str)
    addLogEntry = pyqtSignal(tuple)

    def __init__(self, mainWindow, appSettings):
        super().__init__()
        
        self.sequencePlaying = False
        self.appSettings = appSettings
        self.mainWindow = mainWindow
        self.sequenceController = SequenceController(self, appSettings)
        
        self.initializePositioning()
        self.initializeSwarm()
        self.scanInProgress = False
        self.scanForDrones()
        self.sequenceTimer = QTimer()
        self.sequenceTimer.timeout.connect(self.updateSequence)
        self.sequenceTimer.setInterval(20) # in MS
        self.elapsedTime = 0
        self.sequenceProgress = 0
        self.startTimestamp = None

        self.killTimer = QTimer()
        self.killTimer.timeout.connect(self.hardKill)
        self.killTimer.setSingleShot(True)

        self.startTimer.connect(self.startSequenceTimer)
        self.sequenceFinished.connect(self.stopSequenceTimer)
        self.droneDisconnected.connect(self.onDroneDisconnected)


    def initializePositioning(self):
        self.vrSystem = openvr.init(openvr.VRApplication_Other)
        self.baseStationController = BaseStationController(self)

    def initializeSwarm(self):
        cflib.crtp.init_drivers()
        self.swarmController = SwarmController(self)


    def scanForDrones(self, force = False):
        if self.scanInProgress:
            return

        if not force and self.sequencePlaying:
            self.mainWindow.showStatusMessage("Cannot scan for drones while sequence is playing.")
            return

        self.scanInProgress = True
        self.scanStarted.emit()
        threadUtil.runInBackground(self.performScan)
        

    def performScan(self):
        self.swarmController.scan()
        self.scanFinished.emit()
        self.scanInProgress = False


    def openSequence(self, file):
        if self.sequenceController.loadSequence(file):
            sequenceFileList = list(map(lambda seq: seq.fullPath, self.sequenceController.sequences))
            self.appSettings.updateSequences(sequenceFileList)
            self.sequenceLoaded.emit()
            return True
            
        return False

    def selectSequence(self, index):
        if self.sequencePlaying:
            self.mainWindow.showStatusMessage("Cannot change sequence while playing.")
            return

        if self.sequenceController.selectSequence(index):
            self.sequenceSelected.emit()

    def onSequenceClick(self):
        if not self.sequencePlaying:
            self.startSequence()
        else:
            self.abortSequence()

    def startSequence(self):
        requiredDrones = SequenceController.CURRENT.drones
        availableDrones = len(self.swarmController.drones)
        if availableDrones < requiredDrones:
            message = "Sequence requires " + str(requiredDrones) + " drone(s).\n" + str(availableDrones) + " drone(s) available."
            dialogUtil.modalDialog("Cannot start sequence", message)
            return

        exceptionUtil.setInterrupt(False)
        self.sequencePlaying = True
        self.sequenceStarted.emit()
        self.killTimer.stop()

        
        self.elapsedTime = 0
        self.sequenceProgress = 0
        self.startTimestamp = None
        self.sequenceUpdated.emit()

        threadUtil.runInBackground(self.sequenceController.run)
        

    def startSequenceTimer(self):
        self.sequenceTimer.start()
        self.startTimestamp = time.time()
        self.elapsedTime = 0
        self.sequenceProgress = 0

    def stopSequenceTimer(self):
        self.sequenceTimer.stop()

    def abortSequence(self): 
        if self.sequencePlaying:
            exceptionUtil.setInterrupt(True)
            self.stopSequenceTimer()
            self.killTimer.start(5000)

    def hardKill(self):
        if (self.sequencePlaying):    
            self.swarmController.disconnectSwarm()
            self.sequenceFinished.emit()
            self.sequenceUpdated.emit()
            self.sequencePlaying = False

    def onSequenceCompleted(self):
        self.sequenceFinished.emit()
        self.sequencePlaying = False
        # self.elapsedTime = 0
        # self.sequenceProgress = 0
        # self.startTimestamp = None
        # self.sequenceUpdated.emit()

    def updateSequence(self):
        if self.startTimestamp:
            sequenceDuration = SequenceController.CURRENT.duration
            self.elapsedTime = min(time.time() - self.startTimestamp, sequenceDuration)
            self.sequenceProgress = self.elapsedTime / sequenceDuration
        else:
            self.elapsedTime = 0
            self.sequenceProgress = 0

        self.sequenceUpdated.emit()

    def onDroneDisconnected(self, uri):
        print("-- Drone Disconnected: " + uri + " --")
        if self.swarm and uri in self.swarm._cfs:
            dialogUtil.nonModalDialog(
                "Drone Connection Error",
                "Lost connection to a drone!\nAborting sequence.",
                self.mainWindow
            )
            self.abortSequence()
            QThreadPool.globalInstance().waitForDone()
            self.scanForDrones(True)

    def cleanup(self):
        self.swarmController.disconnectSwarm()
    
    @property
    def swarm(self):
        return self.swarmController.swarm

    @property
    def swarmArguments(self):
        return self.swarmController.swarmArguments