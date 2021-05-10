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
    public Vector3 StartPosition = Vector3.zero;

    public byte[] CompressedTrajectory = { };
    public byte[] LedTimings = { };
}

[Serializable]
public class SequenceCollection
{
    public int DroneCount = 0;
    public float Length = 0.0f;
    public List<SequenceTrack> Tracks = new List<SequenceTrack>();
}



public class TimelineExporter : MonoBehaviour
{
    public static string ExportTimeline(TimelineAsset timeline, bool prettyPrint = false)
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
                Tuple<List<ColorKeyframe>, List<Waypoint>> processedKeyframes = ProcessKeyframes(sequenceTrack, markers);
                List<ColorKeyframe> sortedColorKeyframes = processedKeyframes.Item1;
                List<Waypoint> sortedWaypoints = processedKeyframes.Item2;

                sequenceTrack.LedTimings = ColorExporter.Process(sortedColorKeyframes);
                sequenceTrack.CompressedTrajectory = TrajectoryExporter.Process(sortedWaypoints);
                sequenceTrack.StartPosition = GetStart(sortedWaypoints);
                sequenceTrack.Name = crazyflieTrack.name;
                sequenceCollection.Tracks.Add(sequenceTrack);
            }
        }

        sequenceCollection.Length = (float)timeline.duration;
        sequenceCollection.DroneCount = sequenceCollection.Tracks.Count;
        string jsonText = JsonUtility.ToJson(sequenceCollection, prettyPrint);
        return jsonText;
    }

    private static Tuple<List<ColorKeyframe>, List<Waypoint>> ProcessKeyframes(SequenceTrack track, IEnumerable<IMarker> markers)
    {
        List<ColorKeyframe> colorKeyframes = new List<ColorKeyframe>();
        List<Waypoint> waypoints = new List<Waypoint>();

        List<IMarker> sortedMarkers = new List<IMarker>(markers);
        sortedMarkers.Sort(KeyframeUtil.KeyframeComparator);

        foreach(IMarker marker in sortedMarkers)
        {
            if (marker is ColorKeyframe)
            {
                colorKeyframes.Add(marker as ColorKeyframe);
                track.Length = Mathf.Max(track.Length, (float)marker.time);
            }

            if (marker is Waypoint)
            {
                waypoints.Add(marker as Waypoint);
                track.Length = Mathf.Max(track.Length, (float)marker.time);
            }
        }

        colorKeyframes.Sort(KeyframeUtil.KeyframeComparator);
        waypoints.Sort(KeyframeUtil.KeyframeComparator);

        if (waypoints.Count > 0 && waypoints[0].time > 0.0f)
        {
            Waypoint fakeWaypoint = ScriptableObject.CreateInstance<Waypoint>();
            fakeWaypoint.Position = waypoints[0].Position;
            fakeWaypoint.JointType = JointType.Linear;
            fakeWaypoint.time = 0.0;
            waypoints.Insert(0, fakeWaypoint);
        }

        if (colorKeyframes.Count > 0 && colorKeyframes[0].time > 0.0f)
        {
            ColorKeyframe fakeKeyframe = ScriptableObject.CreateInstance<ColorKeyframe>();
            fakeKeyframe.LightColor = colorKeyframes[0].LightColor;
            fakeKeyframe.time = 0.0;
            colorKeyframes.Insert(0, fakeKeyframe);
        }

        int lastIndex = colorKeyframes.Count - 1;
        if (colorKeyframes.Count > 0 && colorKeyframes[lastIndex].time < track.Length)
        {
            ColorKeyframe lastKeyframe = ScriptableObject.CreateInstance<ColorKeyframe>();
            lastKeyframe.LightColor = colorKeyframes[lastIndex].LightColor;
            lastKeyframe.time = track.Length;
            colorKeyframes.Add(lastKeyframe);
        }

        return new Tuple<List<ColorKeyframe>, List<Waypoint>>(colorKeyframes, waypoints);
    }

    private static Vector3 GetStart(List<Waypoint> waypoints)
    {
        if (waypoints.Count == 0)
            return Vector3.zero;

        return waypoints[0].Position.ToCrazyflieCoordinates();
    }
}
