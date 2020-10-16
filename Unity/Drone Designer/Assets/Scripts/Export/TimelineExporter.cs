using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;
using System.Linq;
using UnityEditor;
using System;

public enum SequenceAction
{
    Move,
    Light
}

[Serializable]
public struct SequenceStep
{
    public SequenceAction Action;
    public float Timestamp;
    public float Duration;
    public Vector4 Data;

    public override string ToString()
    {
        return $"{Action} | {Timestamp.ToString("N2")} | {Duration.ToString("N2")} | {Data}";
    }
}

[Serializable]
public class SequenceWrapper
{
    public List<SequenceStep> Sequence = new List<SequenceStep>();
}

[Serializable]
public class SequenceCollection
{
    public List<SequenceWrapper> Sequences = new List<SequenceWrapper>();
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
                SequenceWrapper sequenceWrapper = new SequenceWrapper();
                sequenceWrapper.Sequence.AddRange(animationTrack.GetClips().SelectMany(ExtractSteps));
                sequenceWrapper.Sequence.AddRange(ExtractSteps(animationTrack.infiniteClip));
                sequenceWrapper.Sequence.Sort((x, y) => x.Timestamp.CompareTo(y.Timestamp));
                sequenceCollection.Sequences.Add(sequenceWrapper);
            }
        }
        
        string jsonText = JsonUtility.ToJson(sequenceCollection, true);
        return jsonText;
    }

    public static IEnumerable<SequenceStep> ExtractSteps(TimelineClip clip)
    {
        return ExtractSteps(clip.animationClip, clip.start, clip.timeScale);
    }

    public static IEnumerable<SequenceStep> ExtractSteps(AnimationClip clip, double startTime = 0.0, double timeScale = 1.0)
    {
        List<SequenceStep> results = new List<SequenceStep>();
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

    private static void  ConvertKeyframes(Dictionary<float, Dictionary<string, float>> keyframes, List<SequenceStep> sequence, float clipStart, float clipSpeed)
    {
        float previousTime = Mathf.Max(clipStart, 0.0f);
        foreach(float timestamp in keyframes.Keys)
        {
            Dictionary<string, float> timestampValues = keyframes[timestamp];
            float realTimestamp = clipStart + (timestamp / clipSpeed);
            float duration = realTimestamp - previousTime;
            
            if (HasMoveKeyframe(timestampValues))
                sequence.Add(GetMoveStep(timestampValues, realTimestamp, duration));

            if (HasLightKeyframe(timestampValues))
                sequence.Add(GetLightStep(timestampValues, realTimestamp, duration));

            previousTime = realTimestamp;
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

    private static SequenceStep GetMoveStep(Dictionary<string, float> keyframeValues, float timestamp, float duration)
    {
        SequenceStep result = new SequenceStep();
        result.Action = SequenceAction.Move;
        result.Timestamp = timestamp;
        result.Duration = duration;

        // TODO - Coordinate system conversion
        result.Data = new Vector4
        (
            keyframeValues["m_LocalPosition.x"],
            keyframeValues["m_LocalPosition.y"],
            keyframeValues["m_LocalPosition.z"],
            0.0f
        );
        return result;
    }

    private static SequenceStep GetLightStep(Dictionary<string, float> keyframeValues, float timestamp, float duration)
    {
        SequenceStep result = new SequenceStep();
        result.Action = SequenceAction.Light;
        result.Timestamp = timestamp;
        result.Duration = duration;

        // TODO - [0, 1] -> [0, 255]
        result.Data = new Vector4
        (
            keyframeValues["LightColor.r"],
            keyframeValues["LightColor.g"],
            keyframeValues["LightColor.b"],
            0.0f
        );
        return result;
    }
}
