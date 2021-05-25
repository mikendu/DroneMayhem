import math
from PyQt5.QtWidgets import QFrame, QPushButton, QLabel, QScroller, QScrollArea, QSizePolicy, QLineEdit
from PyQt5.QtGui import QIcon, QIntValidator
from PyQt5.QtCore import Qt, QSize

from application.util import layoutUtil
from application.model import DroneState, Drone
from application.view import LayoutType
from application.view.widgets import Spinner, ElidedLabel
from application.common import AppSettings, SettingsKey


class SwarmPanel(QFrame):
    
    def __init__(self, appController, *args, **kwargs):
        super().__init__(*args, **kwargs)
        self.appController = appController
        self.appSettings = self.appController.appSettings
        
        self.layout = layoutUtil.createLayout(LayoutType.VERTICAL, self)
        self.createTitle()
        self.createDroneList()
        self.createSettings()

        self.appController.scanStarted.connect(self.clearList) 
        self.appController.scanFinished.connect(self.updateList) 
        self.appController.sequenceUpdated.connect(self.updateDroneState)
        self.clearList()


    def createTitle(self):
        titleLayout = layoutUtil.createLayout(LayoutType.HORIZONTAL)
        title = QLabel("Swarm")
        title.setProperty("class", "titleText")
        titleLayout.addWidget(title)

        testButton = QPushButton()
        testButton.setProperty("class", ["swarmButton", "swarmTestButton"])
        testButton.setCursor(Qt.PointingHandCursor)
        testButton.setSizePolicy(QSizePolicy.Expanding, QSizePolicy.Expanding)
        testButton.setIcon(QIcon(":/images/up_down.png"))
        testButton.setStatusTip("Takeoff & landing test")
        testButton.setIconSize(QSize(25, 25))
        testButton.clicked.connect(self.appController.takeoffTest)
        titleLayout.addWidget(testButton)

        killButton = QPushButton()
        killButton.setProperty("class", ["swarmButton", "killButton"])
        killButton.setCursor(Qt.PointingHandCursor)
        killButton.setSizePolicy(QSizePolicy.Expanding, QSizePolicy.Expanding)
        killButton.setIcon(QIcon(":/images/cancel.png"))
        killButton.setStatusTip("Stop & land all drones")
        killButton.setIconSize(QSize(21, 21))
        killButton.clicked.connect(self.appController.emergencyKill)
        titleLayout.addWidget(killButton)

        refreshButton = QPushButton()
        refreshButton.setProperty("class", ["swarmButton", "refreshButton"])
        refreshButton.setCursor(Qt.PointingHandCursor)
        refreshButton.setSizePolicy(QSizePolicy.Expanding, QSizePolicy.Expanding)
        refreshButton.setIcon(QIcon(":/images/refresh.svg"))
        refreshButton.setStatusTip("Scan for drones")
        refreshButton.setIconSize(QSize(25, 25))
        refreshButton.clicked.connect(self.appController.scanForDrones)
        titleLayout.addWidget(refreshButton)

        self.layout.addLayout(titleLayout)

    def createSettings(self):
        settingsFrame = QFrame()
        settingsFrame.setProperty("class", "settingsFrame")
        settingsLayout = layoutUtil.createLayout(LayoutType.VERTICAL, settingsFrame)
        self.createChannelSetting(settingsLayout)
        self.createAddressSetting(settingsLayout)
        self.layout.addWidget(settingsFrame)

    def createChannelSetting(self, settingsLayout):
        channelFrame = QFrame()
        channelFrame.setProperty("class", "channelFrame")
        channelLayout = layoutUtil.createLayout(LayoutType.HORIZONTAL, channelFrame)

        channelLabel = QLabel("Channel")
        channelLabel.setProperty("class", "channelLabel")
        channelLayout.addWidget(channelLabel)

        channelText = str(self.appSettings.getValue(SettingsKey.RADIO_CHANNEL, AppSettings.DEFAULT_RADIO_CHANNEL))
        self.channelEdit = QLineEdit()
        self.channelEdit.setText(channelText)
        self.channelEdit.setValidator(QIntValidator(0, 127))
        self.channelEdit.setMaxLength(3)
        self.channelEdit.setAlignment(Qt.AlignCenter)
        self.channelEdit.textEdited.connect(self.channelChanged)
        self.channelEdit.setProperty("class", "channelEdit")

        channelLayout.addWidget(self.channelEdit)
        settingsLayout.addWidget(channelFrame)

    def createAddressSetting(self, settingsLayout):
        addressFrame = QFrame()
        addressFrame.setProperty("class", "addressFrame")
        addressLayout = layoutUtil.createLayout(LayoutType.HORIZONTAL, addressFrame)

        addressLabel = QLabel("Address Range")
        addressLabel.setProperty("class", "addressLabel")
        addressLayout.addWidget(addressLabel)

        [min, max] = self.appSettings.getValue(SettingsKey.RADIO_ADDRESSES, AppSettings.DEFAULT_ADDRESS_RANGE)
        self.addressMin = QLineEdit()
        self.addressMin.setText(min)
        self.addressMin.setMaxLength(10)
        self.addressMin.setAlignment(Qt.AlignCenter)
        self.addressMin.textEdited.connect(self.addressChanged)
        self.addressMin.setProperty("class", "addressMin")
        addressLayout.addWidget(self.addressMin)

        toLabel = QLabel("to")
        toLabel.setProperty("class", "toLabel")
        addressLayout.addWidget(toLabel)

        self.addressMax = QLineEdit()
        self.addressMax.setText(max)
        self.addressMax.setMaxLength(10)
        self.addressMax.setAlignment(Qt.AlignCenter)
        self.addressMax.textEdited.connect(self.addressChanged)
        self.addressMax.setProperty("class", "addressMax")
        addressLayout.addWidget(self.addressMax)

        settingsLayout.addWidget(addressFrame)

    def channelChanged(self, val):
        try:
            value = int(val)
            if value < 0 or value > 127:
                raise ValueError("Value outside of acceptable range!")

            self.appSettings.setValue(SettingsKey.RADIO_CHANNEL, value)
            self.channelEdit.setProperty("class", "channelEdit")
            self.channelEdit.style().polish(self.channelEdit)
        except ValueError:
            self.channelEdit.setProperty("class", ["channelEdit", "error"])
            self.channelEdit.style().polish(self.channelEdit)
            # self.appController.mainWindow.showStatusMessage("Invalid channel!")

    def addressChanged(self, text):
        minError = False
        maxError = False
        addressMinText = self.addressMin.text()
        addressMaxText = self.addressMax.text()
        invalidValue = False
        try:
            addressMin = int(addressMinText, 16)
        except ValueError:
            minError = True
            invalidValue = True

        try:
            addressMax = int(addressMaxText, 16)
        except ValueError:
            invalidValue = True
            maxError = True

        if not invalidValue:
            if addressMin >= addressMax:
                minError = True
                maxError = True

            if addressMin < 0xE7E7E7E701:
                minError = True

            if addressMax > 0xE7E7E7E7100:
                maxError = True

        if minError:
            self.addressMin.setProperty("class", ["addressMin", "error"])
            self.addressMin.style().polish(self.addressMin)

        if maxError:
            self.addressMax.setProperty("class", ["addressMax", "error"])
            self.addressMax.style().polish(self.addressMax)

        if not minError and not maxError:
            self.appSettings.setValue(SettingsKey.RADIO_ADDRESSES, [addressMinText, addressMaxText])
            self.addressMin.setProperty("class", "addressMin")
            self.addressMin.style().polish(self.addressMin)
            self.addressMax.setProperty("class", "addressMax")
            self.addressMax.style().polish(self.addressMax)

    def createDroneList(self):        
        scrollArea = QScrollArea()       
        self.cardList = QFrame()

        self.listLayout = layoutUtil.createLayout(LayoutType.GRID, self.cardList)
        self.cardList.setObjectName("DroneHolder")

        scrollArea.setWidget(self.cardList)
        scrollArea.setWidgetResizable(True)
        scrollArea.setHorizontalScrollBarPolicy(Qt.ScrollBarAlwaysOff)
        # scrollArea.setVerticalScrollBarPolicy(Qt.ScrollBarAlwaysOff)
        scrollArea.verticalScrollBar().setSingleStep(10)
        QScroller.grabGesture(scrollArea.viewport(), QScroller.LeftMouseButtonGesture)

        self.layout.addWidget(scrollArea)
    
    def clearList(self):
        layoutUtil.clearLayout(self.listLayout)

        self.listLayout.addWidget(EmptyCard(0), 0, 0)
        self.listLayout.addWidget(Spinner(), 1, 0)
        self.listLayout.addWidget(EmptyCard(0), 2, 0)

        self.listLayout.setRowStretch(0, 20)
        self.listLayout.setRowStretch(1, 0)
        self.listLayout.setRowStretch(2, 20)

    def updateList(self):
        layoutUtil.clearLayout(self.listLayout)   
        numColumns = round(self.width() / 300.0)
        droneList = self.appController.swarmController.availableDrones

        for i, drone in enumerate(droneList):
            row = math.floor(i / numColumns)
            col = i % numColumns
            card = DroneCard(drone)
            self.listLayout.addWidget(card, row, col)
            self.listLayout.setRowStretch(i, 1) 

        # Add "empty" cards if there's only 1 drone
        if len(droneList) == 1:
            for i in range(1, 2):
                row = math.floor(i / numColumns)
                col = i % numColumns
                self.listLayout.addWidget(EmptyCard(i), row, col)
                self.listLayout.setRowStretch(i, 1)

        self.cardList.setMaximumWidth(self.width())



    def repositionCards(self):
        numColumns = round(self.width() / 300.0)
        for i in reversed(range(self.listLayout.count())): 
            card = self.listLayout.itemAt(i).widget()
            if isinstance(card, Spinner):
                return

            index = card.index
            row = math.floor(index / numColumns)
            col = index % numColumns

            self.listLayout.removeWidget(card)
            self.listLayout.addWidget(card, row, col)

        
    def resizeEvent(self, event):
        self.repositionCards()
        super().resizeEvent(event)
        self.cardList.setMaximumWidth(self.width())


    def updateDroneState(self):
        droneList = self.appController.swarmController.availableDrones
        droneCount = len(droneList)
        for i in reversed(range(self.listLayout.count())): 
            card = self.listLayout.itemAt(i).widget()
            if isinstance(card, DroneCard):

                swarmIndex = card.index
                if swarmIndex >= 0 and swarmIndex < droneCount:
                    drone = droneList[swarmIndex]
                    card.setDroneState(drone)




