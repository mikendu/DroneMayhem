import os


class Sequence():
    EMPTY = None

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
    def displayedDroneCount(self):
        return max(self.sequenceData["DroneCount"], 0)

    @property
    def duration(self):
        return float(self.sequenceData["Length"])

    @property
    def fullPath(self):
        return os.path.join(self.location, self.name + ".json")

    def getTrack(self, swarmIndex):
        allTracks = self.sequenceData["Tracks"]
        trackCount = len(allTracks)
        if swarmIndex >= 0 and swarmIndex < trackCount:
            return allTracks[swarmIndex]

        return None

    def getStartingColor(self, swarmIndex):
        track = self.getTrack(swarmIndex)
        if track is None:
            return None

        if 'StartColor' in track:
            color = track['StartColor']
            r, g, b = [color[key] for key in ('r', 'g', 'b')]
            return int(r), int(g), int(b)
        else:
            return 0, 0, 0

    def getStartingPosition(self, swarmIndex):
        track = self.getTrack(swarmIndex)
        if track is None:
            return None

        startPosition = track['StartPosition']
        x, y, z = [startPosition[key] for key in ('x', 'y', 'z')]
        return float(x), float(y), float(z)


# -- Special Static Instance -- #
Sequence.EMPTY = Sequence("Takeoff & Landing Test", "", {"DroneCount" : -1, "Length" : 0, "Tracks": []})
