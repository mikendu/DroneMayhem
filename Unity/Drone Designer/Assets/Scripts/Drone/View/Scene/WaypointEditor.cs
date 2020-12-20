using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;


[CustomEditor(typeof(Waypoint))]
public class WaypointEditor : CustomEditor<Waypoint>
{
    protected static int targetPoint = 0;
    protected static Vector3 LabelOffset = new Vector3(0.0f, -0.015f, 0.0f);

    protected override void OnEnable()
    {
        base.OnEnable();
        targetPoint = 0;
    }

    protected override void OnDrawScene(SceneView scene)
    {
        Waypoint keyframe = Target;
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


        // -- GUI -- //
        DrawGUI(keyframe);
    }

    protected void DrawTangent(Waypoint keyframe, bool invert)
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


    public static void DrawSelector(Waypoint keyframe, bool showTime = false)
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

        // -- LABEL -- //
        if (showTime)
        {
            string timestamp = $"{keyframe.time.ToString("0.0")} s";
            Handles.Label(position + LabelOffset, timestamp, CustomGUI.LabelStyle);
        }
    }

    private static void FreeMove(Waypoint keyframe, Vector3 position, float size, Handles.CapFunction capFunction, Action<Vector3> applyFunction)
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

    private static void MoveHandle(Waypoint keyframe, Vector3 position, float size, float offset, Action<Vector3> applyFunction)
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

    public static void DrawGUI(Waypoint waypoint)
    {
        Rect toolsRect = new Rect(20, 290, 300, 300);
        CustomGUI.Window(toolsRect, "Waypoint", DrawWaypointTools, waypoint);
    }

    private static void DrawWaypointTools(Waypoint waypoint)
    {
        Crazyflie drone = waypoint.Drone;

        EditorGUI.BeginChangeCheck();
        float updatedTime = EditorGUILayout.FloatField("Time (seconds)", (float)waypoint.time);

        JointType updatedJointType = (JointType)EditorGUILayout.EnumPopup("Joint Type", waypoint.JointType);
        EditorGUILayout.Space(10);

        Vector3 updatedPosition = EditorGUILayout.Vector3Field(new GUIContent("Position"), waypoint.Position);
        EditorGUILayout.Space(10);

        GUI.enabled = (updatedJointType == JointType.Continuous);
        Vector3 updatedTangent = EditorGUILayout.Vector3Field(new GUIContent("Tangent"), waypoint.Tangent);
        EditorGUILayout.Space(30);
        GUI.enabled = true;

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(waypoint, "Change Waypoint");
            waypoint.time = Mathf.Max(0, updatedTime);
            waypoint.Position = updatedPosition;
            waypoint.Tangent = updatedTangent;
            waypoint.JointType = updatedJointType;
            drone.UpdateView();
            TimelineEditor.Refresh(RefreshReason.ContentsModified);
        }

        if (GUILayout.Button("Delete"))
            drone.RemoveWaypoint(waypoint);
    }
}