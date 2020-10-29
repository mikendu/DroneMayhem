import openvr

from PyQt5.QtCore import *

from model import *
from .sequenceController import *
from .baseStationManager import *


class ApplicationManager(QObject):
    
    sequencePlaying = False
    sequenceLoaded = pyqtSignal()

    def __init__(self, appSettings):
        super().__init__()
        
        self.sequenceController = SequenceController(self, appSettings)
        self.vrSystem = openvr.init(openvr.VRApplication_Other)
        self.baseStationManager = BaseStationManager(self)
        self.appSettings = appSettings

    def openSequence(self, file):
        if self.sequenceController.loadSequence(file):
            sequenceFileList = list(map(lambda seq: seq.fullPath, self.sequenceController.sequences))
            self.appSettings.updateSequences(sequenceFileList)
            self.sequenceLoaded.emit()
            return True
            
        return False

        