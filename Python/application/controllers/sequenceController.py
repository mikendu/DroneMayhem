import json
import os
import time
import pathlib

from cflib.crazyflie.light_controller import RingEffect

from application.model import Sequence, DroneState, Drone
from application.common.exceptions import SequenceInterrupt, DroneException
from application.util import exceptionUtil, threadUtil, Logger
from application.planner import DroneMatcher
from application.constants import Constants
from application.util import vectorMath


class SequenceController:

    CURRENT = None

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

    def clearSelection(self):
        SequenceController.CURRENT = None
        self.sequenceIndex = None


    def run(self):
        # Update the order of the recent sequences
        if self.sequenceIndex:
            self.sequences.insert(0, self.sequences.pop(self.sequenceIndex))
            self.appController.sequenceOrderUpdated.emit()

        try:
            Logger.log("SEQUENCE STARTING")
            swarmController = self.appController.swarmController
            sequence = SequenceController.CURRENT

            # connect to required drones
            numDrones = min(sequence.drones, self.appController.availableDrones)
            self.appController.swarmController.connectSwarm(numDrones)
            self.appController.sequenceUpdated.emit()

            # ensure all drones have updated light house info & know their positions
            uploadLighthouseData = self.appController.trajectoryEnabled or self.appController.positioningEnabled
            swarmController.initializeSensors(uploadLighthouseData)
            self.appController.sequenceUpdated.emit()

            # get all drones in position
            if sequence is not None and sequence.tracks is not None and len(sequence.tracks) > 0:
                startingPositions = sequence.allStartingPositions
                DroneMatcher.assign(swarmController.connectedDrones, startingPositions)
                swarmController.parallel(self.uploadFlightData)

            self.synchronizedTakeoff()
            self.setInitialPositions()

            # kick off the actual sequence action4
            self.runSequence()

            # land all drones
            self.landDrones()

            # Wrap up
            self.completeSequence()

        except SequenceInterrupt:
            Logger.log("ABORTING SEQUENCE")
            self.abort()
            return

        except DroneException:
            Logger.error("Drones did not reach starting positions, aborting!")
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
        track = sequence.getTrack(drone.trackIndex)
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

        # drone.disableAutoPing()

    def synchronizedTakeoff(self):
        if not (self.appController.trajectoryEnabled or self.appController.positioningEnabled):
            return

        Logger.log("Taking off")
        swarmController = self.appController.swarmController
        commander = swarmController.broadcaster.high_level_commander
        light_controller = swarmController.broadcaster.light_controller

        light_controller.set_color(0, 0, 0, 0.1, True)
        light_controller.set_color(0, 0, 0, 0.1, True)
        commander.takeoff(Constants.MIN_HEIGHT, 1.5)
        commander.takeoff(Constants.MIN_HEIGHT, 1.5)

        for drone in swarmController.connectedDrones:
            drone.state = DroneState.IN_FLIGHT

        self.appController.sequenceUpdated.emit()
        threadUtil.interruptibleSleep(1.6)


    def setInitialPositions(self):
        if not (self.appController.trajectoryEnabled or self.appController.positioningEnabled):
            return

        sequence = SequenceController.CURRENT
        if sequence is None or sequence.tracks is None or len(sequence.tracks) == 0:
            return

        swarmController = self.appController.swarmController
        exceptionUtil.checkInterrupt()
        drones = swarmController.connectedDrones

        maxTime = 0.0
        layers = {}

        for drone in drones:
            x, y, z = drone.targetPosition
            travelDistance = vectorMath.distance(drone.currentPosition, drone.targetPosition)
            drone.travelTime = travelDistance / Drone.MAX_VELOCITY
            maxTime = max(drone.travelTime, maxTime)

            layer = int(round(z * 10))
            if layer not in layers:
                layers[layer] = []

            layers[layer].append(drone)

        layerIndexes = list(layers.keys())
        layerIndexes.sort()

        delayPerLayer = 0.25 * maxTime
        delay = 0.0

        for layer in layerIndexes:
            layerDrones = layers[layer]
            for drone in layerDrones:
                drone.takeoffDelay = delay

            delay += delayPerLayer

        exceptionUtil.checkInterrupt()
        swarmController.parallel(self.moveToStartingPosition)



    def moveToStartingPosition(self, drone):

        sequence = SequenceController.CURRENT
        track = sequence.getTrack(drone.trackIndex)
        commander = drone.commander
        self.appController.updateSequence()
        exceptionUtil.checkInterrupt()

        if track is not None:

            r, g, b = sequence.getStartingColor(drone.trackIndex)
            x, y, z = sequence.getStartingPosition(drone.trackIndex)
            Logger.log("Getting into position", drone.swarmIndex)

            # Step 1 - Make sure the drone has reached its takeoff position
            tx, ty, tz = drone.currentPosition
            drone.waitForTargetPosition(tx, ty, Constants.MIN_HEIGHT, 1.0, 5.0)

            # Step 2 - Wait for takeoff delay, then move to starting position
            threadUtil.interruptibleSleep(drone.takeoffDelay)

            commander.go_to(x, y, z, 0.0, drone.travelTime)
            threadUtil.interruptibleSleep(drone.travelTime)
            drone.waitForTargetPosition(x, y, z, 1.0, drone.travelTime + 5.0)

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
            commander.start_trajectory(Drone.TRAJECTORY_ID)

        # Start the automated LED sequence
        if self.appController.colorSequenceEnabled:
            light_controller.set_effect(RingEffect.TIMING_EFFECT)
            light_controller.set_effect(RingEffect.TIMING_EFFECT)

        self.appController.startTimer.emit()
        threadUtil.interruptibleSleep(duration + 0.25)

    def landDrones(self, ignoreInterrupt=False):
        if not (self.appController.trajectoryEnabled or self.appController.positioningEnabled):
            return

        Logger.log("Landing drones...")
        swarmController = self.appController.swarmController
        commander = swarmController.broadcaster.high_level_commander
        commander.land(Constants.LANDING_HEIGHT, 2.0)
        commander.land(Constants.LANDING_HEIGHT, 2.0)

        for drone in swarmController.connectedDrones:
            if (drone.state == DroneState.IN_FLIGHT):
                drone.state = DroneState.LANDING

        self.appController.sequenceUpdated.emit()
        threadUtil.interruptibleSleep(2.0, ignoreInterrupt)

        light_controller = swarmController.broadcaster.light_controller
        light_controller.set_color(0, 0, 0, 0.25, True)
        light_controller.set_color(0, 0, 0, 0.25, True)
        threadUtil.interruptibleSleep(0.25, ignoreInterrupt)
        commander.stop()
        commander.stop()
        time.sleep(0.25)

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

