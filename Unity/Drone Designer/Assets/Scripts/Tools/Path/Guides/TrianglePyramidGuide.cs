using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
public class TrianglePyramidGuide : GuideShape
{
    [Range(0.0f, 2.0f)] public float Padding = 0.0f;
    [Range(1, 10)] public int RowsX = 2;
    [Range(1, 10)] public int RowsZ = 2;


    protected override List<AttachmentPoint> CreateGuidePoints()
    {
        List<AttachmentPoint> points = new List<AttachmentPoint>();

        return points;
    }
}



[CustomEditor(typeof(TrianglePyramidGuide))]
public class TrianglePyramidGuideEditor : GuideEditor<TrianglePyramidGuide>
{
    private void OnSceneGUI()
    {
        DrawGuide(Target.transform, Color.white);
        DrawPointHandles(Target.AttachmentPoints);
    }

    [DrawGizmo(GizmoType.NonSelected | GizmoType.Active | GizmoType.Selected)]
    private static void DrawGizmo(TrianglePyramidGuide shape, GizmoType gizmo)
    {
        DrawGuide(shape.transform, Palette.Translucent);
    }

    private static void DrawGuide(Transform transform, Color color)
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
