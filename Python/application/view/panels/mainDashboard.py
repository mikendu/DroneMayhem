from PyQt5.QtWidgets import QFrame

from .recentsPanel import RecentPanel
from .baseStationPanel import BaseStationPanel
from .sequencePanel import SequencePanel
from .swarmPanel import SwarmPanel
from application.view import LayoutType
from application.util import layoutUtil


class MainDashboard(QFrame):
    
    def __init__(self, appController, *args, **kwargs):
        super().__init__(*args, **kwargs)
        self.appController = appController
        
        layout = layoutUtil.createLayout(LayoutType.HORIZONTAL, self)
        
        self.recentPanel = RecentPanel(self.appController)
        self.baseStations = BaseStationPanel(self.appController)
        leftLayout = layoutUtil.createLayout(LayoutType.VERTICAL)
        leftLayout.addWidget(self.recentPanel)
        leftLayout.addWidget(self.baseStations)
        layout.addLayout(leftLayout, 20)

        self.sequencePanel = SequencePanel(self.appController)
        layout.addWidget(self.sequencePanel, 40)

        self.swarmPanel = SwarmPanel(self.appController)
        layout.addWidget(self.swarmPanel, 40)


