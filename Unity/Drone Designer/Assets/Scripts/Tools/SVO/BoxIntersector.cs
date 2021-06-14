using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public static class BoxIntersector
{
    public static Tuple<Vector3, Vector3> Intersect(Vector3 origin, Vector3 direction, Bounds bounds)
    {
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;

        float tmin = (min.x - origin.x) / direction.x;
        float tmax = (max.x - origin.x) / direction.x;

        if (tmin > tmax) Swap(ref tmin, ref tmax);

        float tymin = (min.y - origin.y) / direction.y;
        float tymax = (max.y - origin.y) / direction.y;

        if (tymin > tymax) Swap(ref tymin, ref tymax);

        if ((tmin > tymax) || (tymin > tmax))
            return null;

        if (tymin > tmin)
            tmin = tymin;

        if (tymax < tmax)
            tmax = tymax;

        float tzmin = (min.z - origin.z) / direction.z;
        float tzmax = (max.z - origin.z) / direction.z;

        if (tzmin > tzmax) Swap(ref tzmin, ref tzmax);

        if ((tmin > tzmax) || (tzmin > tmax))
            return null;

        if (tzmin > tmin)
            tmin = tzmin;

        if (tzmax < tmax)
            tmax = tzmax;

        Vector3 hit1 = origin + (tmin * direction);
        Vector3 hit2 = origin + (tmax * direction);

        return new Tuple<Vector3, Vector3>(hit1, hit2);
    }

    private static void Swap(ref float a, ref float b)
    {
        float temp = a;
        a = b;
        b = temp;
    }

}