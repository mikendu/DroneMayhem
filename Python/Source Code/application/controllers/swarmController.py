import time
from cflib.crtp.radiodriver import RadioDriver
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
        self.radioDriver = RadioDriver()
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

    def connectSwarm(self, numDrones = None):
        uris = self.uris
        if numDrones and (isinstance(numDrones, int) or numDrones.is_integer()):
            listLength = len(self.uris)
            end = max(0, min(numDrones, listLength))
            uris = uris[:end]

        self.swarm = Swarm(uris, factory = SwarmController.FACTORY)            
        self.swarm.open_links()
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



def initializePositioning(syncCrazyflie, drone, appController):
    crazyflie = syncCrazyflie.cf
    crazyflie.param.set_value('lighthouse.method', '1')
    crazyflie.param.set_value('stabilizer.controller', '2') # Mellinger controller
    crazyflie.param.set_value('commander.enHighLevel', '1')    
    crazyflie.param.set_value('ring.effect', '14')
    exceptionUtil.checkInterrupt()

    appController.baseStationController.writeBaseStationData(crazyflie, drone)
    resetEstimator(syncCrazyflie)
    drone.state = DroneState.IDLE
    exceptionUtil.checkInterrupt()
    

def resetEstimator(syncCrazyflie):
    
    crazyflie = syncCrazyflie.cf
    crazyflie.param.set_value('kalman.resetEstimation', '1')
    time.sleep(0.1)
    
    exceptionUtil.checkInterrupt()
    crazyflie.param.set_value('kalman.resetEstimation', '0')
    waitForEstimator(crazyflie)

def waitForEstimator(syncCrazyflie):
    log_config = LogConfig(name='Kalman Variance', period_in_ms=500)
    log_config.add_variable('kalman.varPX', 'float')
    log_config.add_variable('kalman.varPY', 'float')
    log_config.add_variable('kalman.varPZ', 'float')

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

            
            if (max_x - min_x) < threshold and (max_y - min_y) < threshold and (max_z - min_z) < threshold:
                break