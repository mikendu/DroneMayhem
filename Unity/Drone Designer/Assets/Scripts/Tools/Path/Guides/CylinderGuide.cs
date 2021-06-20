using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
public class CylinderGuide : GuideShape
{
    [Range(1, 15)] public int SpotsPerRing = 4;
    [Range(0, 10)] public int InnerRings = 0;
    [Range(2, 15)] public int VerticalLevels = 2;


    protected override List<AttachmentPoint> CreateGuidePoints()
    {
        List<AttachmentPoint> points = new List<AttachmentPoint>();

        float interval = 1.0f / (VerticalLevels - 1);
        for(int i = 0; i < VerticalLevels; i++)
        {
            float y = (i * interval) - 0.5f;
            CreateLevel(points, y);
        }

        return points;
    }

    protected void CreateLevel(List<AttachmentPoint> points, float height)
    {
        points.Add(new AttachmentPoint(transform, new Vector3(0, height, 0)));

        float interval = 1.0f / (InnerRings + 1);
        for (int i = 0; i < InnerRings + 1; i++)
        {
            float size = (1.0f - (i * interval));
            CreateCylinder(points, size, height);
        }
    }

    protected void CreateCylinder(List<AttachmentPoint> points, float size, float height)
    {
        float angleInterval = (2.0f * Mathf.PI) / SpotsPerRing;
        for (int i = 0; i < SpotsPerRing; i++)
        {
            float angle = (i * angleInterval);
            float radius = size * 0.5f;
            float x = radius * Mathf.Cos(angle);
            float z = radius * Mathf.Sin(angle);
            points.Add(new AttachmentPoint(transform, new Vector3(x, height, z)));
        }
    }

    public override void DrawSceneGUI()
    {
        CylinderGuideEditor.Draw(this);
    }
}



[CustomEditor(typeof(CylinderGuide))]
public class CylinderGuideEditor : GuideShapeEditor<CylinderGuide>
{
    public static void Draw(CylinderGuide guide)
    {
        DrawGuide(guide, Color.white);
        DrawPointHandles(guide.AttachmentPoints, guide.Guide.Dynamic);
    }

    [DrawGizmo(GizmoType.NonSelected | GizmoType.Active | GizmoType.Selected)]
    private static void DrawGizmo(CylinderGuide shape, GizmoType gizmo)
    {
        DrawGuide(shape, Palette.Translucent);
    }

    private static void DrawGuide(CylinderGuide shape, Color color)
    {
        Matrix4x4 matrix = Handles.matrix;
        Color previousColor = Handles.color;
        Color darkened = 0.5f * color;

        Handles.color = color;
        Handles.matrix = shape.transform.localToWorldMatrix;

        float ringInterval = 1.0f / (shape.InnerRings + 1);
        for (int i = 0; i < shape.InnerRings + 1; i++)
        {
            float size = (1.0f - (i * ringInterval));
            float levelInterval = 1.0f / (shape.VerticalLevels - 1);

            for (int j = 0; j < shape.VerticalLevels; j++)
            {
                float y = (j * levelInterval) - 0.5f;
                if ((i > 0) || !(Mathf.Approximately(y, -0.5f) || Mathf.Approximately(y, 0.5f)))
                    Handles.color = darkened;
                else
                    Handles.color = color;

                Handles.DrawWireDisc(new Vector3(0, y, 0), Vector3.up, size * 0.5f);
            }


            float angleInterval = (2.0f * Mathf.PI) / shape.SpotsPerRing;
            for (int k = 0; k < shape.SpotsPerRing; k++)
            {
                float angle = (k * angleInterval);
                float radius = size * 0.5f;
                float x = radius * Mathf.Cos(angle);
                float z = radius * Mathf.Sin(angle);
                Handles.DrawLine(new Vector3(x, -0.5f, z), new Vector3(x, 0.5f, z));
            }
        }

        //Handles.color = Palette.HyperTranslucent;
        //Handles.CylinderHandleCap(0, Vector3.zero, Quaternion.Euler(90, 0, 0), 1.0f, EventType.Repaint);

        Handles.matrix = matrix;
        Handles.color = previousColor;
    }
}
