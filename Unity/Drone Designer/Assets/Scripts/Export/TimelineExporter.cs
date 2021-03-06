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
    public List<SequenceColorKeyframe> ColorKeyframes = new List<SequenceColorKeyframe>();
}

[Serializable]
public class SequenceCollection
{
    public int DroneCount = 0;
    public float Length = 0.0f;
    public List<SequenceTrack> Tracks = new List<SequenceTrack>();
}


[Serializable]
public class SequenceColorKeyframe
{
    public float Duration;
    public Color LightColor;

    public SequenceColorKeyframe(ColorKeyframe keyframe)
    {
        float keyframeTime = (float)keyframe.time;
        this.Duration = (keyframeTime - keyframe.PreviousKeyframeTime);
        this.LightColor = keyframe.LightColor.ToCrazyflieColor();
    }
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

                sequenceTrack.ColorKeyframes = sortedColorKeyframes.Select(keyframe => new SequenceColorKeyframe(keyframe)).ToList();
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
        float lastColorKeyframeTime = 0.0f;

        foreach(IMarker marker in markers)
        {
            if (marker is ColorKeyframe)
            {
                ColorKeyframe colorKeyframe = marker as ColorKeyframe;
                colorKeyframe.PreviousKeyframeTime = lastColorKeyframeTime;
                lastColorKeyframeTime = (float)colorKeyframe.time;

                colorKeyframes.Add(colorKeyframe);
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
            Waypoint fakeWaypoint = new Waypoint();
            fakeWaypoint.Position = waypoints[0].Position;
            fakeWaypoint.JointType = JointType.Linear;
            fakeWaypoint.time = 0.0;
            waypoints.Insert(0, fakeWaypoint);
        }


        if (colorKeyframes.Count > 0 && colorKeyframes[0].time > 0.0f)
        {
            ColorKeyframe fakeKeyframe = new ColorKeyframe();
            fakeKeyframe.LightColor = Color.black;
            fakeKeyframe.PreviousKeyframeTime = 0.0f;
            fakeKeyframe.time = 0.0;
            colorKeyframes.Insert(0, fakeKeyframe);
        }

        return new Tuple<List<ColorKeyframe>, List<Waypoint>>(colorKeyframes, waypoints);
    }

    private static SequenceColorKeyframe ToSequenceKeyframe(ColorKeyframe keyframe)
    {
        return new SequenceColorKeyframe(keyframe);
    }

    private static Vector3 GetStart(List<Waypoint> waypoints)
    {
        if (waypoints.Count == 0)
            return Vector3.zero;

        return waypoints[0].Position.ToCrazyflieCoordinates();
    }
}
