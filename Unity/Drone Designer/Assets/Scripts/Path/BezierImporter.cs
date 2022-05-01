using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class BezierImporter : ScriptableObject
{

    public static void Import(string jsonData)
    {
        ExpandedSequence sequence = JsonUtility.FromJson<ExpandedSequence>(jsonData);
        List<BezierSegment> importedSegments = new List<BezierSegment>();
        foreach (ExpandedTrajectory trajectory in sequence.sequences)
        {
            foreach (SerializedSegment segment in trajectory.segments)
            {
                importedSegments.Add(segment.Expand());
            }
        }

        BezierSegmentTest test = FindObjectOfType<BezierSegmentTest>();
        if (test != null)
        {
            test.Segments = importedSegments;
        }
    }
}