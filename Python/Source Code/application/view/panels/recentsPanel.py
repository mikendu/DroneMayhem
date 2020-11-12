import time
from PyQt5.QtWidgets import QFrame, QLabel, QScroller, QScrollArea, QPushButton, QSizePolicy
from PyQt5.QtGui import QIcon
from PyQt5.QtCore import Qt

from application.util import layoutUtil
from application.view import LayoutType
from application.view.widgets import ElidedLabel


class RecentPanel(QFrame):
    
    def __init__(self, appController, *args, **kwargs):
        super().__init__(*args, **kwargs)
        self.appController = appController
        
        self.layout = layoutUtil.createLayout(LayoutType.VERTICAL, self)
        self.layout.addWidget(self.createTitle())
        self.layout.addWidget(self.createCardHolder())
        self.refreshList()
        self.appController.sequenceLoaded.connect(self.refreshList)
        # self.appController.sequenceSelected.connect(self.refreshList)

    def createTitle(self):
        title = QLabel("Recent Sequences")
        title.setProperty("class", "titleText")
        return title

    def createCardHolder(self):
        scrollArea = QScrollArea()       
        self.cardList = QFrame()

        self.listLayout = layoutUtil.createLayout(LayoutType.VERTICAL, self.cardList)
        self.cardList.setObjectName("SequenceHolder")

        scrollArea.setWidget(self.cardList)
        scrollArea.setWidgetResizable(True)
        scrollArea.setHorizontalScrollBarPolicy(Qt.ScrollBarAlwaysOff)
        scrollArea.setVerticalScrollBarPolicy(Qt.ScrollBarAlwaysOff)
        scrollArea.verticalScrollBar().setSingleStep(10)
        QScroller.grabGesture(scrollArea.viewport(), QScroller.LeftMouseButtonGesture)
        return scrollArea    

    def refreshList(self):
        layoutUtil.clearLayout(self.listLayout)
        for index, sequence in enumerate(self.appController.sequenceController.sequences):
            self.listLayout.addWidget(SequenceCard(sequence, index, self.appController))
        
        self.listLayout.addStretch(1)
        
    def resizeEvent(self, event):
        self.cardList.setFixedWidth(event.size().width())
        super().resizeEvent(event)


# -- Internal Class -- #
class SequenceCard(QFrame):
    """ For displaying a recent sequence in the sidebar. Used only in this file"""

    ICON = None

    def __init__(self, sequence, index, appController, *args, **kwargs):
        super().__init__(*args, **kwargs)
        self.index = index
        self.appController = appController

        self.setProperty("class", "sequenceCard")
        outerLayout = layoutUtil.createLayout(LayoutType.HORIZONTAL, self)
        innerLayout = layoutUtil.createLayout(LayoutType.VERTICAL)
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

        if not SequenceCard.ICON:
            SequenceCard.ICON = QIcon(":/images/right_button.png")

        button = QPushButton()
        button.setProperty("class", "sequenceButton")
        button.setCursor(Qt.PointingHandCursor)
        button.setSizePolicy(QSizePolicy.Expanding, QSizePolicy.Expanding)
        button.setIcon(SequenceCard.ICON)
        button.setStatusTip("Select \"" + sequence.name + "\"")
        button.clicked.connect(self.onClick)

        outerLayout.addStretch(1)
        outerLayout.addWidget(button)

    def onClick(self):
        self.appController.selectSequence(self.index)