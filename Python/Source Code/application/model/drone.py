from enum import Enum

class DroneState(Enum):
    DISCONNECTED = 0
    IDLE = 1
    INITIALIZING = 2
    POSITIONING = 3
    READY = 4
    IN_FLIGHT = 5
    LANDING = 6

class Drone():

    def __init__(self):
        self.crazyflie = None # SyncCrazyflie instance
        self.address = None
        self.swarmIndex = None
        self.state = DroneState.DISCONNECTED
        self.dataWritten = False