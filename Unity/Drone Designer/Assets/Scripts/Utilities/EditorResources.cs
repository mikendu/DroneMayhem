using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public static class EditorResources
{

    private static Mesh cubeMesh;
    private static Mesh sphereMesh;
    private static Material colorVolumeMaterial;

    static EditorResources()
    {
        LoadResources();
    }

    private static void LoadResources()
    {
        cubeMesh = Resources.GetBuiltinResource<Mesh>(GetPrimitiveMeshPath(PrimitiveType.Cube));
        sphereMesh = Resources.GetBuiltinResource<Mesh>(GetPrimitiveMeshPath(PrimitiveType.Sphere));
        colorVolumeMaterial = Resources.Load<Material>("Materials/Color Volume");
    }

    public static Mesh CubeMesh
    {
        get
        {
            if (cubeMesh == null)
                LoadResources();

            return cubeMesh;
        }
    }

    public static Mesh SphereMesh
    {
        get
        {
            if (sphereMesh == null)
                LoadResources();

            return sphereMesh;
        }
    }

    public static Material ColorVolumeMaterial
    {
        get
        {
            if (colorVolumeMaterial == null)
                LoadResources();

            return colorVolumeMaterial;
        }
    }


    private static string GetPrimitiveMeshPath(PrimitiveType primitiveType)
    {
        switch (primitiveType)
        {
            case PrimitiveType.Sphere:
                return "New-Sphere.fbx";
            case PrimitiveType.Capsule:
                return "New-Capsule.fbx";
            case PrimitiveType.Cylinder:
                return "New-Cylinder.fbx";
            case PrimitiveType.Cube:
                return "Cube.fbx";
            case PrimitiveType.Plane:
                return "New-Plane.fbx";
            case PrimitiveType.Quad:
                return "Quad.fbx";
            default:
                throw new ArgumentOutOfRangeException(nameof(primitiveType), primitiveType, null);
        }
    }
}