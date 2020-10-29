import openvr

from PyQt5.QtCore import *

from model import *
from .sequenceController import *
from .baseStationController import *
from .swarmController import *


class ApplicationController(QObject):
    
    sequencePlaying = False
    sequenceLoaded = pyqtSignal()
    dronesLoaded = pyqtSignal()

    def __init__(self, mainWindow, appSettings):
        super().__init__()
        
        self.sequenceController = SequenceController(self, appSettings)
        self.vrSystem = openvr.init(openvr.VRApplication_Other)
        self.baseStationController = BaseStationController(self)
        self.swarmController = SwarmController(self)
        self.appSettings = appSettings
        self.mainWindow = mainWindow

        self.scanForDrones()

    def scanForDrones(self):
        if self.sequencePlaying:
            self.mainWindow.showStatusMessage("Cannot scan for drones while sequence is playing.")
            return

        self.swarmController.scan()
        self.dronesLoaded.emit()

    def openSequence(self, file):
        if self.sequenceController.loadSequence(file):
            sequenceFileList = list(map(lambda seq: seq.fullPath, self.sequenceController.sequences))
            self.appSettings.updateSequences(sequenceFileList)
            self.sequenceLoaded.emit()
            return True
            
        return False

        