from PyQt5.QtWidgets import *
from PyQt5.QtGui import *
from PyQt5.QtCore import *

from util import *
from .panels import *

class MainDashboard(QFrame):
    
    
    def __init__(self, appController, *args, **kwargs):
        super().__init__(*args, **kwargs)
        self.appController = appController
        
        layout = createLayout(LayoutType.HORIZONTAL, self)
        
        self.recentPanel = RecentPanel(self.appController)
        self.baseStations = BaseStationPanel(self.appController)
        leftLayout = createLayout(LayoutType.VERTICAL)
        leftLayout.addWidget(self.recentPanel)
        leftLayout.addWidget(self.baseStations)
        layout.addLayout(leftLayout, 20)

        self.sequencePanel = SequencePanel(self.appController)
        layout.addWidget(self.sequencePanel, 40)

        self.swarmPanel = SwarmPanel(self.appController)
        layout.addWidget(self.swarmPanel, 40)


