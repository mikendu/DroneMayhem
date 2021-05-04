using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
public class SquarePyramidGuide : GuideShape
{
    [Range(0, 10)] public int InnerRings = 0;
    [Range(0, 10)] public int SpotsPerSide = 0;


    protected override List<AttachmentPoint> CreateGuidePoints()
    {
        List<AttachmentPoint> points = new List<AttachmentPoint>();
        points.Add(new AttachmentPoint(transform, Vector3.zero));

        float interval = 1.0f / (InnerRings + 1);
        for (int i = 0; i < InnerRings + 1; i++)
        {
            float size = (1.0f - (i * interval));
            CreateLayer(points, size);
        }

        return points;

    }

    protected void CreateLayer(List<AttachmentPoint> points, float sizeMultiplier = 1.0f)
    {

        float size = 0.5f * sizeMultiplier;
        float height = ShapeUtils.UnitOctahedronHeight * sizeMultiplier;
        float downward = -height * 0.25f;
        float upwards = height * 0.75f;

        Vector3 leftBack = new Vector3(-size, downward, -size);
        Vector3 leftFront = new Vector3(-size, downward, size);
        Vector3 rightBack = new Vector3(size, downward, -size);
        Vector3 rightFront = new Vector3(size, downward, size);

        Vector3 top = new Vector3(0, upwards, 0);
        Vector3 leftBackSpoke = leftBack - top;
        Vector3 leftFrontSpoke = leftFront - top;
        Vector3 rightBackSpoke = rightBack - top;
        Vector3 rightFrontSpoke = rightFront - top;

        // Base
        CreateSide(points, leftBack, Vector3.right, true, false, sizeMultiplier);
        CreateSide(points, rightBack, Vector3.forward, true, false, sizeMultiplier);
        CreateSide(points, rightFront, Vector3.left, true, false, sizeMultiplier);
        CreateSide(points, leftFront, Vector3.back, true, false, sizeMultiplier);

        CreateSide(points, top, leftFrontSpoke, true, false, sizeMultiplier);
        CreateSide(points, top, leftBackSpoke, false, false, sizeMultiplier);
        CreateSide(points, top, rightFrontSpoke, false, false, sizeMultiplier);
        CreateSide(points, top, rightBackSpoke, false, false, sizeMultiplier);
    }

    protected void CreateSide(List<AttachmentPoint> points, Vector3 position, Vector3 direction,
                                bool includeStart = true, bool includeEnd = true, float sizeMultiplier = 1.0f)
    {
        direction = direction.normalized;

        if (includeStart)
            points.Add(new AttachmentPoint(transform, position));

        float interval = 1.0f / (SpotsPerSide + 1);
        for (int i = 0; i < SpotsPerSide; i++)
        {
            float progress = (interval * (i + 1)) * sizeMultiplier;
            Vector3 spotPosition = position + (progress * direction);
            points.Add(new AttachmentPoint(transform, spotPosition));
        }

        if (includeEnd)
            points.Add(new AttachmentPoint(transform, position + (sizeMultiplier * direction)));
    }
}



[CustomEditor(typeof(SquarePyramidGuide))]
public class SquarePyramidGuideEditor : GuideEditor<SquarePyramidGuide>
{
    private void OnSceneGUI()
    {
        DrawGuide(Target, Color.white);
        DrawPointHandles(Target.AttachmentPoints);
    }

    [DrawGizmo(GizmoType.NonSelected | GizmoType.Active | GizmoType.Selected)]
    private static void DrawGizmo(SquarePyramidGuide shape, GizmoType gizmo)
    {
        DrawGuide(shape, Palette.Translucent);
    }

    private static void DrawGuide(SquarePyramidGuide shape, Color color)
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
            DrawLayer(size);
        }

        Handles.matrix = matrix;
        Handles.color = previousColor;
    }

    private static void DrawLayer(float sizeMultiplier = 1.0f)
    {
        float size = 0.5f * sizeMultiplier;
        float height = ShapeUtils.UnitOctahedronHeight * sizeMultiplier;
        float downward = -height * 0.25f;
        float upwards = height * 0.75f;

        Handles.RectangleHandleCap(0, new Vector3(0, downward, 0), Quaternion.Euler(90, 0, 0), size, EventType.Repaint);
        Handles.DrawLine(new Vector3(-size, downward, -size), new Vector3(0, upwards, 0));
        Handles.DrawLine(new Vector3( size, downward, -size), new Vector3(0, upwards, 0));
        Handles.DrawLine(new Vector3(-size, downward,  size), new Vector3(0, upwards, 0));
        Handles.DrawLine(new Vector3( size, downward,  size), new Vector3(0, upwards, 0));
    }
}
