using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
public class CircleGuide : GuideShape
{
    [Range(1, 15)] public int SpotsPerRing = 4;
    [Range(0, 10)] public int InnerRings = 0;


    protected override List<AttachmentPoint> CreateGuidePoints()
    {
        List<AttachmentPoint> points = new List<AttachmentPoint>();
        points.Add(new AttachmentPoint(transform, Vector3.zero));

        float interval = 1.0f / (InnerRings + 1);
        for (int i = 0; i < InnerRings + 1; i++)
        {
            float size = (1.0f - (i * interval));
            CreateCircle(points, size);
        }

        return points;
    }

    protected void CreateCircle(List<AttachmentPoint> points, float size)
    {
        float angleInterval = (2.0f * Mathf.PI) / SpotsPerRing;
        for(int i = 0; i < SpotsPerRing; i++)
        {
            float angle = (i * angleInterval);
            float radius = size * 0.5f;
            float x = radius * Mathf.Cos(angle);
            float z = radius * Mathf.Sin(angle);
            points.Add(new AttachmentPoint(transform, new Vector3(x, 0, z)));
        }
    }

    public override void DrawSceneGUI()
    {
        CircleGuideEditor.Draw(this);
    }
}



[CustomEditor(typeof(CircleGuide))]
public class CircleGuideEditor : GuideShapeEditor<CircleGuide>
{
    private void OnSceneGUI()
    {
    }

    public static void Draw(CircleGuide guide)
    {
        DrawGuide(guide, Color.white);
        DrawPointHandles(guide.AttachmentPoints, guide.Guide.Dynamic);
    }

    [DrawGizmo(GizmoType.NonSelected | GizmoType.Active | GizmoType.Selected)]
    private static void DrawGizmo(CircleGuide shape, GizmoType gizmo)
    {
        DrawGuide(shape, Palette.Translucent);
    }

    private static void DrawGuide(CircleGuide shape, Color color)
    {
        Matrix4x4 matrix = Handles.matrix;
        Color previousColor = Handles.color;
        Color darkened = 0.5f * color;

        Handles.color = color;
        Handles.matrix = shape.transform.localToWorldMatrix;

        float interval = 1.0f / (shape.InnerRings + 1);
        for (int i = 0; i < shape.InnerRings + 1; i++)
        {
            if (i > 0)
                Handles.color = darkened;

            float size = (1.0f - (i * interval));
            Handles.DrawWireDisc(Vector3.zero, Vector3.up, size * 0.5f);
        }

        Handles.matrix = matrix;
        Handles.color = previousColor;
    }
}
