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
            List<PositionKeyframe> waypoints = drone.PositionKeyframes;
            CustomHandles.DrawBezierPath(waypoints, Palette.UltraTranslucent, 2.0f);
        }
    }


    /// -------- DRONE HANDLES -------- ///

    public static void DrawDroneHandles(Crazyflie drone, bool active)
    {
        if (active)
            DrawDroneBounds(drone, Color.white);

        List<PositionKeyframe> waypoints = drone.PositionKeyframes;        

        CustomHandles.DrawBezierPath(waypoints, Color.white, 2.0f);
        DrawWaypoints(waypoints);
        ColorKeyframeEditor.DrawColorKeyframes(drone, waypoints);
    }


    private static void DrawDroneBounds(Crazyflie drone, Color wireColor)
    {
        Vector3 offsetPosition = drone.transform.position + DroneOffset;
        Color previousColor = Handles.color;

        Handles.color = wireColor;
        Handles.DrawWireCube(offsetPosition, DroneSize);
        Handles.color = previousColor;
    }

    public static void DrawWaypoints(List<PositionKeyframe> waypoints)
    {
        foreach (PositionKeyframe waypoint in waypoints)
            WaypointEditor.DrawSelector(waypoint);
    }



    /// -------- DRONE GUI -------- ///


    public static void DrawDroneGUI(Crazyflie drone)
    {

    }



    /*
    private static Color SelectedColor = new Color(1, 1, 1, 0.65f);
    private static Color InactiveColor = new Color(1, 1, 1, 0.275f);
    private static Color ToolsBackground = new Color(0, 0, 0, 0.375f);
    private static Vector3 DroneSize = new Vector3(0.125f, 0.075f, 0.125f);
    private static Vector3 DroneOffset = new Vector3(0.0f, -0.0075f, 0.0f);

    DroneKeyframe selectedKeyframe;
    PointType selectedPointType;
    Tool LastTool = Tool.None;
    bool showText;

    GUIStyle labelStyle;
    GUIStyle titleStyle;
    GUIStyle headerStyle;

    public void OnEnable()
    {
        LastTool = Tools.current;
        Tools.current = Tool.None;

        if (labelStyle == null)
        {
            labelStyle = new GUIStyle();
            labelStyle.normal.textColor = Color.white;
        }

        if (titleStyle == null) titleStyle = new GUIStyle();
        titleStyle.normal.textColor = Color.white;
        titleStyle.fontStyle = FontStyle.Normal;
        titleStyle.fontSize = 15;

        if (headerStyle == null) headerStyle = new GUIStyle();
        headerStyle.normal.textColor = Color.white;
        headerStyle.fontStyle = FontStyle.Normal;
        headerStyle.fontSize = 20;
    }

    void OnDisable()
    {
        Tools.current = LastTool;
    }


    public void OnSceneGUI()
    {
        showText = EditorPrefs.GetBool("drone_text", true);
        var waypoints = Drone?.PositionKeyframes;
        if (Drone == null)
            return;

        if (waypoints != null && waypoints.Count > 0)
        {
            DrawBezierHandles();
            DrawColorHandles();

        }
        DrawGUI();
    }

    void DrawColorHandles()
    {
        var colorKeyframes = Drone?.ColorKeyframes;
        var positionKeyframes = Drone.PositionKeyframes;
        if (Drone == null || colorKeyframes == null || colorKeyframes.Count == 0)
            return;

        Color previousColor = Handles.color;
        Vector3 previousPosition = Vector3.zero;
        float offset = 0.0375f;

        for (int i = 0; i < colorKeyframes.Count; i++)
        {
            ColorKeyframe keyframe = colorKeyframes[i];
            Vector3 currentPosition = KeyframeUtil.GetPosition(positionKeyframes, keyframe.time, Drone.transform.position);
            bool closeToPrevious = Vector3.Distance(currentPosition, previousPosition) < 0.025f;

            if (i > 0 && closeToPrevious)
                offset += 0.0375f;
            else
                offset = 0.0375f;

            DrawColorPoint(keyframe, currentPosition, offset, 0.0125f);
            previousPosition = currentPosition;
        }
        Handles.color = previousColor;
    }

    void DrawColorPoint(ColorKeyframe keyframe, Vector3 rawPosition, float offset, float size)
    {
        Color frameColor = keyframe.LightColor;
        bool selected = (this.selectedKeyframe == keyframe);
        frameColor.a = selected ? 1.0f : 0.35f;

        float hitboxSize = 1.5f * size;
        Vector3 offsetPoint = rawPosition + new Vector3(0.0f, offset, 0.0f);

        Handles.color = (selected) ? SelectedColor : InactiveColor;
        Handles.DrawLine(offsetPoint, rawPosition);
        Handles.color = frameColor;

        if (HandleUtility.DistanceToCircle(offsetPoint, hitboxSize) <= 0.0f)
        {
            Handles.color = Color.white;
            //Handles.DrawWireCube(point, hitboxSize * Vector3.one);
            Vector3 normal = ViewUtility.GetNormal(offsetPoint);
            Handles.DrawWireDisc(offsetPoint, normal, hitboxSize);
            HandleUtility.Repaint();
        }

        if (Handles.Button(offsetPoint, Quaternion.identity, size * 0.575f, hitboxSize, ViewUtility.DiscCap))
        {
            this.selectedKeyframe = keyframe;
            //SetInspectorLock(true);
        }

        if (selected)
        {
            Vector3 normal = ViewUtility.GetNormal(offsetPoint);
            Handles.DrawWireDisc(offsetPoint, normal, size);
        }
    }

    void DrawBezierHandles()
    {
        var waypoints = Drone?.PositionKeyframes;
        if (Drone == null || waypoints == null || waypoints.Count == 0)
            return;

        Color previousColor = Handles.color;
        int numSegments = waypoints.Count - 1;
        for (int i = 0; i < numSegments; i++)
        {
            DrawMoveablePoint(waypoints, i, PointType.Start, 0.015f);

            if (waypoints[i].JointType == JointType.Continuous)
            {
                DrawTangentLine(waypoints, i, PointType.StartTangent, InactiveColor);
                DrawTangentLine(waypoints, i - 1, PointType.EndTangent, InactiveColor);
                DrawMoveablePoint(waypoints, i, PointType.StartTangent, 0.010f);
                DrawMoveablePoint(waypoints, i - 1, PointType.EndTangent, 0.010f);
            }
        }

        // Last point
        DrawMoveablePoint(waypoints, waypoints.Count - 1, PointType.Start, 0.015f);

        // Labels
        if (showText)
        {
            Handles.Label(waypoints[0].Position + new Vector3(0.0f, -0.065f, 0.0f), "START", titleStyle);
            Handles.Label(waypoints[waypoints.Count - 1].Position + new Vector3(0.0f, 0.075f, 0.0f), "END", titleStyle);
        }
        
        Handles.color = previousColor;
    }

    
    void DrawMoveablePoint(List<PositionKeyframe> waypoints, int index, PointType pointType, float size)
    {
        Vector3 point = GetPoint(waypoints, index, pointType);
        bool selected = (selectedKeyframe == waypoints[index] && selectedPointType == pointType);
        Color drawColor = selected ? Color.white : InactiveColor;
        Handles.color = drawColor;
        
        float hitboxSize = 1.5f * size;
        if (HandleUtility.DistanceToCircle(point, hitboxSize) <= 0.0f)
        {
            Handles.color = Color.white;
            //Handles.DrawWireCube(point, hitboxSize * Vector3.one);

            Vector3 normal = ViewUtility.GetNormal(point);
            Handles.DrawWireDisc(point, normal, hitboxSize);
            HandleUtility.Repaint();
        }

        if (Handles.Button(point, Quaternion.identity, size, hitboxSize, Handles.SphereHandleCap))
        {
            selectedKeyframe = waypoints[index];
            selectedPointType = pointType;

            if (pointType == PointType.End)
            {
                selectedKeyframe = waypoints[index + 1];
                selectedPointType = PointType.Start;
            }
        }

        if (selected)
        {
            Vector3 normal = ViewUtility.GetNormal(point);
            Handles.DrawWireDisc(point, normal, size);

            EditorGUI.BeginChangeCheck();
            Vector3 newPoint = Handles.PositionHandle(point, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                SetPoint(waypoints, index, newPoint, pointType);
                TimelineEditor.Refresh(RefreshReason.ContentsModified);
                Drone.UpdateView();
            }
        }

        if (showText && (pointType == PointType.Start || pointType == PointType.End))
        {
            PositionKeyframe keyframe = (pointType == PointType.Start) ? waypoints[index] : waypoints[index + 1];
            string timestamp = $"{keyframe.time.ToString("0.0")} s";
            Handles.Label(point + new Vector3(0.0f, -0.015f, 0.0f), timestamp, labelStyle);
        }
    }

    void DrawTangentLine(List<PositionKeyframe> waypoints, int index, PointType pointType, Color color)
    {
        Handles.color = color;
        PositionKeyframe currentSegment = waypoints[index];
        PositionKeyframe nextSegment = waypoints[index + 1];

        if (pointType == PointType.StartTangent)
            Handles.DrawLine(currentSegment.Position, currentSegment.Position + currentSegment.StartTangent);

        if (pointType == PointType.EndTangent)
            Handles.DrawLine(nextSegment.Position, nextSegment.Position + currentSegment.EndTangent);
    }

    Vector3 GetPoint(List<PositionKeyframe> waypoints, int index, PointType pointType)
    {
        PositionKeyframe current = waypoints[index];
        PositionKeyframe next = ((index + 1) < waypoints.Count) ? waypoints[index + 1] : null;

        switch (pointType)
        {
            case PointType.Start:
                return current.Position;

            case PointType.End:
                return next.Position;

            case PointType.StartTangent:
                return current.Position + current.StartTangent;

            case PointType.EndTangent:
                return next.Position + current.EndTangent;
        }

        return Vector3.zero;
    }


    void SetPoint(List<PositionKeyframe> waypoints, int index, Vector3 value, PointType pointType)
    {
        PositionKeyframe current = waypoints[index];
        PositionKeyframe next = ((index + 1) < waypoints.Count) ? waypoints[index + 1] : null;
        PositionKeyframe previous = (index > 0) ? waypoints[index - 1] : null;
        Vector3 tangentDirection;

        switch (pointType)
        {
            case PointType.Start:
                Undo.RecordObject(current, "Change Drone Waypoint");
                current.Position = value;
                return;

            case PointType.End:
                Undo.RecordObject(next, "Change Drone Waypoint");
                next.Position = value;
                return;

            case PointType.StartTangent:
                Undo.RecordObject(current, "Change Drone Waypoint");
                tangentDirection = value - current.Position;
                current.StartTangent = tangentDirection;

                if (previous != null)
                {
                    Undo.RecordObject(previous, "Change Drone Waypoint");
                    previous.EndTangent = -tangentDirection;
                }

                return;

            case PointType.EndTangent:
                Undo.RecordObject(current, "Change Drone Waypoint");
                Undo.RecordObject(next, "Change Drone Waypoint");
                tangentDirection = value - next.Position;
                current.EndTangent = tangentDirection;
                next.StartTangent = -tangentDirection;
                return;
        }
    }

    static void DrawDroneBounds(Crazyflie drone, Color wireColor)
    {
        if (drone != null)
        {
            Color previousColor = Handles.color;
            Handles.color = wireColor;
            Handles.DrawWireCube(drone.transform.position + DroneOffset, DroneSize);
            Handles.color = previousColor;
        }
    }

    void DrawGUI()
    {
        //float sceneHeight = SceneView.currentDrawingSceneView.camera.pixelHeight;
        Rect guiRect = new Rect(20, 20, 300, 400);

        int controlId = GUIUtility.GetControlID(FocusType.Passive);
        Event currentEvent = Event.current;

        if (currentEvent.type == EventType.MouseDown && guiRect.Contains(currentEvent.mousePosition))
            GUIUtility.hotControl = controlId;

        Color guiColor = GUI.color;
        Handles.BeginGUI();        
        GUILayout.BeginArea(guiRect);
        GUI.backgroundColor = ToolsBackground;
        EditorGUILayout.BeginVertical("Window");
        CustomGUI.SetLabelColors();

        EditorGUILayout.BeginVertical(EditorStyles.inspectorFullWidthMargins);
        CustomGUI.DrawTitle("Drone Tools", headerStyle);
        CustomGUI.DrawSplitter(15, 15, 3.0f);

        DrawTextOption();
        DrawAddButtons();
        GUILayout.Space(15.0f);
        DrawSelected();
        EditorGUILayout.EndVertical();

        CustomGUI.UnsetLabelColors();
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndVertical();
        GUILayout.EndArea();
        Handles.EndGUI();
        GUI.color = guiColor;

    }

    void DrawTextOption()
    {
        EditorGUI.BeginChangeCheck();
        bool newValue = EditorGUILayout.Toggle("Show Text", showText);
        if (EditorGUI.EndChangeCheck())
            EditorPrefs.SetBool("drone_text", newValue);
    }

    void DrawAddButtons()
    {
        EditorGUILayout.Space(30.0f);
        if (GUILayout.Button("Add Color Keyframe"))
        {
            DroneKeyframe keyframe = Drone.SetColorKeyframe();
            selectedKeyframe = keyframe;
        }

        if (GUILayout.Button("Add Waypoint"))
        {
            DroneKeyframe keyframe = Drone.SetWaypoint();
            selectedKeyframe = keyframe;
            selectedPointType = PointType.Start;
        }

        EditorGUILayout.Space(30.0f);
    }

    void DrawSelected()
    {
        if (selectedKeyframe == null)
            return;

        if (selectedKeyframe is ColorKeyframe)
            DrawColorKeyframe(Drone, selectedKeyframe as ColorKeyframe);

        if (selectedKeyframe is PositionKeyframe)
            DrawPositionKeyframe(Drone, selectedKeyframe as PositionKeyframe, selectedPointType);
    }

    void DrawColorKeyframe(Crazyflie drone, ColorKeyframe keyframe)
    {
        EditorGUILayout.BeginVertical();
        CustomGUI.DrawSplitter(5, 5);
        CustomGUI.DrawTitle("Color Keyframe", titleStyle);
        CustomGUI.DrawSplitter(5, 5);

        EditorGUI.BeginChangeCheck();
        float updatedTime = EditorGUILayout.FloatField("Time (seconds)", (float)keyframe.time);
        Color updatedColor = EditorGUILayout.ColorField(new GUIContent("Light Color"), keyframe.LightColor, false, false, false);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(keyframe, "Change Color Keyframe");
            keyframe.time = Mathf.Max(0, updatedTime);
            keyframe.LightColor = updatedColor;
            drone.UpdateView();
            TimelineEditor.Refresh(RefreshReason.ContentsModified);
        }

        if (GUILayout.Button("Delete"))
            drone.RemoveColorKeyframe(keyframe);


        EditorGUILayout.EndVertical();
    }

    void DrawPositionKeyframe(Crazyflie drone, PositionKeyframe keyframe, PointType pointType)
    {
        if (pointType == PointType.EndTangent || pointType == PointType.StartTangent)
            return;

        List<PositionKeyframe> waypoints = drone.PositionKeyframes;
        EditorGUILayout.BeginVertical();
        CustomGUI.DrawSplitter(5, 5);
        CustomGUI.DrawTitle("Waypoint", titleStyle);
        CustomGUI.DrawSplitter(5, 5);

        EditorGUI.BeginChangeCheck();
        float updatedTime = EditorGUILayout.FloatField("Time (seconds)", (float)keyframe.time);
        GUI.enabled = (keyframe != waypoints[0] && keyframe != waypoints[waypoints.Count - 1]);

        JointType updatedJointType = (JointType)EditorGUILayout.EnumPopup("Joint Type", keyframe.JointType);
        GUI.enabled = true;
        Vector3 updatedPosition = EditorGUILayout.Vector3Field(new GUIContent("Position"), keyframe.Position);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(keyframe, "Change Waypoint");
            keyframe.time = Mathf.Max(0, updatedTime);
            keyframe.Position = updatedPosition;
            keyframe.JointType = updatedJointType;
            drone.UpdateView();
            TimelineEditor.Refresh(RefreshReason.ContentsModified);
        }

        if (GUILayout.Button("Delete"))
            drone.RemoveWaypoint(keyframe);

        EditorGUILayout.EndVertical();
    }*/
}
