using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Timeline;

public static class KeyframeUtil
{

    public static Color GetColor(List<ColorKeyframe> keyframes, double time, Color defaultColor)
    {
        for (int i = 0; i < keyframes.Count - 1; i++)
        {
            ColorKeyframe keyframe = keyframes[i];
            ColorKeyframe nextKeyframe = keyframes[i + 1];

            if (time >= keyframe.time && time < nextKeyframe.time)
            {
                double duration = (nextKeyframe.time - keyframe.time);
                double interpolationValue = (time - keyframe.time) / duration;
                return Color.Lerp(keyframe.LightColor, nextKeyframe.LightColor, Mathf.Clamp01((float)interpolationValue));
            }
        }

        if (keyframes.Count > 0)
        {
            if (time < keyframes[0].time)
                return keyframes[0].LightColor;
            else
                return keyframes[keyframes.Count - 1].LightColor;
        }

        return defaultColor;
    }

    public static Vector3 GetPosition(List<PositionKeyframe> keyframes, double time, Vector3 defaultPosition)
    {
        for (int i = 0; i < keyframes.Count - 1; i++)
        {
            PositionKeyframe keyframe = keyframes[i];
            PositionKeyframe nextKeyframe = keyframes[i + 1];

            if (time >= keyframe.time && time < nextKeyframe.time)
            {
                double duration = (nextKeyframe.time - keyframe.time);
                double interpolationValue = (time - keyframe.time) / duration;
                return EvaluateBezier(keyframe, nextKeyframe, Mathf.Clamp01((float)interpolationValue));
            }
        }

        if (keyframes.Count > 0)
        {
            if (time < keyframes[0].time)
                return keyframes[0].Position;
            else
                return keyframes[keyframes.Count - 1].Position;
        }

        return defaultPosition;
    }


    private static Vector3 EvaluateBezier(PositionKeyframe currentKeyframe, PositionKeyframe nextKeyframe, float interpolation)
    {
        bool linearStart = (currentKeyframe.JointType == JointType.Linear);
        bool linearEnd = (nextKeyframe.JointType == JointType.Linear);

        Vector3 startPos = currentKeyframe.Position;
        Vector3 endPos = nextKeyframe.Position;
        Vector3 startTangent = linearStart ? startPos : startPos + currentKeyframe.StartTangent;
        Vector3 endTangent = linearEnd ? endPos : endPos + currentKeyframe.EndTangent;

        Vector3 quadOne = EvaluateQuadratic(startPos, startTangent, endTangent, interpolation);
        Vector3 quadTwo = EvaluateQuadratic(startTangent, endTangent, endPos, interpolation);
        return Vector3.Lerp(quadOne, quadTwo, interpolation);
    }

    private static Vector3 EvaluateQuadratic(Vector3 q0, Vector3 q1, Vector3 q2, float segmentTime)
    {
        Vector3 lineOne = Vector3.Lerp(q0, q1, segmentTime);
        Vector3 lineTwo = Vector3.Lerp(q1, q2, segmentTime);
        return Vector3.Lerp(lineOne, lineTwo, segmentTime);
    }
}