using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

static class PascalTriangle
{
    private static float[,] triangle = new float[8, 8]
    {
        { 1, 0, 0, 0, 0, 0, 0, 0 },
        { 1, 1, 0, 0, 0, 0, 0, 0 },
        { 1, 2, 1, 0, 0, 0, 0, 0 },
        { 1, 3, 3, 1, 0, 0, 0, 0 },
        { 1, 4, 6, 4, 1, 0, 0, 0 },
        { 1, 5, 10, 10, 5, 1, 0, 0 },
        { 1, 6, 15, 20, 15, 6, 1, 0 },
        { 1, 7, 21, 35, 35, 21, 7, 1 }
    };

    private static readonly float[,] DegreeSeven = GetCoefficients(7);


    public static float Get(int degree, int index)
    {
        if (degree >= 0 && degree < 8 
            && index >= 0 && index < 8)
        {
            return triangle[degree, index];
        }
        return 0;
    }

    public static float[,] GetCoefficients(int degree)
    {
        if (degree == 7 && DegreeSeven != null)
            return DegreeSeven;

        float[,] coefficients = new float[degree + 1, degree + 1];

        for (int i = 0; i <= degree; i++)
        {
            float levelMultiplier = PascalTriangle.Get(degree, i);
            float levelSign = Mathf.Pow(-1, i);

            for (int j = 0; j <= i; j++)
            {
                float itemMultiplier = PascalTriangle.Get(i, j) * levelMultiplier;
                float itemSign = Mathf.Pow(-1, j) * levelSign;
                coefficients[i, j] = itemSign * itemMultiplier;
            }
        }
        return coefficients;
    }

    public static float[,] DegreeSevenCoefficients { get { return DegreeSeven;  } }
}