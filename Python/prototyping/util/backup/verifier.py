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

LOG_STATUS = "lighthouse.status"
LOG_RECEIVE = "lighthouse.bsReceive"
LOG_CALIBRATION_EXISTS = "lighthouse.bsCalVal"
LOG_CALIBRATION_CONFIRMED = "lighthouse.bsCalCon"
LOG_CALIBRATION_UPDATED = "lighthouse.bsCalUd"
LOG_GEOMETERY_EXISTS = "lighthouse.bsGeoVal"
LOG_ACTIVE = "lighthouse.bsActive"


def isEstimationReady(uri, numBaseStations=1):

    return False


def isFlightReady(uri, numBaseStations=1):

    return False



def getLighthouseInfo(uri, num):
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

