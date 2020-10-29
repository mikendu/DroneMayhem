from enum import Enum
import random


from model import *
    

class SwarmController():

    drones = []
    
    def __init__(self, appController):
        self.appController = appController

    def scan(self):
        # TODO - Deduplication
        self.drones = []
        droneCount = random.randint(20, 50)
        for i in range(0, droneCount):
            drone = Drone()
            drone.state = DroneState(random.randint(0, 5))
            drone.swarmIndex = i
            drone.address = "0xDEADBEEF"
            self.drones.append(drone)
