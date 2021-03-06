import os
import time
import openvr
import cflib.crtp
from PyQt5.QtCore import QObject, QTimer, pyqtSignal

from application.model import SequenceTestMode
from application.util import dialogUtil, threadUtil, exceptionUtil, Logger
from .sequenceController import SequenceController
from .baseStationController import BaseStationController
from .swarmController import SwarmController


class ApplicationController(QObject):

    scanStarted = pyqtSignal()
    scanFinished = pyqtSignal()

    sequenceSelected = pyqtSignal()
    sequenceLoaded = pyqtSignal()
    sequenceOrderUpdated = pyqtSignal()

    startTimer = pyqtSignal()
    sequenceStarted = pyqtSignal()
    sequenceUpdated = pyqtSignal()
    swarmUpdated = pyqtSignal()
    sequenceFinished = pyqtSignal()
    resetTestMode = pyqtSignal()

    droneDisconnected = pyqtSignal(str)
    connectionFailed = pyqtSignal()
    takeoffTestComplete = pyqtSignal()

    POSITION_DRONES = True
    RUN_COLORS = True
    RUN_SEQUENCE = True

    def __init__(self, mainWindow, appSettings):
        super().__init__()
        Logger.initialize()

        self.sequencePlaying = False
        self.appSettings = appSettings
        self.mainWindow = mainWindow
        self.sequenceController = SequenceController(self, appSettings)
        self.takeoffTestDialog = None
        
        self.initializePositioning()
        self.initializeSwarm()
        self.scanInProgress = False
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
        self.sequenceOrderUpdated.connect(self.saveSequenceSettings)
        self.connectionFailed.connect(self.onConnectionFailed)
        self.takeoffTestComplete.connect(self.completeTakeoffTest)
        self.scanForDrones()


    def initializePositioning(self):
        self.vrSystem = openvr.init(openvr.VRApplication_Other)
        self.baseStationController = BaseStationController(self)

    def initializeSwarm(self):
        os.environ["USE_CFLINK"] = "cpp"
        cflib.crtp.init_drivers()
        self.swarmController = SwarmController(self, self.appSettings)


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

    def saveSequenceSettings(self):
        sequenceFileList = list(map(lambda seq: seq.fullPath, self.sequenceController.sequences))
        self.appSettings.updateSequences(sequenceFileList)

    def openSequence(self, file):
        if self.sequenceController.loadSequence(file):
            self.saveSequenceSettings()
            self.sequenceLoaded.emit()
            return True
            
        return False

    def selectSequence(self, index):
        if self.sequencePlaying:
            self.mainWindow.showStatusMessage("Cannot change sequence while playing.")
            return

        if self.sequenceController.selectSequence(index):
            self.sequenceSelected.emit()
            # self.resetTestMode.emit()

    def removeSequence(self, index):
        if (self.sequenceController.removeSequence(index)):
            self.saveSequenceSettings()
            self.sequenceLoaded.emit()

    def onSequenceClick(self):
        if not self.sequencePlaying:
            self.startSequence()
        else:
            self.abortSequence()

    def startSequence(self):
        if not self.droneRequirementMet:
            message = "Sequence is designed " + str(SequenceController.CURRENT.displayedDroneCount) \
                      + " drone(s)\n but only " + str(self.availableDrones) + " drone(s) are available."
            dialogUtil.modalDialog("Warning", message)

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
            self.clearSequenceSelection()

    def hardKill(self):
        if (self.sequencePlaying):    
            self.swarmController.disconnectSwarm()
            self.sequenceFinished.emit()
            self.sequencePlaying = False
            self.sequenceUpdated.emit()
            self.clearSequenceSelection()

    def onSequenceCompleted(self):
        self.sequenceFinished.emit()
        self.sequencePlaying = False
        self.clearSequenceSelection()

        # self.elapsedTime = 0
        # self.sequenceProgress = 0
        # self.startTimestamp = None
        # self.sequenceUpdated.emit()

    def updateSequence(self):
        if self.startTimestamp and self.sequenceDuration > 0 and (self.trajectoryEnabled or self.colorSequenceEnabled):
            self.elapsedTime = self.sequenceClock
            self.sequenceProgress = self.elapsedTime / self.sequenceDuration
        else:
            self.elapsedTime = 0
            self.sequenceProgress = 0

        self.sequenceUpdated.emit()

    def onConnectionFailed(self):
        self.hardKill()
        self.scanForDrones(True)
        dialogUtil.nonModalDialog(
            "Drone Connection Error",
            "Connection attempt failed,\ncancelling sequence.",
            self.mainWindow
        )

    def onDroneDisconnected(self, uri):
        print("-- Drone Disconnected: " + uri + " --")
        dialogUtil.nonModalDialog(
            "Drone Connection Error",
            "Lost connection to drone\n" + str(uri),
            self.mainWindow
        )

    def cleanup(self):
        self.swarmController.disconnectSwarm()

    @property
    def sequenceDuration(self):
        return SequenceController.CURRENT.duration if SequenceController.CURRENT is not None else 0

    @property
    def sequenceClock(self):
        if self.startTimestamp is not None:
            return min(time.time() - self.startTimestamp, self.sequenceDuration)
        return 0

    @property
    def requiredDrones(self):
        return SequenceController.CURRENT.drones if SequenceController.CURRENT else 0

    @property
    def availableDrones(self):
        return len(self.swarmController.availableDrones)

    @property
    def droneRequirementMet(self):
        return self.checkDroneRequirement()

    def checkDroneRequirement(self):
        if SequenceController.CURRENT is None:
            return False

        available = len(self.swarmController.availableDrones)
        required = SequenceController.CURRENT.displayedDroneCount
        return available >= required


    def setTestMode(self, index):
        mode = SequenceTestMode(index)
        if mode == SequenceTestMode.NONE:
            ApplicationController.POSITION_DRONES = True
            ApplicationController.RUN_COLORS = True
            ApplicationController.RUN_SEQUENCE = True

        elif mode == SequenceTestMode.COLORS:
            ApplicationController.POSITION_DRONES = False
            ApplicationController.RUN_COLORS = True
            ApplicationController.RUN_SEQUENCE = False

        elif mode == SequenceTestMode.FIRST_WAYPOINT:
            ApplicationController.POSITION_DRONES = True
            ApplicationController.RUN_COLORS = False
            ApplicationController.RUN_SEQUENCE = False

    def emergencyKill(self):
        if not dialogUtil.confirmModal("Emergency Shut-Down", "Stop all drones immediately?"):
            return

        self.mainWindow.showStatusMessage("Stopping all drones", 0)
        print("Stopping all drones")

        self.hardKill()
        self.swarmController.scan()                     # Scan for any rogue drones that lost connection
        self.swarmController.connectSwarm(-1)           # Connect to all drones found
        self.swarmController.disconnectSwarm()          # Stop flight & disconnect from any found

        self.mainWindow.showStatusMessage("Done", 500)
        print("Emergency Shutdown Complete")

    def takeoffTest(self):
        if self.sequencePlaying:
            self.mainWindow.showStatusMessage("Cannot run takeoff & land test while a sequence is running.")
            return

        result = dialogUtil.optionsModal("Takeoff & Land Test", "Run take off & landing test?", "All drones", "One at a time")
        proceed = (result == 0) or (result == 1)

        if not proceed:
            return

        if self.availableDrones == 0:
            self.mainWindow.showStatusMessage("No drones found!")
            return

        sequential = (result == 1)
        self.clearSequenceSelection()
        self.mainWindow.setEnabled(False)

        self.takeoffTestDialog = dialogUtil.nonModalDialog("Takeoff Test", "Running...", self.mainWindow, False)
        threadUtil.runInBackground(lambda: self.swarmController.takeoffTest(sequential))

    def completeTakeoffTest(self):
        self.takeoffTestDialog.accept()
        self.takeoffTestDialog = None
        self.mainWindow.setEnabled(True)

    def clearSequenceSelection(self):
        self.sequenceController.clearSelection()
        self.sequenceSelected.emit()
        self.resetTestMode.emit()

    def checkBattery(self):

        if self.sequencePlaying:
            self.mainWindow.showStatusMessage("Cannot check battery while sequence is running!")
            return

        if len(self.swarmController.availableDrones) == 0:
            self.mainWindow.showStatusMessage("No drones found!")
            return

        self.swarmController.parallel(lambda drone: drone.checkBatteryLevel(), self.swarmController.availableDrones)
        self.swarmUpdated.emit()

    @property
    def positioningEnabled(self):
        return ApplicationController.POSITION_DRONES

    @property
    def colorSequenceEnabled(self):
        return ApplicationController.RUN_COLORS

    @property
    def trajectoryEnabled(self):
        return ApplicationController.RUN_SEQUENCE