import sys
import numpy as np
import math
import time
import collections
from threading import Event

from cflib.crazyflie import Crazyflie
from cflib.crazyflie.mem import LighthouseBsGeometry
from cflib.crazyflie.mem import LighthouseMemHelper
from cflib.crazyflie.log import LogConfig
from cflib.crazyflie.syncCrazyflie import SyncCrazyflie
from cflib.localization import LighthouseConfigWriter
from cflib.localization import LighthouseBsGeoEstimator
from cflib.localization import LighthouseSweepAngleAverageReader

class GeometryBuffer:

    def __init__(self, size):
        self.rotationBuffer = collections.deque(maxlen=size)
        self.positionBuffer = collections.deque(maxlen=size)

    def addPosition(self, position):
        self.positionBuffer.append(position)

    def addRotation(self, rotation):
        self.rotationBuffer.append(rotation)

    @property
    def size(self):
        return len(self.positionBuffer)

    @property
    def position(self):
        return np.mean(self.positionBuffer, axis=0)

    @property
    def rotation(self):
        return np.mean(self.rotationBuffer, axis=0)

    def add(self, position, rotation):
        self.addPosition(position)
        self.addRotation(rotation)

class Calibrator:

    ROLLING_QUEUE_SIZE = 25
    VARIANCE_THRESHOLD = 0.001
    ERROR_THRESHOLD = 0.45

    def __init__(self, address):
        self.address = address
        self.geometryBuffers = {}
        self.baseStationStatus = [False, False, False, False]
        self.queueMap = {
            "varianceX": collections.deque(maxlen=Calibrator.ROLLING_QUEUE_SIZE),
            "varianceY": collections.deque(maxlen=Calibrator.ROLLING_QUEUE_SIZE),
            "positionX": collections.deque(maxlen=Calibrator.ROLLING_QUEUE_SIZE),
            "positionY": collections.deque(maxlen=Calibrator.ROLLING_QUEUE_SIZE),
            "positionZ": collections.deque(maxlen=Calibrator.ROLLING_QUEUE_SIZE),
            "yaw": collections.deque(maxlen=Calibrator.ROLLING_QUEUE_SIZE),
            "baseStationHistory": [
                collections.deque(maxlen=Calibrator.ROLLING_QUEUE_SIZE),
                collections.deque(maxlen=Calibrator.ROLLING_QUEUE_SIZE),
                collections.deque(maxlen=Calibrator.ROLLING_QUEUE_SIZE),
                collections.deque(maxlen=Calibrator.ROLLING_QUEUE_SIZE)
            ]
        }
        self.sweepAngles = None
        self.savedGeometry = None

    def begin(self):
        self.cf = Crazyflie(ro_cache='./cache', rw_cache='./cache')
        self.scf = SyncCrazyflie(self.address, cf=self.cf)
        self.scf.open_link()

        self.log_config = LogConfig(name='Calibration Monitoring', period_in_ms=15)
        self.log_config.add_variable('kalman.varPX', 'float')
        self.log_config.add_variable('kalman.varPY', 'float')
        self.log_config.add_variable('stateEstimate.x', 'float')
        self.log_config.add_variable('stateEstimate.y', 'float')
        self.log_config.add_variable('stateEstimate.z', 'float')
        self.log_config.add_variable('stateEstimate.yaw', 'float')
        self.log_config.add_variable('lighthouse.bsReceive')

        self.cf.log.add_config(self.log_config)
        self.log_config.data_received_cb.add_callback(self.onDataReceived)
        self.log_config.start()

    def end(self):
        self.log_config.stop()
        self.cf.close_link()

    def updateQueue(self, data, dataKey, mapKey):
        if dataKey not in data:
            return
        queue = self.queueMap[mapKey]
        queue.append(data[dataKey])

    def onDataReceived(self, timestamp, data, logConfig):
        self.updateQueue(data, dataKey='kalman.varPX', mapKey='varianceX')
        self.updateQueue(data, dataKey='kalman.varPY', mapKey='varianceY')
        self.updateQueue(data, dataKey='stateEstimate.x', mapKey='positionX')
        self.updateQueue(data, dataKey='stateEstimate.y', mapKey='positionY')
        self.updateQueue(data, dataKey='stateEstimate.z', mapKey='positionZ')
        self.updateQueue(data, dataKey='stateEstimate.yaw', mapKey='yaw')

        if 'lighthouse.bsReceive' in data:
            self.handleBaseStationStatus(data['lighthouse.bsReceive'])


    def handleBaseStationStatus(self, bitMask):
        for i in range(0, 4):
            receiving = bool(bitMask & (1 << i))
            history_value = 1.0 if receiving else 0.0
            self.baseStationStatus[i] = receiving
            self.queueMap['baseStationHistory'][i].append(history_value)

    def getAverage(self, mapKey):
        return np.mean(self.queueMap[mapKey])

    def clearBuffer(self, mapKey):
        self.queueMap[mapKey].clear()

    def getBaseStationAverage(self, index):
        return np.mean(self.queueMap['baseStationHistory'][index])

    def dump(self):
        print("varianceX: ", self.getAverage('varianceX'))
        print("varianceY: ", self.getAverage('varianceY'))
        print("positionX: ", self.getAverage('positionX'))
        print("positionY: ", self.getAverage('positionY'))
        print("positionZ: ", self.getAverage('positionZ'))
        print("yaw: ", self.getAverage('yaw'))

    @property
    def geometry(self):
        geos = {}
        for id in self.geometryBuffers:
            buffer = self.geometryBuffers[id]
            if (buffer.size <= 0):
                continue

            geo = LighthouseBsGeometry()
            geo.rotation_matrix = buffer.rotation
            geo.origin = buffer.position
            geo.valid = True
            geos[id] = geo

            if (np.mean(geo.origin) < 0.01):
                print("\n\n\n")
                geo.dump()
                raise Exception("Invalid position!!")

        return geos

    def findOrigin(self):
        print("\n\nFinding origin position..")
        estimatedPoses = self.findBaseStationPoses()

        geos = {}
        for id in estimatedPoses:
            geo = LighthouseBsGeometry()
            position, rotation = estimatedPoses[id]
            geo.rotation_matrix = rotation
            geo.origin = position
            geo.valid = True
            geos[id] = geo

        self.writeGeometry(geos)
        self.getDronePose()

    def takeSample(self):
        print("\n\nPosition sample started")

        self.readSavedGeometry()
        dronePose = self.getDronePose()
        estimatedPoses = self.findBaseStationPoses()
        correctedPoses = self.correctPoses(estimatedPoses, dronePose)
        self.writeNewGeometry(correctedPoses)
        self.getDronePose()


    def writeNewGeometry(self, correctedPoses):
        print("Writing new geometry to crazyflie...")
        for id in correctedPoses:
            if id not in self.geometryBuffers:
                self.geometryBuffers[id] = GeometryBuffer(Calibrator.ROLLING_QUEUE_SIZE)

            buffer = self.geometryBuffers[id]
            position, rotation = correctedPoses[id]
            buffer.add(position, rotation)

        geometries = self.geometry
        self.writeGeometry(geometries)


    def writeGeometry(self, geometries):
        numBasestations = len(geometries)
        helper = LighthouseMemHelper(self.cf)
        writer = LighthouseConfigWriter(self.cf, nr_of_base_stations=numBasestations)
        self.write(lambda: helper.write_geos(geometries, self.writeComplete))
        self.write(lambda: writer.write_and_store_config(self.writeComplete, geos=geometries))
        print("Write finished.")

    def write(self, writeOperation):
        self.writeSuccess = False
        self.writeEvent = Event()
        writeOperation()

        self.writeEvent.wait()
        if not self.writeSuccess:
            raise Exception("Write failed!")

    def writeComplete(self, *args):
        self.writeSuccess = True
        if self.writeEvent:
            self.writeEvent.set()


    def correctPoses(self, estimatedPoses, dronePose):
        print("Applying position correction...")
        x, y, z, yaw = dronePose
        dronePosition = np.array([x, y, z])
        correctedPoses = {}

        for id in estimatedPoses:
            print("\tapplying correction for base station", str(id) + "...")
            position, rotationMatrix = estimatedPoses[id]
            correctionMatrix = MathHelper.getYawMatrix(yaw)
            correctedPosition = np.add(np.matmul(correctionMatrix, position), dronePosition)
            correctedRotation = np.matmul(correctionMatrix, rotationMatrix)

            positionError, rotationError = self.calculatePoseError(id, correctedPosition, correctedRotation)
            print("\tERROR STATS[", id, "] | Position error: {:.1f}%, rotation error: {:.1f}%".format((positionError * 100), (rotationError * 100)))
            if positionError > Calibrator.ERROR_THRESHOLD or rotationError > Calibrator.ERROR_THRESHOLD:
                print("\tskipping data for base station", id, "due to high error margin.", file=sys.stderr)
                continue

            correctedPoses[id] = (correctedPosition, correctedRotation)

        print("Correction step complete")
        return correctedPoses

    def calculatePoseError(self, id, correctedPosition, correctedRotation):
        if id not in self.savedGeometry:
            return 0.0

        geo = self.savedGeometry[id]
        savedPosition = geo.origin
        savedRotation = geo.rotation_matrix

        positionErrorAbsolute = np.subtract(correctedPosition, savedPosition)
        rotationErrorAbsolute = np.subtract(correctedRotation, savedRotation)
        positionError = np.absolute(np.divide(positionErrorAbsolute, savedPosition))
        rotationError = np.absolute(np.divide(rotationErrorAbsolute, savedRotation))

        return np.mean(positionError), np.mean(rotationError)

    def getDronePose(self):
        print("Finding current drone position...")
        self.setPositioningMethod()
        time.sleep(0.25)
        self.cf.param.set_value('kalman.resetEstimation', '1')
        time.sleep(0.25)
        self.cf.param.set_value('kalman.resetEstimation', '0')

        self.clearBuffer('varianceX')
        self.clearBuffer('varianceY')
        self.clearBuffer('positionX')
        self.clearBuffer('positionY')
        self.clearBuffer('positionZ')
        time.sleep(0.25)
        print("\twaiting for sensor to converge...")
        while not self.hasValidPosition():
            time.sleep(0.25)

        sampleCount = 10
        buffer = []

        for i in range(0, sampleCount):
            print("\ttaking position sample...")
            x = self.getAverage('positionX')
            y = self.getAverage('positionY')
            z = self.getAverage('positionZ')
            yaw = self.getAverage('yaw')
            buffer.append((x, y, z, yaw))
            time.sleep(0.25)

        finalPosition = np.mean(buffer, axis=0)
        x, y, z, yaw = tuple(finalPosition)
        print('Position found: \t\t\t({:.2f}, {:.2f}, {:.2f}, {:.2f})'.format(x, y, z, yaw))
        return x, y, z, yaw

    def setPositioningMethod(self):
        # data = [self.queueMap['baseStationHistory'][i] for i in range(0, 4)]
        # print("\n\n\nBASE STATION HISTORY:\n\n", data, "\n\n\n")

        averages = [self.getBaseStationAverage(i) for i in range(0, 4)]
        validBaseStations = list(filter(lambda avg: avg >= 0.5, averages))
        self.cf.param.set_value('lighthouse.systemType', '2')
        self.cf.param.set_value('lighthouse.method', '1')
        # if len(validBaseStations) > 1:
        #     print("\tusing crossing beam method...")
        #     self.cf.param.set_value('lighthouse.method', '0')
        # else:
        #     print("\tusing sweep method...")
        #     self.cf.param.set_value('lighthouse.method', '1')

    def hasValidPosition(self):
        varianceX = self.getAverage('varianceX')
        varianceY = self.getAverage('varianceY')
        return (varianceX < Calibrator.VARIANCE_THRESHOLD) and (varianceY < Calibrator.VARIANCE_THRESHOLD)

    def findBaseStationPoses(self):
        sampleSize = 5
        buffers = [
            GeometryBuffer(sampleSize),
            GeometryBuffer(sampleSize),
            GeometryBuffer(sampleSize),
            GeometryBuffer(sampleSize)
        ]

        for i in range(0, sampleSize):
            geos = self.estimateBaseStationPoses()

            for id in geos:
                position, rotation = geos[id]
                buffers[id].add(position, rotation)

        finalGeometry = {}
        for i in range(0, 4):
            buffer = buffers[i]
            if (buffer.size <= 0):
                continue

            finalGeometry[i] = (buffer.position, buffer.rotation)

        return finalGeometry

    def estimateBaseStationPoses(self):
        self.collectAngles()
        if self.sweepAngles is None:
            return None

        geometries = {}
        estimator = LighthouseBsGeoEstimator()
        print("Finding base station positions...")

        for id in sorted(self.sweepAngles.keys()):

            print("\testimating position and rotation of base station", str(id) + "...")
            averagedAngles = self.sweepAngles[id][1]
            rotationMatrix, positionVector = estimator.estimate_geometry(averagedAngles)
            valid = estimator.sanity_check_result(positionVector)
            if valid:
                geometries[id] = (positionVector, rotationMatrix)

        print("Estimation step complete.")
        return geometries

    def collectAngles(self):
        print("Collecting data from base stations...")
        self.sweepAngles = None
        self.anglesEvent = Event()
        angleReader = LighthouseSweepAngleAverageReader(self.cf, self.onAnglesCollected)
        angleReader.start_angle_collection()
        self.anglesEvent.wait()

    def onAnglesCollected(self, angles):
        print("Base station data collected.")
        self.sweepAngles = angles
        self.anglesEvent.set()

    def readSavedGeometry(self):
        print("Reading geometry...")
        self.readEvent = Event()
        helper = LighthouseMemHelper(self.cf)
        helper.read_all_geos(self.onGeometryRead)
        self.readEvent.wait()

    def onGeometryRead(self, geometries):
        print("Saved geometry read.")
        self.savedGeometry = geometries
        self.readEvent.set()


