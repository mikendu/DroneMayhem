using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;

public enum FitMode
{
    Interpolation,
    Approximation
}

[ExecuteInEditMode]
public class BezierTester: MonoBehaviour
{
    public bool DrawLine = false;
    public bool DrawTangents = false;
    public FitMode Mode = FitMode.Interpolation;
    public Transform TargetObject;
    public List<Transform> Poses;

    public Transform Drone;

    [Range(0, 10)]
    public float TotalTime = 3.0f;

    [Range(0, 10)]
    public float CurrentTime = 0.0f;

    public bool AutoPlay = false;

    [Range(0, 1.0f)]
    public float ErrorThreshold = 1.0f;

    [Range(0.0f, 0.25f)]
    public float SampleInterval = 0.25f;

    private List<Vector3> points = new List<Vector3>();
    private List<CubicBezier> curves = new List<CubicBezier>();

    private void OnDrawGizmos()
    {
        if (Points.Count > 1)
        {
            Gizmos.color = new Color(1, 1, 1, 0.15f);
            Gizmos.DrawSphere(Points[0], 0.0125f);

            for (int i = 1; i < Points.Count; i++)
            {
                Vector3 start = Points[i - 1];
                Vector3 end = Points[i];
                Gizmos.DrawSphere(end, 0.0125f);

                if (DrawLine)
                    Gizmos.DrawLine(start, end);
            }
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


            Gizmos.DrawSphere(bezier.anchor1, 0.075f);
            Gizmos.DrawSphere(bezier.anchor2, 0.075f);

            if (DrawTangents)
            {
                Gizmos.DrawLine(bezier.anchor1, bezier.control1);
                Gizmos.DrawLine(bezier.anchor2, bezier.control2);

                Gizmos.DrawSphere(bezier.control1, 0.025f);
                Gizmos.DrawSphere(bezier.control2, 0.025f);
            }
        }
    }

    public void Update()
    {
        if (Mode == FitMode.Interpolation)
            curves = BezierInterpolator.Interpolate(Points);
        else
            curves = BezierFitter.Fit(Points, ErrorThreshold);

        if (AutoPlay)
            CurrentTime = Mathf.Clamp(CurrentTime + Time.deltaTime, 0.0f, TotalTime);

        if (Drone != null)
            Drone.position = BezierEvaluator.FindPosition(curves, Mathf.Clamp01(CurrentTime / TotalTime));

        if (Poses.Count > 1)
            SetPose();
    }

    private void SetPose()
    {
        int n = Poses.Count - 1;
        float interval = TotalTime / n;

        int indexOne = Mathf.FloorToInt(n * CurrentTime / TotalTime);
        int indexTwo = Mathf.CeilToInt(n * CurrentTime / TotalTime);

        indexOne = Mathf.Max(0, Mathf.Min(n, indexOne));
        indexTwo = Mathf.Max(0, Mathf.Min(n, indexTwo));

        Transform poseOne = Poses[indexOne];
        Transform poseTwo = Poses[indexTwo];

        float bottom = indexOne * interval;
        float top = indexTwo * interval;
        float value = Mathf.InverseLerp(bottom, top, CurrentTime);
        value = Mathf.SmoothStep(0.0f, 1.0f, value);

        TargetObject.position = Vector3.Lerp(poseOne.position, poseTwo.position, value);
        TargetObject.rotation = Quaternion.Slerp(poseOne.rotation, poseTwo.rotation, value);
        TargetObject.localScale = Vector3.Lerp(poseOne.lossyScale, poseTwo.lossyScale, value);

    }

    public void Clear()
    {
        points = null;
        EditorApplication.QueuePlayerLoopUpdate();
    }
    public void Play()
    {
        CurrentTime = 0.0f;
        AutoPlay = true;
    }

    public void OnValidate()
    {
        AutoPlay = false;
    }


    private List<Vector3> Points
    {
        get
        {
            if (points == null)
                points = new List<Vector3>();
            
            if (points.Count == 0)
            {
                for(float t = 0; t < TotalTime; t += SampleInterval)
                {
                    CurrentTime = t;
                    SetPose();
                    points.Add(TargetObject.TransformPoint(new Vector3(0.5f, 0.5f, 0.5f)));
                }
            }

            return points;
        }
    }
}



[CustomEditor(typeof(BezierTester))]
public class BezierTesterEditor : Editor
{

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(30.0f);

        if (GUILayout.Button("Resample"))
            ((BezierTester)target).Clear();

        if (GUILayout.Button("Play"))
            ((BezierTester)target).Play();


        EditorGUILayout.Space(30.0f);
    }
}