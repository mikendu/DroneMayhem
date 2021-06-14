using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public static class BezierEvaluator
{
    public static float RescaleTime(CubicBezier bezier, float targetSpeed)
    {
        float lastTime = bezier.startTime;
        Vector3 lastPosition = bezier.anchor1;
        float peakVelocity = 0.0f;

        for (float t = 0.0f; t < 1.0f; t += 0.01f)
        {
            Vector3 newPosition = EvaluateBezier(bezier, t);
            float newTime = Mathf.Lerp(bezier.startTime, bezier.endTime, t);

            float distance = (newPosition - lastPosition).magnitude;
            float time = newTime - lastTime;

            lastPosition = newPosition;
            lastTime = newTime;

            float velocity = distance / time;
            peakVelocity = Mathf.Max(velocity, peakVelocity);
        }

        //Debug.Log("Start: " + bezier.startTime + ", end: " + bezier.endTime + ",  Peak velocity: " + peakVelocity);
        return peakVelocity / targetSpeed;
    }

    public static float Integrate(CubicBezier bezier, float targetSpeed, float timestep = 0.01f)
    {
        float totalTime = 0.0f;
        float desiredMoveDistance = targetSpeed * timestep;

        float t = timestep;
        Vector3 v1 = (-3 * bezier.anchor1) + (9 * bezier.control1) + (-9 * bezier.control2) + (3 * bezier.anchor2);
        Vector3 v2 = (6 * bezier.anchor1) + (-12 * bezier.control1) + (6 * bezier.control2);
        Vector3 v3 = (-3 * bezier.anchor1) + (3 * bezier.control1);

        float distanceMoved = 0.0f;
        while (t < 1.0f)
        {
            Vector3 moveVector = (Mathf.Pow(t, 2) * v1) + (t * v2) + v3;
            Debug.Log("Move vector: " + moveVector);
            float moveAmount = desiredMoveDistance / moveVector.magnitude;
            Debug.Log("T move amount: " + moveAmount + ", desired move dist: " + desiredMoveDistance);
            t += moveAmount;
            distanceMoved += desiredMoveDistance;
        }
        totalTime = distanceMoved / targetSpeed;

        /*
        Vector3 lastPosition = bezier.anchor1;

        for (float time = 0.0f; time <= 1.0f; time += timestep)
        {
            Vector3 newPosition = EvaluateBezier(bezier, time);
            float distance = (newPosition - lastPosition).magnitude;
            lastPosition = newPosition;

            float travelTime = distance / targetSpeed;
            totalTime += travelTime;
        }
        */
        return totalTime;
    }

    public static Tuple<CubicBezier, float> Interpolate(List<CubicBezier> curves, float time)
    {
        bool found = false;
        CubicBezier curve = new CubicBezier();
        float value;

        foreach(CubicBezier currentCurve in curves)
        {
            if (time >= currentCurve.startTime && time < currentCurve.endTime)
            {
                found = true;
                value = Mathf.InverseLerp(currentCurve.startTime, currentCurve.endTime, time);
                curve = currentCurve;
                break;
            }
        }

        if (!found)
            curve = (time < curves[0].startTime) ? curves[0] : curves[curves.Count - 1];

        value = Mathf.InverseLerp(curve.startTime, curve.endTime, time);
        return new Tuple<CubicBezier, float>(curve, Mathf.Clamp01(value));
    }

    public static Vector3 FindPosition(List<CubicBezier> curves, float time)
    {
        Tuple<CubicBezier, float> interpolationData = Interpolate(curves, time);
        return EvaluateBezier(interpolationData.Item1, interpolationData.Item2);
    }
    private static Vector3 EvaluateBezier(CubicBezier curve, float t)
    {
        Vector3 startPos = curve.anchor1;
        Vector3 endPos = curve.anchor2;
        Vector3 startTangent = curve.control1;
        Vector3 endTangent = curve.control2;

        // P(t) = (1 - t)^3 * P0 + 3t(1-t)^2 * P1 + 3t^2 (1-t) * P2 + t^3 * P3
        float inverse = 1.0f - t;
        float C0 = Mathf.Pow(inverse, 3.0f);
        float C1 = 3.0f * t * Mathf.Pow(inverse, 2.0f);
        float C2 = 3.0f * inverse * Mathf.Pow(t, 2.0f);
        float C3 = Mathf.Pow(t, 3.0f);

        Vector3 result = (C0 * startPos) + (C1 * startTangent) + (C2 * endTangent) + (C3 * endPos);
        return result;
    }

    private static Vector3 Differentiate(CubicBezier bezier, float t)
    {
        Vector3 startPos = bezier.anchor1;
        Vector3 endPos = bezier.anchor2;
        Vector3 startTangent = bezier.control1;
        Vector3 endTangent = bezier.control2;

        // dP(t) / dt =  (-3(1-t)^2 * P0) + (3(1-t)^2 * P1) + (-6t(1-t) * P1) + (-3t^2 * P2) + (6t(1-t) * P2) + (3t^2 * P3) 
        float inverse = 1.0f - t;
        float C0 = -3.0f * Mathf.Pow(inverse, 2.0f);
        float C1 = 3.0f * Mathf.Pow(inverse, 2.0f);
        float C2 = -6.0f * inverse * t;
        float C3 = -3.0f * Mathf.Pow(t, 2.0f);
        float C4 = 6.0f * t * inverse;
        float C5 = 3.0f * Mathf.Pow(t, 2.0f);


        Vector3 result = (C0 * startPos) + ((C1 + C2) * startTangent) + ((C3 + C4) * endTangent) + (C5 * endPos);
        return result;
    }
}