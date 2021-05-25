import openvr
import time
from cflib.crazyflie.mem import LighthouseBsGeometry

from application.common.exceptions import VRException
from application.util import exceptionUtil, transformUtil


class BaseStation():

    def __init__(self, index, vrSystem):
        self.index = index
        self.vrSystem = vrSystem
        self.positionGeometry = LighthouseBsGeometry()
        self.initialized = False
        self.initialize()

    def initialize(self):        
        self.updatePositionMatrix()
        self.initialized = True

    def updatePositionMatrix(self):
        poses = self.vrSystem.getDeviceToAbsoluteTrackingPose(openvr.TrackingUniverseStanding, 0, self.index)
        
        if not poses or len(poses) < self.index + 1:
            exceptionUtil.raiseError("Could not get pose for OpenVR device with index: " + str(self.index), VRException)

        pose = poses[self.index]
        if pose and pose.bPoseIsValid:
            poseMatrix = pose.mDeviceToAbsoluteTracking
            self.positionGeometry.origin = transformUtil.getPosition(poseMatrix)
            self.positionGeometry.rotation_matrix = transformUtil.getRotation(poseMatrix)
            self.positionGeometry.valid = True
        else:
            exceptionUtil.raiseError("Got invalid pose for OpenVR device with index: " + str(self.index), VRException)


class BaseStationController():


    def __init__(self, appController):
        self.appController = appController
        self.baseStations = []
        self.connectToBaseStations()

    def connectToBaseStations(self):
        vrSystem = self.appController.vrSystem
        for i in range(openvr.k_unMaxTrackedDeviceCount):
            device_class = vrSystem.getTrackedDeviceClass(i)
            if device_class == openvr.TrackedDeviceClass_TrackingReference:
                try:
                    station = BaseStation(i, vrSystem)
                    self.baseStations.append(station)
                except VRException:
                    continue

    @property
    def geometryOne(self):
        return self.baseStations[0].positionGeometry

    @property
    def geometryTwo(self):
        return self.baseStations[1].positionGeometry
