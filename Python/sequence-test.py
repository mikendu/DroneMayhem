import cflib.crtp
from cflib.crazyflie import Crazyflie
from cflib.crazyflie.mem import LighthouseBsGeometry
from cflib.crazyflie.mem import MemoryElement
from cflib.crazyflie.mem import Poly4D
from cflib.crazyflie.log import LogConfig
from cflib.crazyflie.syncCrazyflie import SyncCrazyflie
from cflib.crazyflie.syncLogger import SyncLogger


import logging
import time
import signal
import sys
import keyboard
import openvr
import math
import numpy as np


uri = None
vr = None


baseOne = LighthouseBsGeometry()
baseTwo = LighthouseBsGeometry()
baseStationIndexes = []
baseStations = [baseOne, baseTwo]
dataWritten = False
sequence = None

# Rotation matrixes to convert to the CF coordinate system
openvr_to_cf = np.array([
    [0.0, 0.0, -1.0],
    [-1.0, 0.0, 0.0],
    [0.0, 1.0, 0.0],
])

cf_to_openvr = np.array([
    [0.0, -1.0, 0.0],
    [0.0, 0.0, 1.0],
    [-1.0, 0.0, 0.0],
])


def initCrazyflie():
    cflib.crtp.init_drivers()
    print('Scanning interfaces for Crazyflies...')
    available = cflib.crtp.scan_interfaces()

    if len(available) == 0:
        print('No crazyflie found!')
        exit(0)

    uri = available[0][0]
    print('Found crazyflie: ', uri)
    return uri


def signal_handler(sig, frame):
    print('Interrupt signal received, exiting!')
    sys.exit(0)


def setLED(scf, r, g, b, time): 
    cf = scf.cf
    cf.param.set_value('ring.fadeTime', str(max(time, 0.01)))
    color = (int(r) << 16) | (int(g) << 8) | int(b)
    cf.param.set_value('ring.fadeColor', str(color))

def findBaseStations():
    print('\n\nListing OpenVR Devices...')
    for i in range(openvr.k_unMaxTrackedDeviceCount):
        device_class = vr.getTrackedDeviceClass(i)
        if device_class == openvr.TrackedDeviceClass_TrackingReference:
            baseStationIndexes.append(i)
            print('\t- Found Base Station with index: ', i)


def getPosition(poseMatrix):
    rawPosition = [
        poseMatrix[0][3], 
        poseMatrix[1][3], 
        poseMatrix[2][3]
    ]
    return np.dot(openvr_to_cf, rawPosition)

def getRotation(poseMatrix): 
    rawRotation = [poseMatrix[0][:3], poseMatrix[1][:3], poseMatrix[2][:3]]
    return np.dot(openvr_to_cf, np.dot(rawRotation, cf_to_openvr))


def updateBaseStations(cf):
    maxIndex = max(baseStationIndexes[0], baseStationIndexes[1])
    poses = vr.getDeviceToAbsoluteTrackingPose(openvr.TrackingUniverseStanding, 0, maxIndex)
    for i in range(0, maxIndex + 1):
        if poses[i].bPoseIsValid and i in baseStationIndexes:
            pose = poses[i]
            listIndex = baseStationIndexes.index(i)
            baseStation = baseStations[listIndex]
            baseStation.origin = getPosition(pose.mDeviceToAbsoluteTracking)
            baseStation.rotation_matrix = getRotation(pose.mDeviceToAbsoluteTracking)

    writeBaseStationData(cf, baseOne, baseTwo)


def dataWrittenCallback(mem, addr):
    global dataWritten
    dataWritten = True
    # print('Data written')

def writeBaseStationData(cf, baseOne, baseTwo):
    global dataWritten
    dataWritten = False
    mems = cf.mem.get_mems(MemoryElement.TYPE_LH)
    count = len(mems)
    if count != 1:
        raise Exception('Unexpected nr of memories found:', count)

    mems[0].geometry_data = [baseOne, baseTwo]
    mems[0].write_data(dataWrittenCallback)

    while not dataWritten:
        time.sleep(0.01)