class MathHelper:

    @staticmethod
    def getYawMatrix(yaw):
        return MathHelper.getRotationMatrix([0, 0, 1], np.radians(yaw))

    @staticmethod
    def getRotationMatrix(axis, theta):
        """"Return the rotation matrix associated with counterclockwise rotation about
            the given axis by theta radians."""
        axis = np.asarray(axis)
        axis = axis / math.sqrt(np.dot(axis, axis))
        a = math.cos(theta / 2.0)
        b, c, d = -axis * math.sin(theta / 2.0)
        aa, bb, cc, dd = a * a, b * b, c * c, d * d
        bc, ad, ac, ab, bd, cd = b * c, a * d, a * c, a * b, b * d, c * d
        return np.array([[aa + bb - cc - dd, 2 * (bc + ad), 2 * (bd - ac)],
                         [2 * (bc - ad), aa + cc - bb - dd, 2 * (cd + ab)],
                         [2 * (bd + ac), 2 * (cd - ab), aa + dd - bb - cc]])

"""
def get_saved_geo(cf):
    readEvent = Event()
    helper = LighthouseMemHelper(cf)
    helper.read_all_geos(lambda success: read_finished(readEvent, success))
    readEvent.wait()

    global geos
    return geos


def read_finished(event, geometries):
    global geos
    geos = geometries
    event.set()




def reset_estimator(cf):
    cf.param.set_value('kalman.resetEstimation', '1')
    time.sleep(0.25)
    cf.param.set_value('kalman.resetEstimation', '0')


def wait_for_estimator(cf):
    log_config = LogConfig(name='Kalman Variance & Position', period_in_ms=100)
    log_config.add_variable('kalman.varPX', 'float')
    log_config.add_variable('kalman.varPY', 'float')
    log_config.add_variable('kalman.varPZ', 'float')

    var_y_history = [1000] * 10
    var_x_history = [1000] * 10
    var_z_history = [1000] * 10
    threshold = 0.001

    with SyncLogger(cf, log_config) as logger:
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

            has_converged = (max_x - min_x) < threshold and (max_y - min_y) < threshold and (
                        max_z - min_z) < threshold

            if has_converged:
                break


def get_pose(cf):
    sample_count = 10.0
    reset_estimator(cf)
    wait_for_estimator(cf)

    log_config = LogConfig(name='Kalman Variance & Position', period_in_ms=50)
    log_config.add_variable('stateEstimate.x', 'float')
    log_config.add_variable('stateEstimate.y', 'float')
    log_config.add_variable('stateEstimate.z', 'float')
    log_config.add_variable('stateEstimate.yaw', 'float')

    data_history = [[], [], [], []]
    count = 0

    with SyncLogger(cf, log_config) as logger:
        for log_entry in logger:

            data = log_entry[1]

            data_history[0].append(data['stateEstimate.x'])
            data_history[1].append(data['stateEstimate.y'])
            data_history[2].append(data['stateEstimate.z'])
            data_history[3].append(data['stateEstimate.yaw'])

            count += 1
            if (count >= sample_count):
                break

    final_data = [
        sum(data_history[0]) / sample_count,
        sum(data_history[1]) / sample_count,
        sum(data_history[2]) / sample_count,
        sum(data_history[3]) / sample_count
    ]
    return tuple(final_data)

def stream_pose(cf):
    log_config = LogConfig(name='Kalman Variance & Position', period_in_ms=10)
    log_config.add_variable('stateEstimate.x', 'float')
    log_config.add_variable('stateEstimate.y', 'float')
    log_config.add_variable('stateEstimate.z', 'float')
    log_config.add_variable('stateEstimate.yaw', 'float')

    with SyncLogger(cf, log_config) as logger:
        for log_entry in logger:

            data = log_entry[1]

            x = data['stateEstimate.x']
            y = data['stateEstimate.y']
            z = data['stateEstimate.z']
            yaw = data['stateEstimate.yaw']
            # print('\rPose: ({:.2f}, {:.2f}, {:.2f}, {:.2f})'.format(x, y, z, yaw), end="")

            vector = np.array([1, 0, 0])
            matrix = get_yaw_matrix(yaw)
            multiplied = np.dot(matrix, vector)

            message1 = 'Un-normalized: ({:.4f}, {:.4f}, {:.4f}) | '.format(multiplied[0], multiplied[1], multiplied[2])
            message2 = 'Yaw: {:.4f} '.format(yaw)
            print('\r', message1, message2, end="")



"""