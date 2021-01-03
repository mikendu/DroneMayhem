using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;


[ExecuteInEditMode]
[RequireComponent(typeof(GuideShape))]
public class StaticGuide : MonoBehaviour
{
    public ShapeType ShapeType = ShapeType.Rectangle;

    public void SetShape(ShapeType shapeType)
    {
        this.ShapeType = shapeType;
        Type componentType = GuideShape.GetType(ShapeType);

        // Invalid type
        if (componentType == null)
            return;

        // Already has the right type
        if (GetComponent(componentType) != null)
            return;
        
        GuideShape[] shapes = GetComponents<GuideShape>();
        foreach (GuideShape shape in shapes)
            Undo.DestroyObjectImmediate(shape);

        GuideShape newShape = gameObject.AddComponent(componentType) as GuideShape;
        Undo.RegisterCreatedObjectUndo(newShape, "Guide Shape");
    }

    private void Update()
    {
    }

    private void OnValidate()
    {
        //SetShape(ShapeType);
    }

}