using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public static class ConversionUtils
{
    public static Vector3 ToCrazyflieCoordinates(this Vector3 vector)
    {
        return new Vector3(vector.z, -vector.x, vector.y);
    }

    public static Vector4 ToCrazyflieColor(this Color color)
    {
        return new Color(
            Mathf.RoundToInt(255 * color.r),
            Mathf.RoundToInt(255 * color.g),
            Mathf.RoundToInt(255 * color.b),
            0
        );
    }
}