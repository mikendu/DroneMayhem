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


class ApplicationController(QObject):
    
    sequenceLoaded = pyqtSignal()    
    scanStarted = pyqtSignal()
    scanFinished = pyqtSignal()

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

    def initializePositioning(self):
        self.vrSystem = openvr.init(openvr.VRApplication_Other)
        self.baseStationController = BaseStationController(self)

    def initializeSwarm(self):
        self.swarmController = SwarmController(self)


    def scanForDrones(self):
        if self.scanInProgress:
            return

        if self.sequencePlaying:
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


    def cleanup(self):
        # TODO - abort all running sequences and land all drones
        print("Cleaning up")