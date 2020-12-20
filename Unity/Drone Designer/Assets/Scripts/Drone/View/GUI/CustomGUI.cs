using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEditor.Timeline;

[InitializeOnLoad]
public static class CustomGUI
{
    public static GUIStyle LabelStyle;
    public static GUIStyle TitleStyle;
    public static GUIStyle HeaderStyle;

    static CustomGUI()
    {
        LabelStyle = new GUIStyle();
        LabelStyle.normal.textColor = Color.white;

        TitleStyle = new GUIStyle();
        TitleStyle.normal.textColor = Color.white;
        TitleStyle.fontStyle = FontStyle.Normal;
        TitleStyle.fontSize = 15;

        HeaderStyle = new GUIStyle();
        HeaderStyle.normal.textColor = Color.white;
        HeaderStyle.fontStyle = FontStyle.Normal;
        HeaderStyle.fontSize = 20;
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

    public static void Window<T>(Rect windowRect, string title, Action<T> contentFunction, T argument)
    {
        int controlId = GUIUtility.GetControlID(FocusType.Passive);
        Event currentEvent = Event.current;

        if (currentEvent.type == EventType.MouseDown && windowRect.Contains(currentEvent.mousePosition))
            GUIUtility.hotControl = controlId;

        Color guiColor = GUI.color;

        Handles.BeginGUI();
        GUILayout.BeginArea(windowRect);
        GUI.backgroundColor = Palette.GuiBackground;
        EditorGUILayout.BeginVertical("Window");
        CustomGUI.SetLabelColors();

        EditorGUILayout.BeginVertical(EditorStyles.inspectorFullWidthMargins);

        CustomGUI.DrawTitle(title, CustomGUI.HeaderStyle);
        CustomGUI.DrawSplitter(15, 15, 1.0f);
        contentFunction(argument);

        EditorGUILayout.EndVertical();

        CustomGUI.UnsetLabelColors();
        EditorGUILayout.EndVertical();
        GUILayout.EndArea();
        Handles.EndGUI();

        GUI.color = guiColor;
    }
}