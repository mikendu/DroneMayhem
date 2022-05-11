using System.Collections.Generic;
using UnityEngine;

public static class BezierMath
{
    public static List<Vector3> EnforceContinuity(List<Vector3> unscaled, float time)
    {
        List<Vector3> scaled = new List<Vector3>();

        float oneMinus = 1.0f - time;
        Vector3 q0 = unscaled[0];
        Vector3 q1 = unscaled[1];
        Vector3 q2 = unscaled[2];
        Vector3 q3 = unscaled[3];
        Vector3 p4 = unscaled[4];
        Vector3 p5 = unscaled[5];
        Vector3 p6 = unscaled[6];
        Vector3 p7 = unscaled[7];


        scaled.Add(q0);
        scaled.Add((oneMinus * q0) + (time * q1));
        scaled.Add((Mathf.Pow(oneMinus, 2) * q0) + (2 * oneMinus * time * q1) + (Mathf.Pow(time, 2) * q2));
        scaled.Add((Mathf.Pow(oneMinus, 3) * q0) + (3 * Mathf.Pow(oneMinus, 2) * time * q1)
            + (3 * oneMinus * Mathf.Pow(time, 2) * q2) + (Mathf.Pow(time, 3) * q3));


        scaled.Add((Mathf.Pow(oneMinus, 3) * p7) + (3 * Mathf.Pow(oneMinus, 2) * time * p6)
            + (3 * oneMinus * Mathf.Pow(time, 2) * p5) + (Mathf.Pow(time, 3) * p4));
        scaled.Add((Mathf.Pow(oneMinus, 2) * p7) + (2 * oneMinus * time * p6) + (Mathf.Pow(time, 2) * p5));
        scaled.Add((oneMinus * p7) + (time * p6));
        scaled.Add(p7);
        return scaled;
    }

    public static float[][] ToPolynomial(List<Vector3> points)
    {
        float[][] coefficients = new float[][] { new float[8], new float[8], new float[8] };
        for (int d = 0; d < 3; d++)
        {
            for (int j = 0; j < 8; j++)
            {
                float s = 0.0f;
                for (int i = 0; i < j + 1; i++)
                {
                    s += Mathf.Pow(-1, i + j) * GetDimension(points, i, d) / (Factorial(i) * Factorial(j - i));
                }

                float c = s * Factorial(7) / Factorial(7 - j);
                coefficients[d][j] = c;
            }
        }
        return coefficients;
    }

    public static float[][] ScalePolynomial(float[][] coefficients, float time)
    {
        float[][] scaledCoefficients = new float[][] { new float[8], new float[8], new float[8] };
        float reciprocal = 1.0f / time;

        for (int d = 0; d < 3; d++)
        {
            float scale = 1.0f;
            for (int j = 0; j < 8; j++)
            {
                scaledCoefficients[d][j] = (scale * coefficients[d][j]);
                scale *= reciprocal;
            }
        }
        return scaledCoefficients;
    }



    private static int Factorial(int i)
    {
        if (i <= 1)
            return 1;
        else
            return i * Factorial(i - 1);
    }

    private static float GetDimension(List<Vector3> items, int index, int dimension)
    {
        Vector3 item = items[index];
        switch (dimension)
        {
            case 0:
                return item.x;
            case 1:
                return item.y;
            case 2:
                return item.z;
            default:
                return 0;
        }
    }
}