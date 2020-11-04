from PyQt5.QtWidgets import *
from PyQt5.QtGui import *
from PyQt5.QtCore import *
import time

from util import *
from ..widgets import *
import random


class SequencePanel(QFrame):
    
    
    def __init__(self, appController, *args, **kwargs):
        super().__init__(*args, **kwargs)
        self.appController = appController
        self.appController.sequenceSelected.connect(self.onSequenceSelected)
        self.appController.sequenceStarted.connect(self.onSequenceStarted)
        self.appController.sequenceFinished.connect(self.onSequenceFinished)
        self.appController.droneDisconnected.connect(self.onButtonClick)

        self.layout = createLayout(LayoutType.VERTICAL, self)
        self.createTitle()
        self.createDataPanel()
        self.createSequenceLog()

    def createTitle(self):
        titleLayout = createLayout(LayoutType.HORIZONTAL)
        title = QLabel("Sequence Runner")
        title.setProperty("class", "titleText")
        titleLayout.addWidget(title)        

        self.startButton = QPushButton("START")
        self.startButton.setProperty("class", "startButton")
        self.startButton.setCursor(Qt.PointingHandCursor)
        self.startButton.setSizePolicy(QSizePolicy.Expanding, QSizePolicy.Expanding)
        self.startButton.setStatusTip("Start the sequence")
        self.startButton.clicked.connect(self.onButtonClick)
        self.startButton.clicked.connect(self.appController.onSequenceClick)
        self.startButton.setEnabled(False)
        self.startButton.setObjectName("StartButton")
        titleLayout.addWidget(self.startButton)

        self.layout.addLayout(titleLayout)

    def createDataPanel(self):
        self.dataPanel = SequenceData(self.appController)
        self.layout.addWidget(self.dataPanel, 25)

    def onButtonClick(self):
        self.startButton.setEnabled(False)
        self.startButton.style().polish(self.startButton)

    def createSequenceLog(self):
        self.sequenceLog = SequenceLog(self.appController)
        self.layout.addWidget(self.sequenceLog, 75)

    def onSequenceSelected(self):
        self.startButton.setEnabled(True)
        self.startButton.setText("START")
        self.startButton.setProperty("class", "stopped")
        self.startButton.style().polish(self.startButton)

    def onSequenceStarted(self):
        self.startButton.setEnabled(True)
        self.startButton.setText("ABORT")
        self.startButton.setProperty("class", "running")
        self.startButton.style().polish(self.startButton)

    def onSequenceFinished(self):
        self.startButton.setEnabled(True)
        self.startButton.setText("START")
        self.startButton.setProperty("class", "stopped")
        self.startButton.style().polish(self.startButton)

class SequenceData(QFrame):
    
    def __init__(self, appController, *args, **kwargs):
        super().__init__(*args, **kwargs)
        self.appController = appController
        self.appController.sequenceSelected.connect(self.onSequenceSelected)
        self.appController.sequenceUpdated.connect(self.updateProgress)

        self.layout = createLayout(LayoutType.VERTICAL, self)
        innerLayout = createLayout(LayoutType.VERTICAL)
        innerLayout.setContentsMargins(30, 30, 30, 0)
        self.layout.addLayout(innerLayout)

        self.sequenceTitle = self.createLabel("--", "sequenceTitle", innerLayout)
        self.droneText = self.createLabel("--", "sequenceDroneText", innerLayout)

        innerLayout.addStretch(1)
        self.progressText = self.createLabel("00:00 / 00:00", "sequenceProgressText", innerLayout)
        self.progressBar = self.createProgressBar()

    def createLabel(self, text, labelClass, layout):
        label = QLabel(text)
        if labelClass:
            label.setProperty("class", labelClass)

        layout.addWidget(label)
        return label

    def createProgressBar(self):
        progressBar = QProgressBar()
        progressBar.setProperty("class", "progressBar")
        progressBar.setRange(0, 1000)
        progressBar.setValue(0)
        progressBar.setTextVisible(False)
        self.layout.addWidget(progressBar)
        return progressBar

    def onSequenceSelected(self):
        sequence = self.appController.sequenceController.CURRENT
        if (sequence):
            self.durationString = time.strftime('%M:%S', time.gmtime(sequence.duration))
            self.sequenceTitle.setText(sequence.name)
            self.droneText.setText(str(sequence.drones) + " Drone Sequence")
            self.progressText.setText("00:00 / " + self.durationString)
            self.progressBar.setValue(0)

    def updateProgress(self):
        
        timeString = time.strftime('%M:%S', time.gmtime(self.appController.elapsedTime))
        progress = round(self.appController.sequenceProgress * 1000)

        self.progressText.setText(timeString  + " / " + self.durationString)
        self.progressBar.setValue(progress)

            


class SequenceLog(QFrame):
        
    def __init__(self, appController, *args, **kwargs):
        super().__init__(*args, **kwargs)
        self.appController = appController
        self.appController.sequenceSelected.connect(self.clearLog)
        self.appController.sequenceStarted.connect(self.clearLog)
        # self.appController.sequenceFinished.connect(self.clearLog)
        self.appController.addLogEntry.connect(self.addLogEntry)
        self.appController.sequenceUpdated.connect(self.scrollBottom)
        
        self.layout = createLayout(LayoutType.VERTICAL, self)
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
        self.listLayout = createLayout(LayoutType.VERTICAL, self.logList)
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
        clearLayout(self.listLayout)
        self.logEntries = []
        self.latestEntry = None

    def addLogEntry(self, logData):
        swarmIndex, actionString, x, y, z, time = logData
        dataString = "({:4.2f}, {:4.2f}, {:4.2f})".format(x, y, z)
        entry = LogEntry(swarmIndex, actionString + dataString, time)
        self.listLayout.addWidget(entry)
        self.latestEntry = entry

    def scrollBottom(self):
        if self.latestEntry:
            self.scrollArea.ensureWidgetVisible(self.latestEntry)
        



class LogEntry(QFrame):
    
    def __init__(self, droneNumber, actionText, timeDuration, *args, **kwargs):
        super().__init__(*args, **kwargs)
        
        self.drone = droneNumber
        self.action = actionText
        self.time = timeDuration
        
        self.layout = createLayout(LayoutType.HORIZONTAL, self)
        self.createDroneLabel()
        self.createActionText()
        self.createTimeLabel()
        self.createIndicator()

    def createDroneLabel(self):
        label = QLabel("Drone " + str(self.drone))
        label.setProperty("class", "sequenceDroneLabel")
        self.layout.addWidget(label, 25)
        
    def createActionText(self):
        label = QLabel(self.action)
        self.layout.addWidget(label, 50)

    
    def createTimeLabel(self):
        label = QLabel("{:0.2f}".format(self.time) + " s")
        label.setProperty("class", "timeLabel")
        self.layout.addWidget(label, 25)

    def createIndicator(self):
        pass


