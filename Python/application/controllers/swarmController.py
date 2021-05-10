from threading import Thread
from cflib.crazyflie.broadcaster import Broadcaster
from cflib.crtp.cflinkcppdriver import CfLinkCppDriver
from application.model import Drone, DroneState
from application.common import SettingsKey, AppSettings


class SwarmController():
    
    def __init__(self, appController, appSettings):
        self.appController = appController
        self.appSettings = appSettings
        self.availableDrones = []
        self.connectedDrones = []
        self.droneMapping = {}
        self.broadcaster = None

    def scan(self):
        self.removeDisconnected()
        uris = CfLinkCppDriver.scan_selected(self.uriPool)

        for uri in uris:
            if uri in self.droneMapping:
                continue

            drone = Drone()
            drone.swarmIndex = len(self.availableDrones)
            drone.address = uri
            self.availableDrones.append(drone)
            self.droneMapping[uri] = drone

    def connectSwarm(self, numDrones = None):
        toConnect = []
        if numDrones and (isinstance(numDrones, int) or numDrones.is_integer()):
            totalCount = len(self.availableDrones)
            desiredCount = totalCount if numDrones < 0 else numDrones
            end = max(0, min(desiredCount, totalCount))
            toConnect = self.availableDrones[:end]

        self.broadcaster = Broadcaster(self.channel)
        self.broadcaster.open_link()
        self.connectedDrones = self.parallel(self.connectToDrone, toConnect)

    def connectToDrone(self, drone):
        try:
            address = drone.address + "?safelink=1&autoping=1"
            drone.initialize(address, self.onDisconnect)
        except Exception as e:
            if "Too many packets lost" in str(e):
                drone.state = DroneState.DISCONNECTED
                raise ConnectionAbortedError()
            else:
                raise

    def onDisconnect(self, uri, errorMessage):
        print(errorMessage)
        if uri in self.droneMapping:
            drone = self.droneMapping[uri]
            drone.state = DroneState.DISCONNECTED
            self.appController.droneDisconnected.emit(uri)
            self.appController.sequenceUpdated.emit()

    def disconnectSwarm(self):
        if self.broadcaster:
            self.broadcaster.close_link()
            self.broadcaster = None

        for drone in self.connectedDrones:
            drone.crazyflie.cf.high_level_commander.stop()
            drone.crazyflie.cf.close_link()
            drone.state = DroneState.DISCONNECTED

    def removeDisconnected(self):
        filtered = []
        for drone in self.availableDrones:
            if drone.state == DroneState.DISCONNECTED:
                del self.droneMapping[drone.address]
            else:
                drone.swarmIndex = len(filtered)
                filtered.append(drone)
        self.availableDrones = filtered

    def initializeSensors(self):
        geometryOne = self.appController.baseStationController.geometryOne
        geometryTwo = self.appController.baseStationController.geometryTwo
        self.parallel(lambda drone: drone.updateSensors(geometryOne, geometryTwo))

    def parallel(self, function, droneCollection=None):
        """
        Call the given function in parallel for each drone in the swarm
        :param function: A function taking a single Drone instance
        :param droneCollection: The collection of drones to call on. Defaults to the connected drones from the swarm
        :return:
        """
        drones = droneCollection if droneCollection is not None else self.connectedDrones
        threads = []
        for drone in drones:
            args = [drone]
            thread = Thread(target=function, args=args)
            threads.append(thread)
            thread.start()

        for thread in threads:
            thread.join()

        return drones


    @property
    def uriPool(self):
        min, max = self.addresses
        addressMin = int(min, 16)
        addressMax = int(max, 16)
        uris = []
        for i in range(addressMin, addressMax + 1):
            uri = "radio://*/" + str(self.channel) + "/2M/" + format(i, 'X')
            uris.append(uri)
        return uris

    @property
    def channel(self):
        return int(self.appSettings.getValue(SettingsKey.RADIO_CHANNEL))

    @property
    def addresses(self):
        [min, max] = self.appSettings.getValue(SettingsKey.RADIO_ADDRESSES, AppSettings.DEFAULT_ADDRESS_RANGE)
        return min, max