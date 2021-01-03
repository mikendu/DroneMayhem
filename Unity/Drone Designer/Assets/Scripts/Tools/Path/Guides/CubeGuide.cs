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
    [Range(0.0f, 2.0f)] public float Padding = 0.0f;
    [Range(1, 10)] public int RowsX = 2;
    [Range(1, 10)] public int RowsY = 2;
    [Range(1, 10)] public int RowsZ = 2;


    protected override List<AttachmentPoint> CreateGuidePoints()
    {
        List<AttachmentPoint> points = new List<AttachmentPoint>();

        Tuple<float, float> rowInfo = CalculateDivisionInfo(RowsX, Padding);
        Tuple<float, float> columnInfo = CalculateDivisionInfo(RowsZ, Padding);
        Tuple<float, float> depthInfo = CalculateDivisionInfo(RowsY, Padding);

        for(int i = 0; i < RowsX; i++)
        {
            float x = GetPosition(rowInfo, i);

            for(int j = 0; j < RowsZ; j++)
            {
                float z = GetPosition(columnInfo, j);

                for(int k = 0; k < RowsY; k++)
                {
                    float y = GetPosition(depthInfo, k);
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
        DrawCube(Target.transform, Color.white);
        DrawPointHandles(Target.AttachmentPoints);
    }

    [DrawGizmo(GizmoType.NonSelected | GizmoType.Active | GizmoType.Selected)]
    private static void DrawGizmo(CubeGuide shape, GizmoType gizmo)
    {
        DrawCube(shape.transform, Palette.Translucent);
    }

    private static void DrawCube(Transform transform, Color color)
    {
        Matrix4x4 matrix = Handles.matrix;
        Color previousColor = Handles.color;

        Handles.color = color;
        Handles.matrix = transform.localToWorldMatrix * Matrix4x4.Rotate(Quaternion.Euler(90, 0, 0));
        Handles.DrawWireCube(Vector3.zero, Vector3.one);

        Handles.matrix = matrix;
        Handles.color = previousColor;
    }
}
