using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public static class ShapeUtils
{
    public static float UnitTriangleSmallRadius = 0.5f * Mathf.Tan(Mathf.Deg2Rad * 30);
    public static float UnitTriangleLargeRadius = 0.5f / Mathf.Cos(Mathf.Deg2Rad * 30);
}