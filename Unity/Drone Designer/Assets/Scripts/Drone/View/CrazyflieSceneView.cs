using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public enum PointType
{
    Start,
    End, 
    StartTangent,
    EndTangent,
}

[CustomEditor(typeof(Crazyflie))]
[InitializeOnLoad]
public class CrazyflieSceneView : Editor
{
    private static Color SelectedColor = new Color(1, 1, 1, 0.65f);
    private static Color InactiveColor = new Color(1, 1, 1, 0.275f);
    private static Vector3 DroneSize = new Vector3(0.125f, 0.075f, 0.125f);
    private static Vector3 DroneOffset = new Vector3(0.0f, -0.0075f, 0.0f);

    Crazyflie drone;
    int segmentId = -1;
    PointType pointType;
    GUIStyle labelStyle;

    public void OnEnable()
    {
        drone = target as Crazyflie;

        if (labelStyle == null)
        {
            labelStyle = new GUIStyle();
            labelStyle.normal.textColor = Color.white;
        }
    }

    public void OnSceneGUI()
    {
        var waypoints = drone?.PositionKeyframes;
        if (drone == null || waypoints == null || waypoints.Count == 0)
            return;

        DrawBezierHandles(drone);
    }


    void DrawBezierHandles(Crazyflie drone)
    {
        var waypoints = drone?.PositionKeyframes;
        if (drone == null || waypoints == null || waypoints.Count == 0)
            return;

        Color previousColor = Handles.color;
        int numSegments = waypoints.Count - 1;
        for (int i = 0; i < numSegments; i++)
        {
            PositionKeyframe segment = waypoints[i];
            DrawMoveablePoint(waypoints, i, PointType.Start, 0.025f);

            if (i == (numSegments - 1))
                DrawMoveablePoint(waypoints, i, PointType.End, 0.025f);

            if (segment.JointType == JointType.Continuous)
            {
                DrawTangentLine(waypoints, i, PointType.StartTangent, InactiveColor);
                DrawTangentLine(waypoints, i - 1, PointType.EndTangent, InactiveColor);
                DrawMoveablePoint(waypoints, i, PointType.StartTangent, 0.015f);
                DrawMoveablePoint(waypoints, i - 1, PointType.EndTangent, 0.015f);
            }
        }

        // Labels
        Handles.Label(waypoints[0].Position + new Vector3(0.0f, -0.05f, 0.0f), "START", labelStyle);
        Handles.Label(waypoints[waypoints.Count - 1].Position + new Vector3(0.0f, 0.05f, 0.0f), "END", labelStyle);
        
        Handles.color = previousColor;
    }

    
    void DrawMoveablePoint(List<PositionKeyframe> waypoints, int segmentId, PointType pointType, float size)
    {
        Vector3 point = GetPoint(waypoints, segmentId, pointType);
        bool selected = (this.segmentId == segmentId && this.pointType == pointType);
        Color drawColor = selected ? SelectedColor : InactiveColor;
        Handles.color = drawColor;

        float hitboxSize = 2.0f * size;
        if (HandleUtility.DistanceToCircle(point, hitboxSize) <= 0.0f)
        {
            Handles.color = Color.white;
            Handles.DrawWireCube(point, hitboxSize * Vector3.one);
            HandleUtility.Repaint();
        }

        if (Handles.Button(point, Quaternion.identity, size, hitboxSize, Handles.SphereHandleCap))
        {
            this.segmentId = segmentId;
            this.pointType = pointType;
        }

        if (selected)
        {
            EditorGUI.BeginChangeCheck();
            Vector3 newPoint = Handles.PositionHandle(point, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, "Change Drone Waypoint");
                SetPoint(waypoints, segmentId, pointType, newPoint);
                drone.UpdateView();
            }
        }

