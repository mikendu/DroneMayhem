using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ColorKeyframe))]
public class ColorKeyframeEditor : CustomEditor<ColorKeyframe>
{
    protected override void OnDrawScene(SceneView scene)
    {
        ColorKeyframe keyframe = Target;
        Crazyflie drone = keyframe.Drone;
        CrazyflieEditor.Draw(drone);

    }


    public static void DrawColorKeyframes(Crazyflie drone, List<PositionKeyframe> waypoints)
    {
        List<ColorKeyframe> colorKeyframes = drone.ColorKeyframes;
        Vector3 dronePosition = drone.transform.position;
        Vector3[] cachedPositions = new Vector3[colorKeyframes.Count];

        for (int i = 0; i < colorKeyframes.Count; i++)
        {
            ColorKeyframe keyframe = colorKeyframes[i];
            Vector3 position = KeyframeUtil.GetPosition(waypoints, keyframe.time, dronePosition);
            cachedPositions[i] = position;

            float offset = GetOffset(i, cachedPositions);
            ColorKeyframeEditor.DrawSelector(keyframe, position, offset);
        }
    }

    public static void DrawSelector(ColorKeyframe keyframe, Vector3 position, float offsetAmount)
    {
        Vector3 offsetPosition = position + new Vector3(0, offsetAmount, 0);

        float size = 0.02f;
        float hitboxSize = 1.5f * size;

        // -- DOT & LINE -- // 
        Handles.color = Palette.Translucent;
        Handles.DrawLine(position, offsetPosition);
        CustomHandles.DrawDisc(offsetPosition, size * 0.5f, keyframe.LightColor);

        // -- SELECTION -- // 
        if (CustomHandles.SelectableButton(offsetPosition, hitboxSize, keyframe.LightColor))
            Selection.activeObject = keyframe;

        if (Selection.activeObject == keyframe)
            CustomHandles.DrawCircle(offsetPosition, hitboxSize, keyframe.LightColor);
    }

    private static float GetOffset(int index, Vector3[] positions)
    {
        if (index == 0)
            return 0.075f;

        int offsetCounter = 1;
        Vector3 position = positions[index];

        for(int i = index - 1; i >= 0; i -= 1)
        {
            float distance = Vector3.Distance(position, positions[i]);
            if (distance < 0.035f)
                offsetCounter += 1;
            else
                break;
        }

        return offsetCounter * 0.075f;
    }
}
