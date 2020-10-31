import time
import cflib.crtp
from cflib.crtp.radiodriver import RadioDriver

from model import *    

class SwarmController():

    RADIO_ADDRESS = 0xE7E7E7E7E7
    
    def __init__(self, appController):
        self.appController = appController
        self.radioDriver = RadioDriver()
        self.drones = []
        self.droneMapping = {}

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

    def removeDisconnected(self):
        filtered = []
        for drone in self.drones:
            if drone.state == DroneState.DISCONNECTED:
                del self.droneMapping[drone.address]
            else:
                drone.swarmIndex = len(filtered)
                filtered.append (drone)

        self.drones = filtered


    

