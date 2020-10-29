
class DroneException(Exception):
    pass

class OpenVRException(Exception):
    pass

class UnknownException(Exception):
    pass

def raiseError(message, exceptionType = UnknownException):
    print(message)
    raise exceptionType(message)