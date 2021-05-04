import time
from cflib.crazyflie.mem import MemoryElement

from .droneState import DroneState
from application.util import exceptionUtil


class Drone():

    TRAJECTORY_ID = 1

    def __init__(self):
        # SyncCrazyflie instance
        self.crazyflie = None
        self.address = None
        self.swarmIndex = None
        self.state = DroneState.DISCONNECTED
        self.dataWritten = False
        self.trajectoryUploaded = False
        self.startPos = 0.0

    def writeTrajectory(self, data):
        self.trajectoryUploaded = False
        if self.crazyflie is None or len(data) == 0:
            return

        trajectoryMemory = self.crazyflie.cf.mem.get_mems(MemoryElement.TYPE_TRAJ)[0]
        trajectoryMemory.write_raw_data(data, self.writeComplete)

        while not self.trajectoryUploaded:
            time.sleep(0.01)

        exceptionUtil.checkInterrupt()
        self.crazyflie.cf.high_level_commander.define_trajectory_compressed(Drone.TRAJECTORY_ID, 0)

    def writeComplete(self, mem, addr):
        self.trajectoryUploaded = True

    def startTrajectory(self):
        self.crazyflie.cf.high_level_commander.start_trajectory(Drone.TRAJECTORY_ID, 1.0, False)