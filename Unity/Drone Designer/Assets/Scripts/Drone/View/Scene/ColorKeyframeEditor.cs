using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;

[CustomEditor(typeof(ColorKeyframe))]
public class ColorKeyframeEditor : CustomEditor<ColorKeyframe>
{
    protected override void OnDrawScene(SceneView scene)
    {
        ColorKeyframe keyframe = Target;
        Crazyflie drone = keyframe.Drone;
        CrazyflieEditor.Draw(drone);

        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Delete)
            drone.RemoveColorKeyframe(Target);
    }


    public static void DrawColorKeyframes(Crazyflie drone, List<PositionKeyframe> waypoints)
    {
        List<ColorKeyframe> colorKeyframes = drone.UnsortedKeyframes;
        List<ColorKeyframe> sorted = new List<ColorKeyframe>(colorKeyframes);

        sorted.Sort((x, y) => x.time.CompareTo(y.time));
        Vector3 dronePosition = drone.transform.position;

        for (int i = 0; i < sorted.Count; i++)
        {
            ColorKeyframe keyframe = sorted[i];
            keyframe.Position = KeyframeUtil.GetPosition(waypoints, keyframe.time, dronePosition);
            keyframe.Tangent = KeyframeUtil.GetTangent(waypoints, keyframe.time, true);
            keyframe.Offset = GetOffset(i, sorted);
            keyframe.SortedIndex = i;
        }
        
        foreach(ColorKeyframe keyframe in colorKeyframes)
            DrawSelector(keyframe);
        
    }

    public static void DrawSelector(ColorKeyframe keyframe)
    {
        Vector3 offsetPosition = keyframe.Position + new Vector3(0, keyframe.Offset, 0);

        float size = 0.02f;
        float hitboxSize = 1.5f * size;

        // -- DOT & LINE -- // 
        Handles.color = Palette.Translucent;
        Handles.DrawLine(keyframe.Position, offsetPosition);
        CustomHandles.DrawDisc(offsetPosition, size * 0.5f, keyframe.LightColor);

        // -- SELECTION -- // 
        if (CustomHandles.SelectableButton(offsetPosition, hitboxSize, keyframe.LightColor))
            Selection.activeObject = keyframe;

        bool selected = Selection.activeObject == keyframe;
        if (selected)
            CustomHandles.DrawCircle(offsetPosition, hitboxSize, keyframe.LightColor);

        
        /// -- MOVEMENT -- //
        EditorGUI.BeginChangeCheck();
        Vector3 newPosition = Handles.FreeMoveHandle(offsetPosition, Quaternion.identity, size, DefaultSnap, CustomHandles.NullCap);
        if (EditorGUI.EndChangeCheck() && selected)
        {
            Undo.RecordObject(keyframe, "Change Color Keyframe");

            Vector3 delta = (newPosition - offsetPosition);
            ApplyDelta(keyframe, keyframe.Tangent, delta);

            keyframe.Drone.UpdateView();
            TimelineEditor.Refresh(RefreshReason.ContentsModified);
        }
    }


    private static void ApplyDelta(ColorKeyframe keyframe, Vector3 tangent, Vector3 delta)
    {
        float nearest = 100.0f;
        float dot = Vector3.Dot(tangent, delta);
        keyframe.time += dot;
        keyframe.time = Mathf.Round((float)keyframe.time * nearest) / nearest;
    }

    private static float GetOffset(int index, List<ColorKeyframe> sortedKeyframes)
    {
        if (index == 0)
            return 0.075f;

        int offsetCounter = 1;
        Vector3 position = sortedKeyframes[index].Position;

        for(int i = index - 1; i >= 0; i -= 1)
        {
            float distance = Vector3.Distance(position, sortedKeyframes[i].Position);
            if (distance < 0.035f)
                offsetCounter += 1;
            else
                break;
        }

        return offsetCounter * 0.075f;
    }
}