def mainLoop(scf = None):    
    """
    if (scf is None):
        print("No crazyflie connection provided, exiting.")
        exit(0)

    cf = scf.cf
    updateBaseStations(cf)
    #commander = cf.high_level_commander"""

    setLED(scf, 0, 0, 0, 0.0)
    print("First red")
    setLED(scf, 255, 0, 0, 1.0)
    time.sleep(1.0)
    
    print("First green")
    setLED(scf, 0, 255, 0, 1.0)
    time.sleep(1.0)

    setLED(scf, 0, 0, 255, 1.0)
    time.sleep(1.0)

    setLED(scf, 255, 255, 255, 1.0)
    time.sleep(1.0)

    setLED(scf, 255, 0, 0, 0.0)
    time.sleep(0.1)

    setLED(scf, 0, 0, 0, 0.0)
    time.sleep(0.1)

    setLED(scf, 255, 0, 0, 0.0)
    time.sleep(0.1)

    setLED(scf, 0, 0, 0, 0.0)
    time.sleep(0.1)
    
    """
    commander.takeoff(1.0, 3.0)
    time.sleep(3.0)



    commander.go_to(0.0, 0.0, 0.2, 0.0, 3.0)
    time.sleep(3.0)

    commander.go_to(0.0, 0.0, 1.5, 0.0, 3.0)
    time.sleep(3.0)



    commander.go_to(1.0, 0.0, 1.5, 0.0, 3.0)
    time.sleep(3.0)

    commander.go_to(0.0, 0.0, 1.5, 0.0, 3.0)
    time.sleep(3.0)




    commander.go_to(0.0, 1.0, 1.5, 0.0, 3.0)
    time.sleep(3.0)

    commander.go_to(0.0, 0.0, 1.5, 0.0, 3.0)
    time.sleep(3.0)




    commander.go_to(0.0, 0.0, 0.2, 0.0, 3.0)
    time.sleep(3.0)

    commander.land(0.05, 3.0)"""
    #commander.stop()





def waitForEstimator(scf):
    print('Waiting for estimator to find position...')

    log_config = LogConfig(name='Kalman Variance', period_in_ms=500)
    log_config.add_variable('kalman.varPX', 'float')
    log_config.add_variable('kalman.varPY', 'float')
    log_config.add_variable('kalman.varPZ', 'float')

    var_y_history = [1000] * 10
    var_x_history = [1000] * 10
    var_z_history = [1000] * 10

    threshold = 0.001

    with SyncLogger(scf, log_config) as logger:
        for log_entry in logger:
            data = log_entry[1]

            var_x_history.append(data['kalman.varPX'])
            var_x_history.pop(0)
            var_y_history.append(data['kalman.varPY'])
            var_y_history.pop(0)
            var_z_history.append(data['kalman.varPZ'])
            var_z_history.pop(0)

            min_x = min(var_x_history)
            max_x = max(var_x_history)
            min_y = min(var_y_history)
            max_y = max(var_y_history)
            min_z = min(var_z_history)
            max_z = max(var_z_history)

            # print("{} {} {}".
            #       format(max_x - min_x, max_y - min_y, max_z - min_z))

            if (max_x - min_x) < threshold and (
                    max_y - min_y) < threshold and (
                    max_z - min_z) < threshold:
                break


def resetEstimator(scf):
    print('\n')
    cf = scf.cf
    cf.param.set_value('kalman.resetEstimation', '1')
    time.sleep(0.1)
    cf.param.set_value('kalman.resetEstimation', '0')
    waitForEstimator(cf)


def positionCallback(timestamp, data, logconf):
    x = data['kalman.stateX']
    y = data['kalman.stateY']
    z = data['kalman.stateZ']
    print('pos: ({}, {}, {})'.format(x, y, z))
    global height
    height = z


def startPrinting(scf):
    log_conf = LogConfig(name='Position', period_in_ms=100)
    log_conf.add_variable('kalman.stateX', 'float')
    log_conf.add_variable('kalman.stateY', 'float')
    log_conf.add_variable('kalman.stateZ', 'float')

    scf.cf.log.add_config(log_conf)
    log_conf.data_received_cb.add_callback(positionCallback)
    log_conf.start()






signal.signal(signal.SIGINT, signal_handler)
vr = openvr.init(openvr.VRApplication_Other)
findBaseStations()
print('\n\n')

uri = initCrazyflie()
with SyncCrazyflie(uri, cf=Crazyflie(rw_cache='./cache')) as scf:
    cf = scf.cf
    cf.param.set_value('lighthouse.method', '1')
    cf.param.set_value('stabilizer.controller', '2') # Mellinger controller
    cf.param.set_value('commander.enHighLevel', '1')    
    cf.param.set_value('ring.effect', '14')

    #updateBaseStations(cf)
    #resetEstimator(scf)
    mainLoop(scf)
    
