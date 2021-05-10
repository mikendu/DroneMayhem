from enum import Enum

class DroneState(Enum):
    DISCONNECTED = 0
    INITIALIZING = 1
    CONNECTED = 2
    IN_FLIGHT = 3
    LANDING = 4
