from random import *


class Sequence():

    def __init__(self, name, duration, drones):
        self.name = name
        self.duration = duration
        self.drones = drones
        self.location = "C:/Users/Michael/AppData/LocalLow/Veserium/VeseriumTest/Test/Test2"

class SequenceController:

    sequences = []
    currentSequence = None

    def __init__(self):
        self.loadSequences()

    def loadSequences(self):
        for i in range(0, 2):
            sequence = Sequence("Sequence " + str(i + 1), randint(10, 100), randint(1, 10))
            self.sequences.append(sequence)

    