from threading import Event

INTERRUPT_FLAG = Event()

class DroneException(Exception):
    pass

class OpenVRException(Exception):
    pass

class UnknownException(Exception):
    pass

class SequenceInterrupt(Exception):
    pass

def raiseError(message, exceptionType = UnknownException):
    print(message)
    raise exceptionType(message)

def checkInterrupt():
    global INTERRUPT_FLAG
    if INTERRUPT_FLAG.is_set():
        raise SequenceInterrupt()