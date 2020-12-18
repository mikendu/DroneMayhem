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
        {
            CustomHandles.DrawCircle(keyframe.Position, 0.0375f, Color.yellow);
            MoveHandle(keyframe, keyframe.Position, 0.06f, 0.045f, keyframe.SetPosition);
        }
        else
        {
            CustomHandles.DrawCircle(keyframe.Position, 0.0375f, Color.white);
        }

        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Delete)
            drone.RemoveWaypoint(Target);

    }

    protected void DrawTangent(PositionKeyframe keyframe, bool invert)
    {
        Vector3 position = keyframe.Position;
        Vector3 tangentPosition = invert ? keyframe.InverseWorldTangent : keyframe.WorldTangent;
        Action<Vector3> applyFunction = invert ? (Action<Vector3>)keyframe.SetInverseTangent : keyframe.SetTangent;

        Handles.color = Color.white;
        Handles.SphereHandleCap(0, tangentPosition, Quaternion.identity, 0.01f, EventType.Repaint);

        Handles.color = Palette.Translucent;
        Handles.DrawLine(position, tangentPosition);

        if (CustomHandles.SelectableButton(tangentPosition, 0.015f, Color.white))
            targetPoint = (invert ? 2 : 1);

        // Circle
        FreeMove(keyframe, tangentPosition, 0.015f, CustomHandles.CircleCap, applyFunction);

        // Selected
        if ((!invert && targetPoint == 1) || (invert && targetPoint == 2))
        {
            CustomHandles.DrawCircle(tangentPosition, 0.015f, Color.yellow);
            MoveHandle(keyframe, tangentPosition, 0.04f, 0.025f, applyFunction);
        }
    }


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
        FreeMove(keyframe, position, hitboxSize, CustomHandles.NullCap, keyframe.SetPosition);
    }

    private static void FreeMove(PositionKeyframe keyframe, Vector3 position, float size, Handles.CapFunction capFunction, Action<Vector3> applyFunction)
    {
        EditorGUI.BeginChangeCheck();
        Vector3 newPosition = Handles.FreeMoveHandle(position, Quaternion.identity, size, DefaultSnap, capFunction);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(keyframe, "Change Waypoint");
            applyFunction.Invoke(newPosition);
            keyframe.Drone.UpdateView();
        }

    }

    private static void MoveHandle(PositionKeyframe keyframe, Vector3 position, float size, float offset, Action<Vector3> applyFunction)
    {
        EditorGUI.BeginChangeCheck();
        Vector3 updatedPosition = CustomHandles.MoveHandle(position, offset, size);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(keyframe, "Change Waypoint");
            applyFunction.Invoke(updatedPosition);
            keyframe.Drone.UpdateView();
        }

    }
}