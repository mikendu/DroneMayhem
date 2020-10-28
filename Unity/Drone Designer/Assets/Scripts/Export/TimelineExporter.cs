using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;
using System.Linq;
using UnityEditor;
using System;

public enum SequenceActionType
{
    Move = 0,
    Light = 1
}

[Serializable]
public struct SequenceAction
{
    public SequenceActionType ActionType;
    public float Duration;
    public Vector4 Data;

    public override string ToString()
    {
        return $"{ActionType} | {Duration.ToString("N2")} | {Data}";
    }
}

[Serializable]
public class SequenceKeyframe
{
    public float Timestamp;
    public List<SequenceAction> Actions = new List<SequenceAction>();

    public SequenceKeyframe(float timestamp)
    {
        this.Timestamp = timestamp;
    }


    public override string ToString()
    {
        return $"Keyframe [{Timestamp.ToString("N2")}]n\t- {string.Join("\n\t- ", Actions)}";
    }
}

[Serializable]
public class Sequence
{
    public List<SequenceKeyframe> Keyframes = new List<SequenceKeyframe>();
}

[Serializable]
public class SequenceCollection
{
    public List<Sequence> Sequences = new List<Sequence>();
    public float TotalSeconds;
    public int Tracks;
}


public class TimelineExporter : MonoBehaviour
{
    public static string ExportTimeline(TimelineAsset timeline)
    {
        List<TrackAsset> tracks = new List<TrackAsset>(timeline.GetOutputTracks());
        SequenceCollection sequenceCollection = new SequenceCollection();

        foreach (TrackAsset track in tracks)
        {
            if (track is AnimationTrack)
            {
                AnimationTrack animationTrack = track as AnimationTrack;
                Vector3 positionOffset = animationTrack.position;
                Sequence sequence = new Sequence();
                sequence.Keyframes.AddRange(animationTrack.GetClips().SelectMany(ExtractKeyframes));
                sequence.Keyframes.AddRange(ExtractKeyframes(animationTrack.infiniteClip));
                sequence.Keyframes.Sort((x, y) => x.Timestamp.CompareTo(y.Timestamp));
                sequenceCollection.Sequences.Add(sequence);
            }
        }

        sequenceCollection.TotalSeconds = (float)timeline.duration;
        sequenceCollection.Tracks = sequenceCollection.Sequences.Count;
        string jsonText = JsonUtility.ToJson(sequenceCollection, true);
        return jsonText;
    }

    public static IEnumerable<SequenceKeyframe> ExtractKeyframes(TimelineClip clip)
    {
        return ExtractKeyframes(clip.animationClip, clip.start, clip.timeScale);
    }

    public static IEnumerable<SequenceKeyframe> ExtractKeyframes(AnimationClip clip, double startTime = 0.0, double timeScale = 1.0)
    {
        List<SequenceKeyframe> results = new List<SequenceKeyframe>();
        if (clip != null)
        {
            EditorCurveBinding[] curveBindings = AnimationUtility.GetCurveBindings(clip);
            Dictionary<float, Dictionary<string, float>> keyframeValues = new Dictionary<float, Dictionary<string, float>>();
            foreach (EditorCurveBinding curveBinding in curveBindings)
                ExtractKeyframeData(keyframeValues, clip, curveBinding);

            // PrintKeyframeData(keyframeValues);
            ConvertKeyframes(keyframeValues, results, (float)startTime, (float)timeScale);
        }

        return results;
    }

    private static void  ConvertKeyframes(Dictionary<float, Dictionary<string, float>> unityKeyframes, List<SequenceKeyframe> sequenceKeyframes, float clipStart, float clipSpeed)
    {
        float previousTime = Mathf.Max(clipStart, 0.0f);
        List<float> sortedTimestamps = new List<float>(unityKeyframes.Keys);
        sortedTimestamps.Sort();

        foreach(float timestamp in sortedTimestamps)
        {
            Dictionary<string, float> timestampValues = unityKeyframes[timestamp];
            float realTimestamp = clipStart + (timestamp / clipSpeed);
            float duration = realTimestamp - previousTime;

            SequenceKeyframe keyframe = new SequenceKeyframe(realTimestamp);
            
            if (HasMoveKeyframe(timestampValues))
                keyframe.Actions.Add(GetMoveStep(timestampValues, duration));

            if (HasLightKeyframe(timestampValues))
                keyframe.Actions.Add(GetLightStep(timestampValues, duration));

            previousTime = realTimestamp;
            sequenceKeyframes.Add(keyframe);
        }
    }

    private static void ExtractKeyframeData(Dictionary<float, Dictionary<string, float>> keyframeValues, AnimationClip clip, EditorCurveBinding curveBinding)
    {
        string propertyName = curveBinding.propertyName;
        AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, curveBinding);;
        for (int i = 0; i < curve.length; i++)
        {
            Keyframe keyframe = curve[i];
            float timestamp = keyframe.time;
            float value = keyframe.value;

            if (!keyframeValues.ContainsKey(timestamp))
                keyframeValues[timestamp] = new Dictionary<string, float>();

            Dictionary<string, float> timestampValues = keyframeValues[timestamp];
            timestampValues[propertyName] = value;
        }

    }
    private static void PrintKeyframeData(Dictionary<float, Dictionary<string, float>> keyframeValues)
    {
        string result = "";
        foreach (KeyValuePair<float, Dictionary<string, float>> outerPair in keyframeValues)
        {
            float timestamp = outerPair.Key;
            Dictionary<string, float> timestampValues = outerPair.Value;

            result += $"Timestamp: {timestamp.ToString("N2")}\n";
            foreach (KeyValuePair<string, float> pair in timestampValues)
            {
                result += $"\t- {pair.Key} -> {pair.Value.ToString("N2")}\n";
            }
        }
        Debug.Log(result);
    }

    private static bool HasMoveKeyframe(Dictionary<string, float> keyframeValues)
    {
        return keyframeValues.Keys.Any(propertyName => propertyName.Contains("LocalPosition"));
    }
    private static bool HasLightKeyframe(Dictionary<string, float> keyframeValues)
    {
        return keyframeValues.Keys.Any(propertyName => propertyName.Contains("LightColor"));
    }

    private static SequenceAction GetMoveStep(Dictionary<string, float> keyframeValues, float duration)
    {
        SequenceAction result = new SequenceAction();
        result.ActionType = SequenceActionType.Move;
        result.Duration = duration;
        result.Data = new Vector4
        (
             keyframeValues["m_LocalPosition.z"],
            -keyframeValues["m_LocalPosition.x"],
             keyframeValues["m_LocalPosition.y"],
             0.0f
        );
        return result;
    }

    private static SequenceAction GetLightStep(Dictionary<string, float> keyframeValues, float duration)
    {
        SequenceAction result = new SequenceAction();
        result.ActionType = SequenceActionType.Light;
        result.Duration = duration;
        result.Data = new Vector4
        (
            Mathf.RoundToInt(255 * keyframeValues["LightColor.r"]),
            Mathf.RoundToInt(255 * keyframeValues["LightColor.g"]),
            Mathf.RoundToInt(255 * keyframeValues["LightColor.b"]),
            0.0f
        );
        return result;
    }
}
