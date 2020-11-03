import openvr
import cflib.crtp
from cflib.crazyflie import Crazyflie
from cflib.crazyflie.syncCrazyflie import SyncCrazyflie

from PyQt5.QtCore import *

from model import *
from .sequenceController import *
from .baseStationController import *
from .swarmController import *
from util.threadUtil import *
from util.dialogUtil import *
from util.exceptionUtil import *


class ApplicationController(QObject):
    
    sequenceLoaded = pyqtSignal()    
    scanStarted = pyqtSignal()
    scanFinished = pyqtSignal()
    sequenceSelected = pyqtSignal()
    startTimer = pyqtSignal()
    sequenceStarted = pyqtSignal()
    sequenceUpdated = pyqtSignal()
    sequenceFinished = pyqtSignal()
    droneDisconnected = pyqtSignal()

    def __init__(self, mainWindow, appSettings):
        super().__init__()
        
        INTERRUPT_FLAG = False
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
        self.sequenceTimer.setInterval(100) # in MS

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
        runInBackground(self.performScan)
        

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
        requiredDrones = self.sequenceController.currentSequence.drones
        availableDrones = len(self.swarmController.drones)
        if availableDrones < requiredDrones:
            message = "Sequence requires " + str(requiredDrones) + " drone(s).\n" + str(availableDrones) + " drone(s) available."
            showDialog("Cannot start sequence", message, None, True)
            return
        
        global INTERRUPT_FLAG
        INTERRUPT_FLAG.clear()
        self.sequencePlaying = True
        self.sequenceStarted.emit()
        self.killTimer.stop()
        runInBackground(self.sequenceController.run)

    def startSequenceTimer(self):
        self.sequenceTimer.start()

    def stopSequenceTimer(self):
        self.sequenceTimer.stop()

    def abortSequence(self): 
        if self.sequencePlaying:
            global INTERRUPT_FLAG 
            INTERRUPT_FLAG.set()
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

    def updateSequence(self):
        self.sequenceUpdated.emit()

    def onDroneDisconnected(self):
        print("-- Drone Disconnected --")
        showDialog("Drone Connection Error", "Lost connection to a drone!", self.mainWindow, False, True)
        self.abortSequence()
        self.scanForDrones(True)


    def cleanup(self):
        self.swarmController.disconnectSwarm()