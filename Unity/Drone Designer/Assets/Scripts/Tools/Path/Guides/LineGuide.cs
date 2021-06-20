using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
public class LineGuide : GuideShape
{
    [Range(2, 15)] public int Spots = 2;


    protected override List<AttachmentPoint> CreateGuidePoints()
    {
        List<AttachmentPoint> points = new List<AttachmentPoint>();

        float interval = 1.0f / (Spots - 1);
        for (int i = 0; i < Spots; i++)
        {
            float x = (i * interval);
            points.Add(new AttachmentPoint(transform, new Vector3(x - 0.5f, 0, 0)));
        }

        return points;
    }

    public override void DrawSceneGUI()
    {
        LineGuideEditor.Draw(this);
    }
}



[CustomEditor(typeof(LineGuide))]
public class LineGuideEditor : GuideShapeEditor<LineGuide>
{
    public static void Draw(LineGuide guide)
    {
        DrawGuide(guide, Color.white);
        DrawPointHandles(guide.AttachmentPoints, guide.Guide.Dynamic);
    }

    [DrawGizmo(GizmoType.NonSelected | GizmoType.Active | GizmoType.Selected)]
    private static void DrawGizmo(LineGuide shape, GizmoType gizmo)
    {
        DrawGuide(shape, Palette.Translucent);
    }

    private static void DrawGuide(LineGuide shape, Color color)
    {
        Matrix4x4 matrix = Handles.matrix;
        Color previousColor = Handles.color;

        Handles.color = color;
        Handles.matrix = shape.transform.localToWorldMatrix;
        Handles.DrawLine(new Vector3(-0.5f, 0, 0), new Vector3(0.5f, 0, 0));
        Handles.matrix = matrix;
        Handles.color = previousColor;
    }
}
