from PyQt5.QtCore import QSettings
from os import path

class AppSettings():

    settings = None
    sequences = None

    def __init__(self): 
        self.settings = QSettings('DroneMayhem', 'SwarmController')
        self.loadSequences()


    def loadSequences(self):
        self.sequences = self.settings.value('recentSequences', [], str)
        self.sequences = list(filter(lambda file: path.exists(file), self.sequences))
        del self.sequences[10:] 
        self.settings.setValue('recentSequences', self.sequences)

    def updateSequences(self, sequenceList):
        self.sequences = sequenceList        
        self.settings.setValue('recentSequences', self.sequences)

    def getValue(self, key, default = None, valueType = None):
        if default is not None:
            if valueType is not None:
                return self.settings.value(key, default, valueType)
            return self.settings.value(key, default)
        return self.settings.value(key)

    def setValue(self, key, value):
        self.settings.setValue(key, value)


