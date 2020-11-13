import json
import os
import time
import pathlib

from application.model import Sequence, DroneState, DroneActionType
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
            if sequence.fullPath == desiredMatch.fullPath:
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
            SequenceController.CURRENT = self.sequences[index]
            self.sequenceIndex = index
            return True
        return False

    def run(self):

        # Update the order of the recent sequences
        self.sequences.insert(0, self.sequences.pop(self.sequenceIndex))
        self.appController.sequenceOrderUpdated.emit()

        try: 
            print("\n\n\n\n-------------- SEQUENCE STARTING --------------")
            # connect to required drones
            numDrones = SequenceController.CURRENT.drones
            self.appController.swarmController.connectSwarm(numDrones)
            self.appController.sequenceUpdated.emit()
                    
            # ensure all drones have updated light house info & know their positions
            self.appController.swarmController.initializeSensors()
            self.appController.sequenceUpdated.emit()

            # get all drones in position
            self.takeoffDrones()

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
        print("\n-- Getting Drones into Position --")          
        self.appController.swarm.parallel(applyInitialState, self.appController.swarmArguments)
        threadUtil.interruptibleSleep(1.0)

    def runSequence(self):
        print("\n-- Running Sequence --")
        self.appController.startTimer.emit()
        self.appController.swarm.parallel(runSequenceSteps, self.appController.swarmArguments)

    def landDrones(self):
        print("\n-- Landing Drones --")      
        self.appController.swarm.parallel(landDrone, self.appController.swarmArguments)



def applyInitialState(syncCrazyflie, drone, appController):
    
    exceptionUtil.checkInterrupt()
    drone.state = DroneState.POSITIONING
    appController.updateSequence()

    crazyflie = syncCrazyflie.cf
    crazyflie.param.set_value('ring.effect', '14')
    sequence = SequenceController.CURRENT

    r, g, b = sequence.getInitialState(drone.swarmIndex, DroneActionType.LIGHT)
    x, y, z = sequence.getInitialState(drone.swarmIndex, DroneActionType.MOVE)

    setLED(appController, drone.swarmIndex, crazyflie, r, g, b, 4.0)
    commander = crazyflie.high_level_commander    
    commander.takeoff(0.5, 3.0)
    threadUtil.interruptibleSleep(3.0)

    moveTo(appController, drone.swarmIndex, crazyflie, x, y, z, 3.0)
    threadUtil.interruptibleSleep(3.0)

    drone.state = DroneState.READY
    appController.updateSequence()
    exceptionUtil.checkInterrupt()


def runSequenceSteps(syncCrazyflie, drone, appController):
    
    exceptionUtil.checkInterrupt()
    drone.state = DroneState.IN_FLIGHT
    crazyflie = syncCrazyflie.cf
    sequence = SequenceController.CURRENT
    track = sequence.getTrack(drone.swarmIndex)
    
    
    for keyframe in track['Keyframes']:

        actions = keyframe['Actions']
        sleepDuration = 1000
        for action in actions:
            duration = runAction(appController, drone.swarmIndex, crazyflie, action)
            sleepDuration = min(sleepDuration, duration)

        if (sleepDuration > 0):
            threadUtil.interruptibleSleep(sleepDuration)
            
    exceptionUtil.checkInterrupt()



def landDrone(syncCrazyflie, drone, appController):
    if (drone.state == DroneState.DISCONNECTED):
        return

    crazyflie = syncCrazyflie.cf
    setLED(appController, drone.swarmIndex, crazyflie, 0, 0, 0, 0.5)

    if (drone.state == DroneState.POSITIONING or drone.state == DroneState.READY or
        drone.state == DroneState.IN_FLIGHT or drone.state == DroneState.LANDING):

        drone.state = DroneState.LANDING
        appController.updateSequence()
        commander = crazyflie.high_level_commander
        commander.land(0.05, 2.5)
        time.sleep(2.5)

    commander.stop()
    drone.state = DroneState.IDLE


def runAction(controller, index, crazyflie, action):    
    actionType = DroneActionType(action['ActionType'])
    duration = action['Duration']
    if (duration == 0):
        return 0

    data = action['Data']
    x, y, z = [data[key] for key in ('x', 'y','z')]

    if (actionType == DroneActionType.MOVE):
        moveTo(controller, index, crazyflie, x, y, z, duration)

    elif (actionType == DroneActionType.LIGHT):
        setLED(controller, index, crazyflie, x, y, z, duration)

    return duration


def setLED(controller, index, crazyflie, r, g, b, time): 
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

