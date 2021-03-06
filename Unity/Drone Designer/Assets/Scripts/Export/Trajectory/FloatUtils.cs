using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public static class FloatUtils
{
    public const float EPSILON = 0.001f;

    public static bool ApproximatelyEqual(float first, float second)
    {
        return Mathf.Abs(first - second) <= EPSILON;
    }
}