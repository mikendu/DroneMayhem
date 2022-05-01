using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

class CoefficientSolver
{

    public static float[] Unscale(float[] coefficients, int offset, int degree, float duration)
    {
        float[] unscaledCoefficients = new float[degree + 1];
        float power = 1;
        for (int i = 0; i <= degree; i++)
        {
            unscaledCoefficients[i] = coefficients[offset + i] * power;
            power *= duration;
        }

        return unscaledCoefficients;

    }

    public static float[] Solve(float[] unscaledCoefficients, int degree)
    {
        float[] results = new float[degree + 1];
        results[0] = unscaledCoefficients[0];

        for (int i = 1; i <= degree; i++)
        {
            float levelMultiplier = PascalTriangle.Get(degree, i);
            float levelSign = Mathf.Pow(-1, (i - 1));
            float currentResult = 0.0f;

            for (int j = 0; j < i; j++)
            {
                float itemSign = Mathf.Pow(-1, j) * levelSign;
                float itemMultiplier = PascalTriangle.Get(i, j);
                float term = results[j] * itemMultiplier * itemSign;
                currentResult += term;
            }

            // last term is always constant / level mult & always positive
            currentResult += unscaledCoefficients[i] / levelMultiplier;
            results[i] = currentResult;
        }

        return results;
    }
}