from munkres import Munkres
from application.util.vectorMath import distance
from application.constants import Constants

class DroneMatcher:

    @staticmethod
    def assign(drones, positions):
        solver = Munkres()
        costMatrix = [DroneMatcher.getCostArray(drone, positions) for drone in drones]

        if (len(costMatrix) != len((costMatrix[0]))):
            raise ValueError("Number of tracks and drones must be equal to do matching!")

        result = solver.compute(costMatrix)
        for assignment in result:
            droneIndex, positionIndex = assignment
            drones[droneIndex].trackIndex = positionIndex
            drones[droneIndex].targetPosition = positions[positionIndex]

    @staticmethod
    def getCostArray(drone, positions):
        x, y, z = drone.currentPosition
        takeoffPosition = (x, y, Constants.MIN_HEIGHT)
        return [distance(takeoffPosition, position) for position in positions]