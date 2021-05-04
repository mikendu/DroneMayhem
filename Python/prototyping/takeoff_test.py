import os
import struct

import cflib.crtp

from cflib.crtp.cflinkcppdriver import CfLinkCppDriver
from cflib.crtp.radiodriver import RadioDriver
from cflib.crtp.crtpstack import CRTPPacket
from cflib.crtp.crtpstack import CRTPPort
from cflib.crazyflie import Crazyflie
from cflib.crazyflie import HighLevelCommander
from cflib.crazyflie.mem import LighthouseBsGeometry
from cflib.crazyflie.mem import LighthouseMemHelper
from cflib.crazyflie.mem import MemoryElement
from cflib.crazyflie.mem import Poly4D
from cflib.crazyflie.log import LogConfig
from cflib.crazyflie.syncCrazyflie import SyncCrazyflie
from cflib.crazyflie.syncLogger import SyncLogger

from threading import Event


import logging
import time
import signal
import sys
import openvr
import math
import numpy as np



uri = None
vr = None

# Only output errors from the logging framework
logging.basicConfig(level=logging.ERROR)

baseOne = LighthouseBsGeometry()
baseTwo = LighthouseBsGeometry()
baseStationIndexes = []
baseStations = [baseOne, baseTwo]
dataWrittenEvent = Event()

START_Z = 0.0

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
            baseStation.valid = True

    writeBaseStationData(cf, baseOne, baseTwo)


def dataWrittenCallback(success):
    if success:
        print('Data written')
    else:
        print('Write failed')

    dataWrittenEvent.set()

def writeBaseStationData(cf, baseOne, baseTwo):
    global dataWritten
    dataWritten = False
    mems = cf.mem.get_mems(MemoryElement.TYPE_LH)
    count = len(mems)
    if count != 1:
        raise Exception('Unexpected nr of memories found:', count)

    helper = LighthouseMemHelper(scf.cf)

    geo_dict = { 0: baseOne, 1: baseTwo }
    helper.write_geos(geo_dict, dataWrittenCallback)
    dataWrittenEvent.wait()


def mainLoop(scf = None):    
    if (scf is None):
        print("No crazyflie connection provided, exiting.")
        exit(0)

    cf = scf.cf
    updateBaseStations(cf)
    commander = cf.high_level_commander
    
    print('Taking off')
    commander.takeoff(START_Z + 0.05, 0.5)
    time.sleep(0.5)

    print('Landing')
    commander.land(START_Z, 0.5)
    time.sleep(0.5)
    commander.stop()

    print('Sequence done!!')


def waitForEstimator(scf):
    print('Waiting for estimator to find position...')

    log_config = LogConfig(name='Kalman Variance', period_in_ms=500)
    log_config.add_variable('kalman.varPX', 'float')
    log_config.add_variable('kalman.varPY', 'float')
    log_config.add_variable('kalman.varPZ', 'float')
    log_config.add_variable('kalman.stateX', 'float')
    log_config.add_variable('kalman.stateY', 'float')
    log_config.add_variable('kalman.stateZ', 'float')

    var_y_history = [1000] * 10
    var_x_history = [1000] * 10
    var_z_history = [1000] * 10

    threshold = 0.01

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

            x = data['kalman.stateX']
            y = data['kalman.stateY']
            z = data['kalman.stateZ']
            global START_Z
            START_Z = z
            # print("{:6.4f} {:6.4f} {:6.4f}".format(x, y, z))

            if (max_x - min_x) < threshold and (
                    max_y - min_y) < threshold and (
                    max_z - min_z) < threshold:
                
                #print("-- POSITION FOUND --")
                print("Found position: (", x, ",", y, ",", z, ")")
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
    print(f"pos: ({x:6.4f} {y:6.4f} {z:6.4f})\r", end="")


def startPrinting(scf):
    print("\n-- STARTING PRINT LOOP --\n\n")
    log_conf = LogConfig(name='Position', period_in_ms=15)
    log_conf.add_variable('kalman.stateX', 'float')
    log_conf.add_variable('kalman.stateY', 'float')
    log_conf.add_variable('kalman.stateZ', 'float')

    scf.cf.log.add_config(log_conf)
    log_conf.data_received_cb.add_callback(positionCallback)
    log_conf.start()


# os.environ["USE_CFLINK"] = "cpp"

# if 'USE_CFLINK' in os.environ:
    # del os.environ['USE_CFLINK']
signal.signal(signal.SIGINT, signal_handler)
vr = openvr.init(openvr.VRApplication_Other)
findBaseStations()

print("Found crazyflie with URI: ", uri)

uri = initCrazyflie()
with SyncCrazyflie(uri, cf=Crazyflie(rw_cache='./cache')) as scf:

    cf = scf.cf
    cf.param.set_value('lighthouse.method', '0')
    cf.param.set_value('stabilizer.controller', '2') # Mellinger controller
    cf.param.set_value('commander.enHighLevel', '1')

    # updateBaseStations(cf)
    # resetEstimator(scf)

    # mainLoop(scf)

def fakeTakeoff(radio, height, time):
    data = struct.pack('<BBff?f',
                                      HighLevelCommander.COMMAND_TAKEOFF_2,
                                      HighLevelCommander.ALL_GROUPS,
                                      height,
                                      0,
                                      False,
                                      time)
    pk = CRTPPacket()
    pk.port = CRTPPort.SETPOINT_HL
    pk.data = data
    radio.send_packet(pk)


def fakeLand(radio, height, time):
    data = struct.pack('<BBff?f',
                                      HighLevelCommander.COMMAND_LAND_2,
                                      HighLevelCommander.ALL_GROUPS,
                                      height,
                                      0,
                                      False,
                                      time)
    pk = CRTPPacket()
    pk.port = CRTPPort.SETPOINT_HL
    pk.data = data
    radio.send_packet(pk)


def fakeStop(radio):
    data = struct.pack('<BB',
                                      HighLevelCommander.COMMAND_STOP,
                                      HighLevelCommander.ALL_GROUPS)
    pk = CRTPPacket()
    pk.port = CRTPPort.SETPOINT_HL
    pk.data = data
    radio.send_packet(pk)

# Broadcast address == 0xFFE7E7E7E7 ??
radioDriver = RadioDriver()
radioDriver.connect("radio://0/55/2M/E7E7E7E7E7", None, None)

print('Taking off')
fakeTakeoff(radioDriver, START_Z + 0.05, 4.0)
time.sleep(0.25)

print('Landing')
fakeLand(radioDriver, START_Z, 4.0)
time.sleep(0.25)

fakeStop(radioDriver)

print('Sequence done!!')

time.sleep(1.5)
radioDriver.close()