import time
import pprint
import numpy as np
from threading import Event

from PyQt5.QtWidgets import QFrame, QPushButton, QLabel, QSizePolicy
from PyQt5.QtCore import Qt

from cflib.crazyflie import Crazyflie
from cflib.crazyflie.mem import LighthouseBsGeometry
from cflib.crazyflie.mem import LighthouseMemHelper
from cflib.crazyflie.syncCrazyflie import SyncCrazyflie
from cflib.crazyflie.log import LogConfig
from cflib.crazyflie.syncLogger import SyncLogger
from cflib.localization import LighthouseBsGeoEstimator
from cflib.localization import LighthouseSweepAngleAverageReader

from application.common import SettingsKey
from application.constants import Constants
from application.util import layoutUtil, threadUtil
from application.view import LayoutType

class BaseStationPanel(QFrame):

    LOG_STATUS = "lighthouse.status"
    LOG_RECEIVE = "lighthouse.bsReceive"
    LOG_CALIBRATION_EXISTS = "lighthouse.bsCalVal"
    LOG_CALIBRATION_CONFIRMED = "lighthouse.bsCalCon"
    LOG_CALIBRATION_UPDATED = "lighthouse.bsCalUd"
    LOG_GEOMETERY_EXISTS = "lighthouse.bsGeoVal"
    LOG_ACTIVE = "lighthouse.bsActive"

    def __init__(self, appController, *args, **kwargs):
        super().__init__(*args, **kwargs)
        self.appController = appController
        self.baseStationDisplay = BaseStationDisplay(self.appController)

        layout = layoutUtil.createLayout(LayoutType.VERTICAL, self)
        layout.addWidget(self.baseStationDisplay)
        self.createButtons(layout)
        self.isCalibrating = False
        self.geometry = {}


    def createButtons(self, layout):
        buttonLayout = layoutUtil.createLayout(LayoutType.HORIZONTAL)
        layout.addLayout(buttonLayout)
        buttonLayout.addWidget(self.createButton("Start", "Start calibration", self.onCalibrationStart))
        buttonLayout.addWidget(self.createButton("Snapshot", "Take calibration snapshot", self.onCalibrationSnapshot))
        buttonLayout.addWidget(self.createButton("Finish", "Finish calibration", self.onCalibrationEnd))

    def createButton(self, text, tooltip, callback):
        button = QPushButton(text)
        button.setProperty("class", ["calibrationButton"])
        button.setCursor(Qt.PointingHandCursor)
        button.setSizePolicy(QSizePolicy.Expanding, QSizePolicy.Minimum)
        button.setStatusTip(tooltip)
        button.clicked.connect(callback)
        return button

    def onCalibrationStart(self):
        if self.isCalibrating:
            return

        swarmController = self.appController.swarmController
        if len(swarmController.availableDrones) == 0:
            self.appController.mainWindow.showStatusMessage("No drones found!")
            return

        self.geometry = {}
        self.angles = None
        self.offset = (0, 0, 0)
        self.isCalibrating = True
        self.takeSnapshot()


    def onCalibrationSnapshot(self):
        if not self.isCalibrating:
            return

        self.updatePosition()
        self.takeSnapshot()

    def onCalibrationEnd(self):
        if not self.isCalibrating:
            return


        finalGeometry = {}
        for id in self.geometry:
            geo = self.averageGeometry(id)
            if (geo):
                finalGeometry[id] = geo
                print("Final geometry for base station:", id)
                geo.dump()
                print()

        swarmController = self.appController.swarmController
        address = swarmController.availableDrones[0].address
        crazyflie = Crazyflie(rw_cache='./cache')
        with SyncCrazyflie(address, cf=crazyflie) as syncCrazyflie:
            self.dialog.setText("Uploading geo data to CF")
            writeEvent = Event()
            readEvent = Event()
            helper = LighthouseMemHelper(syncCrazyflie.cf)
            helper.write_geos(finalGeometry, lambda success: self.onWriteComplete(writeEvent, success))
            writeEvent.wait()

            helper.read_all_calibs(lambda calibData: self.onReadComplete(readEvent, calibData))
            readEvent.wait()

            appSettings = self.appController.appSettings
            appSettings.setValue(SettingsKey.GEO_DATA, finalGeometry)
            appSettings.setValue(SettingsKey.CALIB_DATA, self.calibrationData)
        self.baseStationDisplay.updateDisplay()
        self.isCalibrating = False

    def takeSnapshot(self):
        self.dialog = self.appController.nonModalDialog("Lighthouse Calibration", "Connecting to crazyflie...", False)
        threadUtil.runInBackground(self.getSnapshot)

    def getSnapshot(self):
        swarmController = self.appController.swarmController
        address = swarmController.availableDrones[0].address
        crazyflie = Crazyflie(rw_cache='./cache')
        with SyncCrazyflie(address, cf=crazyflie) as syncCrazyflie:
            syncCrazyflie.cf.param.set_value('lighthouse.method', '1')
            syncCrazyflie.cf.param.set_value('lighthouse.systemType', '2')
            time.sleep(0.35)

            self.dialog.setText("Finding lighthouses...")
            if not self.waitForLighthouseDetection(syncCrazyflie):
                self.dialog.setText("Lighthouses not detected!")
                time.sleep(1.5)
                self.completeCalibration()
                return

            self.dialog.setText("Reading sensor data...")
            collectionEvent = Event()
            sweep_angle_reader = LighthouseSweepAngleAverageReader(syncCrazyflie.cf, lambda angles: self.onAnglesCollected(collectionEvent, angles))
            sweep_angle_reader.start_angle_collection()
            collectionEvent.wait()

            self.dialog.setText("Estimating position\nof base stations...")
            estimator = LighthouseBsGeoEstimator()
            for id in sorted(self.angles.keys()):
                average_data = self.angles[id]
                sensor_data = average_data[1]
                rotation_bs_matrix, position_bs_vector = estimator.estimate_geometry(sensor_data)
                is_valid = estimator.sanity_check_result(position_bs_vector)
                if is_valid:
                    geo = LighthouseBsGeometry()
                    geo.rotation_matrix = rotation_bs_matrix
                    geo.origin = position_bs_vector
                    geo.valid = True
                    if id not in self.geometry:
                        self.geometry[id] = []

                    self.geometry[id].append((self.offset, geo))
                    print('---- Got geometry for base station', id, "at position: ", self.offset)

                else:
                    self.dialog.setText("Could not find valid configuration!")
                    time.sleep(1.5)
                    self.completeCalibration()
                    return

        self.completeCalibration()


    def onReadComplete(self, event, calib_data):
        self.calibrationData = calib_data
        # for id, data in calib_data.items():
        #     print('---- Calibration data for base station', id)
        #     data.dump()
        #     print()
        event.set()

    def onWriteComplete(self, event, success):
        if not success:
            self.dialog.setText("Write to CF failed!")
            time.sleep(1.5)

        event.set()

    def onAnglesCollected(self, event, angles):
        #print("\n\n-------- ANGLES --------\n\n")
        #pprint.pprint(angles, indent=4, width=160)
        #print()
        self.angles = angles
        event.set()

    def waitForLighthouseDetection(self, syncCrazyflie):
        startTime = time.time()
        logConfig = LogConfig("lighthouseStatus", 100)
        logConfig.add_variable(self.LOG_RECEIVE)
        logConfig.add_variable(self.LOG_ACTIVE)
        logConfig.add_variable(self.LOG_STATUS)
        logConfig.add_variable(self.LOG_CALIBRATION_CONFIRMED)
        logConfig.add_variable(self.LOG_CALIBRATION_UPDATED)
        logConfig.add_variable(self.LOG_CALIBRATION_EXISTS)

        with SyncLogger(syncCrazyflie, logConfig) as logger:
            for log_entry in logger:
                data = log_entry[1]

                if self.isReadyToCalibrate(data):
                    return True

                elapsed = time.time() - startTime
                if (elapsed > 3.0):
                    return False


    def isReadyToCalibrate(self, data):
        if self.LOG_CALIBRATION_EXISTS in data and data[self.LOG_CALIBRATION_EXISTS]:
            return True
        return False

    def completeCalibration(self):
        self.appController.acceptDialog(self.dialog)


    def averageGeometry(self, id):
        if id not in self.geometry:
            return None

        buffer = self.geometry[id]
        offsets = [geo[0] for geo in buffer]
        positions = [geo[1].origin for geo in buffer]
        rotations = [geo[1].rotation_matrix for geo in buffer]

        for i in range(0, len(buffer)):
            offset = offsets[i]
            positions[i] = np.add(positions[i],  offset)

        geo = LighthouseBsGeometry()
        geo.rotation_matrix = np.mean(rotations, axis=0)
        geo.origin = np.mean(positions, axis=0)
        geo.valid = True
        return geo


    def updatePosition(self):
        swarmController = self.appController.swarmController
        address = swarmController.availableDrones[0].address
        crazyflie = Crazyflie(rw_cache='./cache')
        with SyncCrazyflie(address, cf=crazyflie) as syncCrazyflie:
            cf = syncCrazyflie.cf
            cf.param.set_value('kalman.resetEstimation', '1')
            time.sleep(0.25)

            cf.param.set_value('kalman.resetEstimation', '0')
            self.offset = self.waitForPosition(syncCrazyflie)


    def waitForPosition(self, scf):
        log_config = LogConfig(name='Kalman Variance & Position', period_in_ms=100)
        log_config.add_variable('kalman.varPX', 'float')
        log_config.add_variable('kalman.varPY', 'float')
        log_config.add_variable('kalman.varPZ', 'float')
        log_config.add_variable('kalman.stateX', 'float')
        log_config.add_variable('kalman.stateY', 'float')
        log_config.add_variable('kalman.stateZ', 'float')

        var_y_history = [1000] * 10
        var_x_history = [1000] * 10
        var_z_history = [1000] * 10
        threshold = 0.001

        with SyncLogger(scf, log_config) as logger:
            for log_entry in logger:
                data = log_entry[1]

                var_x_history.append(data['kalman.varPX'])
                var_x_history.pop(0)
                var_y_history.append(data['kalman.varPY'])
                var_y_history.pop(0)
                var_z_history.append(data['kalman.varPZ'])
                var_z_history.pop(0)

                min_x = min(var_x_history)
                max_x = max(var_x_history)
                min_y = min(var_y_history)
                max_y = max(var_y_history)
                min_z = min(var_z_history)
                max_z = max(var_z_history)

                x = data['kalman.stateX']
                y = data['kalman.stateY']
                z = data['kalman.stateZ']

                has_converged = (max_x - min_x) < threshold and (max_y - min_y) < threshold and (max_z - min_z) < threshold
                if has_converged:
                    return (x, y, z)

