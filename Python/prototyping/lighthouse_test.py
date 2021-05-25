import sys
import time
import os
import openvr
import numpy as np

from threading import Event

import cflib.crtp
from cflib.crazyflie.syncCrazyflie import SyncCrazyflie
from cflib.crazyflie import Crazyflie
from cflib.crazyflie.log import LogConfig
from cflib.crazyflie.syncLogger import SyncLogger
from cflib.crazyflie.mem import MemoryElement
from cflib.crazyflie.mem import LighthouseMemHelper, LighthouseBsGeometry, LighthouseBsCalibration
from cflib.localization import LighthouseConfigWriter


os.environ["USE_CFLINK"] = "cpp"
cflib.crtp.init_drivers()
writeSuccess = False
writeEvent = Event()
readEvent = Event()

# URI to the Crazyflie to connect to
uri = 'radio://*/55/2M/E7E7E7E704?safelink=1&autoping=1'
vr = openvr.init(openvr.VRApplication_Other)


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
    transformed = np.dot(OpenVR_To_CF, rawPosition)
    print("Transformed position: ", transformed)
    return transformed

def getRotation(poseMatrix):
    rawRotation = [poseMatrix[0][:3], poseMatrix[1][:3], poseMatrix[2][:3]]
    transformed = np.dot(OpenVR_To_CF, np.dot(rawRotation, CF_To_OpenVR))
    print("Transformed rotation: ", transformed)
    return transformed



def get_geometry():
    geometry = []
    for i in range(openvr.k_unMaxTrackedDeviceCount):
        device_class = vr.getTrackedDeviceClass(i)
        if device_class == openvr.TrackedDeviceClass_TrackingReference:
            poses = vr.getDeviceToAbsoluteTrackingPose(openvr.TrackingUniverseStanding, 0, i)
            if not poses or len(poses) < i + 1:
                raise Exception("Could not get pose for OpenVR device with index: " + str(i))

            pose = poses[i]
            if pose and pose.bPoseIsValid:
                poseMatrix = pose.mDeviceToAbsoluteTracking
                positionGeometry = LighthouseBsGeometry()
                positionGeometry.origin = getPosition(poseMatrix)
                positionGeometry.rotation_matrix = getRotation(poseMatrix)
                positionGeometry.valid = True
                geometry.append(positionGeometry)
            else:
                raise Exception("Got invalid pose for OpenVR device with index: " + str(i))
    return geometry

def wait_for_position_estimator(scf):
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

            # print("Waiting for kalman filter to converge. var x: ", (max_x - min_x), ", var y: ", (max_y - min_y), ", var z: ", (max_z - min_z))

            var_x = (max_x - min_x)
            var_y = (max_y - min_y)
            var_z = (max_z - min_z)

            print('var: ({}, {}, {})'.format(var_x, var_y, var_z))
            if (max_x - min_x) < threshold and (
                    max_y - min_y) < threshold and (
                    max_z - min_z) < threshold:
                print('\n\nCONVERGED!!! VAR: ({}, {}, {})'.format(var_x, var_y, var_z))
                break


def reset_estimator(scf):
    cf = scf.cf
    cf.param.set_value('kalman.resetEstimation', '1')
    time.sleep(0.1)
    cf.param.set_value('kalman.resetEstimation', '0')

    wait_for_position_estimator(cf)


def position_callback(timestamp, data, logconf):
    x = data['kalman.stateX']
    y = data['kalman.stateY']
    z = data['kalman.stateZ']
    print('\rpos: ({:.2f}, {:.2f}, {:.2f})'.format(x, y, z), end='', flush=True)

def start_position_printing(scf):
    log_conf = LogConfig(name='Position', period_in_ms=30)
    log_conf.add_variable('kalman.stateX', 'float')
    log_conf.add_variable('kalman.stateY', 'float')
    log_conf.add_variable('kalman.stateZ', 'float')

    scf.cf.log.add_config(log_conf)
    log_conf.data_received_cb.add_callback(position_callback)
    log_conf.start()

def write(writeOperation):
    global writeSuccess
    writeSuccess = False
    writeEvent.clear()
    writeOperation()
    writeEvent.wait()

    if not writeSuccess:
        raise Exception("Write failed!")
    else:
        print("Write success!!")

