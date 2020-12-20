using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEditor.Timeline;

[CustomEditor(typeof(Crazyflie))]
public class CrazyflieEditor : Editor
{
    private static Vector3 DroneSize = new Vector3(0.125f, 0.075f, 0.125f);
    private static Vector3 DroneOffset = new Vector3(0.0f, -0.0075f, 0.0f);
    private static Vector3 DefaultSnap = 0.01f * Vector3.one;


    private static Vector3 LabelOffset = new Vector3(0.0f, -0.065f, 0.0f);
    private static Vector3 AlternateLabelOffset = new Vector3(0.0f, 0.075f, 0.0f);
    private static bool showTimestamps;
    private static bool showEndpoints;

    private const string TIMESTAMPS_PREF_KEY = "show_path_timestamps_text";
    private const string ENDPOINTS_PREF_KEY = "show_path_endpoints_text";


    private Crazyflie Drone { get { return target as Crazyflie; } }
    private Tool LastTool = Tool.None;    



    private void OnEnable()
    {
        LastTool = Tools.current;
        Tools.current = Tool.None;
    }

    private void OnDisable()
    {
        Tools.current = LastTool;
    }


    /// -------- ENTRY POINTS -------- ///

    private void OnSceneGUI()
    {
        Draw(Drone, true);
    }

    public static void Draw(Crazyflie drone, bool active = false)
    {
        DrawDroneHandles(drone, active);
        DrawDroneGUI(drone);
    }



    [DrawGizmo(GizmoType.Selected | GizmoType.InSelectionHierarchy)]
    private static void DrawSelected(Crazyflie drone, GizmoType gizmo) { }


    [DrawGizmo(GizmoType.NonSelected | GizmoType.NotInSelectionHierarchy)]
    private static void DrawNonSelected(Crazyflie drone, GizmoType gizmo)
    {
        if (DronePathMenu.AlwaysOn)
        {
            // DrawDroneBounds(drone, Palette.UltraTranslucent);
            List<Waypoint> waypoints = drone.Waypoints;
            CustomHandles.DrawBezierPath(waypoints, Palette.UltraTranslucent, 2.0f);
        }
    }


    /// -------- DRONE HANDLES -------- ///

    public static void DrawDroneHandles(Crazyflie drone, bool active)
    {
        float time = (float)TimelineUtilities.Director.time;
        List<Waypoint> waypoints = drone.Waypoints;
        CustomHandles.DrawBezierPath(waypoints, Color.white, 2.0f);
        DrawWaypoints(waypoints);
        ColorKeyframeEditor.DrawColorKeyframes(drone, waypoints);

        if (active)
        {
            // DrawDroneBounds(drone, Color.white);

            EditorGUI.BeginChangeCheck();
            Vector3 updatedPosition = CustomHandles.MoveHandle(drone.transform.position, 0.075f, 0.085f);
            if (EditorGUI.EndChangeCheck())
                drone.SetWaypoint(updatedPosition, time);
        }

        //CustomHandles.DrawTangent(waypoints, 0.25f, time);
    }


    private static void DrawDroneBounds(Crazyflie drone, Color wireColor)
    {
        Vector3 offsetPosition = drone.transform.position + DroneOffset;
        Color previousColor = Handles.color;

        Handles.color = wireColor;
        Handles.DrawWireCube(offsetPosition, DroneSize);
        Handles.color = previousColor;
    }

    public static void DrawWaypoints(List<Waypoint> waypoints)
    {
        foreach (Waypoint waypoint in waypoints)
            WaypointEditor.DrawSelector(waypoint, showTimestamps);


        if (showEndpoints)
        {
            Waypoint firstWaypoint = waypoints[0];
            Waypoint lastWaypoint = waypoints[waypoints.Count - 1];
            Vector3 endOffset = (waypoints.Count < 2) ? AlternateLabelOffset : LabelOffset;

            Handles.Label(firstWaypoint.Position + LabelOffset, "START", CustomGUI.TitleStyle);
            Handles.Label(lastWaypoint.Position + endOffset, "END", CustomGUI.TitleStyle);
        }
    }



    /// -------- DRONE GUI -------- ///


    public static void DrawDroneGUI(Crazyflie drone)
    {
        showTimestamps = EditorPrefs.GetBool(TIMESTAMPS_PREF_KEY, false);
        showEndpoints = EditorPrefs.GetBool(ENDPOINTS_PREF_KEY, false);

        Rect toolsRect = new Rect(20, 20, 300, 200);
        CustomGUI.Window(toolsRect, "Drone Tools", DrawDroneTools, drone);
    }


    private static void DrawDroneTools(Crazyflie drone)
    {
        EditorGUI.BeginChangeCheck();
        bool timestampsEnabled = EditorGUILayout.Toggle("Show Timestamps", showTimestamps);
        bool endpointsEnabled = EditorGUILayout.Toggle("Show Start/End Text", showEndpoints);

        if (EditorGUI.EndChangeCheck())
        {
            EditorPrefs.SetBool(TIMESTAMPS_PREF_KEY, timestampsEnabled);
            EditorPrefs.SetBool(ENDPOINTS_PREF_KEY, endpointsEnabled);
        }
        
        EditorGUILayout.Space(30.0f);
        EditorGUILayout.BeginHorizontal();
        float buttonWidth = 140.0f;

        if (GUILayout.Button("Add Color Keyframe", GUILayout.Width(buttonWidth)))
            drone.SetColorKeyframe(drone.LightColor, drone.Time);

        if (GUILayout.Button("Add Waypoint", GUILayout.Width(buttonWidth)))
            drone.SetWaypoint(drone.transform.position, drone.Time);

        EditorGUILayout.EndHorizontal();
    }
}
