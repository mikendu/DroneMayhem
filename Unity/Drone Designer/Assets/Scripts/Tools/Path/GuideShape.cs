using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


public enum ShapeType
{
    Line,
    Triangle,
    Rectangle,
    Circle,
    TrianglePyramid,
    SquarePyramid,
    Cube,
    Cylinder,
    Sphere
}

public abstract class GuideShape : MonoBehaviour
{ 
    public List<AttachmentPoint> AttachmentPoints { get; protected set; }

    protected Guide guide;

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

    public static Type GetType(ShapeType shape)
    {
        switch (shape)
        {
            case ShapeType.Line:
                return typeof(LineGuide);

            case ShapeType.Triangle:
                return typeof(TriangleGuide);

            case ShapeType.Rectangle:
                return typeof(RectangleGuide);

            case ShapeType.Circle:
                return typeof(CircleGuide);

            case ShapeType.TrianglePyramid:
                return typeof(TrianglePyramidGuide);

            case ShapeType.SquarePyramid:
                return typeof(SquarePyramidGuide);

            case ShapeType.Cube:
                return typeof(CubeGuide);

            case ShapeType.Cylinder:
                return typeof(CylinderGuide);

            case ShapeType.Sphere:
                return typeof(SphereGuide);
        }

        return null;
    }

    public abstract void DrawSceneGUI();

    public Guide Guide
    {
        get
        {
            if (this.guide == null)
                this.guide = GetComponent<Guide>();

            return this.guide;
        }
    }
    
}

