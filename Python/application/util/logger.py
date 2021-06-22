from enum import Enum

from PyQt5.QtCore import QObject, pyqtSignal

class LogLevel(Enum):
    INFO = 0
    WARN = 1
    ERROR = 2
    SUCCESS = 3

    def __str__(self):
        return self.name

class Logger(QObject):

    _instance = None
    LOG_SIGNAL = pyqtSignal(tuple)

    def __init__(self):
        super().__init__()

    @staticmethod
    def initialize():
        Logger._instance = Logger()

    @staticmethod
    def bind_handler(signal_handler):
        if Logger._instance:
            Logger._instance.LOG_SIGNAL.connect(signal_handler)

    @staticmethod
    def log(message, droneIndex=None):
        if Logger._instance:
            Logger._instance.LOG_SIGNAL.emit((message, LogLevel.INFO, droneIndex))

    @staticmethod
    def success(message, droneIndex=None):
        if Logger._instance:
            Logger._instance.LOG_SIGNAL.emit((message, LogLevel.SUCCESS, droneIndex))

    @staticmethod
    def warn(message, droneIndex=None):
        if Logger._instance:
            Logger._instance.LOG_SIGNAL.emit((message, LogLevel.WARN, droneIndex))

    @staticmethod
    def error(message, droneIndex=None):
        if Logger._instance:
            Logger._instance.LOG_SIGNAL.emit((message, LogLevel.ERROR, droneIndex))
