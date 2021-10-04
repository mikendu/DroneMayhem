using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class RotationTest : MonoBehaviour
{

    public Transform Target;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnDrawGizmos()
    {
        if (Target == null)
            return;

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
    }
}
