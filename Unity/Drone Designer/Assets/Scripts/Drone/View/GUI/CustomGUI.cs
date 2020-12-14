using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEditor.Timeline;

public static class CustomGUI
{
    static CustomGUI()
    {
        //TimelineEditor.Refresh(RefreshReason.SceneNeedsUpdate);
    }


    public static void DrawTitle(string text, GUIStyle style = null)
    {
        if (style == null)
            style = EditorStyles.label;

        Rect rect = EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.Label(text, style);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    public static void DrawSplitter(float paddingBefore, float paddingAfter, float thickness = 1.0f)
    {
        GUILayout.Space(paddingBefore);

        Rect rect = GUILayoutUtility.GetRect(10000.0f, thickness);
        GUI.color = Color.white;
        GUI.backgroundColor = Color.white;
        GUI.Box(rect, GUIContent.none);

        GUILayout.Space(paddingAfter);
    }

    public static void SetLabelColors()
    {

        EditorStyles.label.active.textColor = Color.white;
        EditorStyles.label.focused.textColor = Color.white;
        EditorStyles.label.hover.textColor = Color.white;
        EditorStyles.label.normal.textColor = Color.white;
    }
    public static void UnsetLabelColors()
    {
        EditorStyles.label.active.textColor = Color.black;
        EditorStyles.label.focused.textColor = Color.blue;
        EditorStyles.label.hover.textColor = Color.blue;
        EditorStyles.label.normal.textColor = Color.black;
    }
}