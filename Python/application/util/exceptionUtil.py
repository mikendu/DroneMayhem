from threading import Event
from application.common.exceptions import UnknownException, SequenceInterrupt

INTERRUPT_FLAG = Event()

def raiseError(message, exceptionType = UnknownException):
    print(message)
    raise exceptionType(message)

def checkInterrupt(ignore=False):
    global INTERRUPT_FLAG
    if INTERRUPT_FLAG.is_set() and not ignore:
        raise SequenceInterrupt()

def setInterrupt(value):
    global INTERRUPT_FLAG
    if value:
        INTERRUPT_FLAG.set()
    else:
        INTERRUPT_FLAG.clear()