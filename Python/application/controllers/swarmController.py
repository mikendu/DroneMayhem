import time
import sys
import math

from concurrent.futures import ThreadPoolExecutor, wait

from cflib.crazyflie.broadcaster import Broadcaster
from cflib.crtp.cflinkcppdriver import CfLinkCppDriver

from application.model import Drone, DroneState
from application.common import SettingsKey, AppSettings
from application.common.exceptions import DroneException
from application.constants import Constants
from application.util import Logger


class SwarmController():
    
    def __init__(self, appController, appSettings):
        self.appController = appController
        self.appSettings = appSettings
        self.allDrones = []
        self.connectedDrones = []
        self.droneMapping = {}
        self.broadcasters = []

    def broadcast(self, operation):
        for broadcaster in self.broadcasters:
            operation(broadcaster)
            operation(broadcaster)

    def scan(self):
        self.removeDisconnected()
        uris = []

        # Scan a few times to help pick up any and all drones
        uris.extend(CfLinkCppDriver.scan_selected(self.uriPool))
        uris.extend(CfLinkCppDriver.scan_selected(self.uriPool))
        uris.extend(CfLinkCppDriver.scan_selected(self.uriPool))

        for uri in uris:
            if uri in self.droneMapping:
                continue

            drone = Drone()
            drone.swarmIndex = len(self.allDrones)
            drone.address = uri
            self.allDrones.append(drone)
            self.droneMapping[uri] = drone

    def connectSwarm(self, numDrones = None):
        toConnect = []
        if numDrones and (isinstance(numDrones, int) or numDrones.is_integer()):
            totalCount = len(self.allDrones)
            desiredCount = totalCount if numDrones < 0 else numDrones
            end = max(0, min(desiredCount, totalCount))
            toConnect = self.allDrones[:end]
            toConnect = list(filter(lambda drone: drone.enabled, toConnect))

        Logger.log("Attempting to open " + str(len(toConnect)) + " drone connections")
        for channel in self.channels:
            broadcaster = Broadcaster(channel)
            broadcaster.open_link()
            self.broadcasters.append(broadcaster)

        self.connectedDrones = self.parallel(self.connectToDrone, toConnect)
        Logger.log("Successfully opened " + str(len(self.connectedDrones)) + " drone connections")

    def connectToDrone(self, drone):
        try:
            address = drone.address + "?safelink=1&autoping=1"
            drone.initialize(address, self.onDisconnect)
            return drone
        except Exception as e:
            if "Too many packets lost" in str(e):
                drone.state = DroneState.DISCONNECTED
                raise ConnectionAbortedError()
            else:
                raise

    def onDisconnect(self, uri, errorMessage):
        Logger.error(errorMessage)
        if uri in self.droneMapping:
            drone = self.droneMapping[uri]
            drone.state = DroneState.DISCONNECTED
            self.appController.droneDisconnected.emit(uri)
            self.appController.sequenceUpdated.emit()

    def disconnectSwarm(self):
        self.broadcast(lambda broadcaster: broadcaster.high_level_commander.stop())
        time.sleep(0.25)

        self.broadcast(lambda broadcaster: broadcaster.close_link())
        self.broadcasters = []

        for drone in self.connectedDrones:
            drone.crazyflie.cf.high_level_commander.stop()
            drone.crazyflie.cf.close_link()
            drone.state = DroneState.DISCONNECTED

    def removeDisconnected(self):
        filtered = []
        for drone in self.allDrones:
            if drone.state == DroneState.DISCONNECTED:
                del self.droneMapping[drone.address]
            else:
                drone.swarmIndex = len(filtered)
                filtered.append(drone)
        self.allDrones = filtered

    def initializeSensors(self, uploadGeometry=True):
        appSettings = self.appController.appSettings
        geo_data = appSettings.getValue(SettingsKey.GEO_DATA)
        calib_data = appSettings.getValue(SettingsKey.CALIB_DATA)
        numBaseStations = Constants.BASE_STATION_COUNT
        self.parallel(lambda drone: drone.updateSensors(uploadGeometry, geo_data, calib_data, numBaseStations))

    def parallel(self, function, droneCollection=None):
        """
        Call the given function in parallel for each drone in the swarm
        :param function: A function taking a single Drone instance
        :param droneCollection: The collection of drones to call on. Defaults to the connected drones from the swarm
        :return:
        """
        drones = droneCollection if droneCollection is not None else self.connectedDrones
        with ThreadPoolExecutor() as executor:
            futures = [executor.submit(function, drone) for drone in drones]
            wait(futures) # Await all
            results = [future.result() for future in futures]
            results = list(filter(None, results))

        return results

    def takeoffTest(self, sequential=False):
        mode = "(sequentially)" if sequential else "(in parallel)"
        Logger.log("Running takeoff test " + mode)

        try:
            self.connectSwarm(-1)
            self.initializeSensors(True)

            if (sequential):
                for drone in self.connectedDrones:
                    self.runTakeoffTest(drone)
            else:
                Logger.log("Taking off")
                self.broadcast(lambda broadcaster: broadcaster.light_controller.set_color(0, 0, 0, 0.1, True))
                self.broadcast(lambda broadcaster: broadcaster.high_level_commander.takeoff(Constants.MIN_HEIGHT, 1.5))

                time.sleep(1.25)
                results = self.parallel(self.verifyTakeoff)

                self.broadcast(lambda broadcaster: broadcaster.high_level_commander.go_to(x=0.0, y=0.5, z=0.0, yaw=math.radians(180), duration_s=2.0, relative=True))
                time.sleep(2.0)
                self.broadcast(lambda broadcaster: broadcaster.light_controller.set_color(255, 255, 255, 0.0, False))
                time.sleep(0.25)
                self.broadcast(lambda broadcaster: broadcaster.light_controller.set_color(0, 0, 0, 0.0, False))

                self.broadcast(lambda broadcaster: broadcaster.high_level_commander.go_to(x=0.0, y=-1.0, z=0.0, yaw=math.radians(-360), duration_s=3.0, relative=True))
                time.sleep(3.0)
                self.broadcast(lambda broadcaster: broadcaster.light_controller.set_color(255, 255, 255, 0.0, False))
                time.sleep(0.25)
                self.broadcast(lambda broadcaster: broadcaster.light_controller.set_color(0, 0, 0, 0.0, False))

                self.broadcast(lambda broadcaster: broadcaster.high_level_commander.go_to(x=0.0, y=0.5, z=0.0, yaw=math.radians(0), duration_s=2.0, relative=True))
                time.sleep(2.0)
                self.broadcast(lambda broadcaster: broadcaster.light_controller.set_color(255, 255, 255, 0.0, False))
                time.sleep(0.25)
                self.broadcast(lambda broadcaster: broadcaster.light_controller.set_color(0, 0, 0, 0.0, False))

                Logger.log("Landing drones...")

                self.broadcast(lambda broadcaster: broadcaster.high_level_commander.land(Constants.LANDING_HEIGHT, 2.0))
                time.sleep(2.0)

                self.broadcast(lambda broadcaster: broadcaster.light_controller.set_color(0, 0, 0, 0.25, True))
                self.broadcast(lambda broadcaster: broadcaster.high_level_commander.stop())
                time.sleep(0.25)

                noFailure = all(results)
                if noFailure:
                    Logger.success("Takeoff test successful!")

        except ConnectionAbortedError as e:
            Logger.error("Connection error!")

        except Exception as e:
            Logger.error("Unknown error occurred!")
            print("Error: ", e, file=sys.stderr)

        self.disconnectSwarm()

        Logger.log("Takeoff test complete")
        self.appController.takeoffTestComplete.emit()

    def verifyTakeoff(self, drone):
        try:
            tx, ty, tz = drone.currentPosition
            drone.waitForTargetPosition(tx, ty, Constants.MIN_HEIGHT, 0.5, 5.0)
            return True
        except DroneException as e:
            Logger.error("Takeoff test failed!", drone.swarmIndex)
            return False


    def runTakeoffTest(self, drone):
        Logger.log("Taking off", drone.swarmIndex)

        commander = drone.commander
        light_controller = drone.light_controller

        light_controller.set_color(0, 0, 0, 0.1, True)
        commander.takeoff(Constants.MIN_HEIGHT, 1.5)
        success = self.verifyTakeoff(drone)

        commander.go_to(x=0.0, y=0.5, z=0.0, yaw=math.radians(180), duration_s=2.0, relative=True)
        time.sleep(2.0)
        light_controller.set_color(255, 255, 255, 0.0, False)
        time.sleep(0.25)
        light_controller.set_color(0, 0, 0, 0.0, False)

        commander.go_to(x=0.0, y=-1.0, z=0.0, yaw=math.radians(-360), duration_s=3.0, relative=True)
        time.sleep(3.0)
        light_controller.set_color(255, 255, 255, 0.0, False)
        time.sleep(0.25)
        light_controller.set_color(0, 0, 0, 0.0, False)

        commander.go_to(x=0.0, y=0.5, z=0.0, yaw=math.radians(180), duration_s=2.0, relative=True)
        time.sleep(2.0)
        light_controller.set_color(255, 255, 255, 0.0, False)
        time.sleep(0.25)
        light_controller.set_color(0, 0, 0, 0.0, False)

        Logger.log("Landing...", drone.swarmIndex)
        commander.land(Constants.LANDING_HEIGHT, 2.0)
        time.sleep(2.0)

        light_controller.set_color(0, 0, 0, 0.25, True)
        commander.stop()
        time.sleep(0.25)

        if success:
            Logger.success("Takeoff test successful!", drone.swarmIndex)


    @property
    def uriPool(self):
        min, max = self.addresses
        addressMin = int(min, 16)
        addressMax = int(max, 16)
        channels = self.channels

        uris = []

        for channel in channels:
            for i in range(addressMin, addressMax + 1):
                uri = "radio://*/" + str(channel) + "/2M/" + format(i, 'X')
                uris.append(uri)
        return uris

    @property
    def channels(self):
        return [int(channel) for channel in self.appSettings.getValue(SettingsKey.RADIO_CHANNELS)]

    @property
    def addresses(self):
        [min, max] = self.appSettings.getValue(SettingsKey.RADIO_ADDRESSES, AppSettings.DEFAULT_ADDRESS_RANGE)
        return min, max

    @property
    def availableDrones(self):
        return [drone for drone in self.allDrones if drone.enabled]

    def setDronesEnabled(self, enabled):
        for drone in self.allDrones:
            drone.enabled = enabled