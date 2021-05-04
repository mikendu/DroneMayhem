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

    public static float UnitTetrahedronSmallRadius = Mathf.Sqrt(6.0f) / 12.0f;
    public static float UnitTetrahedronLargeRadius = Mathf.Sqrt(6.0f) / 4.0f;
    public static float UnitTetrahedronHeight = Mathf.Sqrt(6.0f) / 3.0f;

    public static float UnitOctahedronHeight = 1.0f / Mathf.Sqrt(2.0f);

    public static string Format(this ShapeType shape)
    {
        switch (shape)
        {
            case ShapeType.TrianglePyramid:
                return "Triangle Pyramid";

            case ShapeType.SquarePyramid:
                return "Square Pyramid";
        }
        return shape.ToString();
    }
}