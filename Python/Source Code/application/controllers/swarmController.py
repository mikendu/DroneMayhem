import time
from cflib.crtp.cflinkcppdriver import CfLinkCppDriver
from cflib.crazyflie.swarm import CachedCfFactory
from cflib.crazyflie.swarm import Swarm
from cflib.crazyflie.log import LogConfig
from cflib.crazyflie.syncLogger import SyncLogger

from application.model import Drone, DroneState
from application.util import exceptionUtil


class SwarmController():

    RADIO_ADDRESS = 0xE7E7E7E7E7
    FACTORY = CachedCfFactory(rw_cache = './cache')
    
    def __init__(self, appController):
        self.appController = appController
        self.radioDriver = CfLinkCppDriver()
        self.drones = []
        self.droneMapping = {}
        self.swarmArguments = {}
        self.swarm = None

    def scan(self):
        self.removeDisconnected()

        # crazyflies = cflib.crtp.scan_interfaces(self.RADIO_ADDRESS)
        crazyflies = self.radioDriver.scan_interface(self.RADIO_ADDRESS)
        for crazyflie in crazyflies: 
            droneAddress =  crazyflie[0] + "/" + format(self.RADIO_ADDRESS, 'X') 
            if droneAddress in self.droneMapping:
                continue

            drone = Drone()
            drone.swarmIndex = len(self.drones)
            drone.address = droneAddress
            self.drones.append(drone)
            self.droneMapping[droneAddress] = drone
            self.swarmArguments[droneAddress] = [drone, self.appController]

        self.uris = [drone.address for drone in self.drones]
        time.sleep(1.0)

    def connectSwarm(self, numDrones = None):
        uris = self.uris
        if numDrones and (isinstance(numDrones, int) or numDrones.is_integer()):
            totalCount = len(self.uris)
            desiredCount = totalCount if numDrones < 0 else numDrones
            end = max(0, min(desiredCount, totalCount))
            uris = uris[:end]

        self.swarm = Swarm(uris, factory=SwarmController.FACTORY)
        self.swarm.parallel_safe(connectToDrone, self.swarmArguments)
        #self.swarm.open_links()

        for uri in self.swarm._cfs.keys():
            drone = self.droneMapping[uri]
            crazyflie = self.swarm._cfs[uri]
            drone.crazyflie = crazyflie
            drone.state = DroneState.INITIALIZING
            crazyflie.cf.connection_lost.add_callback(lambda uri, error: self.onDisconnect(uri, error))

    def onDisconnect(self, uri, errorMessage):
        print(errorMessage)
        drone = self.droneMapping[uri]
        drone.state = DroneState.DISCONNECTED
        self.appController.droneDisconnected.emit(uri)
        self.appController.sequenceUpdated.emit()

        
    def initializeSensors(self):
        self.swarm.parallel(initializePositioning, self.swarmArguments)

    def disconnectSwarm(self):
        if (self.swarm):
            for uri in self.swarm._cfs:
                if not uri in self.droneMapping:
                    continue

                drone = self.droneMapping[uri]
                drone.crazyflie.cf.high_level_commander.stop()
                drone.state = DroneState.DISCONNECTED

            self.swarm.close_links()
            self.swarm = None

    def removeDisconnected(self):
        filtered = []
        for drone in self.drones:
            if drone.state == DroneState.DISCONNECTED:
                del self.droneMapping[drone.address]
                del self.swarmArguments[drone.address]
            else:
                drone.swarmIndex = len(filtered)
                filtered.append (drone)

        self.drones = filtered


def connectToDrone(syncCrazyflie, drone, appController):
    try:
        syncCrazyflie.open_link()
    except Exception as e:
        if "Too many packets lost" in str(e):
            drone.state = DroneState.DISCONNECTED
            raise ConnectionAbortedError()
        else:
            raise


def initializePositioning(syncCrazyflie, drone, appController):
    crazyflie = syncCrazyflie.cf
    crazyflie.param.set_value('lighthouse.method', '0')
    crazyflie.param.set_value('stabilizer.controller', '2') # Mellinger controller
    crazyflie.param.set_value('commander.enHighLevel', '1')    
    crazyflie.param.set_value('ring.effect', '14')
    exceptionUtil.checkInterrupt()

    appController.baseStationController.writeBaseStationData(crazyflie, drone)
    resetEstimator(drone, syncCrazyflie)
    drone.state = DroneState.IDLE
    exceptionUtil.checkInterrupt()
    

def resetEstimator(drone, syncCrazyflie):
    
    crazyflie = syncCrazyflie.cf
    crazyflie.param.set_value('kalman.resetEstimation', '1')
    time.sleep(0.1)
    
    exceptionUtil.checkInterrupt()
    crazyflie.param.set_value('kalman.resetEstimation', '0')
    drone.startHeight = 0.0
    waitForEstimator(drone, crazyflie)

def waitForEstimator(drone, syncCrazyflie):
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

    threshold = 0.001
    print("\n\n\n-- WAITING FOR POSITION -- ")
    with SyncLogger(syncCrazyflie, log_config) as logger:
        for log_entry in logger:
            
            exceptionUtil.checkInterrupt()
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

            
            if (max_x - min_x) < threshold and (max_y - min_y) < threshold and (max_z - min_z) < threshold:
                drone.startHeight = z
                break