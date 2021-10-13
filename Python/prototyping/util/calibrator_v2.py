import sys
import numpy as np
import math
import time
import collections
from threading import Event

from cflib.crazyflie import Crazyflie
from cflib.crazyflie.mem import LighthouseBsGeometry
from cflib.crazyflie.mem import LighthouseMemHelper
from cflib.crazyflie.log import LogConfig
from cflib.crazyflie.syncLogger import SyncLogger
from cflib.crazyflie.syncCrazyflie import SyncCrazyflie
from cflib.localization import LighthouseConfigWriter
from cflib.localization import LighthouseBsGeoEstimator
from cflib.localization import LighthouseSweepAngleAverageReader


class Calibrator2:

    POSITIONS = [-0.4, -0.3, -0.2, -0.1, 0, 0.1, 0.2, 0.3, 0.4]

    def __init__(self):
        self.geometry_buffer = {}


    def finish(self, address):
        with SyncCrazyflie(address, cf=Crazyflie(ro_cache='./cache', rw_cache='./cache')) as scf:
            cf = scf.cf
            finalGeometry = {}
            for id in self.geometry_buffer:
                finalGeometry[id] = self.average_geometry(id)

            self.writeGeometry(cf, finalGeometry)


    def average_geometry(self, id):
        if id not in self.geometry_buffer:
            return None

        buffer = self.geometry_buffer[id]
        positions = [geo.origin for geo in buffer]
        rotations = [geo.rotation_matrix for geo in buffer]

        count = min(len(Calibrator2.POSITIONS), len(buffer))
        for i in range(0, count):
            offset = Calibrator2.POSITIONS[i]
            positions[i][1] = positions[i][1] + offset

        positions = positions[:count]
        geo = LighthouseBsGeometry()
        geo.rotation_matrix = np.mean(rotations, axis=0)
        geo.origin = np.mean(positions, axis=0)
        geo.valid = True
        print('\n\n>>>> ----FINAL  Geometry for base station', id, '---- <<<< \n')
        geo.dump()
        print()
        return geo


    def writeGeometry(self, cf, geometries):
        if len(geometries) == 0:
            return

        numBasestations = len(geometries)
        helper = LighthouseMemHelper(cf)
        writer = LighthouseConfigWriter(cf, nr_of_base_stations=numBasestations)
        self.write(lambda: helper.write_geos(geometries, self.writeComplete))
        self.write(lambda: writer.write_and_store_config(self.writeComplete, geos=geometries))
        print("Write finished.")

    def write(self, writeOperation):
        self.writeSuccess = False
        self.writeEvent = Event()
        writeOperation()

        self.writeEvent.wait()
        if not self.writeSuccess:
            raise Exception("Write failed!")

    def writeComplete(self, *args):
        self.writeSuccess = True
        if self.writeEvent:
            self.writeEvent.set()



    def next(self, address):
        with SyncCrazyflie(address, cf=Crazyflie(ro_cache='./cache', rw_cache='./cache')) as scf:
            geometries = self.estimate(scf)
            for id in geometries:
                if id not in self.geometry_buffer:
                    self.geometry_buffer[id] = []
                self.geometry_buffer[id].append(geometries[id])

    def getPosition(self, addresss):
        with SyncCrazyflie(addresss, cf=Crazyflie(ro_cache='./cache', rw_cache='./cache')) as scf:
            cf = scf.cf
            cf.param.set_value('kalman.resetEstimation', '1')
            time.sleep(0.25)

            cf.param.set_value('kalman.resetEstimation', '0')
            position = self.waitForPosition(scf)
            self.estimate(scf)
            return position


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
        threshold = 0.01

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



    def onAnglesCollected(self, event, angles):
        self.angles = angles
        event.set()

    def estimate(self, scf):
        scf.cf.param.set_value('lighthouse.method', '1')
        scf.cf.param.set_value('lighthouse.systemType', '2')

        collectionEvent = Event()
        sweep_angle_reader = LighthouseSweepAngleAverageReader(scf.cf, lambda angles: self.onAnglesCollected(collectionEvent, angles))
        sweep_angle_reader.start_angle_collection()
        collectionEvent.wait()

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
        return geometries