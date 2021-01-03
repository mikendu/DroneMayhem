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
    [Range(0.0f, 2.0f)] public float Padding = 0.0f;
    [Range(1, 10)] public int RowsX = 2;
    [Range(1, 10)] public int RowsZ = 2;


    protected override List<AttachmentPoint> CreateGuidePoints()
    {
        List<AttachmentPoint> points = new List<AttachmentPoint>();

        Tuple<float, float> rowInfo = CalculateDivisionInfo(RowsX, Padding);
        Tuple<float, float> columnInfo = CalculateDivisionInfo(RowsZ, Padding);

        for(int i = 0; i < RowsX; i++)
        {
            float x = GetPosition(rowInfo, i);

            for(int j = 0; j < RowsZ; j++)
            {
                float z = GetPosition(columnInfo, j);
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
        DrawSquare(Target.transform, Color.white);
        DrawPointHandles(Target.AttachmentPoints);
    }

    [DrawGizmo(GizmoType.NonSelected | GizmoType.Active | GizmoType.Selected)]
    private static void DrawGizmo(RectangleGuide shape, GizmoType gizmo)
    {
        DrawSquare(shape.transform, Palette.Translucent);
    }

    private static void DrawSquare(Transform transform, Color color)
    {
        Matrix4x4 matrix = Handles.matrix;
        Color previousColor = Handles.color;

        Handles.color = color;
        Handles.matrix = transform.localToWorldMatrix * Matrix4x4.Rotate(Quaternion.Euler(90, 0, 0));
        Handles.RectangleHandleCap(0, Vector3.zero, Quaternion.identity, 0.5f, EventType.Repaint);

        Handles.matrix = matrix;
        Handles.color = previousColor;
    }
}
