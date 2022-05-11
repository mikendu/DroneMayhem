using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
class BezierNodeTest : MonoBehaviour
{
    public List<BSegment> Segments = new List<BSegment>();
    public BNode LastNode = new BNode();


    [Range(0.0f, 1.0f)]
    public float Progress = 0.0f;
    public float Time = 0.0f;

    public bool DrawUnscaledPoints = true;
    public bool DrawScaledPoints = true;
    public bool DrawUnscaledLine = true;
    public bool DrawScaledLine = true;
    public bool DrawUnscaledMarker = true;
    public bool DrawScaledMarker = true;

    public bool playing = false;
    public bool ApplyScaling = true;

    private float totalTime;

    public void Update()
    {
        if (playing)
        {
            Progress += UnityEngine.Time.deltaTime / totalTime;
            Progress = Mathf.Clamp01(Progress);
            if (Time >= 1.0f)
            {
                playing = false;
            }
        }
    }

    public void Generate()
    {
        totalTime = 0.0f;
        for (int i = 0; i < Segments.Count; i++) 
        {
            var currentSegment = Segments[i];
            var nextNode = (i < (Segments.Count - 1)) ? Segments[i + 1].HeadNode : LastNode;
            nextNode.Generate();
            currentSegment.Generate(nextNode, ApplyScaling);
            totalTime += currentSegment.Time;
        }
    }


    private void OnValidate()
    {
        Time = CurrentTime;
    }

    private void OnDrawGizmos()
    {
        if (DrawUnscaledPoints)
        {
            foreach (BSegment segment in Segments)
                segment.DrawUnscaledNodes();
        }

        if (DrawScaledPoints)
        {
            foreach (BSegment segment in Segments)
                segment.DrawScaledNodes();
        }

        if (DrawUnscaledLine)
        {
            foreach (BSegment segment in Segments)
                segment.DrawUnscaledLine();
        }

        if (DrawScaledLine)
        {
            foreach (BSegment segment in Segments)
                segment.DrawScaledLine();
        }

        /*
        if (DrawUnscaledMarker)
        {
        }

        if (DrawScaledMarker)
        {
        }*/

        if (DrawScaledMarker)
        {
            var segmentAndTime = CurrentSegmentAndTime;
            BSegment segment = segmentAndTime.Item1;
            segment.DrawMarker(segmentAndTime.Item2);
        }
    }

    private float CurrentTime
    {
        get
        {
            return Progress * totalTime;
        }
    }

    private Tuple<BSegment, float> CurrentSegmentAndTime
    {
        get
        {
            if (Segments.Count == 0)
                return null;

            float time = CurrentTime;
            foreach (BSegment segment in Segments)
            {
                if (time <= segment.Time)
                    return Tuple.Create(segment, time);
                time -= segment.Time;
            }
            return null;
        }
    }
}


[CustomEditor(typeof(BezierNodeTest))]
[CanEditMultipleObjects]
public class TestEditor: Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.BeginVertical();
        if (GUILayout.Button("Generate"))
        {
            foreach (UnityEngine.Object unityObj in targets)
                ((BezierNodeTest)unityObj).Generate();
        }

        EditorGUILayout.Space(30.0f);
        EditorGUILayout.EndVertical();
        base.OnInspectorGUI();
    }

}