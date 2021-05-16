from PyQt5.QtWidgets import QFrame, QPushButton, QLabel, QSizePolicy, QProgressBar
from PyQt5.QtCore import Qt
import time

from .sequenceLog import SequenceLog
from application.model import SequenceTestMode
from application.util import layoutUtil
from application.view import LayoutType
from application.view.widgets import ButtonBar

class SequencePanel(QFrame):
    
    
    def __init__(self, appController, *args, **kwargs):
        super().__init__(*args, **kwargs)
        self.appController = appController
        self.appController.sequenceSelected.connect(self.onSequenceSelected)
        self.appController.sequenceStarted.connect(self.onSequenceStarted)
        self.appController.sequenceFinished.connect(self.onSequenceFinished)
        self.appController.scanFinished.connect(self.updateButtonStatus)
        self.appController.droneDisconnected.connect(self.onButtonClick)
        self.appController.resetTestMode.connect(self.onTestModeReset)

        self.layout = layoutUtil.createLayout(LayoutType.VERTICAL, self)
        self.createTitle()
        self.createButtonBar()
        self.createDataPanel()
        self.createSequenceLog()

    def createButtonBar(self):
        items = list(map(lambda x: str(x).upper(), SequenceTestMode))
        self.buttonBar = ButtonBar("TESTING MODE", items, self.appController.setTestMode)
        self.layout.addWidget(self.buttonBar)

    def createTitle(self):
        titleLayout = layoutUtil.createLayout(LayoutType.HORIZONTAL)
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

    def onTestModeReset(self):
        self.buttonBar.setSelected(int(SequenceTestMode.NONE))

    def onButtonClick(self):
        self.startButton.setEnabled(False)
        self.startButton.style().polish(self.startButton)

    def updateButtonStatus(self):
        met = self.appController.droneRequirementMet
        self.startButton.setEnabled(met)


    def createSequenceLog(self):
        self.sequenceLog = SequenceLog(self.appController)
        self.layout.addWidget(self.sequenceLog, 75)

    def onSequenceSelected(self):
        self.updateButtonStatus()
        self.startButton.setText("START")
        self.startButton.setProperty("class", "stopped")
        self.startButton.style().polish(self.startButton)

    def onSequenceStarted(self):
        self.startButton.setEnabled(True)
        self.startButton.setText("ABORT")
        self.startButton.setProperty("class", "running")
        self.startButton.style().polish(self.startButton)

    def onSequenceFinished(self):
        self.updateButtonStatus()
        self.startButton.setText("START")
        self.startButton.setProperty("class", "stopped")
        self.startButton.style().polish(self.startButton)


## ----- INTERNAL CLASSES  ----- ##

class SequenceData(QFrame):
    """ Used only in this file"""
    
    def __init__(self, appController, *args, **kwargs):
        super().__init__(*args, **kwargs)
        self.appController = appController
        self.appController.sequenceSelected.connect(self.onSequenceSelected)
        self.appController.sequenceUpdated.connect(self.updateProgress)

        self.layout = layoutUtil.createLayout(LayoutType.VERTICAL, self)
        innerLayout = layoutUtil.createLayout(LayoutType.VERTICAL)
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
        if sequence:
            self.durationString = time.strftime('%M:%S', time.gmtime(sequence.duration))
            self.sequenceTitle.setText(sequence.name)
            self.droneText.setText(str(sequence.displayedDroneCount) + " Drone Sequence")
            self.progressText.setText("00:00 / " + self.durationString)
            self.progressBar.setValue(0)
        else:
            self.sequenceTitle.setText("--")
            self.droneText.setText("--")
            self.progressText.setText("00:00 / 00:00")
            self.progressBar.setValue(0)

    def updateProgress(self):
        
        timeString = time.strftime('%M:%S', time.gmtime(self.appController.elapsedTime))
        progress = round(self.appController.sequenceProgress * 1000)

        self.progressText.setText(timeString  + " / " + self.durationString)
        self.progressBar.setValue(progress)
