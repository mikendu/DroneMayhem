import time
import pprint
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
        layout.addWidget(self.createCalibrateButton())


    def createCalibrateButton(self):
        calibrationButton = QPushButton("CALIBRATE TRACKING")
        calibrationButton.setProperty("class", ["calibrationButton"])
        calibrationButton.setCursor(Qt.PointingHandCursor)
        calibrationButton.setSizePolicy(QSizePolicy.Expanding, QSizePolicy.Minimum)
        calibrationButton.setStatusTip("Calibrate tracking setup")
        calibrationButton.clicked.connect(self.onCalibrate)
        return calibrationButton

    def onCalibrate(self):
        self.dialog = self.appController.nonModalDialog("Lighthouse Calibration", "Connecting to crazyflie...", False)
        threadUtil.runInBackground(self.runCalibration)

    def runCalibration(self):
        swarmController = self.appController.swarmController
        if len(swarmController.availableDrones) == 0:
            self.dialog.setText("No drones found!")
            time.sleep(1.5)
            self.completeCalibration()
            return

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

            numBaseStations = len(self.angles.keys())
            if (numBaseStations < Constants.BASE_STATION_COUNT):
                self.dialog.setText("Found " + str(numBaseStations) + " basestation(s).\nExpecting at least " + str(Constants.BASE_STATION_COUNT))
                time.sleep(1.5)
                self.completeCalibration()
                return

            self.dialog.setText("Estimating position\nof base stations...")
            geometries = {}
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
                    geometries[id] = geo
                    print('---- Geometry for base station', id)
                    geo.dump()
                    print()

                else:
                    self.dialog.setText("Could not find valid configuration!")
                    time.sleep(1.5)
                    self.completeCalibration()
                    return

            numBaseStations = len(geometries.keys())
            if (numBaseStations < Constants.BASE_STATION_COUNT):
                self.dialog.setText("Calculated position for " + str(numBaseStations) + " basestation(s).\nExpecting at least " + str(
                    Constants.BASE_STATION_COUNT))
                time.sleep(1.5)
                self.completeCalibration()
                return

            self.dialog.setText("Uploading geo data to CF")
            writeEvent = Event()
            readEvent = Event()
            helper = LighthouseMemHelper(syncCrazyflie.cf)
            helper.write_geos(geometries, lambda success: self.onWriteComplete(writeEvent, success))
            writeEvent.wait()

            helper.read_all_calibs(lambda calibData: self.onReadComplete(readEvent, calibData))
            readEvent.wait()

            appSettings = self.appController.appSettings
            appSettings.setValue(SettingsKey.GEO_DATA, geometries)
            appSettings.setValue(SettingsKey.CALIB_DATA, self.calibrationData)
            self.baseStationDisplay.updateDisplay()

        self.completeCalibration()


    def onReadComplete(self, event, calib_data):
        self.calibrationData = calib_data
        for id, data in calib_data.items():
            print('---- Calibration data for base station', id)
            data.dump()
            print()
        event.set()

    def onWriteComplete(self, event, success):
        if not success:
            self.dialog.setText("Write to CF failed!")
            time.sleep(1.5)

        event.set()

    def onAnglesCollected(self, event, angles):
        # print("\n\n-------- ANGLES --------\n\n")
        # pprint.pprint(angles, indent=4, width=160)
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

# -- Internal Class -- #
class BaseStationDisplay(QFrame):
    """ Used only in this file"""

    def __init__(self, appController, *args, **kwargs):
        super().__init__(*args, **kwargs)

        self.appController = appController
        self.indicators = {}
        appSettings = appController.appSettings
        layout = layoutUtil.createLayout(LayoutType.HORIZONTAL, self)
        layout.addWidget(QLabel("Tracking Status"))

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
        print("\n\nChecking validity for id", baseStationId, ", got geo:", geo, ", got calib:", calib)
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

