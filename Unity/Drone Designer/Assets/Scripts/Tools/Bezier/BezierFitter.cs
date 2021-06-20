using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public static class BezierFitter
{
    public static List<CubicBezier> Fit(List<Vector3> points, float threshold, float startTime = 0.0f, float endTime = 1.0f)
    {
        // Approximate -> CubicBezer
        // Calculate error per point, keeping track of max value & index
        // Sum to see if break is necessary
        // if necessary
            // Find ratio of start & end times, 
            // Break up based on criitical point (may need to overlap the critical vertex)
            // Recurse on two new halves
            // Add results to the list
            // Yield the list
        // else
            // add the single curve to the list
            // yield the list

        List<CubicBezier> curves = new List<CubicBezier>();

        CubicBezier curve = Approximate(points, startTime, endTime);
        Tuple<float, int> errorMetrics = GetErrorMetrics(curve, points);

        float errorSize = errorMetrics.Item1;
        int maxIndex = errorMetrics.Item2;

        if (errorSize > threshold)
        {
            float ratio = maxIndex / ((float)points.Count - 1);
            float midpoint = Mathf.Lerp(startTime, endTime, ratio);
            int remainderCount = points.Count - maxIndex;

            curves.AddRange(Fit(points.GetRange(0, maxIndex + 1), threshold, startTime, midpoint));
            curves.AddRange(Fit(points.GetRange(maxIndex, remainderCount), threshold, midpoint, endTime));    
        }
        else
        {
            curves.Add(curve);
        }


        return curves;
    }


    public static CubicBezier Approximate(List<Vector3> points)
    {
        return Approximate(points, 0.0f, 1.0f);
    }

    private static CubicBezier Approximate(List<Vector3> points, float startTime, float endTime)
    {
        int n = points.Count;
        CubicBezier curve = new CubicBezier();

        curve.anchor1 = points[0];
        curve.anchor2 = points[n - 1];

        float a1, a2, a12;
        Vector3 c1, c2;

        a1 = a2 = a12 = 0;
        c1 = c2 = Vector3.zero;

        float interval = 1.0f / (n - 1);

        for(int i = 0; i <= (n - 1); i++)
        {
            float t = i * interval;
            float tSquared = (t * t);
            float tCubed = tSquared * t;
            float tFourth = tCubed * t;

            float oneMinusT = 1.0f - t;
            float oneMinusTSquared = oneMinusT * oneMinusT;
            float oneMinusTCubed = oneMinusTSquared * oneMinusT;
            float oneMinusTFourth = oneMinusTCubed * oneMinusT;

            a1 += tSquared * oneMinusTFourth;
            a2 += tFourth * oneMinusTSquared;
            a12 += tCubed * oneMinusTCubed;

            Vector3 point = points[i] - (oneMinusTCubed * curve.anchor1) - (tCubed * curve.anchor2);
            c1 += (3.0f * t * oneMinusTSquared) * point;
            c2 += (3.0f * tSquared * oneMinusT) * point;
        }

        a1 *= 9.0f;
        a2 *= 9.0f;
        a12 *= 9.0f;

        float divisor = ((a1 * a2) - (a12 * a12));

        curve.control1 = ((a2 * c1) - (a12 * c2)) / divisor;
        curve.control2 = ((a1 * c2) - (a12 * c1)) / divisor;

        curve.startTime = startTime;
        curve.endTime = endTime;
        return curve;
    }

    private static Tuple<float, int> GetErrorMetrics(CubicBezier bezier, List<Vector3> points)
    {
        int n = points.Count;
        float interval = 1.0f / (n - 1);

        float maxError = float.MinValue;
        float errorSum = 0.0f;
        int maxIndex = -1;

        for (int i = 0; i <= (n - 1); i++)
        {
            float t = i * interval;
            Vector3 errorVector = points[i] - BezierEvaluator.EvaluateBezier(bezier, t);
            float error = errorVector.sqrMagnitude;

            errorSum += error;
            if (error > maxError)
            {
                maxIndex = i;
                maxError = error;
            }
        }

        return new Tuple<float, int>(errorSum, maxIndex);
    }

}