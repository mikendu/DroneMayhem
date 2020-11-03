from pathlib import Path
import json
import os
import time
import traceback

from util.exceptionUtil import *
from util.threadUtil import *

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


    def __init__(self, appController, settings):
        self.appController = appController
        self.settings = settings

        self.sequences = []
        self.currentSequence = None
        self.sequencePlaying = False
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

    
    def selectSequence(self, index):
        if (index >= 0 and index < len(self.sequences)):
            self.currentSequence = self.sequences[index]
            # self.sequences.insert(0, self.sequences.pop(index))
            return True
        return False

    def run(self):
        try: 
            print("\n\n\n\n-------------- SEQUENCE STARTING --------------")
            # connect to required drones
            numDrones = self.currentSequence.drones
            self.appController.swarmController.connectSwarm(numDrones)
            self.appController.sequenceUpdated.emit()
                    
            # ensure all drones have updated light house info & know their positions
            self.appController.swarmController.initializeSensors()
            self.appController.sequenceUpdated.emit()

            # get all drones in position
            self.takeoffDrones()

            # run sequence for each drone
            self.appController.startTimer.emit()
            self.runSequence()

            # land all drones
            self.landDrones()
            self.appController.sequenceUpdated.emit()
            
            # Wrap up
            self.completeSequence()
        
        except SequenceInterrupt:
            print("-- Aborting Sequence --")
            self.abort()


    def completeSequence(self):
        self.appController.swarmController.disconnectSwarm()
        self.appController.onSequenceCompleted()
        self.appController.sequenceUpdated.emit()

    def abort(self):
        self.landDrones()
        self.completeSequence()

    
    def takeoffDrones(self):
        print("-- Getting Drones into Position --")  
        time.sleep(0.5)

    def runSequence(self):
        print("--- RUN SEQUENCE, BEFORE SLEEP ---")
        interruptibleSleep(20.0, 0.25)
        print("--- RUN SEQUENCE, AFTER SLEEP ---")

    def landDrones(self):
        # if drone state is one of POSITIONING, READY, IN_FLIGHT, or LANDING
        print("-- Landing Drones --")
        time.sleep(0.5)
    

        

