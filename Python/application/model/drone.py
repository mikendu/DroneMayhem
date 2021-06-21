import time
from threading import Event

from cflib.crazyflie import Crazyflie
from cflib.crazyflie.syncCrazyflie import SyncCrazyflie
from cflib.crazyflie.log import LogConfig
from cflib.crazyflie.syncLogger import SyncLogger
from cflib.crazyflie.mem import MemoryElement
from cflib.crazyflie.mem import LighthouseMemHelper
from cflib.localization import LighthouseConfigWriter

from .droneState import DroneState
from application.common.exceptions import DroneException
from application.util import exceptionUtil, threadUtil, Logger, calibration, vectorMath


class Drone():

    TRAJECTORY_ID = 1
    ESTIMATOR_TIMEOUT_SEC = 15.0
    MAX_VELOCITY = 0.5

    def __init__(self):
        # SyncCrazyflie instance
        self.crazyflie = None
        self.address = None
        self.swarmIndex = None
        self.trackIndex = None

        self.travelTime = None
        self.takeoffDelay = 0.0

        self.targetPosition = (0, 0, 0)
        self.currentPosition = (0, 0, 0)

        self.state = DroneState.DISCONNECTED


        self.writeEvent = Event()
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
        self.light_controller.set_color(0, 0, 0, 0.0, True)
        threadUtil.interruptibleSleep(0.5)

        self.state = DroneState.INITIALIZING
        self.light_controller.set_color(255, 200, 0, 0.0, True)

    def startTrajectory(self):
        self.crazyflie.cf.high_level_commander.start_trajectory(Drone.TRAJECTORY_ID, 1.0, False)

    def disableAutoPing(self):
        self.crazyflie.cf.auto_ping = False

    def updateSensors(self, uploadGeometry, geometryOne, geometryTwo):
        Logger.log("Updating sensor & positioning data", self.swarmIndex)
        self.crazyflie.cf.param.set_value('lighthouse.method', '0')
        self.crazyflie.cf.param.set_value('lighthouse.systemType', '1')

        # PID controller
        self.crazyflie.cf.param.set_value('stabilizer.controller', '0')
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
        time.sleep(0.25)

        exceptionUtil.checkInterrupt()
        self.crazyflie.cf.param.set_value('kalman.resetEstimation', '0')
        self.currentPosition = (0, 0, 0)
        self.waitForEstimator()

    def waitForEstimator(self):
        startTime = time.time()
        log_config = LogConfig(name='Kalman Variance', period_in_ms=100)
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

        convergedDuration = 0
        lastTime = None

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

                x = data['kalman.stateX']
                y = data['kalman.stateY']
                z = data['kalman.stateZ']
                has_valid_data = z < 0.25

                has_converged = (max_x - min_x) < threshold and (max_y - min_y) < threshold and (max_z - min_z) < threshold and has_valid_data
                timed_out = (time.time() - startTime) > Drone.ESTIMATOR_TIMEOUT_SEC
                currentTime = time.time()
                elapsed = 0 if lastTime is None else currentTime - lastTime
                lastTime = currentTime

                if has_converged:
                    convergedDuration += elapsed
                    if convergedDuration >= 1.5:
                        print("Initial position for drone", self.swarmIndex, "has converged, to position: ", ("({:.2f}, {:.2f}, {:.2f})".format(x, y, z)))
                        self.currentPosition = (float(x), float(y), float(z))
                        break
                else:
                    convergedDuration = 0
                    if timed_out:
                        message = "Invalid position data received. Height: " + str(z)
                        Logger.error(message, self.swarmIndex)
                        self.setError()
                        raise ConnectionAbortedError(message)

    def waitForTargetPosition(self, targetX, targetY, targetZ, minTime=None, timeoutSeconds=None):
        startTime = time.time()
        log_config = LogConfig(name='Kalman Position', period_in_ms=100)
        log_config.add_variable('kalman.stateX', 'float')
        log_config.add_variable('kalman.stateY', 'float')
        log_config.add_variable('kalman.stateZ', 'float')
        threshold = 0.125

        timeAtTarget = 0
        lastTime = None
        commandIssued = False

        if not (self.crazyflie and self.crazyflie.cf and self.crazyflie.cf.link):
            return

        with SyncLogger(self.crazyflie, log_config) as logger:
            for log_entry in logger:

                exceptionUtil.checkInterrupt()
                data = log_entry[1]

                x = float(data['kalman.stateX'])
                y = float(data['kalman.stateY'])
                z = float(data['kalman.stateZ'])

                has_converged = abs(targetX - x) < threshold and abs(targetY - y) and abs(targetZ - z)
                timed_out = (time.time() - startTime) > timeoutSeconds if timeoutSeconds else False
                currentTime = time.time()
                elapsed = 0 if lastTime is None else currentTime - lastTime
                lastTime = currentTime

                if has_converged:
                    timeAtTarget += elapsed
                    commandIssued = False
                    if minTime is None or timeAtTarget >= minTime:
                        print("Target position for drone", self.swarmIndex, "has converged. Target was:", ("({:.2f}, {:.2f}, {:.2f})".format(targetX, targetY, targetZ)), ", converged to position: ", ("({:.2f}, {:.2f}, {:.2f})".format(x, y, z)))
                        break

                else:
                    timeAtTarget = 0
                    if not commandIssued:
                        distance = vectorMath.distance((x, y, z), (targetX, targetY, targetZ))
                        travelTime = distance / Drone.MAX_VELOCITY
                        self.commander.go_to(x, y, z, 0, travelTime)
                        commandIssued = True

                    if timed_out:
                        Logger.error("Drone failed to reach target position!", self.swarmIndex)
                        self.setError()
                        raise DroneException("Drone did not achieve the target position")


    def writeTrajectory(self, data):
        if self.crazyflie is None or len(data) == 0:
            Logger.error("Could not upload trajectory data!", self.swarmIndex)
            raise ValueError("Cannot write trajectory data")

        trajectoryMemory = self.crazyflie.cf.mem.get_mems(MemoryElement.TYPE_TRAJ)[0]
        self.write(lambda: trajectoryMemory.write_raw(data, self.writeComplete, self.writeFailed))

        exceptionUtil.checkInterrupt()
        self.crazyflie.cf.high_level_commander.define_trajectory_compressed(Drone.TRAJECTORY_ID, 0)

    def writeBaseStationData(self, geometryOne, geometryTwo):
        geo_dict = {0: geometryOne, 1: geometryTwo}
        helper = LighthouseMemHelper(self.crazyflie.cf)
        writer = LighthouseConfigWriter(self.crazyflie.cf, nr_of_base_stations=2)
        self.write(lambda: helper.write_geos(geo_dict, self.writeComplete))
        self.write(lambda: writer.write_and_store_config(self.writeComplete, geos=geo_dict, calibs=calibration.CALIBRATION_DATA))
        exceptionUtil.checkInterrupt()

    def writeLedTimings(self, color_data):
        mems = self.crazyflie.cf.mem.get_mems(MemoryElement.TYPE_DRIVER_LEDTIMING)
        if self.crazyflie is None or len(color_data) == 0:
            Logger.error("Could not upload LED data", self.swarmIndex)
            raise ValueError("Cannot write led timing data")

        self.write(lambda: mems[0].write_raw(color_data, self.writeComplete))
        exceptionUtil.checkInterrupt()

    # -- DATA UTILS -- #

    def write(self, writeOperation):
        self.writeSuccess = False
        self.writeEvent.clear()
        writeOperation()

        self.writeEvent.wait()
        if not self.writeSuccess:
            raise Exception("Write failed!")

    def writeComplete(self, *args):
        self.writeSuccess = True
        if self.writeEvent:
            self.writeEvent.set()

    def writeFailed(self, *args):
        self.writeSuccess = False
        self.setError()
        if self.writeEvent:
            self.writeEvent.set()

    def setError(self):
        self.state = DroneState.ERROR
        self.light_controller.set_color(255, 200, 0, 0.0, True)