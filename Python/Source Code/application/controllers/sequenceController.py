from pathlib import Path
import json
import os

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

class SequenceController:

    sequences = []
    currentSequence = None

    def __init__(self, appController, settings):
        self.appController = appController
        self.settings = settings
        self.loadSequences()

    def loadSequences(self):
        for sequenceFile in reversed(self.settings.sequences):
            self.loadSequence(sequenceFile)

    def findExisting(self, desiredMatch):
        for index, sequence in enumerate(self.sequences):
            if sequence.fullPath == desiredMatch.fullPath:
                return index
        
        return None

    def loadSequence(self, file):
        path = Path(file)
        location = str(path.parent)
        name = os.path.splitext(path.name)[0]
        with open(file, 'r') as jsonFile:
            sequenceData = json.load(jsonFile)
            try:
                sequence = Sequence(name, location, sequenceData)
                existingIndex = self.findExisting(sequence)
                if not existingIndex:
                    self.sequences.insert(0, sequence)
                else:
                    self.sequences.insert(0, self.sequences.pop(existingIndex))

                del self.sequences[10:] # Trim to 10 items or less
                return True
            except ValueError:
                return False
        return False

        

    