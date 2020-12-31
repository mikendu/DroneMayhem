using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;
using System.Linq;
using UnityEditor;
using System;


[Serializable]
public class SequenceTrack
{
    public string Name = "";
    public float Length = 0.0f;
    public List<SequenceColorKeyframe> ColorKeyframes = new List<SequenceColorKeyframe>();
    public List<SequenceWaypoint> Waypoints = new List<SequenceWaypoint>();
}

[Serializable]
public class SequenceCollection
{
    public int DroneCount = 0;
    public float Length = 0.0f;
    public List<SequenceTrack> Tracks = new List<SequenceTrack>();
}

[Serializable]
public class SequenceWaypoint
{
    public float Time;
    public JointType JointType;
    public Vector3 Position;
    public Vector3 Tangent;

    public SequenceWaypoint(Waypoint waypoint)
    {
        this.Time = (float)waypoint.time;
        this.JointType = waypoint.JointType;
        this.Position = waypoint.Position;
        this.Tangent = waypoint.Tangent;
    }
}

[Serializable]
public class SequenceColorKeyframe
{
    public float Time;
    public Color LightColor;

    public SequenceColorKeyframe(ColorKeyframe keyframe)
    {
        this.Time = (float)keyframe.time;
        this.LightColor = keyframe.LightColor;
    }
}


public class TimelineExporter : MonoBehaviour
{
    public static string ExportTimeline(TimelineAsset timeline)
    {
        List<TrackAsset> tracks = new List<TrackAsset>(timeline.GetOutputTracks());
        SequenceCollection sequenceCollection = new SequenceCollection();

        foreach (TrackAsset track in tracks)
        {
            if (track is CrazyflieTrack)
            {
                CrazyflieTrack crazyflieTrack = track as CrazyflieTrack;
                SequenceTrack sequenceTrack = new SequenceTrack();
                IEnumerable<IMarker> markers = crazyflieTrack.GetMarkers();

                foreach (IMarker marker in markers)
                    ProcessMarker(sequenceTrack, marker);

                sequenceTrack.Name = crazyflieTrack.name;
                sequenceTrack.ColorKeyframes.Sort(KeyframeComparator);
                sequenceTrack.Waypoints.Sort(KeyframeComparator);
                sequenceCollection.Tracks.Add(sequenceTrack);
            }
        }

        sequenceCollection.Length = (float)timeline.duration;
        sequenceCollection.DroneCount = sequenceCollection.Tracks.Count;
        string jsonText = JsonUtility.ToJson(sequenceCollection, true);
        return jsonText;
    }

    private static void ProcessMarker(SequenceTrack track, IMarker marker)
    {
        if (marker is ColorKeyframe)
        {
            ColorKeyframe keyframe = marker as ColorKeyframe;
            SequenceColorKeyframe sequenceKeyframe = new SequenceColorKeyframe(keyframe);
            track.ColorKeyframes.Add(sequenceKeyframe);
            track.Length = Mathf.Max(track.Length, sequenceKeyframe.Time);
        }

        if (marker is Waypoint)
        {
            Waypoint waypoint = marker as Waypoint;
            SequenceWaypoint sequenceWaypoint = new SequenceWaypoint(waypoint);
            track.Waypoints.Add(sequenceWaypoint);
            track.Length = Mathf.Max(track.Length, sequenceWaypoint.Time);
        }
    }

    private static int KeyframeComparator(SequenceWaypoint waypointOne, SequenceWaypoint waypointTwo)
    {
        return waypointOne.Time.CompareTo(waypointOne.Time);
    }

    private static int KeyframeComparator(SequenceColorKeyframe keyframeOne, SequenceColorKeyframe keyframeTwo)
    {
        return keyframeOne.Time.CompareTo(keyframeTwo.Time);
    }
}
