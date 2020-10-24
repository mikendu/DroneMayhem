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
        #self.sequences = filter(lambda file: path.exists(file), self.sequences)
        self.settings.setValue('recentSequences', self.sequences)


    def getFlag(self, key):
        return self.settings.value(key, False, bool)

    def setFlag(self, key, value):
        self.settings.setValue(key, value)

    def getSequences(self):
        return self.sequences

    def addSequence(self, file):
        if not file in self.sequences:
            self.sequences.insert(0, file)

