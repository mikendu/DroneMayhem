from enum import Enum

class DroneState(Enum):
    IDLE = 0
    INITIALIZING = 1
    POSITIONING = 2
    READY = 3
    IN_FLIGHT = 4
    LANDING = 5

class Drone():

    crazyflie = None
    address = None
    swarmIndex = None
    state = None