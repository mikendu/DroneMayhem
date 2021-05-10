import json
import os
import time
import pathlib

from application.model import Sequence, DroneState
from application.common.exceptions import SequenceInterrupt
from application.util import exceptionUtil, threadUtil


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

    def setTestSequence(self):
        SequenceController.CURRENT = Sequence.EMPTY
        self.sequenceIndex = None


    def run(self):

        # Update the order of the recent sequences
        if self.sequenceIndex:
            self.sequences.insert(0, self.sequences.pop(self.sequenceIndex))
            self.appController.sequenceOrderUpdated.emit()

        try:
            print("\n\n\n\n-------------- SEQUENCE STARTING --------------")
            # connect to required drones
            numDrones = min(SequenceController.CURRENT.drones, self.appController.availableDrones)
            self.appController.swarmController.connectSwarm(numDrones)
            self.appController.sequenceUpdated.emit()

            # ensure all drones have updated light house info & know their positions
            if self.appController.trajectoryEnabled or self.appController.positioningEnabled:
                self.appController.swarmController.initializeSensors()
                self.appController.sequenceUpdated.emit()

            # get all drones in position
            self.takeoffDrones()
            threadUtil.interruptibleSleep(2)

            # run sequence for each drone
            self.runSequence()

            # land all drones
            self.landDrones()
            self.appController.sequenceUpdated.emit()

            # Wrap up
            self.completeSequence()

        except SequenceInterrupt:
            print("-- Aborting Sequence --")
            self.abort()

        except ConnectionAbortedError:
            print("-- Drone connection failed, cancelling sequence --")
            self.appController.connectionFailed.emit()
            return

        # Re-throw all other exceptions
        except Exception:
            raise

    def completeSequence(self):
        self.appController.swarmController.disconnectSwarm()
        self.appController.onSequenceCompleted()
        self.appController.sequenceUpdated.emit()

    def abort(self):
        self.landDrones()
        self.completeSequence()
    
    def takeoffDrones(self):
        if self.appController.swarm is None:
            return
        print("\n-- Getting Drones into Position --")
        self.appController.swarm.parallel(applyInitialState, self.appController.swarmArguments)
        threadUtil.interruptibleSleep(1.0)

    def runSequence(self):
        if self.appController.swarm is None:
            return
        print("\n-- Running Sequence --")
        self.appController.startTimer.emit()
        self.appController.swarm.parallel(runSequenceSteps, self.appController.swarmArguments)

    def landDrones(self):
        if self.appController.swarm is None:
            return

        print("\n-- Landing Drones --")      
        self.appController.swarm.parallel(landDrone, self.appController.swarmArguments)



def applyInitialState(syncCrazyflie, drone, appController):
    
    exceptionUtil.checkInterrupt()
    drone.state = DroneState.POSITIONING
    appController.updateSequence()

    crazyflie = syncCrazyflie.cf
    crazyflie.param.set_value('ring.effect', '14')
    sequence = SequenceController.CURRENT

    track = sequence.getTrack(drone.swarmIndex)
    commander = crazyflie.high_level_commander

    if track is not None:

        # Before taking action, make sure trajectory is uploaded
        if appController.trajectoryEnabled:
            trajectoryData = bytearray(track['CompressedTrajectory'])
            drone.writeTrajectory(trajectoryData)

        # -- Get drone into position -- #

        r, g, b = sequence.getStartingColor(drone.swarmIndex)
        x, y, z = sequence.getStartingPosition(drone.swarmIndex)

        if appController.trajectoryEnabled or appController.positioningEnabled:
            setLED(appController, drone.swarmIndex, crazyflie, r, g, b, 0.25, True)

            # Step 1 - Takeoff & move to some height determined by index
            targetHeight = drone.startHeight + min(0.3 + (drone.swarmIndex * 0.1), 3.0)
            moveTime = 2.0 + (drone.swarmIndex * 0.3)
            numDrones = min(appController.availableDrones, appController.requiredDrones)
            maxMoveTime = 3.0 * (numDrones * 0.3)

            commander.takeoff(targetHeight, moveTime)
            threadUtil.interruptibleSleep(maxMoveTime)

            # Step 2 - Move to position, maintaining target height
            moveTo(appController, drone.swarmIndex, crazyflie, x, y, targetHeight, 3.0)
            threadUtil.interruptibleSleep(3.0)

            # Step 3 - Move to actual starting position
            moveTo(appController, drone.swarmIndex, crazyflie, x, y, z, 3.0)
            threadUtil.interruptibleSleep(3.0)
        else:
            setLED(appController, drone.swarmIndex, crazyflie, r, g, b, 0.25, True)
            threadUtil.interruptibleSleep(2)

    else:
        setLED(appController, drone.swarmIndex, crazyflie, 0, 0, 0, 1.0, True)
        commander.takeoff(drone.startHeight + 0.25, 4.0)
        threadUtil.interruptibleSleep(4.0)

    drone.state = DroneState.READY
    appController.updateSequence()
    exceptionUtil.checkInterrupt()


