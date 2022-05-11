using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[Serializable]
public class BNode
{
    public List<Vector3> ControlPoints = new List<Vector3>();

    public List<Vector3> HeadPoints { get; private set; } = new List<Vector3>();
    public List<Vector3> TailPoints { get; private set; } = new List<Vector3>();
    public List<Vector3> Points { get; private set; } = new List<Vector3>();

    public void Generate()
    {
        if (ControlPoints.Count == 0)
            return;

        Vector3 q0 = ControlPoints[0];
        Vector3 q1 = (ControlPoints.Count >= 2) ? ControlPoints[1] : q0;
        Vector3 delta = q1 - q0;

        Vector3 q2 = (ControlPoints.Count >= 3) ? ControlPoints[2] : (q0 + (2 * delta));
        Vector3 e = (3 * q2) - (2 * q0) - (6 * delta);
        Vector3 q3 = (ControlPoints.Count >= 4) ? ControlPoints[3] : e + (3 * delta);

        Vector3 p7 = q0;
        Vector3 p6 = q1 - (2 * delta);
        Vector3 p5 = q2 - (4 * delta);
        Vector3 p4 = (2 * e) - q3;

        HeadPoints = new List<Vector3>() { q0, q1, q2, q3 };
        TailPoints = new List<Vector3>() { p4, p5, p6, p7 };
        Points = new List<Vector3>() { q0, q1, q2, q3, p4, p5, p6, p7 };
    }

    public void Draw()
    {
        Gizmos.color = (0.85f * Color.white);
        foreach (Vector3 v in Points)
            Gizmos.DrawSphere(v, 0.015f);
    }
}
