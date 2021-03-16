using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityEditor.Timeline;

public class ColorAnimator : MonoBehaviour
{
    public VolumeMode Mode = VolumeMode.Color;
    public List<Color> Colors = new List<Color>();

    [Range(1, 10)]
    public int Cycles = 1;

    [Range(1, 20)]
    public int OffsetIncrements = 5;

    public float TotalDuration = 1.0f;
    public bool AnimateOffset = true;

    private ColorVolume colorVolume;
    private ColorVolume ColorVolume
    {
        get
        {
            if (colorVolume == null)
                colorVolume = GetComponent<ColorVolume>();

            return colorVolume;
        }
    }

    public void ApplyColors()
    {
        int cycleCount = Cycles * Colors.Count;
        float durationInterval = TotalDuration / cycleCount;
        float startTime = TimelineUtilities.CurrentTime;
        ColorVolume.Mode = VolumeMode.Color;

        for (int i = 0; i < cycleCount; i++)
        {
            TimelineUtilities.CurrentTime = startTime + (i * durationInterval);
            int colorIndex = (i % Colors.Count);
            ColorVolume.Color = Colors[colorIndex];
            ColorVolume.Apply();
        }

        ColorVolume.Color = Color.black;
        TimelineEditor.Refresh(RefreshReason.ContentsAddedOrRemoved);
    }

    public void ApplyOffset()
    {
        int cycleCount = Cycles * OffsetIncrements;
        float durationInterval = TotalDuration / cycleCount;
        float startTime = TimelineUtilities.CurrentTime;
        float offsetInterval = 1.0f / OffsetIncrements;
        ColorVolume.Mode = VolumeMode.Gradient;

        for (int i = 0; i <= cycleCount; i++)
        {
            TimelineUtilities.CurrentTime = startTime + (i * durationInterval);
            int gradientIndex = (i % OffsetIncrements);
            ColorVolume.GradientOffset = (AnimateOffset) ? gradientIndex * offsetInterval : 0.0f;
            ColorVolume.Apply();
        }

        TimelineEditor.Refresh(RefreshReason.ContentsAddedOrRemoved);
    }
}

[CustomEditor(typeof(ColorAnimator))]
public class ColorAnimatorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        ColorAnimator animator = target as ColorAnimator;

        EditorGUILayout.Space(17);
        EditorGUILayout.LabelField("Mode", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("Mode"), true);

        if (animator.Mode == VolumeMode.Color)
        {
            EditorGUILayout.Space(17);
            EditorGUILayout.LabelField("Colors", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Colors"), true);
        }
        else
        {
            EditorGUILayout.Space(17);
            EditorGUILayout.LabelField("Gradient Increments", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("AnimateOffset"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("OffsetIncrements"), true);
        }

        EditorGUILayout.Space(17);
        EditorGUILayout.LabelField("Timing", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("TotalDuration"), true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("Cycles"), true);

        EditorGUILayout.Space(17);
        if (Button("Apply") && animator.gameObject.activeInHierarchy)
        {
            if (animator.Mode == VolumeMode.Color)
                animator.ApplyColors();
            else
                animator.ApplyOffset();
        }

        serializedObject.ApplyModifiedProperties();
    }

    private bool Button(string text)
    {
        bool result = false;
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        if (GUILayout.Button(text, GUILayout.Width(200)))
            result = true;

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        return result;
    }
}