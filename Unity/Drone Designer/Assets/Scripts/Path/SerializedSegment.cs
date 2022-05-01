using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class SerializedSegment
{
    public float[] data;

    public BezierSegment Expand()
    {
        int degree = ((data.Length - 1) / 4) - 1;
        int numPoints = degree + 1;
        float duration = data[0];

        List<float[]> unscaledCoefficients = new List<float[]>() { null, null, null, null };
        List<float[]> controlPoints = new List<float[]>() { null, null, null, null };

        for (int i = 0; i < 4; i++)
        {
            int offset = (numPoints * i) + 1;
            float[] unscaled = CoefficientSolver.Unscale(data, offset, degree, duration);
            float[] points = CoefficientSolver.Solve(unscaled, degree);

            int index = IndexFromDroneToUnity(i);
            unscaledCoefficients[index] = unscaled;
            controlPoints[index] = points;
        }

        return new BezierSegment(degree, duration, unscaledCoefficients, controlPoints);
    }


    private static int IndexFromDroneToUnity(int index)
    {
        // Crazyflie X (Unity Z)
        if (index == 0)
        {
            return 2;
        }

        // Crazyflie Y (Unity -X)
        if (index == 1)
        {
            return 0;
        }

        // Crazyflie Z (Unity Y)
        if (index == 2)
        {
            return 1;
        }

        return index;
    }
}