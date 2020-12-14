using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;


[CustomEditor(typeof(PositionKeyframe))]
public class WaypointEditor : CustomEditor<PositionKeyframe>
{
    protected static int targetPoint = 0;

    protected override void OnEnable()
    {
        base.OnEnable();
        targetPoint = 0;
    }

    protected override void OnDrawScene(SceneView scene)
    {
        PositionKeyframe keyframe = Target;
        Crazyflie drone = keyframe.Drone;
        CrazyflieEditor.Draw(drone);

        if (keyframe.JointType != JointType.Linear)
        {
            DrawTangent(keyframe, false);
            DrawTangent(keyframe, true);
        }

        if (targetPoint == 0)
            CustomHandles.DrawCircle(keyframe.Position, 0.0375f, Color.green);
        else
            CustomHandles.DrawCircle(keyframe.Position, 0.0375f, Color.white);
    }

    protected void DrawTangent(PositionKeyframe keyframe, bool invert)
    {
        Vector3 position = keyframe.Position;
        Vector3 tangentPosition = position + (invert ? keyframe.Tangent : -keyframe.Tangent);

        Handles.color = Color.white;
        Handles.SphereHandleCap(0, tangentPosition, Quaternion.identity, 0.01f, EventType.Repaint);

        Handles.color = Palette.Translucent;
        Handles.DrawLine(position, tangentPosition);

        if (CustomHandles.SelectableButton(tangentPosition, 0.015f, Color.white))
            targetPoint = (invert ? 2 : 1);

        EditorGUI.BeginChangeCheck();
        Vector3 newTangent = Handles.FreeMoveHandle(tangentPosition, Quaternion.identity, 0.015f, 0.01f * Vector3.one, CustomHandles.CircleCap);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(keyframe, "Change Waypoint");
            Vector3 delta = newTangent - position;
            keyframe.Tangent = (invert ? delta : -delta);
        }

        if ((!invert && targetPoint == 1) || (invert && targetPoint == 2))
        {
            CustomHandles.DrawCircle(tangentPosition, 0.015f, Color.green);

        }    }


    public static void DrawSelector(PositionKeyframe keyframe)
    {
        Vector3 position = keyframe.Position;
        float size = 0.025f;
        float hitboxSize = 1.5f * size;

        // -- DOT -- // 
        Handles.color = Color.white;
        Handles.SphereHandleCap(0, position, Quaternion.identity, size, EventType.Repaint);

        // -- SELECTION -- // 
        if (CustomHandles.SelectableButton(position, hitboxSize, Color.white))
        {
            Selection.activeObject = keyframe;
            targetPoint = 0;
        }

        // -- MOVEMENT -- // 
        EditorGUI.BeginChangeCheck();
        Vector3 newPosition = Handles.FreeMoveHandle(position, Quaternion.identity, hitboxSize, 0.01f * Vector3.one, CustomHandles.NullCap);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(keyframe, "Change Waypoint");
            keyframe.Position = newPosition;
        }
    }
}