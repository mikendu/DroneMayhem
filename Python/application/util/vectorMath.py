
def distance(v1, v2):
    x1, y1, z1 = v1
    x2, y2, z2 = v2
    delta = (x2 - x1, y2 - y1, z2 - z1)
    return magnitude(delta)

def magnitude(v):
    x, y, z = v
    sqrMagnitude = pow(x, 2) + pow(y, 2) + pow(z, 2)
    return pow(sqrMagnitude, 0.5)