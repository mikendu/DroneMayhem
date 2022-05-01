from cflib.crazyflie import Crazyflie
from cflib.crazyflie.mem import LighthouseBsGeometry
from cflib.crazyflie.syncCrazyflie import SyncCrazyflie
from cflib.crazyflie.log import LogConfig
from cflib.crazyflie.syncLogger import SyncLogger
from cflib.localization import LighthouseBsGeoEstimator
from cflib.localization import LighthouseSweepAngleAverageReader

from application.common import SettingsKey
from application.constants import Constants
from application.util import layoutUtil, threadUtil
from application.view import LayoutType


def estimate_geo(uri):
    crazyflie = Crazyflie(rw_cache='./cache')
    with SyncCrazyflie(address, cf=crazyflie) as syncCrazyflie:
        syncCrazyflie.cf.param.set_value('lighthouse.method', '1')
        syncCrazyflie.cf.param.set_value('lighthouse.systemType', '2')
        time.sleep(0.35)

        collectionEvent = Event()
        sweep_angle_reader = LighthouseSweepAngleAverageReader(syncCrazyflie.cf,
                                                               lambda angles: self.onAnglesCollected(collectionEvent,
                                                                                                     angles))
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
