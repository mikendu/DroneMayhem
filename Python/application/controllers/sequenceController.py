import json
import os
import time
import pathlib

from cflib.crazyflie.light_controller import RingEffect

from application.model import Sequence, DroneState, Drone
from application.common.exceptions import SequenceInterrupt
from application.util import exceptionUtil, threadUtil, Logger


class SequenceController:

    CURRENT = None
    MIN_HEIGHT = 0.3
    MAX_HEIGHT = 3.0
    LANDING_HEIGHT = 0.05

    def __init__(self, appController, settings):
        self.appController = appController
        self.settings = settings

        self.sequences = []
        self.sequencePlaying = False
        self.loadSequences()
        self.sequenceIndex = None

    def loadSequences(self):
        for sequenceFile in reversed(self.settings.sequences):
            self.loadSequence(sequenceFile)

    def findExisting(self, desiredMatch):
        for index, sequence in enumerate(self.sequences):
            if sequence.fullPath == desiredMatch.fullPath or sequence.name == desiredMatch.name:
                return index
        
        return None

    def loadSequence(self, file):
        path = pathlib.Path(file)
        location = str(path.parent)
        name = os.path.splitext(path.name)[0]
        with open(file, 'r') as jsonFile:
            sequenceData = json.load(jsonFile)
            try:
                sequence = Sequence(name, location, sequenceData)
                existingIndex = self.findExisting(sequence)
                if existingIndex is None:
                    self.sequences.insert(0, sequence)
                else:
                    self.sequences.insert(0, self.sequences.pop(existingIndex))

                del self.sequences[10:] # Trim to 10 items or less
                return True
            except ValueError:
                return False
        return False

    def removeSequence(self, index):
        if (index >= 0 and index < len(self.sequences)):
            self.sequences.pop(index)
            return True
        return False

    def selectSequence(self, index):
        if (index >= 0 and index < len(self.sequences)):
            SequenceController.CURRENT = self.sequences[index]
            self.sequenceIndex = index
            return True
        return False

    def setTestSequence(self):
        SequenceController.CURRENT = Sequence.EMPTY
        self.sequenceIndex = None


    def run(self):

        # Update the order of the recent sequences
        if self.sequenceIndex:
            self.sequences.insert(0, self.sequences.pop(self.sequenceIndex))
            self.appController.sequenceOrderUpdated.emit()

        try:
            Logger.log("SEQUENCE STARTING")
            swarmController = self.appController.swarmController

            # connect to required drones
            numDrones = min(SequenceController.CURRENT.drones, self.appController.availableDrones)
            self.appController.swarmController.connectSwarm(numDrones)
            self.appController.sequenceUpdated.emit()
            threadUtil.interruptibleSleep(0.25)

            # ensure all drones have updated light house info & know their positions
            uploadLighthouseData = self.appController.trajectoryEnabled or self.appController.positioningEnabled
            swarmController.initializeSensors(uploadLighthouseData)
            self.appController.sequenceUpdated.emit()
            threadUtil.interruptibleSleep(0.25)

            # get all drones in position
            swarmController.parallel(self.uploadFlightData)
            self.synchronizedTakeoff()
            swarmController.parallel(self.setInitialPosition)
            threadUtil.interruptibleSleep(0.25)

            # kick off the actual sequence action
            self.runSequence()

            # land all drones
            self.landDrones()

            # Wrap up
            self.completeSequence()

        except SequenceInterrupt:
            Logger.log("ABORTING SEQUENCE")
            self.abort()
            return

        except ConnectionAbortedError:
            print("-- Drone connection failed, cancelling sequence --")
            self.appController.connectionFailed.emit()
            return

        # Re-throw all other exceptions
        except Exception:
            raise

    def uploadFlightData(self, drone):
        sequence = SequenceController.CURRENT
        track = sequence.getTrack(drone.swarmIndex)
        exceptionUtil.checkInterrupt()

        if track is not None:
            Logger.log("Uploading trajectory & LED data", drone.swarmIndex)

            if self.appController.trajectoryEnabled:
                trajectoryData = bytearray(track['CompressedTrajectory'])
                drone.writeTrajectory(trajectoryData)
                exceptionUtil.checkInterrupt()

            if self.appController.colorSequenceEnabled:
                lightData = bytearray(track['LedTimings'])
                drone.writeLedTimings(lightData)
                exceptionUtil.checkInterrupt()

        drone.disableAutoPing()

    def synchronizedTakeoff(self):
        if not (self.appController.trajectoryEnabled or self.appController.positioningEnabled):
            return

        Logger.log("Taking off")
        swarmController = self.appController.swarmController
        commander = swarmController.broadcaster.high_level_commander
        light_controller = swarmController.broadcaster.light_controller

        light_controller.set_color(0, 127, 255, 0.1, True)
        commander.takeoff(SequenceController.MIN_HEIGHT, 3.0)

        for drone in swarmController.connectedDrones:
            drone.state = DroneState.IN_FLIGHT

        self.appController.sequenceUpdated.emit()
        threadUtil.interruptibleSleep(3.0)


    def setInitialPosition(self, drone):
        exceptionUtil.checkInterrupt()

        sequence = SequenceController.CURRENT
        track = sequence.getTrack(drone.swarmIndex)
        commander = drone.commander
        self.appController.updateSequence()
        exceptionUtil.checkInterrupt()

        if track is not None:

            r, g, b = sequence.getStartingColor(drone.swarmIndex)
            x, y, z = sequence.getStartingPosition(drone.swarmIndex)
            drone.light_controller.set_color(r, g, b, 0.25, True)

            if self.appController.trajectoryEnabled or self.appController.positioningEnabled:
                Logger.log("Getting into position", drone.swarmIndex)

                # Step 1 - Move to some height determined by index
                targetHeight = min(SequenceController.MIN_HEIGHT + (drone.swarmIndex * 0.1), SequenceController.MAX_HEIGHT)
                relativeHeight = targetHeight - SequenceController.MIN_HEIGHT
                numDrones = len(self.appController.swarmController.connectedDrones)
                moveTime = 3.0 * (numDrones * 0.3)

                commander.go_to(0, 0, relativeHeight, 0, moveTime, True) # relative move
                threadUtil.interruptibleSleep(moveTime + 0.25)

                # Step 2 - Move to position, maintaining target height
                commander.go_to(x, y, targetHeight, 0, 3.0)
                threadUtil.interruptibleSleep(3.25)

                # Step 3 - Move to actual starting position
                commander.go_to(x, y, z, 0.0, 3.0)
                threadUtil.interruptibleSleep(3.25)

            Logger.log("Ready!", drone.swarmIndex)
            self.appController.updateSequence()
            exceptionUtil.checkInterrupt()


    def runSequence(self):
        Logger.log("Starting flight paths!")
        swarmController = self.appController.swarmController
        commander = swarmController.broadcaster.high_level_commander
        light_controller = swarmController.broadcaster.light_controller
        duration = SequenceController.CURRENT.duration

        # Start the automated trajectory
        if self.appController.trajectoryEnabled:
            commander.start_trajectory(Drone.TRAJECTORY_ID)

        # Start the automated LED sequence
        if self.appController.colorSequenceEnabled:
            light_controller.set_effect(RingEffect.TIMING_EFFECT)

        self.appController.startTimer.emit()
        threadUtil.interruptibleSleep(duration + 0.25)

    def landDrones(self, ignoreInterrupt=False):
        if not (self.appController.trajectoryEnabled or self.appController.positioningEnabled):
            return

        Logger.log("Landing drones...")
        swarmController = self.appController.swarmController
        commander = swarmController.broadcaster.high_level_commander
        light_controller = swarmController.broadcaster.light_controller

        light_controller.set_color(32, 32, 32, 4.0, True)
        commander.land(SequenceController.LANDING_HEIGHT, 4.0)

        for drone in swarmController.connectedDrones:
            if (drone.state == DroneState.IN_FLIGHT):
                drone.state = DroneState.LANDING

        self.appController.sequenceUpdated.emit()
        threadUtil.interruptibleSleep(4.5, ignoreInterrupt)

        light_controller.set_color(0, 0, 0, 0.25, True)
        commander.stop()

        for drone in swarmController.connectedDrones:
            if (drone.state == DroneState.LANDING):
                drone.state = DroneState.CONNECTED

        self.appController.sequenceUpdated.emit()
        exceptionUtil.checkInterrupt(ignoreInterrupt)

    def completeSequence(self):
        Logger.log("SEQUENCE COMPLETE")
        self.appController.swarmController.disconnectSwarm()
        self.appController.onSequenceCompleted()
        self.appController.sequenceUpdated.emit()

    def abort(self):
        self.landDrones(True)
        self.completeSequence()

