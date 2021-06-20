using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEditor;
using UnityEditor.Timeline;


[CustomEditor(typeof(DynamicGuide))]
public class DynamicGuideEditor : GuideEditor
{
    private DynamicGuide Guide { get { return target as DynamicGuide; } }
    private Tool LastTool = Tool.None;

    private void OnEnable()
    {
        LastTool = Tools.current;
        Tools.current = Tool.None;
    }

    private void OnDisable()
    {
        Tools.current = LastTool;
    }
    private void OnSceneGUI()
    {
        Draw(Guide);
    }
    public static void Draw(DynamicGuide guide)
    {
        DrawGuideHandles(guide);
        DrawGuideGUI(guide);
        guide.DrawShapeGUI();
    }


    public static void DrawGuideHandles(DynamicGuide guide)
    {
        float time = (float)TimelineUtilities.Director.time;
        EditorGUI.BeginChangeCheck();
        Vector3 updatedPosition = CustomHandles.MoveHandle(guide.transform.position, 0.05f, 0.17f);
        if (EditorGUI.EndChangeCheck())
            guide.SetPosition(updatedPosition, time);
    }


    public static void DrawGuideGUI(DynamicGuide guide)
    {
        Rect toolsRect = new Rect(20, 20, 300, 275);
        CustomGUI.Window(toolsRect, "Guide Tools", DrawGuideTools, guide);
    }


    private static void DrawGuideTools(DynamicGuide guide)
    {
        // Position
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.Space(10);
        EditorGUI.BeginChangeCheck();
        Vector3 updatedPosition = EditorGUILayout.Vector3Field(new GUIContent("Position"), guide.transform.position);
        if (EditorGUI.EndChangeCheck())
            guide.SetPosition(updatedPosition, guide.Time);

        // Rotation
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.Space(10);
        EditorGUI.BeginChangeCheck();
        Quaternion updatedRotation = Quaternion.Euler(EditorGUILayout.Vector3Field(new GUIContent("Rotation"), guide.transform.rotation.eulerAngles));
        if (EditorGUI.EndChangeCheck())
            guide.SetRotation(updatedRotation, guide.Time);

        // Position
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.Space(10);
        EditorGUI.BeginChangeCheck();
        Vector3 updatedScale = EditorGUILayout.Vector3Field(new GUIContent("Scale"), guide.transform.lossyScale);
        if (EditorGUI.EndChangeCheck())
            guide.SetScale(updatedScale, guide.Time);

        EditorGUILayout.Space(25);

        if (GUILayout.Button("Apply"))
            guide.Apply();

    }

    public override void OnInspectorGUI()
    {
        DrawGuideSelector();

        if (GUILayout.Button("Apply"))
            Guide.Apply();

        EditorGUILayout.Space(30);
    }
}