import sys
from enum import Enum

from PyQt5.QtWidgets import QFrame, QLabel, QScroller, QScrollArea
from PyQt5.QtCore import Qt

from application.util import layoutUtil, Logger, LogLevel
from application.view import LayoutType

class SequenceLog(QFrame):

    def __init__(self, appController, *args, **kwargs):
        super().__init__(*args, **kwargs)
        self.appController = appController
        self.appController.sequenceSelected.connect(self.clearLog)
        self.appController.sequenceStarted.connect(self.clearLog)
        # self.appController.sequenceFinished.connect(self.clearLog)
        self.appController.sequenceUpdated.connect(self.scrollBottom)
        Logger.bind_handler(self.log)

        self.layout = layoutUtil.createLayout(LayoutType.VERTICAL, self)
        self.createTitle()
        self.createLogPanel()
        self.clearLog()


    def createTitle(self):
        label = QLabel("Action Log")
        label.setProperty("class", "sequenceLogTitle")
        self.layout.addWidget(label)

    def createLogPanel(self):
        self.scrollArea = QScrollArea()
        self.logList = QFrame()
        self.listLayout = layoutUtil.createLayout(LayoutType.VERTICAL, self.logList)
        self.listLayout.setAlignment(Qt.AlignTop)
        self.logList.setObjectName("LogHolder")

        self.scrollArea.setWidget(self.logList)
        self.scrollArea.setWidgetResizable(True)
        self.scrollArea.setHorizontalScrollBarPolicy(Qt.ScrollBarAlwaysOff)
        # self.scrollArea.setVerticalScrollBarPolicy(Qt.ScrollBarAlwaysOff)
        self.scrollArea.verticalScrollBar().setSingleStep(10)
        QScroller.grabGesture(self.scrollArea.viewport(), QScroller.LeftMouseButtonGesture)

        self.layout.addWidget(self.scrollArea)

    def clearLog(self):
        layoutUtil.clearLayout(self.listLayout)
        self.logEntries = []
        self.latestEntry = None

    def log(self, data):
        message, logLevel, droneIndex = data
        entry = LogEntry(message, logLevel, droneIndex)
        self.listLayout.addWidget(entry)
        self.latestEntry = entry

    def scrollBottom(self):
        if self.latestEntry:
            self.scrollArea.ensureWidgetVisible(self.latestEntry)



class LogColors():

    WARN = '\033[93m'
    SUCCESS = '\033[92m'
    ERROR = '\033[91m'
    RESET = '\033[0m'


class LogEntry(QFrame):


    def __init__(self, message, logLevel, droneNumber, *args, **kwargs):
        super().__init__(*args, **kwargs)

        self.context = "Drone " + str(droneNumber + 1) if droneNumber is not None else "GLOBAL"
        self.logLevel = logLevel
        self.message = message

        self.setProperty("class", str(self.logLevel))
        self.layout = layoutUtil.createLayout(LayoutType.HORIZONTAL, self)
        self.createContextLabel()
        self.createMessageLabel()
        self.printToConsole()

    def createContextLabel(self):
        label = QLabel(self.context)
        label.setProperty("class", "logContextLabel")
        self.layout.addWidget(label, 25)

    def createMessageLabel(self):
        label = QLabel(self.message)
        self.layout.addWidget(label, 50)

    def printToConsole(self):
        if self.logLevel == LogLevel.INFO:
            print(LogColors.RESET + "--", self.context, "\t|\t", self.message)

        elif self.logLevel == LogLevel.SUCCESS:
            print(LogColors.SUCCESS + "--", self.context, "\t|\t", self.message)

        elif self.logLevel == LogLevel.WARN:
            print(LogColors.WARN + "--", self.context, "\t|\t", self.message, LogColors.RESET)

        elif self.logLevel == LogLevel.ERROR:
            print(LogColors.ERROR + "--", self.context, "\t|\t", self.message, LogColors.RESET, file=sys.stderr)


