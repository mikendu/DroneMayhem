

def b(value):
    if value > 255:
        lsb = (value & 255)
        msb = ((value >> 8) & 255)
        lower = "{0:b}".format(lsb).zfill(8)
        upper = "{0:b}".format(msb).zfill(8)
        return lower + " " + upper
    else:
        return "{0:b}".format(value).zfill(8)

def d(lsb, msb = None):
    if msb is not None:
        lower = int(lsb, 2)
        upper = int(msb, 2)
        return ((upper << 8) | lower)
    else:
        return int(lsb, 2)

def byte_array(val):
    values = val.split(' ')
    return list(map(lambda x: d(x), values))

def concat(lsb, msb):
    return ((msb << 8) | lsb)