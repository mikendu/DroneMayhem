import numpy as np

# Rotation matrixes to convert to the CF coordinate system
OpenVR_To_CF = np.array([
    [0.0, 0.0, -1.0],
    [-1.0, 0.0, 0.0],
    [0.0, 1.0, 0.0],
])

CF_To_OpenVR = np.array([
    [0.0, -1.0, 0.0],
    [0.0, 0.0, 1.0],
    [-1.0, 0.0, 0.0],
])


def getPosition(poseMatrix):
    rawPosition = [
        poseMatrix[0][3],
        poseMatrix[1][3],
        poseMatrix[2][3]
    ]
    return np.dot(OpenVR_To_CF, rawPosition)


def getRotation(poseMatrix):
    rawRotation = [poseMatrix[0][:3], poseMatrix[1][:3], poseMatrix[2][:3]]
    return np.dot(OpenVR_To_CF, np.dot(rawRotation, CF_To_OpenVR))