## ----- INTERNAL CLASSES  ----- ##

class EmptyCard(QFrame):

    def __init__(self, index, *args, **kwargs):
        super().__init__(*args, **kwargs)
        self.index = index


class DroneCard(QFrame):

    def __init__(self, drone, *args, **kwargs):
        super().__init__(*args, **kwargs)

        self.index = drone.swarmIndex
        self.setProperty("class", "droneCard")
        outerLayout = layoutUtil.createLayout(LayoutType.HORIZONTAL, self)
        innerLayout = layoutUtil.createLayout(LayoutType.VERTICAL)
        outerLayout.addLayout(innerLayout)
        innerLayout.setContentsMargins(25, 12, 25, 12)

        nameLabel = ElidedLabel("Drone " + str(drone.swarmIndex))
        nameLabel.setProperty("class", ["droneName"])

        self.addressLabel = ElidedLabel("URI (Address):  " + drone.address)
        innerLayout.addStretch(1)
        innerLayout.addWidget(nameLabel)
        innerLayout.addWidget(self.addressLabel)
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
        if drone.state == DroneState.INITIALIZING:
            self.statusLabel.setText('''Status:  <span style="color:#E2DD52;font-weight:bold">INITIALIZING</span>''')
            self.indicator.setProperty("class", ["droneStatusIndicator", "initializing"])
        elif drone.state == DroneState.CONNECTED:
            self.statusLabel.setText('''Status:  <span style="color:#16a81b;font-weight:bold">CONNECTED</span>''')
            self.indicator.setProperty("class", ["droneStatusIndicator", "connected"])
        elif drone.state == DroneState.IN_FLIGHT:
            self.statusLabel.setText('''Status:  <span style="color:#29bcff;font-weight:bold">IN FLIGHT</span>''')
            self.indicator.setProperty("class", ["droneStatusIndicator", "in_flight"])
        elif drone.state == DroneState.LANDING:
            self.statusLabel.setText('''Status:  <span style="color:#e09422;font-weight:bold">LANDING</span>''')
            self.indicator.setProperty("class", ["droneStatusIndicator", "landing"])
        elif drone.state == DroneState.ERROR:
            self.statusLabel.setText('''Status:  <span style="color:#ed3f00;font-weight:bold">ERROR</span>''')
            self.indicator.setProperty("class", ["droneStatusIndicator", "error"])
        else:
            self.statusLabel.setText('''Status:  <span style="color:#b1b1b1;font-weight:bold">NOT CONNECTED</span>''')
            self.indicator.setProperty("class", ["droneStatusIndicator"])

        self.indicator.style().polish(self.indicator)
