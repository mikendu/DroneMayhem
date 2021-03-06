using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public static class VectorExtensions
{
    public static float Get(this Vector3 vector, TrajectoryExporter.AxisType axis)
    {
        switch(axis)
        {
            case TrajectoryExporter.AxisType.X:
                return vector.x;

            case TrajectoryExporter.AxisType.Y:
                return vector.y;

            case TrajectoryExporter.AxisType.Z:
                return vector.z;
        }

        return 0;
    }
}