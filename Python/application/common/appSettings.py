from PyQt5.QtCore import QSettings
from os import path
from .settingsKey import SettingsKey

class AppSettings():

    DEFAULT_RADIO_CHANNELS = [1, 40, 80, 120]
    DEFAULT_ADDRESS_RANGE = ['E7E7E7E700', 'E7E7E7E715']

    def __init__(self): 
        self.settings = QSettings('DroneMayhem', 'SwarmController')
        self.sequences = None
        self.loadSequences()


    def loadSequences(self):
        self.sequences = self.settings.value(SettingsKey.SEQUENCES, [], str)
        self.sequences = list(filter(lambda file: path.exists(file), self.sequences))
        self.sequences = list(dict.fromkeys(self.sequences))
        del self.sequences[10:] 
        self.settings.setValue(SettingsKey.SEQUENCES, self.sequences)

    def updateSequences(self, sequenceList):
        self.sequences = sequenceList
        self.sequences = list(dict.fromkeys(self.sequences))
        self.settings.setValue(SettingsKey.SEQUENCES, self.sequences)

    def getValue(self, key, default = None, valueType = None):
        if default is not None:
            if valueType is not None:
                return self.settings.value(key, default, valueType)
            return self.settings.value(key, default)
        return self.settings.value(key)

    def setValue(self, key, value):
        self.settings.setValue(key, value)


