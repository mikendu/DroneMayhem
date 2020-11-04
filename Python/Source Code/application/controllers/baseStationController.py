import sys
import openvr
import time

from cflib.crazyflie.mem import MemoryElement
from cflib.crazyflie.mem import LighthouseBsGeometry

from util.exceptionUtil import *
from util.coordinateUtil import *

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
            raiseError("Could not get pose for OpenVR device with index: " + str(self.index), OpenVRException)

        pose = poses[self.index]
        if pose and pose.bPoseIsValid:
            poseMatrix = pose.mDeviceToAbsoluteTracking
            self.positionGeometry.origin = getPosition(poseMatrix)
            self.positionGeometry.rotation_matrix = getRotation(poseMatrix)  
        else:
            raiseError("Got invalid pose for OpenVR device with index: " + str(self.index), OpenVRException)


class BaseStationController():


    def __init__(self, appController):
        self.appController = appController
        self.baseStations = []
        self.connectToBaseStations()
        self.memoryMapping = {}

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
                
    def writeBaseStationData(self, crazyflie, drone):
        drone.dataWritten = False
        mems = crazyflie.mem.get_mems(MemoryElement.TYPE_LH)
        count = len(mems)
        if count != 1:
            raise Exception("Could not find lighthouse memory. Make sure the drone has a lighthouse deck installed!")

        mems[0].geometry_data = [self.baseStations[0].positionGeometry, self.baseStations[1].positionGeometry]
        mems[0].write_data(self.onWriteFinished)
        self.memoryMapping[mems[0]] = drone

        while not drone.dataWritten:
            time.sleep(0.01)
        
        checkInterrupt()
            

    def onWriteFinished(self, mem, addr):
        drone = self.memoryMapping[mem]
        drone.dataWritten = True
