using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
public class SphereGuide : GuideShape
{
    [Range(1, 15)] public int Longitudes = 2;
    [Range(1, 15)] public int Latitudes = 1;
    [Range(0, 10)] public int InnerSpheres = 0;


    protected override List<AttachmentPoint> CreateGuidePoints()
    {
        List<AttachmentPoint> points = new List<AttachmentPoint>();
        points.Add(new AttachmentPoint(transform, Vector3.zero));
        

        float interval = 1.0f / (InnerSpheres + 1);
        for (int i = 0; i < InnerSpheres + 1; i++)
        {
            float size = (1.0f - (i * interval));
            CreateSphere(points, size);
        }

        return points;
    }

    protected void CreateSphere(List<AttachmentPoint> points, float size)
    {
        float latitudeAngleInterval = Mathf.PI / (Latitudes + 1);
        for (int i = 0; i < Latitudes; i++)
        {
            float latitudeAngle = (latitudeAngleInterval * (i + 1));
            Tuple<float, float> latitudeInfo = GetLatitudeInfo(latitudeAngle);
            float latitude = latitudeInfo.Item1;
            float ringSize = latitudeInfo.Item2 * size;

            float y = latitude * size;

            float angleInterval = Mathf.PI / Longitudes;
            for (int j = 0; j < (2 * Longitudes); j++)
            {
                float angle = (angleInterval * j);
                float x = ringSize * Mathf.Cos(angle);
                float z = ringSize * Mathf.Sin(angle);
                points.Add(new AttachmentPoint(transform, new Vector3(x, y, z)));
            }
        }

    }

    public static float GetLatitudeRadius(float height)
    {
        float angle = Mathf.Asin(height / 0.5f);
        float unitRadius = Mathf.Cos(angle);
        return unitRadius * 0.5f;
    }

    public static Tuple<float, float> GetLatitudeInfo(float angle)
    {
        float height = 0.5f * Mathf.Cos(angle);
        float radius = 0.5f * Mathf.Sin(angle);
        return new Tuple<float, float>(height, radius);
    }

}



[CustomEditor(typeof(SphereGuide))]
public class SphereGuideEditor : GuideEditor<SphereGuide>
{
    private void OnSceneGUI()
    {
        DrawGuide(Target, Color.white);
        DrawPointHandles(Target.AttachmentPoints);
    }

    [DrawGizmo(GizmoType.NonSelected | GizmoType.Active | GizmoType.Selected)]
    private static void DrawGizmo(SphereGuide shape, GizmoType gizmo)
    {
        DrawGuide(shape, Palette.Translucent);
    }

    private static void DrawGuide(SphereGuide shape, Color color)
    {
        Matrix4x4 matrix = Handles.matrix;
        Color previousColor = Handles.color;
        Color darkened = 0.5f * color;

        Handles.color = color;
        Handles.matrix = shape.transform.localToWorldMatrix;


        float interval = 1.0f / (shape.InnerSpheres + 1);
        for (int i = 0; i < shape.InnerSpheres + 1; i++)
        {
            float size = (1.0f - (i * interval));
            Color sphereColor = (i == 0) ? color : darkened;

            DrawSphere(shape, sphereColor, size);
        }

        Handles.matrix = matrix;
        Handles.color = previousColor;
    }

    private static void DrawSphere(SphereGuide shape, Color color, float sphereSize)
    {
        Handles.color = color;
        float latitudeAngleInterval = Mathf.PI / (shape.Latitudes + 1);
        for (int i = 0; i < shape.Latitudes; i++)
        {
            float latitudeAngle = (latitudeAngleInterval * (i + 1));
            Tuple<float, float> latitudeInfo = SphereGuide.GetLatitudeInfo(latitudeAngle);
            float latitude = latitudeInfo.Item1;
            float ringSize = latitudeInfo.Item2;

            Handles.CircleHandleCap(0, new Vector3(0, sphereSize * latitude, 0), Quaternion.Euler(90, 0, 0), ringSize * sphereSize, EventType.Repaint);
        }

        float angleInterval = 180.0f / shape.Longitudes;
        float longitudeRadius = sphereSize * 0.5f;
        for (int i = 0; i < shape.Longitudes; i++)
        {
            float angle = (angleInterval * i);
            Handles.CircleHandleCap(0, Vector3.zero, Quaternion.Euler(0, angle, 0), longitudeRadius, EventType.Repaint);
        }

    }
}
