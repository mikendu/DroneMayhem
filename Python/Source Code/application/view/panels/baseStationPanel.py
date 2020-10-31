from PyQt5.QtWidgets import *
from PyQt5.QtGui import *
from PyQt5.QtCore import *

from util import *

class BaseStationDisplay(QFrame):
    def __init__(self, text, *args, **kwargs):
        super().__init__(*args, **kwargs)
        
        self.indicator = None

        layout = createLayout(LayoutType.HORIZONTAL, self)
        layout.addWidget(QLabel(text))
        layout.addWidget(self.createIndicator())

    def createIndicator(self):
        self.indicator = QFrame()
        self.indicator.setProperty("class", ["baseStationIndicator"])
        return self.indicator

    def setConnected(self, connected):
        if connected: 
            self.indicator.setProperty("class", ["baseStationIndicator", "connected"])
        else:
            self.indicator.setProperty("class", ["baseStationIndicator"])
        

class BaseStationPanel(QFrame):
    
    def __init__(self, appController, *args, **kwargs):
        super().__init__(*args, **kwargs)
        self.appController = appController
        self.stationOne = BaseStationDisplay("Base Station I")
        self.stationTwo = BaseStationDisplay("Base Station II")

        layout = createLayout(LayoutType.VERTICAL, self)
        layout.addWidget(self.stationOne)
        layout.addWidget(self.stationTwo)

        self.stationOne.setConnected(self.isBaseStationConnected(0))
        self.stationTwo.setConnected(self.isBaseStationConnected(1))

    def isBaseStationConnected(self, index):
        baseStations = self.appController.baseStationController.baseStations
        return len(baseStations) > index and baseStations[index] and baseStations[index].initialized
