from PyQt5.QtCore import QObject

class Logger(QObject):

    _instance = None

    def __init__(self, signal):
        super().__init__()
        self.signal = signal

    def logMessage(self, message, droneIndex=None):
        self.signal.emit((message, droneIndex))

    @staticmethod
    def initialize(logSignal):
        _instance = Logger(logSignal)

    @staticmethod
    def log(message, droneIndex=None):
        if Logger._instance:
            Logger._instance.logMessage(message, droneIndex)