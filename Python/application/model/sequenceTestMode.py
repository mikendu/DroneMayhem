from enum import Enum


class SequenceTestMode(Enum):
    NONE = 0
    COLORS = 1
    FIRST_WAYPOINT = 2

    def __str__(self):
        if self.value == 0:
            return "OFF"
        if self.value == 1:
            return "COLORS ONLY"
        if self.value == 2:
            return "POSITION + LAND"
        return ""

    def __int__(self):
        return self.value
