from PyQt5.QtWidgets import *
from PyQt5.QtGui import *
from PyQt5.QtCore import *
import math


from util import *
from model import *
from ..widgets import *

class DroneCard(QFrame):

    index = None
    def __init__(self, drone, *args, **kwargs):
        super().__init__(*args, **kwargs)        

        self.index = drone.swarmIndex
        self.setProperty("class", "droneCard")
        outerLayout = createLayout(LayoutType.HORIZONTAL, self)
        innerLayout = createLayout(LayoutType.VERTICAL)        
        outerLayout.addLayout(innerLayout)
        innerLayout.setContentsMargins(25, 12, 25, 12)

        nameLabel = ElidedLabel( "Drone " + str(drone.swarmIndex))
        nameLabel.setProperty("class", ["droneName"])

        innerLayout.addStretch(1)
        innerLayout.addWidget(nameLabel)
        innerLayout.addWidget(ElidedLabel("Address:  " + drone.address))
        innerLayout.addWidget(self.createStatusLabel())
        innerLayout.addStretch(1)

        outerLayout.addStretch(1)
        outerLayout.addWidget(self.createIndicator())
        self.setDroneState(drone)

    def createStatusLabel(self):
        labelText = '''Status:  <span style="color:#b1b1b1">--</span>'''
        self.statusLabel = QLabel(labelText)
        self.statusLabel.setProperty("class", ["statusLabel"])
        return self.statusLabel
    
    def createIndicator(self):
        self.indicator = QFrame()
        self.indicator.setProperty("class", ["droneStatusIndicator"])
        return self.indicator


    def setDroneState(self, drone):
        if drone.state == DroneState.IDLE:
            self.statusLabel.setText('''Status:  <span style="color:#ffffff">Idle</span>''')
            self.indicator.setProperty("class", ["droneStatusIndicator", "idle"])
        elif drone.state == DroneState.INITIALIZING:
            self.statusLabel.setText('''Status:  <span style="color:#E2DD52">Initializing</span>''')
            self.indicator.setProperty("class", ["droneStatusIndicator", "initializing"])
        elif drone.state == DroneState.POSITIONING:
            self.statusLabel.setText('''Status:  <span style="color:#29bcff">Positioning</span>''')
            self.indicator.setProperty("class", ["droneStatusIndicator", "positioning"])
        elif drone.state == DroneState.READY:
            self.statusLabel.setText('''Status:  <span style="color:#16a81b">Ready</span>''')
            self.indicator.setProperty("class", ["droneStatusIndicator", "ready"])
        elif drone.state == DroneState.IN_FLIGHT:
            self.statusLabel.setText('''Status:  <span style="color:#04ff00">In Flight</span>''')
            self.indicator.setProperty("class", ["droneStatusIndicator", "in_flight"])
        elif drone.state == DroneState.LANDING:
            self.statusLabel.setText('''Status:  <span style="color:#e09422">Landing</span>''')
            self.indicator.setProperty("class", ["droneStatusIndicator", "landing"])
        else:
            self.statusLabel.setText('''Status:  <span style="color:#b1b1b1">--</span>''')
            self.indicator.setProperty("class", ["droneStatusIndicator"])


class SwarmPanel(QFrame):
    
    def __init__(self, appController, *args, **kwargs):
        super().__init__(*args, **kwargs)
        self.appController = appController        
        
        self.layout = createLayout(LayoutType.VERTICAL, self)
        self.layout.addWidget(self.createTitle())
        self.layout.addWidget(self.createDroneList())
        self.refreshList()  

        self.appController.dronesLoaded.connect(self.refreshList) 


    def createTitle(self):
        title = QLabel("Swarm")
        title.setProperty("class", "titleText")
        return title

    def createDroneList(self):        
        scrollArea = QScrollArea()       
        self.cardList = QFrame()

        self.listLayout = createLayout(LayoutType.GRID, self.cardList)
        self.cardList.setObjectName("DroneHolder")

        scrollArea.setWidget(self.cardList)
        scrollArea.setWidgetResizable(True)
        scrollArea.setHorizontalScrollBarPolicy(Qt.ScrollBarAlwaysOff)
        scrollArea.setVerticalScrollBarPolicy(Qt.ScrollBarAlwaysOff)
        scrollArea.verticalScrollBar().setSingleStep(10)
        return scrollArea   
    

    def refreshList(self):
        numColumns = round(self.width() / 300.0)
        clearLayout(self.listLayout)       
        for i, drone in enumerate(self.appController.swarmController.drones):
            row = math.floor(i / numColumns)
            col = i % numColumns
            self.listLayout.addWidget(DroneCard(drone), row, col)
            self.listLayout.setRowStretch(i, 1) 

    def repositionCards(self):
        numColumns = round(self.width() / 300.0)
        for i in reversed(range(self.listLayout.count())): 
            card = self.listLayout.itemAt(i).widget()
            index = card.index
            row = math.floor(index / numColumns)
            col = index % numColumns
            
            self.listLayout.removeWidget(card)
            self.listLayout.addWidget(card, row, col)

        
    def resizeEvent(self, event):
        self.repositionCards()
        super().resizeEvent(event)
