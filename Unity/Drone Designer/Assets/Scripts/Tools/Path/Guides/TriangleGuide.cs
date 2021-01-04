using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
public class TriangleGuide : GuideShape
{
    [Range(0, 10)] public int InnerRings = 0;
    [Range(0, 10)] public int SpotsPerSide = 0;


    protected override List<AttachmentPoint> CreateGuidePoints()
    {
        List<AttachmentPoint> points = new List<AttachmentPoint>();
        float smallRadius = ShapeUtils.UnitTriangleSmallRadius;

        //points.Add(new AttachmentPoint(transform, Vector3.zero));
        Vector3 leftCorner = new Vector3(-0.5f, 0, -smallRadius);
        Vector3 rightCorner = new Vector3(0.5f, 0, -smallRadius);

        float interval = 1.0f / (InnerRings + 1);
        for(int i = 0; i < InnerRings + 1; i++)
        {
            Vector3 offsetLeft = leftCorner - ((interval * i) * leftCorner);
            Vector3 offsetRight = rightCorner - ((interval * i) * rightCorner);
            float size = (1.0f - (i * interval));
            CreateRing(points, offsetLeft, offsetRight, size);
        }

        return points;
    }

    protected void CreateRing(List<AttachmentPoint> points, Vector3 leftCorner, Vector3 rightCorner, float sizeMultiplier = 1.0f)
    {
        float slope = Mathf.Tan(Mathf.Deg2Rad * 60);
        Vector3 leftEdgeDirection = new Vector3(1.0f, 0.0f, slope);
        Vector3 rightEdgeDirection = new Vector3(-1.0f, 0.0f, slope);

        CreateSide(points, leftCorner, leftEdgeDirection, true, true, sizeMultiplier);
        CreateSide(points, rightCorner, rightEdgeDirection, true, false, sizeMultiplier);
        CreateSide(points, leftCorner, Vector3.right, false, false, sizeMultiplier);
    }

    protected void CreateSide(List<AttachmentPoint> points, Vector3 position, Vector3 direction, 
                                bool includeStart = true, bool includeEnd = true, float sizeMultiplier = 1.0f)
    {
        direction = direction.normalized;

        if (includeStart)
            points.Add(new AttachmentPoint(transform, position));

        float interval = 1.0f / (SpotsPerSide + 1);
        for(int i = 0; i < SpotsPerSide; i++)
        {
            float progress = (interval * (i + 1)) * sizeMultiplier;
            Vector3 spotPosition = position + (progress * direction);
            points.Add(new AttachmentPoint(transform, spotPosition));
        }

        if (includeEnd)
            points.Add(new AttachmentPoint(transform, position + (sizeMultiplier * direction)));
    }
}



[CustomEditor(typeof(TriangleGuide))]
public class TriangleGuideEditor : GuideEditor<TriangleGuide>
{
    private void OnSceneGUI()
    {
        DrawGuide(Target, Color.white);
        DrawPointHandles(Target.AttachmentPoints);
    }

    [DrawGizmo(GizmoType.NonSelected | GizmoType.Active | GizmoType.Selected)]
    private static void DrawGizmo(TriangleGuide shape, GizmoType gizmo)
    {
        DrawGuide(shape, Palette.Translucent);
    }

    private static void DrawGuide(TriangleGuide shape, Color color)
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
            DrawTriangle(size);
        }

        Handles.matrix = matrix;
        Handles.color = previousColor;
    }

    private static void DrawTriangle(float sizeMultiplier = 1.0f)
    {
        float smallRadius = ShapeUtils.UnitTriangleSmallRadius;
        float bigRadius = ShapeUtils.UnitTriangleLargeRadius;

        Handles.DrawLine(new Vector3(-0.5f, 0, -smallRadius) * sizeMultiplier, new Vector3(0.5f, 0, -smallRadius) * sizeMultiplier);
        Handles.DrawLine(new Vector3(-0.5f, 0, -smallRadius) * sizeMultiplier, new Vector3(0, 0, bigRadius) * sizeMultiplier);
        Handles.DrawLine(new Vector3(0.5f, 0, -smallRadius) * sizeMultiplier, new Vector3(0, 0, bigRadius) * sizeMultiplier);
    }
}
