import sys
import openvr
from cflib.crazyflie.mem import MemoryElement
from cflib.crazyflie.mem import LighthouseBsGeometry

from util.exceptionUtil import *
from util.coordinateUtil import *

class BaseStation():

    index = None
    positionGeometry = LighthouseBsGeometry()
    initialized = False

    def __init__(self, index, vrSystem):
        self.index = index
        self.vrSystem = vrSystem
        self.initialize()

    def initialize(self):        
        self.updatePositionMatrix()
        self.initialized = True

    def updatePositionMatrix(self):
        poses = self.vrSystem.getDeviceToAbsoluteTrackingPose(openvr.TrackingUniverseStanding, self.index, self.index)
        
        if not poses or len(poses) == 0:
            raiseError("Could not get pose for OpenVR device with index: " + str(self.index), OpenVRException)

        pose = poses[0]
        if pose and pose.bPoseIsValid:
            poseMatrix = pose.mDeviceToAbsoluteTracking
            self.positionGeometry.origin = getPosition(poseMatrix)
            self.positionGeometry.rotation_matrix = getRotation(poseMatrix)        
        else:
            raiseError("Got invalid pose for OpenVR device with index: " + str(self.index), OpenVRException)


class BaseStationController():

    baseStations = []
    dataWritten = False

    def __init__(self, appController):
        self.appController = appController
        self.connectToBaseStations()

    def connectToBaseStations(self):
        vrSystem = self.appController.vrSystem
        for i in range(openvr.k_unMaxTrackedDeviceCount):
            device_class = vrSystem.getTrackedDeviceClass(i)
            if device_class == openvr.TrackedDeviceClass_TrackingReference:
                try:
                    station = BaseStation(i, vrSystem)
                    self.baseStations.append(station)
                except OpenVRException:
                    continue
                
    def writeBaseStationData(self, crazyflie):
        self.dataWritten = False
        mems = crazyflie.mem.get_mems(MemoryElement.TYPE_LH)
        count = len(mems)
        if count != 1:
            raise Exception('Unexpected number of memories found:', count)

        mems[0].geometry_data = [baseStations[0].positionGeometry, baseStations[1].positionGeometry]
        mems[0].write_data(onWriteFinished)

        while not self.dataWritten:
            time.sleep(0.01)

    def onWriteFinished(self, mem, addr):
        self.dataWritten = True