        // TODO - Duration
        if (pointType == PointType.Start || pointType == PointType.End)
        {
            PositionKeyframe keyframe = (pointType == PointType.Start) ? waypoints[segmentId] : waypoints[segmentId + 1];
            string timestamp = $"{keyframe.time.ToString("0.0")} s";
            Handles.Label(point + new Vector3(0.0f, -0.015f, 0.0f), timestamp, labelStyle);
        }
    }

    void DrawTangentLine(List<PositionKeyframe> waypoints, int segmentIndex, PointType pointType, Color color)
    {
        Handles.color = color;
        PositionKeyframe currentSegment = waypoints[segmentIndex];
        PositionKeyframe nextSegment = waypoints[segmentIndex + 1];

        if (pointType == PointType.StartTangent)
            Handles.DrawLine(currentSegment.Position, currentSegment.Position + currentSegment.StartTangent);

        if (pointType == PointType.EndTangent)
            Handles.DrawLine(nextSegment.Position, nextSegment.Position + currentSegment.EndTangent);
    }

    Vector3 GetPoint(List<PositionKeyframe> waypoints, int segmentIndex, PointType pointType)
    {
        PositionKeyframe currentSegment = waypoints[segmentIndex];
        PositionKeyframe nextSegment = waypoints[segmentIndex + 1];

        switch (pointType)
        {
            case PointType.Start:
                return currentSegment.Position;

            case PointType.End:
                return nextSegment.Position;

            case PointType.StartTangent:
                return currentSegment.Position + currentSegment.StartTangent;

            case PointType.EndTangent:
                return nextSegment.Position + currentSegment.EndTangent;
        }

        return Vector3.zero;
    }


    void SetPoint(List<PositionKeyframe> waypoints, int segmentIndex, PointType pointType, Vector3 value)
    {
        PositionKeyframe currentSegment = waypoints[segmentIndex];
        PositionKeyframe nextSegment = waypoints[segmentIndex + 1];
        PositionKeyframe previousSegement = (segmentIndex > 0) ? waypoints[segmentIndex - 1] : null;
        Vector3 tangentDirection;

        switch (pointType)
        {
            case PointType.Start:
                currentSegment.Position = value;
                return;

            case PointType.End:
                nextSegment.Position = value;
                return;

            case PointType.StartTangent:
                tangentDirection = value - currentSegment.Position;
                currentSegment.StartTangent = tangentDirection;
                if (previousSegement != null)
                    previousSegement.EndTangent = -tangentDirection;
                return;

            case PointType.EndTangent:
                tangentDirection = value - nextSegment.Position;
                currentSegment.EndTangent = tangentDirection;
                nextSegment.StartTangent = -tangentDirection;
                return;
        }
    }
    

    [DrawGizmo(GizmoType.InSelectionHierarchy)]
    static void DrawSelected(Crazyflie drone, GizmoType gizmo)
    {
        //DrawDroneBounds(drone, SelectedColor);
        //DrawDroneBounds(drone, InactiveColor);
        DrawBezier(drone, true);
    }

    [DrawGizmo(GizmoType.NotInSelectionHierarchy)]
    static void DrawNonSelected(Crazyflie drone, GizmoType gizmo)
    {
        //DrawDroneBounds(drone, InactiveColor);
        if (DronePathMenu.AlwaysOn)
            DrawBezier(drone, false);
    }

    static void DrawBezier(Crazyflie drone, bool selected)
    {
        var waypoints = drone?.PositionKeyframes;
        if (drone == null || waypoints == null || waypoints.Count == 0)
            return;

        
        float lineWidth = selected ? 3.0f : 2.0f;
        Color lineColor = selected ? SelectedColor : InactiveColor;

        int numSegments = waypoints.Count - 1;
        for (int i = 0; i < numSegments; i++)
        {
            PositionKeyframe currentSegment = waypoints[i];
            PositionKeyframe nextSegment = waypoints[i + 1];
            bool linearStart = (currentSegment.JointType == JointType.Linear);
            bool linearEnd = (nextSegment.JointType == JointType.Linear);

            Vector3 startPos = currentSegment.Position;
            Vector3 endPos = nextSegment.Position;
            Vector3 startTangent = linearStart ? startPos : startPos + currentSegment.StartTangent;
            Vector3 endTangent = linearEnd ? endPos : endPos + currentSegment.EndTangent;

            Handles.DrawBezier(
                startPos, 
                endPos,
                startTangent,
                endTangent,
                lineColor,
                null,
                lineWidth
            );
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
}
 