def runSequenceSteps(syncCrazyflie, drone, appController):
    
    exceptionUtil.checkInterrupt()
    drone.state = DroneState.IN_FLIGHT
    crazyflie = syncCrazyflie.cf
    sequence = SequenceController.CURRENT
    track = sequence.getTrack(drone.swarmIndex)

    if track is not None:
        fullDuration = float(track['Length'])

        # Start the automated trajectory
        if appController.trajectoryEnabled:
            drone.startTrajectory()

        # Run the color sequence manually
        if appController.colorSequenceEnabled:
            colorKeyframes = track['ColorKeyframes'][1:]
            for keyframe in colorKeyframes:

                duration = max(float(keyframe['Timestamp']) - appController.sequenceClock, 0)
                lightColor = keyframe['LightColor']

                r, g, b = [int(lightColor[key]) for key in ('r', 'g', 'b')]
                setLED(appController, drone.swarmIndex, crazyflie, r, g, b, duration)
                threadUtil.interruptibleSleep(duration)

                # Sometimes need to "solidify" the color we just faded to
                # Otherwise you get flickering (crazyflies are weird)
                setLED(appController, drone.swarmIndex, crazyflie, r, g, b, 0.0, True)
                threadUtil.interruptibleSleep(min(0.1, duration * 0.25))


        # Make sure we wait for the trajectory to complete, if our color keyframes are shorter (or don't exist)
        if appController.trajectoryEnabled:
            remainingDuration = max(fullDuration - appController.sequenceClock, 0)
            threadUtil.interruptibleSleep(remainingDuration)
    else:
        setLED(appController, drone.swarmIndex, crazyflie, 255, 255, 255, 0.5)
        threadUtil.interruptibleSleep(1.0)

    exceptionUtil.checkInterrupt()



def landDrone(syncCrazyflie, drone, appController):
    if (drone.state == DroneState.DISCONNECTED):
        return

    crazyflie = syncCrazyflie.cf
    setLED(appController, drone.swarmIndex, crazyflie, 0, 0, 0, 0.25, True)
    sequence = SequenceController.CURRENT
    track = sequence.getTrack(drone.swarmIndex)
    targetHeight = 0.1 if track is not None else drone.startHeight + 0.025

    if (drone.state == DroneState.POSITIONING or drone.state == DroneState.READY or
        drone.state == DroneState.IN_FLIGHT or drone.state == DroneState.LANDING):

        drone.state = DroneState.LANDING
        appController.updateSequence()
        commander = crazyflie.high_level_commander
        if appController.trajectoryEnabled or appController.positioningEnabled:
            commander.land(targetHeight, 3.5)
            time.sleep(3.5)

    crazyflie.high_level_commander.stop()
    drone.state = DroneState.IDLE



def setLED(controller, index, crazyflie, r, g, b, time, silent = False):
    if not silent:
        pass
        print("DRONE " + str(index) + " | Fading LED to (", r, ",", g, ",", b, ") over", time, "seconds")
        controller.addLogEntry.emit((index, "FADE LIGHT to color ", r, g, b, time))

    color = (int(r) << 16) | (int(g) << 8) | int(b)
    crazyflie.param.set_value('ring.fadeTime', str(time))
    crazyflie.param.set_value('ring.fadeColor', str(color))


def moveTo(controller, index, crazyflie, x, y, z, time):
    print("DRONE " + str(index) + " | Moving to (", x, ",", y, ",", z, ") over", time, "seconds")
    controller.addLogEntry.emit((index, "FLY TO position ", x, y, z, time))
    
    commander = crazyflie.high_level_commander   
    commander.go_to(x, y, z, 0.0, time) # yaw always set to 0

