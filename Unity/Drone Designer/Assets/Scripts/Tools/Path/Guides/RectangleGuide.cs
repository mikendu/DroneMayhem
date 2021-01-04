using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
public class RectangleGuide : GuideShape
{
    [Range(2, 10)] public int GridSizeX = 2;
    [Range(2, 10)] public int GridSizeZ = 2;


    protected override List<AttachmentPoint> CreateGuidePoints()
    {
        List<AttachmentPoint> points = new List<AttachmentPoint>();

        float xInterval = 1.0f / (GridSizeX - 1);
        float zInterval = 1.0f / (GridSizeZ - 1);

        for(int i = 0; i < GridSizeX; i++)
        {
            float x = xInterval * i;

            for (int j = 0; j < GridSizeZ; j++)
            {
                float z = zInterval * j;
                Vector3 position = new Vector3(x - 0.5f, 0, z - 0.5f);
                points.Add(new AttachmentPoint(transform, position));
            }
        }

        return points;
    }
}



[CustomEditor(typeof(RectangleGuide))]
public class RectangleGuideEditor : GuideEditor<RectangleGuide>
{
    private void OnSceneGUI()
    {
        DrawSquare(Target, Color.white);
        DrawPointHandles(Target.AttachmentPoints);
    }

    [DrawGizmo(GizmoType.NonSelected | GizmoType.Active | GizmoType.Selected)]
    private static void DrawGizmo(RectangleGuide shape, GizmoType gizmo)
    {
        DrawSquare(shape, Palette.Translucent);
    }

    private static void DrawSquare(RectangleGuide shape, Color color)
    {
        Matrix4x4 matrix = Handles.matrix;
        Color previousColor = Handles.color;

        Handles.color = color;
        Handles.matrix = shape.transform.localToWorldMatrix * Matrix4x4.Rotate(Quaternion.Euler(90, 0, 0));
        Handles.RectangleHandleCap(0, Vector3.zero, Quaternion.identity, 0.5f, EventType.Repaint);
        DrawSubGuides(shape, 0.5f * color);

        Handles.matrix = matrix;
        Handles.color = previousColor;
    }

    private static void DrawSubGuides(RectangleGuide shape, Color color)
    {
        Handles.color = color;
        float xInterval = 1.0f / (shape.GridSizeX - 1);
        float zInterval = 1.0f / (shape.GridSizeZ - 1);

        for (int i = 0; i < (shape.GridSizeX - 2); i++)
        {
            float x = xInterval * (i + 1);
            Handles.DrawLine(new Vector3(x - 0.5f, -0.5f, 0), new Vector3(x - 0.5f, 0.5f, 0));
        }

        for (int i = 0; i < (shape.GridSizeZ - 2); i++)
        {
            float z = zInterval * (i + 1);
            Handles.DrawLine(new Vector3(-0.5f, z - 0.5f, 0), new Vector3(0.5f, z - 0.5f, 0));
        }
    }
}
