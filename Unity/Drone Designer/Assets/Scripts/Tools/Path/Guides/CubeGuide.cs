using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
public class CubeGuide : GuideShape
{
    [Range(2, 10)] public int GridSizeX = 2;
    [Range(2, 10)] public int GridSizeY = 2;
    [Range(2, 10)] public int GridSizeZ = 2;


    protected override List<AttachmentPoint> CreateGuidePoints()
    {
        List<AttachmentPoint> points = new List<AttachmentPoint>();

        float xInterval = 1.0f / (GridSizeX - 1);
        float yInterval = 1.0f / (GridSizeY - 1);
        float zInterval = 1.0f / (GridSizeZ - 1);

        for (int i = 0; i < GridSizeX; i++)
        {
            float x = xInterval * i;

            for (int j = 0; j < GridSizeY; j++)
            {
                float y = yInterval * j;

                for (int k = 0; k < GridSizeZ; k++)
                {
                    float z = zInterval * k;
                    Vector3 position = new Vector3(x - 0.5f, y - 0.5f, z - 0.5f);
                    points.Add(new AttachmentPoint(transform, position));
                }
            }
        }
        
        return points;
    }
}



[CustomEditor(typeof(CubeGuide))]
public class CubeGuideEditor : GuideEditor<CubeGuide>
{
    private void OnSceneGUI()
    {
        DrawCube(Target, Color.white);
        DrawPointHandles(Target.AttachmentPoints);
    }

    [DrawGizmo(GizmoType.NonSelected | GizmoType.Active | GizmoType.Selected)]
    private static void DrawGizmo(CubeGuide shape, GizmoType gizmo)
    {
        DrawCube(shape, Palette.Translucent);
    }

    private static void DrawCube(CubeGuide shape, Color color)
    {
        Matrix4x4 matrix = Handles.matrix;
        Color previousColor = Handles.color;

        Handles.color = color;
        Handles.matrix = shape.transform.localToWorldMatrix;
        Handles.DrawWireCube(Vector3.zero, Vector3.one);
        DrawSubGuides(shape, 0.5f * color);


        Handles.matrix = matrix;
        Handles.color = previousColor;
    }


    private static void DrawSubGuides(CubeGuide shape, Color color)
    {
        Handles.color = color;
        float xInterval = 1.0f / (shape.GridSizeX - 1);
        float yInterval = 1.0f / (shape.GridSizeY - 1);
        float zInterval = 1.0f / (shape.GridSizeZ - 1);

        for (int j = 0; j < shape.GridSizeY; j++)
        {
            float y = yInterval * j;

            for (int i = 0; i < shape.GridSizeX; i++)
            {
                float x = xInterval * i;
                Handles.DrawLine(new Vector3(x - 0.5f, y - 0.5f, -0.5f), new Vector3(x - 0.5f, y - 0.5f, 0.5f));
            }

            for (int i = 0; i < shape.GridSizeZ; i++)
            {
                float z = zInterval * i;
                Handles.DrawLine(new Vector3(-0.5f, y - 0.5f, z - 0.5f), new Vector3(0.5f, y - 0.5f, z - 0.5f));
            }
        }

        for (int i = 0; i < shape.GridSizeX; i++)
        {
            float x = xInterval * i;

            for (int j = 0; j < shape.GridSizeZ; j++)
            {
                float z = zInterval * j;
                Handles.DrawLine(new Vector3(x - 0.5f, -0.5f, z - 0.5f), new Vector3(x - 0.5f, 0.5f, z - 0.5f));
            }
        }
    }
}