def writeComplete(*args):
    global writeSuccess
    writeSuccess = True
    if writeEvent:
       writeEvent.set()

def writeFailed(*args):
    global writeSuccess
    writeSuccess = True
    if writeEvent:
       writeEvent.set()


def geoDataReady(geo_data):
    for id, data in geo_data.items():
        print('---- Geometry for base station', id + 1)
        data.dump()
        print()
    readEvent.set()

def calibDataReady(calib_data):
    for id, data in calib_data.items():
        print("\nCalib: ", type(data))
        print('---- Calibration for base station', id + 1)
        data.dump()
        print()
    readEvent.set()



with SyncCrazyflie(uri, cf=Crazyflie(ro_cache='../cache', rw_cache='../cache')) as scf:
    scf.cf.param.set_value('stabilizer.controller', '2')  # Mellinger controller
    scf.cf.param.set_value('commander.enHighLevel', '1')
    scf.cf.param.set_value('lighthouse.method', '0')
    scf.cf.param.set_value('lighthouse.systemType', '1')

    helper = LighthouseMemHelper(scf.cf)
    helper.read_all_geos(geoDataReady)
    readEvent.wait()

    readEvent.clear()
    helper.read_all_calibs(calibDataReady)
    readEvent.wait()


    geometryOne, geometryTwo = get_geometry()

    bs1calib = LighthouseBsCalibration()
    bs1calib.sweeps[0].phase = 1.0
    bs1calib.sweeps[0].tilt = 2.0
    bs1calib.sweeps[0].curve = 3.0
    bs1calib.sweeps[0].gibmag = 4.0
    bs1calib.sweeps[0].gibphase = 5.0
    bs1calib.sweeps[0].ogeemag = 6.0
    bs1calib.sweeps[0].ogeephase = 7.0
    bs1calib.sweeps[1].phase = 1.1
    bs1calib.sweeps[1].tilt = 2.1
    bs1calib.sweeps[1].curve = 3.1
    bs1calib.sweeps[1].gibmag = 4.1
    bs1calib.sweeps[1].gibphase = 5.1
    bs1calib.sweeps[1].ogeemag = 6.1
    bs1calib.sweeps[1].ogeephase = 7.1
    bs1calib.uid = 1234
    bs1calib.valid = True

    bs2calib = LighthouseBsCalibration()
    bs2calib.sweeps[0].phase = 1.5
    bs2calib.sweeps[0].tilt = 2.5
    bs2calib.sweeps[0].curve = 3.5
    bs2calib.sweeps[0].gibmag = 4.5
    bs2calib.sweeps[0].gibphase = 5.5
    bs2calib.sweeps[0].ogeemag = 6.5
    bs2calib.sweeps[0].ogeephase = 7.5
    bs2calib.sweeps[1].phase = 1.51
    bs2calib.sweeps[1].tilt = 2.51
    bs2calib.sweeps[1].curve = 3.51
    bs2calib.sweeps[1].gibmag = 4.51
    bs2calib.sweeps[1].gibphase = 5.51
    bs2calib.sweeps[1].ogeemag = 6.51
    bs2calib.sweeps[1].ogeephase = 7.51
    bs2calib.uid = 9876
    bs2calib.valid = True
    calib_dict = {0: bs1calib, 1: bs2calib}

    # Note: base station ids (channels) are 0-indexed
    geo_dict = {0: geometryOne, 1: geometryTwo}
    write(lambda: helper.write_geos(geo_dict, writeComplete))
    write(lambda: helper.write_calibs(calib_dict, writeComplete))

    writer = LighthouseConfigWriter(scf.cf, nr_of_base_stations=2)
    writer.write_and_store_config(writeComplete, geos=geo_dict, calibs=calib_dict)
    #
    # print()
    # readEvent.clear()
    # helper.read_all_geos(geoDataReady)
    # readEvent.wait()
    #
    # # time.sleep(1.0)
    # print()
    # print()
    # print()
    start_position_printing(scf)
    reset_estimator(scf)

    while True:
        pass

    # scf.cf.high_level_commander.takeoff(0.35, 3.0)
    # time.sleep(3.2)
    # scf.cf.high_level_commander.land(0.05, 3.0)
    # time.sleep(3.2)
    # scf.cf.high_level_commander.stop()
