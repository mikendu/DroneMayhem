import math
from PyQt5.QtWidgets import QFrame, QPushButton, QLabel, QScroller, QScrollArea, QSizePolicy, QLineEdit
from PyQt5.QtGui import QIcon, QIntValidator
from PyQt5.QtCore import Qt, QSize

from application.util import layoutUtil, threadUtil
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
        self.createButtonBar()
        self.createDroneList()
        self.createSettings()

        self.appController.scanStarted.connect(self.clearList) 
        self.appController.scanFinished.connect(self.updateList) 
        self.appController.sequenceUpdated.connect(self.updateDroneState)
        self.appController.swarmUpdated.connect(self.updateDroneState)
        self.clearList()
        self.updateList()


    def createTitle(self):
        titleLayout = layoutUtil.createLayout(LayoutType.HORIZONTAL)
        title = QLabel("Swarm")
        title.setProperty("class", "titleText")
        titleLayout.addWidget(title)

        batteryButton = QPushButton()
        batteryButton.setProperty("class", ["swarmButton", "swarmBatteryButton"])
        batteryButton.setCursor(Qt.PointingHandCursor)
        batteryButton.setSizePolicy(QSizePolicy.Expanding, QSizePolicy.Expanding)
        batteryButton.setIcon(QIcon(":/images/battery_level.png"))
        batteryButton.setStatusTip("Check drone battery levels")
        batteryButton.setIconSize(QSize(25, 25))
        batteryButton.clicked.connect(self.appController.checkBattery)
        titleLayout.addWidget(batteryButton)

        testButton = QPushButton()
        testButton.setProperty("class", ["swarmButton", "swarmTestButton"])
        testButton.setCursor(Qt.PointingHandCursor)
        testButton.setSizePolicy(QSizePolicy.Expanding, QSizePolicy.Expanding)
        testButton.setIcon(QIcon(":/images/up_down_2.png"))
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



    def createButtonBar(self):
        buttonBarLayout = layoutUtil.createLayout(LayoutType.HORIZONTAL)

        enableButton = QPushButton("ENABLE ALL")
        enableButton.setProperty("class", "swarmBarButton")
        enableButton.setCursor(Qt.PointingHandCursor)
        enableButton.setSizePolicy(QSizePolicy.Expanding, QSizePolicy.Expanding)
        enableButton.setStatusTip("Enable all drones")
        enableButton.clicked.connect(lambda: self.appController.setDronesEnabled(True))
        buttonBarLayout.addWidget(enableButton)

        disableButton = QPushButton("DISABLE ALL")
        disableButton.setProperty("class", "swarmBarButton")
        disableButton.setCursor(Qt.PointingHandCursor)
        disableButton.setSizePolicy(QSizePolicy.Expanding, QSizePolicy.Expanding)
        disableButton.setStatusTip("Disable all drones")
        disableButton.clicked.connect(lambda: self.appController.setDronesEnabled(False))
        buttonBarLayout.addWidget(disableButton)
        self.layout.addLayout(buttonBarLayout)

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

        channelLabel = QLabel("Radio Channels")
        channelLabel.setProperty("class", "channelLabel")
        channelLayout.addWidget(channelLabel)

        channels = self.appSettings.getValue(SettingsKey.RADIO_CHANNELS, AppSettings.DEFAULT_RADIO_CHANNELS)
        self.channelTextFields = []
        for i in range(0, SettingsKey.NUM_CHANNELS):
            channelText = str(channels[i])
            channelEdit = QLineEdit()
            channelEdit.setText(channelText)
            channelEdit.setValidator(QIntValidator(0, 127))
            channelEdit.setMaxLength(3)
            channelEdit.setAlignment(Qt.AlignCenter)
            channelEdit.textEdited.connect(lambda val, bound_index=i: self.channelChanged(bound_index, val))
            channelEdit.setProperty("class", "channelEdit")
            self.channelTextFields.append(channelEdit)
            channelLayout.addWidget(channelEdit)

        settingsLayout.addWidget(channelFrame)

    def createAddressSetting(self, settingsLayout):
        addressFrame = QFrame()
        addressFrame.setProperty("class", "addressFrame")
        addressLayout = layoutUtil.createLayout(LayoutType.HORIZONTAL, addressFrame)

        addressLabel = QLabel("Address Range (Per Radio)")
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

    def channelChanged(self, index, val):
        channelEdit = self.channelTextFields[index]
        try:
            value = int(val)
            if value < 0 or value > 127:
                raise ValueError("Value outside of acceptable range!")

            channels = self.appSettings.getValue(SettingsKey.RADIO_CHANNELS, AppSettings.DEFAULT_RADIO_CHANNELS)
            channels[index] = value
            self.appSettings.setValue(SettingsKey.RADIO_CHANNELS, channels)
            channelEdit.setProperty("class", "channelEdit")
            channelEdit.style().polish(channelEdit)
        except ValueError:
            channelEdit.setProperty("class", ["channelEdit", "error"])
            channelEdit.style().polish(channelEdit)
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

            if addressMin < 0xE7E7E7E700:
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
        droneList = self.appController.swarmController.allDrones

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
        droneList = self.appController.swarmController.allDrones
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
        self.drone = drone

        outerLayout = layoutUtil.createLayout(LayoutType.HORIZONTAL, self)
        innerLayout = layoutUtil.createLayout(LayoutType.VERTICAL)
        outerLayout.addLayout(self.createButtons(drone))
        outerLayout.addLayout(innerLayout)
        innerLayout.setContentsMargins(25, 12, 25, 12)

        nameLabel = ElidedLabel("Drone " + str(drone.swarmIndex + 1))
        nameLabel.setProperty("class", ["droneName"])

        self.addressLabel = ElidedLabel("URI (Address):  " + drone.address)
        innerLayout.addStretch(1)
        innerLayout.addWidget(nameLabel)
        innerLayout.addWidget(self.addressLabel)
        innerLayout.addWidget(self.createStatusLabel())
        innerLayout.addStretch(1)

        outerLayout.addStretch(1)
        self.batteryLevel = QLabel("50%")
        self.batteryLevel.setProperty("class", ["batteryLevel"])
        outerLayout.addWidget(self.batteryLevel)
        # outerLayout.addWidget(self.createIndicator())
        self.setDroneState(drone)

    def createButtons(self, drone):
        layout = layoutUtil.createLayout(LayoutType.VERTICAL)
        self.toggleButton = QPushButton()
        self.toggleButton.setProperty("class", ["cardButton", "toggleButton"])
        self.toggleButton.setCursor(Qt.PointingHandCursor)
        self.toggleButton.setSizePolicy(QSizePolicy.Expanding, QSizePolicy.Expanding)
        self.toggleButton.setIcon(QIcon(":/images/check.svg"))
        self.toggleButton.setStatusTip("Enable/disable drone")
        self.toggleButton.setIconSize(QSize(21, 21))
        self.toggleButton.clicked.connect(self.toggle)
        layout.addWidget(self.toggleButton)

        identifyButton = QPushButton()
        identifyButton.setProperty("class", ["cardButton", "identifyButton"])
        identifyButton.setCursor(Qt.PointingHandCursor)
        identifyButton.setSizePolicy(QSizePolicy.Expanding, QSizePolicy.Expanding)
        identifyButton.setIcon(QIcon(":/images/magnifying.png"))
        identifyButton.setStatusTip("Identify drone")
        identifyButton.setIconSize(QSize(25, 25))
        identifyButton.clicked.connect(self.identifyDrone)
        layout.addWidget(identifyButton)

        return layout

    def toggle(self):
        self.drone.enabled = not self.drone.enabled
        self.updateEnabled(self.drone)

    def identifyDrone(self):
        threadUtil.runInBackground(lambda: self.drone.identify())

    def createStatusLabel(self):
        labelText = '''Status:  <span style="color:#b1b1b1">--</span>'''
        self.statusLabel = QLabel(labelText)
        self.statusLabel.setProperty("class", ["statusLabel"])
        return self.statusLabel


    def setBatteryLevel(self, drone):
        level = drone.batteryLevel
        levelText = (str(level) + "%") if level else "---"
        self.batteryLevel.setText(levelText)

        if not level:
            self.batteryLevel.setProperty("class", ["batteryLevel"])
        elif level < 20:
            self.batteryLevel.setProperty("class", ["batteryLevel", "red"])
        elif level < 35:
            self.batteryLevel.setProperty("class", ["batteryLevel", "orange"])
        elif level < 50:
            self.batteryLevel.setProperty("class", ["batteryLevel", "yellow"])
        elif level > 50:
            self.batteryLevel.setProperty("class", ["batteryLevel", "green"])


        self.batteryLevel.style().polish(self.batteryLevel)

    def updateEnabled(self, drone):
        classes = ["droneCard"]
        if not drone.enabled:
            classes.append("disabled")
        self.setProperty("class", classes)
        self.style().polish(self)
        self.toggleButton.style().polish(self.toggleButton)

    def setDroneState(self, drone):
        self.updateEnabled(drone)
        self.setBatteryLevel(drone)
        if drone.state == DroneState.INITIALIZING:
            self.statusLabel.setText('''Status:  <span style="color:#E2DD52;font-weight:bold">INITIALIZING</span>''')
            # self.indicator.setProperty("class", ["droneStatusIndicator", "initializing"])
        elif drone.state == DroneState.CONNECTED:
            self.statusLabel.setText('''Status:  <span style="color:#16a81b;font-weight:bold">CONNECTED</span>''')
            # self.indicator.setProperty("class", ["droneStatusIndicator", "connected"])
        elif drone.state == DroneState.IN_FLIGHT:
            self.statusLabel.setText('''Status:  <span style="color:#29bcff;font-weight:bold">IN FLIGHT</span>''')
            # self.indicator.setProperty("class", ["droneStatusIndicator", "in_flight"])
        elif drone.state == DroneState.LANDING:
            self.statusLabel.setText('''Status:  <span style="color:#e09422;font-weight:bold">LANDING</span>''')
            # self.indicator.setProperty("class", ["droneStatusIndicator", "landing"])
        elif drone.state == DroneState.ERROR:
            self.statusLabel.setText('''Status:  <span style="color:#ed3f00;font-weight:bold">ERROR</span>''')
            # self.indicator.setProperty("class", ["droneStatusIndicator", "error"])
        else:
            self.statusLabel.setText('''Status:  <span style="color:#b1b1b1;font-weight:bold">NOT CONNECTED</span>''')
            # self.indicator.setProperty("class", ["droneStatusIndicator"])

        # self.indicator.style().polish(self.indicator)

    # def createIndicator(self):
    #     self.indicator = QFrame()
    #     self.indicator.setProperty("class", ["droneStatusIndicator"])
    #     return self.indicator