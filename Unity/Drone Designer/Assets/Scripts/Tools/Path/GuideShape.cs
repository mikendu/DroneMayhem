using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


public enum ShapeType
{
    Rectangle,
    Cube,
}


public abstract class GuideShape : MonoBehaviour
{
    public List<AttachmentPoint> AttachmentPoints { get; protected set; }

    public virtual void Update()
    {
        if (AttachmentPoints == null)
            AttachmentPoints = CreateGuidePoints();
    }

    public void Reset()
    {
        AttachmentPoints = CreateGuidePoints();
    }

    protected abstract List<AttachmentPoint> CreateGuidePoints();


    protected virtual void OnValidate()
    {
        Reset();
    }

    public static Tuple<float, float> CalculateDivisionInfo(int rows, float paddingMultiplier)
    {
        float division = 1.0f / (rows + 1);
        float padding = (division * paddingMultiplier);

        float remainder = 2.0f * ((paddingMultiplier - 1.0f) * division) / (rows - 1);
        float interval = division - remainder;
        return new Tuple<float, float>(padding, interval);
    }

    public static float GetPosition(Tuple<float, float> divisonData, int row)
    {
        float padding = divisonData.Item1;
        float interval = divisonData.Item2;
        return (padding) + (row * interval);
    }

    public static Type GetType(ShapeType shape)
    {
        switch (shape)
        {
            case ShapeType.Rectangle:
                return typeof(RectangleGuide);

            case ShapeType.Cube:
                return typeof(CubeGuide);
        }

        return null;
    }
}

