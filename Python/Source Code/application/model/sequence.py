import os
from .droneActionType import DroneActionType



class Sequence():

    def __init__(self, name, location, data):
        self.name = name
        self.location = location
        self.sequenceData = data

        if not "Sequences" in data or not "Tracks" in data or not "TotalSeconds" in data:
            raise ValueError("Could not parse sequence data")

    @property
    def drones(self):
        return self.sequenceData["Tracks"]

    @property
    def duration(self):
        return self.sequenceData["TotalSeconds"]

    @property
    def fullPath(self):
        return os.path.join(self.location, self.name + ".json")

    def getTrack(self, swarmIndex):
        allTracks = self.sequenceData["Sequences"]
        trackCount = len(allTracks)
        if swarmIndex >= 0 and swarmIndex < trackCount:
            return allTracks[swarmIndex]

        raise Exception("Swarm index " + str(swarmIndex) + " is out of range! Found " + str(trackCount) + " tracks in the sequence.")

    def getInitialState(self, swarmIndex, desiredActionType):
        track = self.getTrack(swarmIndex)
        for keyframe in track['Keyframes']:
            for action in keyframe['Actions']:
                actionType = DroneActionType(action['ActionType'])
                if (actionType == desiredActionType):
                    data = action['Data']
                    x, y, z = [data[key] for key in ('x', 'y','z')]
                    return x, y, z

        raise Exception("Initial state not found for action type: " + str(desiredActionType))