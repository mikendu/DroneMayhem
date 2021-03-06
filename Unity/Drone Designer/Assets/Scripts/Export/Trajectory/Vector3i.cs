using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class Vector3i
{
    public short x, y, z;

    public Vector3i(short x, short y, short z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public Vector3i(Vector3 vector)
    {
        this.x = (short)Mathf.RoundToInt(vector.x);
        this.y = (short)Mathf.RoundToInt(vector.y);
        this.z = (short)Mathf.RoundToInt(vector.z);
    }

    public static implicit operator Vector3(Vector3i v) => new Vector3(v.x, v.y, v.z);
    public static implicit operator Vector3i(Vector3 v) => new Vector3i(v);

    public short Get(TrajectoryExporter.AxisType axis)
    {
        switch (axis)
        {
            case TrajectoryExporter.AxisType.X:
                return x;

            case TrajectoryExporter.AxisType.Y:
                return y;

            case TrajectoryExporter.AxisType.Z:
                return z;
        }

        return 0;
    }
}