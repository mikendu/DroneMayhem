using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
public class BezierInterpolatorTest : MonoBehaviour
{

    public Transform Waypoints;

    private List<Transform> points = new List<Transform>();
    private List<CubicBezier> curves = new List<CubicBezier>();

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1, 1, 1, 0.15f);
        for(int i = 1; i < Points.Count; i++)
        {
            Vector3 start = Points[i - 1].position;
            Vector3 end = Points[i].position;

            Gizmos.DrawLine(start, end);
        }

        foreach(CubicBezier bezier in curves)
        {
            Handles.DrawBezier( bezier.anchor1, 
                                bezier.anchor2, 
                                bezier.control1,
                                bezier.control2, 
                                Color.white, 
                                null, 
                                3.0f);
        }
    }

    public void Update()
    {
        curves = BezierInterpolator.Interpolate(Vertices);
    }

    public void Clear()
    {
        points = null;
        curves = BezierInterpolator.Interpolate(Vertices);
        EditorApplication.QueuePlayerLoopUpdate();
    }

    private List<Vector3> Vertices
    {
        get
        {
            return Points.Select(x => x.position).ToList();
        }
    }

    private List<Transform> Points
    {
        get
        {
            if (points == null)
                points = new List<Transform>();

            if (points.Count == 0)
            {
                foreach (Transform child in Waypoints)
                    points.Add(child);
            }

            return points;
        }
    }
}



[CustomEditor(typeof(BezierInterpolatorTest))]
public class BezierInterpolatorTestEditor : Editor
{

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(30.0f);

        if (GUILayout.Button("Reset"))
            ((BezierInterpolatorTest)target).Clear();


        EditorGUILayout.Space(30.0f);
    }
}