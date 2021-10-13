import os
import keyboard
from util import *

import cflib

os.environ["USE_CFLINK"] = "cpp"
# if 'USE_CFLINK' in os.environ:
#     del os.environ['USE_CFLINK']

uri = "radio://*/120/2M/E7E7E7E702"
# uri = "radio://0/120/2M/E7E7E7E703"
cflib.crtp.init_drivers()


# calibrator = Calibrator(uri)
# calibrator.begin()

calibrator = Calibrator2()

def snapshot(arg):
    calibrator.next(uri)
    # calibrator.takeSample()

keyboard.on_press_key('space', snapshot)
keyboard.on_press_key('f', lambda evt: calibrator.finish(uri))
keyboard.wait('escape')
# calibrator.end()
print('Done!')

"""
with SyncCrazyflie(uri, cf=Crazyflie(ro_cache='./cache',rw_cache='./cache')) as scf:
    cf = scf.cf
    x, y, z, yaw = util.get_pose(cf)
    print('Pose: ({:.2f}, {:.2f}, {:.2f}, {:.2f})'.format(x, y, z, yaw))

    geos = util.get_saved_geo(cf)
    saved_position = geos[0].origin
    saved_rotation = geos[0].rotation_matrix
    print("\n\nStored: \nposition: ", saved_position, "\nrotation matrix: ", saved_rotation)

    computed = util.estimate_base_stations(cf)
    rotation = computed[0][0]
    position = computed[0][1]
    print("\n\nComputed: \nposition: ", position, "\nrotation matrix: ", rotation)

    matrix = util.get_yaw_matrix(yaw)
    position = np.add(np.matmul(matrix, position), np.array([x, y, z]))
    rotation = np.matmul(matrix, rotation)
    print("\n\nCorrected: \nposition: ", position, "\nrotation matrix: ", rotation)


    abs_delta_pos = np.subtract(position, saved_position)
    abs_delta_rot = np.subtract(rotation, saved_rotation)
    print("\n\nAbsolute delta: \nposition: ", abs_delta_pos, "\nrotation matrix: ", abs_delta_rot)


    rel_delta_pos = np.divide(abs_delta_pos, saved_position)
    rel_delta_rot = np.divide(abs_delta_rot, saved_rotation)
    print("\n\nPercentage delta: \nposition: ", rel_delta_pos, "\nrotation matrix: ", rel_delta_rot)


    # util.stream_pose(cf)
    # util.estimate_base_stations(cf)
"""


"""

        // same as transform.rotation, just in matrix form
        Matrix4x4 yawRotation = Matrix4x4.Rotate(Quaternion.AngleAxis(transform.eulerAngles.y, Vector3.up));
        Quaternion inverseRotation = Quaternion.Inverse(transform.localRotation);

        Vector3 observedPosition = transform.InverseTransformPoint(Target.position);
        Quaternion observedRotation = inverseRotation * Target.localRotation;
        Vector3 scale = 0.1f * Vector3.one;

        // Observation
        Gizmos.matrix = Matrix4x4.TRS(observedPosition, observedRotation, scale);

        Gizmos.color = Color.white;
        Gizmos.DrawCube(Vector3.zero, Vector3.one);
        Gizmos.color = Color.red;
        Gizmos.DrawCube(1.5f * Vector3.right, new Vector3(2, 0.05f, 0.05f));
        Gizmos.color = Color.green;
        Gizmos.DrawCube(1.5f * Vector3.up, new Vector3(0.05f, 2, 0.05f));
        Gizmos.color = new Color(0, 0.5f, 1);
        Gizmos.DrawCube(1.5f * Vector3.forward, new Vector3(0.05f, 0.05f, 2));



        Vector3 rotated = (yawRotation * observedPosition);
        Gizmos.matrix = Matrix4x4.TRS(rotated + transform.localPosition, transform.localRotation * observedRotation, scale);

        Gizmos.color = Color.black;
        Gizmos.DrawCube(Vector3.zero, Vector3.one);
        Gizmos.color = Color.blue;
        Gizmos.DrawCube(1.5f * Vector3.right, new Vector3(2, 0.05f, 0.05f));
        Gizmos.color = Color.magenta;
        Gizmos.DrawCube(1.5f * Vector3.up, new Vector3(0.05f, 2, 0.05f));
        Gizmos.color = new Color(1, 0.5f, 0);
        Gizmos.DrawCube(1.5f * Vector3.forward, new Vector3(0.05f, 0.05f, 2));

        Gizmos.matrix = Matrix4x4.identity;


"""