# -- Internal Class -- #
class BaseStationDisplay(QFrame):
    """ Used only in this file"""

    def __init__(self, appController, *args, **kwargs):
        super().__init__(*args, **kwargs)

        self.appController = appController
        self.indicators = {}
        appSettings = appController.appSettings
        layout = layoutUtil.createLayout(LayoutType.HORIZONTAL, self)
        layout.addWidget(QLabel("Calibration"))

        geo_data = appSettings.getValue(SettingsKey.GEO_DATA)
        calib_data = appSettings.getValue(SettingsKey.CALIB_DATA)
        self.printSavedData(geo_data, calib_data)

        for id in geo_data:
            layout.addWidget(self.createIndicator(id, geo_data, calib_data))

    def updateDisplay(self):
        appSettings = self.appController.appSettings
        geo_data = appSettings.getValue(SettingsKey.GEO_DATA)
        calib_data = appSettings.getValue(SettingsKey.CALIB_DATA)

        for id in geo_data:
            indicator = self.indicators[id]
            classes = ["baseStationIndicator"]
            if self.isValid(id, geo_data, calib_data):
                classes.append("connected")

            indicator.setProperty("class", classes)
            indicator.style().polish(indicator)

    def createIndicator(self, baseStationId, geo_data, calib_data):
        classes = ["baseStationIndicator"]
        indicator = QFrame()

        if self.isValid(baseStationId, geo_data, calib_data):
            classes.append("connected")

        indicator.setProperty("class", classes)
        self.indicators[baseStationId] = indicator
        return indicator

    def isValid(self, baseStationId, geo_data, calib_data):
        geo = geo_data[baseStationId] if geo_data and baseStationId in geo_data else None
        calib = calib_data[baseStationId] if calib_data and baseStationId in calib_data else None
        return (geo and geo.valid) and (calib and calib.valid)

    def printSavedData(self, geo_data, calib_data):
        print("\n\nSAVED GEOS: ")
        pprint.pprint(geo_data, indent=4, width=160)
        print()
        print()

        print("\n\nSAVED CALIBS: ")
        pprint.pprint(calib_data, indent=4, width=160)
        print()
        print()

