using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public static class TrajectoryExporter
{
    public enum AxisType { X, Y, Z };

    public static byte[] Process(List<Waypoint> waypoints)
    {
        if (waypoints.Count == 0)
            return new byte[0];

        List<byte> results = new List<byte>();
        GetStartingPosition(results, waypoints);

        for(int i = 0; i < waypoints.Count - 1; i++)
        {
            Waypoint first = waypoints[i];
            Waypoint second = waypoints[i + 1];
            ProcessSegment(results, first, second);
        }

        return results.ToArray();
    }

    private static void GetStartingPosition(List<byte> results, List<Waypoint> waypoints)
    {
        Vector3i start = ConvertPosition(waypoints[0].Position);
        ByteUtils.Pack(results, start.x, true);
        ByteUtils.Pack(results, start.y, true);
        ByteUtils.Pack(results, start.z, true);
        ByteUtils.Pack(results, 0, true);           // yaw
    }

    private static void ProcessSegment(List<byte> results, Waypoint first, Waypoint second)
    {
        // -- Header -- //

        /* Dimensions are swapped to account for difference between
         * Unity and crazyflie coordinate systems
         */
        BezierDegree xDegree = GetDegree(first, second, AxisType.Z);
        BezierDegree yDegree = GetDegree(first, second, AxisType.X);
        BezierDegree zDegree = GetDegree(first, second, AxisType.Y);
        BezierDegree yawDegree = BezierDegree.Constant;

        byte curveFormat = ByteUtils.Coalesce(yawDegree.Value(), zDegree.Value(), yDegree.Value(), xDegree.Value());
        results.Add(curveFormat);

        short duration = GetDuration(first, second);
        ByteUtils.Pack(results, duration, true);


        // -- Body -- //

        PackControlPoints(results, first, second, AxisType.X, xDegree);
        PackControlPoints(results, first, second, AxisType.Y, yDegree);
        PackControlPoints(results, first, second, AxisType.Z, zDegree);
    }

    private static void PackControlPoints(List<byte> data, Waypoint first, Waypoint second, AxisType axis, BezierDegree degree)
    {
        switch(degree)
        {
            case BezierDegree.Constant:
                return;

            case BezierDegree.Linear:
                {
                    short endPosition = ConvertPosition(second.Position).Get(axis);
                    ByteUtils.Pack(data, endPosition, true);
                    break;
                }

            case BezierDegree.Cubic:
                {
                    short startTangent = ConvertPosition(first.WorldTangent).Get(axis);
                    short endTangent = ConvertPosition(second.InverseWorldTangent).Get(axis);
                    short endPosition = ConvertPosition(second.Position).Get(axis);
                    ByteUtils.Pack(data, startTangent, true);
                    ByteUtils.Pack(data, endTangent, true);
                    ByteUtils.Pack(data, endPosition, true);
                    break;
                }
        }
    }

    private static BezierDegree GetDegree(Waypoint first, Waypoint second, AxisType axis)
    {
        float firstPos = first.Position.Get(axis);
        float secondPos = second.Position.Get(axis);

        Vector3 startTangent = first.Tangent;
        Vector3 endTangent = -second.Tangent;

        float startTangentValue = (first.JointType == JointType.Linear) ? 0.0f : startTangent.Get(axis);
        float endTangentValue = (second.JointType == JointType.Linear) ? 0.0f : endTangent.Get(axis);

        bool linearStart = Mathf.Approximately(startTangentValue, 0.0f);
        bool linearEnd = Mathf.Approximately(endTangentValue, 0.0f);

        if (linearStart && linearEnd)
            return Mathf.Approximately(firstPos, secondPos) ? BezierDegree.Constant : BezierDegree.Linear;

        return BezierDegree.Cubic;
    }

    private static short GetDuration(Waypoint first, Waypoint second)
    {
        float timeDifference = (float)(second.time - first.time);
        return (short)Mathf.RoundToInt(timeDifference * 1000);
    }

    private static Vector3i ConvertPosition(Vector3 position)
    {
        return new Vector3i(1000 * position.ToCrazyflieCoordinates());
    }
}