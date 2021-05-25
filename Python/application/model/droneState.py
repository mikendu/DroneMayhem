from enum import Enum

class DroneState(Enum):
    DISCONNECTED = 0
    INITIALIZING = 1
    CONNECTED = 3
    IN_FLIGHT = 4
    LANDING = 5
    ERROR = 6
