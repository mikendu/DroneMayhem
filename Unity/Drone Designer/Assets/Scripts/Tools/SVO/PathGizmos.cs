using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public static class PathGizmos
{
    public static void DrawGrid(Bounds bounds, Vector3Int divisions, Color color)
    {
        Color previousColor = Gizmos.color;
        Gizmos.color = color;

        Gizmos.DrawWireCube(bounds.center, bounds.size);

        float intervalX = bounds.size.x / divisions.x;
        float intervalY = bounds.size.y / divisions.y;
        float intervalZ = bounds.size.z / divisions.z;

        float startX = bounds.min.x;
        float startY = bounds.min.y;
        float startZ = bounds.min.z;

        float endX = bounds.max.x;
        float endY = bounds.max.y;
        float endZ = bounds.max.z;

        for (int i = 0; i <= divisions.y; i++)
        {
            float y = startY + (i * intervalY);
            for (int j = 0; j <= divisions.x; j++)
            {
                float x = startX + (j * intervalX);
                Gizmos.DrawLine(new Vector3(x, y, startZ), new Vector3(x, y, endZ));
            }

            for (int k = 0; k <= divisions.z; k++)
            {
                float z = startZ + (k * intervalZ);
                Gizmos.DrawLine(new Vector3(startX, y, z), new Vector3(endX, y, z));
            }
        }

        for (int i = 0; i <= divisions.z; i++)
        {
            float z = startZ + (i * intervalZ);
            for (int j = 0; j <= divisions.x; j++)
            {
                float x = startX + (j * intervalX);
                Gizmos.DrawLine(new Vector3(x, startY, z), new Vector3(x, endY, z));
            }
        }

        Gizmos.color = previousColor;
    }
}