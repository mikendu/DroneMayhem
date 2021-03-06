import os
from .droneActionType import DroneActionType



class Sequence():

    def __init__(self, name, location, data):
        self.name = name
        self.location = location
        self.sequenceData = data

        if not "Tracks" in data or not "DroneCount" in data or not "Length" in data:
            raise ValueError("Could not parse sequence data")

    @property
    def drones(self):
        return self.sequenceData["DroneCount"]

    @property
    def duration(self):
        return self.sequenceData["Length"]

    @property
    def fullPath(self):
        return os.path.join(self.location, self.name + ".json")

    def getTrack(self, swarmIndex):
        allTracks = self.sequenceData["Tracks"]
        trackCount = len(allTracks)
        if swarmIndex >= 0 and swarmIndex < trackCount:
            return allTracks[swarmIndex]

        raise Exception("Swarm index " + str(swarmIndex) + " is out of range! Found " + str(trackCount) + " tracks in the sequence.")

    def getStartingColor(self, swarmIndex):
        track = self.getTrack(swarmIndex)
        keyframes = track['ColorKeyframes']
        if (len(keyframes) > 0):
            color = keyframes[0]['LightColor']
            r, g, b = [color[key] for key in ('r', 'g', 'b')]
            return int(r), int(g), int(b)
        else:
            return 0, 0, 0

    def getStartingPosition(self, swarmIndex):
        track = self.getTrack(swarmIndex)
        startPosition = track['StartPosition']
        x, y, z = [startPosition[key] for key in ('x', 'y', 'z')]
        return float(x), float(y), float(z)