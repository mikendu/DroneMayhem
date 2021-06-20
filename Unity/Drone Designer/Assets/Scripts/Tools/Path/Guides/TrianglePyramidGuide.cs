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
        float halfSide = 0.5f * sizeMultiplier;
        float smallTriangleRadius = ShapeUtils.UnitTriangleSmallRadius * sizeMultiplier;
        float bigTriangleRadius = ShapeUtils.UnitTriangleLargeRadius * sizeMultiplier;

        float smallTetrahedronRadius = ShapeUtils.UnitTetrahedronSmallRadius * sizeMultiplier;
        float bigTetrahedronRadius = ShapeUtils.UnitTetrahedronLargeRadius * sizeMultiplier;
        float spaceHeight = ShapeUtils.UnitTetrahedronHeight * sizeMultiplier;

        Vector3 leftCorner = new Vector3(-halfSide, -smallTetrahedronRadius, -smallTriangleRadius);
        Vector3 rightCorner = new Vector3(halfSide, -smallTetrahedronRadius, -smallTriangleRadius);
        Vector3 frontCorner = new Vector3(0, -smallTetrahedronRadius, bigTriangleRadius);

        float slope = Mathf.Tan(Mathf.Deg2Rad * 60);
        Vector3 leftEdgeDirection = new Vector3(1.0f, 0, slope);
        Vector3 rightEdgeDirection = new Vector3(-1.0f, 0.0f, slope);
        Vector3 leftSpokeDirection = new Vector3(halfSide, spaceHeight, smallTriangleRadius);
        Vector3 rightSpokeDirection = new Vector3(-halfSide, spaceHeight, smallTriangleRadius);
        Vector3 frontSpokeDirection = new Vector3(0, spaceHeight, -bigTriangleRadius);

        CreateSide(points, leftCorner, leftEdgeDirection, true, true, sizeMultiplier);
        CreateSide(points, rightCorner, rightEdgeDirection, true, false, sizeMultiplier);
        CreateSide(points, leftCorner, Vector3.right, false, false, sizeMultiplier);

        CreateSide(points, leftCorner, leftSpokeDirection, false, true, sizeMultiplier);
        CreateSide(points, rightCorner, rightSpokeDirection, false, false, sizeMultiplier);
        CreateSide(points, frontCorner, frontSpokeDirection, false, false, sizeMultiplier);
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

    public override void DrawSceneGUI()
    {
        TrianglePyramidGuideEditor.Draw(this);
    }
}



[CustomEditor(typeof(TrianglePyramidGuide))]
public class TrianglePyramidGuideEditor : GuideShapeEditor<TrianglePyramidGuide>
{
    private void OnSceneGUI()
    {
    }

    public static void Draw(TrianglePyramidGuide guide)
    {
        DrawGuide(guide, Color.white);
        DrawPointHandles(guide.AttachmentPoints, guide.Guide.Dynamic);
    }


    [DrawGizmo(GizmoType.NonSelected | GizmoType.Active | GizmoType.Selected)]
    private static void DrawGizmo(TrianglePyramidGuide shape, GizmoType gizmo)
    {
        DrawGuide(shape, Palette.Translucent);
    }

    private static void DrawGuide(TrianglePyramidGuide shape, Color color)
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
        float halfSide = 0.5f * sizeMultiplier;
        float smallTriangleRadius = ShapeUtils.UnitTriangleSmallRadius * sizeMultiplier;
        float bigTriangleRadius = ShapeUtils.UnitTriangleLargeRadius * sizeMultiplier;

        float smallTetrahedronRadius = ShapeUtils.UnitTetrahedronSmallRadius * sizeMultiplier;
        float bigTetrahedronRadius = ShapeUtils.UnitTetrahedronLargeRadius * sizeMultiplier;
        

        // Base Triangle
        Handles.DrawLine(new Vector3(-halfSide, -smallTetrahedronRadius, -smallTriangleRadius), new Vector3(halfSide, -smallTetrahedronRadius, -smallTriangleRadius));
        Handles.DrawLine(new Vector3(-halfSide, -smallTetrahedronRadius, -smallTriangleRadius), new Vector3(0, -smallTetrahedronRadius, bigTriangleRadius));
        Handles.DrawLine(new Vector3(halfSide, -smallTetrahedronRadius, -smallTriangleRadius), new Vector3(0, -smallTetrahedronRadius, bigTriangleRadius));

        // Vertical Spokes
        Handles.DrawLine(new Vector3(halfSide, -smallTetrahedronRadius, -smallTriangleRadius), new Vector3(0, bigTetrahedronRadius, 0));
        Handles.DrawLine(new Vector3(-halfSide, -smallTetrahedronRadius, -smallTriangleRadius), new Vector3(0, bigTetrahedronRadius, 0));
        Handles.DrawLine(new Vector3(0, -smallTetrahedronRadius, bigTriangleRadius), new Vector3(0, bigTetrahedronRadius, 0));
    }
}
