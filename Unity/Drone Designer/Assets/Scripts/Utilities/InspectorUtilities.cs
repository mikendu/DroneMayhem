using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;

public static class InspectorUtilities
{
    public static void EnumToolbar<T>(SerializedProperty property) where T : Enum
    {
        int currentIndex = property.enumValueIndex;
        EditorGUI.BeginChangeCheck();
        int newIndex = GUILayout.Toolbar(currentIndex, Enum.GetNames(typeof(T)));

        if (EditorGUI.EndChangeCheck())
        {
            property.enumValueIndex = newIndex;
            property.serializedObject.ApplyModifiedProperties();
        }
    }

    public static void EnumToolbar<T>(SerializedProperty property, string label) where T : Enum
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(EditorGUIUtility.labelWidth));
        EnumToolbar<T>(property);
        EditorGUILayout.EndHorizontal();
    }

    public static void Header(string text)
    {
        EditorGUILayout.Space(20.0f);
        EditorGUILayout.LabelField(text, EditorStyles.boldLabel);
    }
}