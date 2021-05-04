using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Timeline;

public static class KeyframeUtil
{
    public class InterpolationSet<T>
    {
        public T first;
        public T second;
        public float value;

        public InterpolationSet(T first, T second, float value)
        {
            this.first = first;
            this.second = second;
            this.value = value;
        }
    }

    public static int KeyframeComparator(IMarker markerOne, IMarker markerTwo)
    {
        return markerOne.time.CompareTo(markerTwo.time);
    }

    public static InterpolationSet<T> Interpolate<T>(List<T> keyframes, double time, bool clamp = false) where T : Marker
    {
        for (int i = 0; i < keyframes.Count - 1; i++)
        {
            T keyframe = keyframes[i];
            T nextKeyframe = keyframes[i + 1];

            if (time >= keyframe.time && time < nextKeyframe.time)
            {
                double duration = (nextKeyframe.time - keyframe.time);
                double interpolationValue = (time - keyframe.time) / duration;
                return new InterpolationSet<T>(keyframe, nextKeyframe, Mathf.Clamp01((float)interpolationValue));
            }
        }

        if (clamp)
        {
            T result = (time < keyframes[0].time) ? keyframes[0] : keyframes[keyframes.Count - 1];
            return new InterpolationSet<T>(result, result, 0);
        }

        return null;
    }


    public static Color GetColor(List<ColorKeyframe> keyframes, double time, Color defaultColor)
    {
        InterpolationSet<ColorKeyframe> interpolationData = Interpolate(keyframes, time);
        if (interpolationData != null)
            return Color.Lerp(interpolationData.first.LightColor, interpolationData.second.LightColor, interpolationData.value);

        if (keyframes.Count > 0)
        {
            if (time < keyframes[0].time)
                return keyframes[0].LightColor;
            else
                return keyframes[keyframes.Count - 1].LightColor;
        }

        return defaultColor;
    }

    public static Vector3 GetPosition(List<Waypoint> keyframes, double time, Vector3 defaultPosition)
    {
        Vector3 position = FindPosition(keyframes, time, defaultPosition);
        return GlobalTransform.Transfomed(position);
    }

    private static Vector3 FindPosition(List<Waypoint> keyframes, double time, Vector3 defaultPosition)
    {
        InterpolationSet<Waypoint> interpolationData = Interpolate(keyframes, time);
        if (interpolationData != null)
            return EvaluateBezier(interpolationData.first, interpolationData.second, interpolationData.value);

        if (keyframes.Count > 0)
        {
            if (time < keyframes[0].time)
                return keyframes[0].Position;
            else
                return keyframes[keyframes.Count - 1].Position;
        }

        return defaultPosition;
    }

    public static Vector3 GetTangent(List<Waypoint> keyframes, double time, bool normalize = false, bool handleCriticalPoints = false)
    {
        if (keyframes.Count == 0)
            return new Vector3(0.25f, 0, 0);

        Vector3 tangent = Vector3.zero;
        InterpolationSet<Waypoint> interpolationData = Interpolate(keyframes, time);
        if (interpolationData != null)
            return CalculateTangent(interpolationData.first, interpolationData.second, interpolationData.value, normalize);

        if (handleCriticalPoints && Mathf.Approximately(tangent.magnitude, 0.0f))
        {
            float maxTime = (float)keyframes[keyframes.Count - 1].time;
            float timef = Mathf.Clamp((float)time, 0.0f, maxTime);
            float startTime = Mathf.Clamp(timef - 0.1f, 0.0f, maxTime);
            float endTime = Mathf.Clamp(timef + 0.1f, 0.0f, maxTime);

            Vector3 startPosition = GetPosition(keyframes, startTime, Vector3.zero);
            Vector3 endPosition = GetPosition(keyframes, endTime, Vector3.zero);
            Vector3 delta = (endPosition - startPosition);
            tangent = normalize ? delta.normalized : delta;
        }

        return tangent;
    }

    private static Vector3 CalculateTangent(Waypoint currentKeyframe, Waypoint nextKeyframe, float interpolation, bool normalize = false)
    {
        bool linearStart = (currentKeyframe.JointType == JointType.Linear);
        bool linearEnd = (nextKeyframe.JointType == JointType.Linear);

        Vector3 startPos = GlobalTransform.Transfomed(currentKeyframe.Position);
        Vector3 endPos = GlobalTransform.Transfomed(nextKeyframe.Position);
        Vector3 startTangent = linearStart ? startPos : GlobalTransform.Transfomed(currentKeyframe.WorldTangent);
        Vector3 endTangent = linearEnd ? endPos : GlobalTransform.Transfomed(nextKeyframe.InverseWorldTangent);

        // dP(t) / dt =  (-3(1-t)^2 * P0) + (3(1-t)^2 * P1) + (-6t(1-t) * P1) + (-3t^2 * P2) + (6t(1-t) * P2) + (3t^2 * P3) 
        float inverse = 1.0f - interpolation;
        float C0 = -3.0f * Mathf.Pow(inverse, 2.0f);
        float C1 = 3.0f * Mathf.Pow(inverse, 2.0f);
        float C2 = -6.0f * inverse * interpolation;
        float C3 = -3.0f * Mathf.Pow(interpolation, 2.0f);
        float C4 = 6.0f * interpolation * inverse;
        float C5 = 3.0f * Mathf.Pow(interpolation, 2.0f);


        Vector3 result = (C0 * startPos) + ((C1 + C2) * startTangent) + ((C3 + C4) * endTangent) + (C5 * endPos);
        return normalize ? result.normalized : result;
    }

    private static Vector3 EvaluateBezier(Waypoint currentKeyframe, Waypoint nextKeyframe, float interpolation)
    {
        bool linearStart = (currentKeyframe.JointType == JointType.Linear);
        bool linearEnd = (nextKeyframe.JointType == JointType.Linear);

        Vector3 startPos = currentKeyframe.Position;
        Vector3 endPos = nextKeyframe.Position;
        Vector3 startTangent = linearStart ? startPos : startPos + currentKeyframe.Tangent;
        Vector3 endTangent = linearEnd ? endPos : endPos - nextKeyframe.Tangent;

        // P(t) = (1 - t)^3 * P0 + 3t(1-t)^2 * P1 + 3t^2 (1-t) * P2 + t^3 * P3
        float inverse = 1.0f - interpolation;
        float C0 = Mathf.Pow(inverse, 3.0f);
        float C1 = 3.0f * interpolation * Mathf.Pow(inverse, 2.0f);
        float C2 = 3.0f * inverse * Mathf.Pow(interpolation, 2.0f);
        float C3 = Mathf.Pow(interpolation, 3.0f);

        Vector3 result = (C0 * startPos) + (C1 * startTangent) + (C2 * endTangent) + (C3 * endPos);
        return result;
    }
}