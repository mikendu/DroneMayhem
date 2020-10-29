from PyQt5.QtWidgets import *
from PyQt5.QtGui import *
from PyQt5.QtCore import *
import time

from util import *
from ..widgets import *


class SequenceCard(QFrame):
    
    def __init__(self, sequence, index, manager, *args, **kwargs):
        super().__init__(*args, **kwargs)
        self.index = index
        self.manager = manager

        self.setProperty("class", "sequenceCard")
        outerLayout = createLayout(LayoutType.HORIZONTAL, self)
        innerLayout = createLayout(LayoutType.VERTICAL)        
        outerLayout.addLayout(innerLayout)
        innerLayout.setContentsMargins(25, 12, 25, 12)

        durationString = "Length: " + time.strftime('%M:%S', time.gmtime(sequence.duration))
        dronesString = str(sequence.drones) + (" Drones" if sequence.drones > 1 else " Drone")
        nameLabel = ElidedLabel(sequence.name)
        nameLabel.setProperty("class", "boldText")

        locationLabel = ElidedLabel(sequence.location)
        locationLabel.setProperty("class", ["italicText", "locationLabel"])

        innerLayout.addStretch(1)
        innerLayout.addWidget(nameLabel)
        innerLayout.addWidget(ElidedLabel(durationString))
        innerLayout.addWidget(ElidedLabel(dronesString))       
        innerLayout.addWidget(locationLabel)
        innerLayout.addStretch(1)

        button = QPushButton()
        button.setProperty("class", "sequenceButton")
        button.setCursor(Qt.PointingHandCursor)
        button.setSizePolicy(QSizePolicy.Expanding, QSizePolicy.Expanding)
        button.setIcon(QIcon("./application/resources/images/right.png"))
        button.setStatusTip("Select \"" + sequence.name + "\"")
        button.clicked.connect(self.onClick)

        outerLayout.addStretch(1)
        outerLayout.addWidget(button)

    def onClick(self):
        print("-- CLICKED:", self.index, "--")
        # self.manager.loadSequenc(self.index)


class RecentPanel(QFrame):
    
    def __init__(self, manager, *args, **kwargs):
        super().__init__(*args, **kwargs)
        self.manager = manager
        
        self.layout = createLayout(LayoutType.VERTICAL, self)
        self.refreshList()
        self.manager.sequenceLoaded.connect(self.refreshList)


    def refreshList(self):
        clearLayout(self.layout)
        self.layout.addWidget(self.createTitle())
        self.layout.addWidget(self.createCards())


    def createTitle(self):
        title = QLabel("Recent Sequences")
        title.setProperty("class", "titleText")
        return title


    def createCards(self):
        scrollArea = QScrollArea()       
        self.cardList = QFrame()

        layout = createLayout(LayoutType.VERTICAL, self.cardList)
        self.cardList.setObjectName("SequenceHolder")

        for index, sequence in enumerate(self.manager.sequenceController.sequences):
            layout.addWidget(SequenceCard(sequence, index, self.manager))

        layout.addStretch(1)
        scrollArea.setWidget(self.cardList)
        scrollArea.setWidgetResizable(True)
        scrollArea.setHorizontalScrollBarPolicy(Qt.ScrollBarAlwaysOff)
        scrollArea.setVerticalScrollBarPolicy(Qt.ScrollBarAlwaysOff)
        scrollArea.verticalScrollBar().setSingleStep(10)
        return scrollArea        
        
    def resizeEvent(self, event):
        self.cardList.setFixedWidth(event.size().width())
        super().resizeEvent(event)