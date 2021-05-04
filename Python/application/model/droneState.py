from enum import Enum

class DroneState(Enum):
    DISCONNECTED = 0
    IDLE = 1
    INITIALIZING = 2
    POSITIONING = 3
    READY = 4
    IN_FLIGHT = 5
    LANDING = 6
