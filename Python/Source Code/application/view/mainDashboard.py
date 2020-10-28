from PyQt5.QtWidgets import *
from PyQt5.QtGui import *
from PyQt5.QtCore import *

from util import *
from .panels import *

class MainDashboard(QFrame):
    
    
    def __init__(self, manager, *args, **kwargs):
        super().__init__(*args, **kwargs)
        self.manager = manager
        
        layout = createLayout(LayoutType.VERTICAL, self)
        layout.addWidget(self.createUpperHalf(), 50)
        layout.addWidget(self.createLowerHalf(), 50)


    def createUpperHalf(self):
        frame = QFrame(self)
        layout = createLayout(LayoutType.HORIZONTAL, frame)

        self.recentPanel = RecentPanel(self.manager)
        layout.addWidget(self.recentPanel, 20)

        self.sequencePanel = SequencePanel(self.manager)
        layout.addWidget(self.sequencePanel, 60)

        self.settingsPanel = SettingsPanel(self.manager)
        layout.addWidget(self.settingsPanel, 20)

        frame.setObjectName("UpperHalf")
        return frame

    def createLowerHalf(self):
        frame = QFrame(self)
        layout = createLayout(LayoutType.HORIZONTAL, frame)        
        frame.setObjectName("LowerHalf")
        return frame

