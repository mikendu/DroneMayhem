import time
from threading import Event

from cflib.crazyflie import Crazyflie
from cflib.crazyflie.syncCrazyflie import SyncCrazyflie
from cflib.crazyflie.log import LogConfig
from cflib.crazyflie.syncLogger import SyncLogger
from cflib.crazyflie.mem import MemoryElement
from cflib.crazyflie.mem import LighthouseMemHelper

from .droneState import DroneState
from application.util import exceptionUtil
from application.util import Logger


class Drone():

    TRAJECTORY_ID = 1
    ESTIMATOR_TIMEOUT_SEC = 15.0

    def __init__(self):
        # SyncCrazyflie instance
        self.crazyflie = None
        self.address = None
        self.swarmIndex = None
        self.state = DroneState.DISCONNECTED
        self.startHeight = 0.0
        self.writeEvent = None
        self.writeSuccess = False
        self.commander = None
        self.light_controller = None

    def initialize(self, address, disconnectCallback):
        Logger.log("Connecting to address " + address, self.swarmIndex)
        crazyflie = Crazyflie(ro_cache='./cache', rw_cache='./cache')
        syncCrazyflie = SyncCrazyflie(address, cf=crazyflie)
        crazyflie.connection_lost.add_callback(disconnectCallback)
        syncCrazyflie.open_link()

        self.crazyflie = syncCrazyflie
        self.commander = syncCrazyflie.cf.high_level_commander
        self.light_controller = syncCrazyflie.cf.light_controller
        self.state = DroneState.INITIALIZING
        self.light_controller.set_color(255, 200, 0, 0.0, True)

    def startTrajectory(self):
        self.crazyflie.cf.high_level_commander.start_trajectory(Drone.TRAJECTORY_ID, 1.0, False)

    def disableAutoPing(self):
        self.crazyflie.cf.auto_ping = False

    def updateSensors(self, uploadGeometry, geometryOne, geometryTwo):
        Logger.log("Updating light house data, sensors & positioning", self.swarmIndex)
        self.crazyflie.cf.param.set_value('lighthouse.method', '0')
        self.crazyflie.cf.param.set_value('stabilizer.controller', '2')  # Mellinger controller
        self.crazyflie.cf.param.set_value('commander.enHighLevel', '1')
        exceptionUtil.checkInterrupt()

        if uploadGeometry:
            self.writeBaseStationData(geometryOne, geometryTwo)
            self.resetEstimator()

        self.state = DroneState.CONNECTED
        self.light_controller.set_color(0, 255, 0, 0.0, True)
        exceptionUtil.checkInterrupt()

    def resetEstimator(self):
        self.crazyflie.cf.param.set_value('kalman.resetEstimation', '1')
        time.sleep(0.1)

        exceptionUtil.checkInterrupt()
        self.crazyflie.cf.param.set_value('kalman.resetEstimation', '0')
        self.startHeight = 0.0
        self.waitForEstimator()

    def waitForEstimator(self):
        startTime = time.time()
        log_config = LogConfig(name='Kalman Variance', period_in_ms=500)
        log_config.add_variable('kalman.varPX', 'float')
        log_config.add_variable('kalman.varPY', 'float')
        log_config.add_variable('kalman.varPZ', 'float')
        log_config.add_variable('kalman.stateZ', 'float')

        var_y_history = [1000] * 10
        var_x_history = [1000] * 10
        var_z_history = [1000] * 10
        threshold = 0.001

        with SyncLogger(self.crazyflie, log_config) as logger:
            for log_entry in logger:

                exceptionUtil.checkInterrupt()
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
                z = data['kalman.stateZ']
                has_converged = (max_x - min_x) < threshold and (max_y - min_y) < threshold and (max_z - min_z) < threshold
                timed_out = (time.time() - startTime) > Drone.ESTIMATOR_TIMEOUT_SEC

                if has_converged:
                    self.startHeight = z
                    break
                elif timed_out:
                    raise ConnectionAbortedError("Drone position was not able to converge!")

    def writeTrajectory(self, data):
        self.writeSuccess = False
        if self.crazyflie is None or len(data) == 0:
            raise ValueError("Cannot write trajectory data")

        trajectoryMemory = self.crazyflie.cf.mem.get_mems(MemoryElement.TYPE_TRAJ)[0]
        trajectoryMemory.write_raw(data, self.writeComplete, self.writeFailed)

        self.waitForCompletion()
        exceptionUtil.checkInterrupt()
        self.crazyflie.cf.high_level_commander.define_trajectory_compressed(Drone.TRAJECTORY_ID, 0)

    def writeBaseStationData(self, geometryOne, geometryTwo):
        self.writeSuccess = False
        geo_dict = {0: geometryOne, 1: geometryTwo}
        helper = LighthouseMemHelper(self.crazyflie.cf)
        helper.write_geos(geo_dict, self.writeComplete)
        self.waitForCompletion()
        exceptionUtil.checkInterrupt()

    def writeLedTimings(self, color_data):
        self.writeSuccess = False
        mems = self.crazyflie.cf.mem.get_mems(MemoryElement.TYPE_DRIVER_LEDTIMING)
        if self.crazyflie is None or len(color_data) == 0:
            raise ValueError("Cannot led timing data")

        mems[0].write_raw(color_data, self.writeComplete)
        self.waitForCompletion()
        exceptionUtil.checkInterrupt()

    # -- DATA UTILS -- #

    def writeComplete(self, *args):
        self.writeSuccess = True
        if self.writeEvent:
            self.writeEvent.set()

    def writeFailed(self, *args):
        self.writeSuccess = False
        if self.writeEvent:
            self.writeEvent.set()

    def waitForCompletion(self):
        self.writeEvent = Event()
        self.writeEvent.wait()
        self.writeEvent = None

        if not self.writeSuccess:
            raise Exception("Write failed!")