
import json
from enum import Enum


class ActionType(Enum): 
    MOVE = 0
    LIGHT = 1


def getInitialState(desiredActionType):
    for keyframe in sequence['Keyframes']:
        for action in keyframe['Actions']:
            actionType = ActionType(action['ActionType'])
            if (actionType == desiredActionType):
                data = action['Data']
                x, y, z = [data[key] for key in ('x', 'y','z')]
                return x, y, z

    raise ValueError("Initial state not found for action type: " + str(desiredActionType))


sequence = None
with open('../../Sequences/Test Sequence.json') as jsonFile:
    sequenceCollection = json.load(jsonFile)
    sequences = sequenceCollection['Sequences']
    sequence = sequences[0]


print("\n\n\nSequence:\n\n", sequence)

print(getInitialState(ActionType.MOVE))
print(getInitialState(ActionType.LIGHT))