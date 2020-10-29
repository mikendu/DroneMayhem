from PyQt5.QtWidgets import *
from PyQt5.QtGui import *
from PyQt5.QtCore import *

from util import *
from .panels import *

class MainDashboard(QFrame):
    
    
    def __init__(self, manager, *args, **kwargs):
        super().__init__(*args, **kwargs)
        self.manager = manager
        
        layout = createLayout(LayoutType.HORIZONTAL, self)
        
        self.recentPanel = RecentPanel(self.manager)
        self.baseStations = BaseStationPanel(self.manager)
        leftLayout = createLayout(LayoutType.VERTICAL)
        leftLayout.addWidget(self.recentPanel)
        leftLayout.addWidget(self.baseStations)
        layout.addLayout(leftLayout, 20)

        self.sequencePanel = SequencePanel(self.manager)
        layout.addWidget(self.sequencePanel, 40)

        self.swarmPanel = SwarmPanel(self.manager)
        layout.addWidget(self.swarmPanel, 40)


