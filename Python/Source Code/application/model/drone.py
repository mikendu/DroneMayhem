from .droneState import DroneState

class Drone():

    def __init__(self):
        # SyncCrazyflie instance
        self.crazyflie = None
        self.address = None
        self.swarmIndex = None
        self.state = DroneState.DISCONNECTED
        self.dataWritten = False