using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;

public abstract class Guide : MonoBehaviour
{
    public ShapeType ShapeType = ShapeType.Rectangle;
    public abstract bool Dynamic { get; }

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
}


public abstract class GuideEditor : Editor
{
    private static readonly string[] options = ((ShapeType[])Enum.GetValues(typeof(ShapeType))).Select(shape => shape.Format()).ToArray();

    public void DrawGuideSelector()
    {
        Guide guide = target as Guide;
        EditorGUILayout.Space(30);

        SerializedProperty shape = serializedObject.FindProperty("ShapeType");
        int currentSelection = shape.enumValueIndex;
        // int newSelection = GUILayout.Toolbar(shape.enumValueIndex, options);
        int newSelection = GUILayout.SelectionGrid(currentSelection, options, 3);

        if (newSelection != currentSelection)
        {
            guide.SetShape((ShapeType)newSelection);
            serializedObject.Update();
            serializedObject.ApplyModifiedProperties();
        }

        EditorGUILayout.Space(30);
    }
}