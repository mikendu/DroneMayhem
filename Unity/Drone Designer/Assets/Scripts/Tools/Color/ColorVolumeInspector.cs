using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ColorVolume))]
public class ColorVolumeInspector : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        ColorVolume volume = target as ColorVolume;

        SerializedProperty modeProperty = serializedObject.FindProperty("Mode");
        SerializedProperty gradientTypeProperty = serializedObject.FindProperty("GradientType");
        SerializedProperty colorProperty = serializedObject.FindProperty("Color");
        SerializedProperty gradientProperty = serializedObject.FindProperty("Gradient");
        SerializedProperty offsetProperty = serializedObject.FindProperty("GradientOffset");
        SerializedProperty invertProperty = serializedObject.FindProperty("InvertGradient");


        EditorGUILayout.Space(20.0f);
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Apply", GUILayout.Width(200)))
            volume.Apply();

        if (GUILayout.Button("Apply & Remove", GUILayout.Width(200)))
            volume.ApplyAndRemove();

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();


        InspectorUtilities.Header("Settings");
        InspectorUtilities.EnumToolbar<VolumeMode>(modeProperty, "Mode");
        VolumeMode mode = (VolumeMode)modeProperty.enumValueIndex;

        if (mode == VolumeMode.Color)
        {
            EditorGUILayout.PropertyField(colorProperty, true);
        }
        else
        {
            InspectorUtilities.EnumToolbar<GradientType>(gradientTypeProperty, "Gradient Type");
            EditorGUILayout.PropertyField(gradientProperty, true);
            EditorGUILayout.PropertyField(offsetProperty, true);
            EditorGUILayout.PropertyField(invertProperty, true);
        }


        EditorGUILayout.Space(20.0f);
        if (target != null)
            serializedObject.ApplyModifiedProperties();
    }
}