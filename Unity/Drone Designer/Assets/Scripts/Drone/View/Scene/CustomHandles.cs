﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;
using UnityEditor;

public class CustomHandles
{
    protected static int CID_X = GUIUtility.GetControlID("CustomMoveX".GetHashCode(), FocusType.Passive);
    protected static int CID_Y = GUIUtility.GetControlID("CustomMoveY".GetHashCode(), FocusType.Passive);
    protected static int CID_Z = GUIUtility.GetControlID("CustomMoveZ".GetHashCode(), FocusType.Passive);

    public static void DiscCap(int controlId, Vector3 position, Quaternion rotation, float size, EventType eventType)
    {
        if (eventType == EventType.Layout)
        {
            float distance = HandleUtility.DistanceToCircle(position, size);
            HandleUtility.AddControl(controlId, distance);
        }

        if (eventType == EventType.Repaint)
        {
            Vector3 normal = CustomHandles.GetNormal(position);
            Handles.DrawSolidDisc(position, normal, size);
        }
    }


    public static void DrawDisc(Vector3 position, float size, Color color)
    {
        Color previousColor = Handles.color;
        Handles.color = color;

        Vector3 normal = GetNormal(position);
        Handles.DrawSolidDisc(position, normal, size);

        Handles.color = previousColor;
    }

    public static void CircleCap(int controlId, Vector3 position, Quaternion rotation, float size, EventType eventType)
    {
        if (eventType == EventType.Layout)
        {
            float distance = HandleUtility.DistanceToCircle(position, size);
            HandleUtility.AddControl(controlId, distance);
        }

        if (eventType == EventType.Repaint)
        {
            Vector3 normal = CustomHandles.GetNormal(position);
            Handles.DrawWireDisc(position, normal, size);
        }
    }
    
    public static void NullCap(int controlId, Vector3 position, Quaternion rotation, float size, EventType eventType)
    {
        if (eventType == EventType.Layout)
        {
            float distance = HandleUtility.DistanceToCircle(position, size);
            HandleUtility.AddControl(controlId, distance);
        }

        if (eventType == EventType.Repaint)
        {
            
        }
    }


    public static Vector3 GetNormal(Vector3 position)
    {
        return (SceneView.currentDrawingSceneView.camera.transform.position - position).normalized;
    }


    public static void DrawBezierPath(List<Waypoint> waypoints, Color pathColor, float thiccness = 3.0f)
    {
        int numKeyframes = waypoints.Count - 1;
        for (int i = 0; i < numKeyframes; i++)
        {
            Waypoint currentKeyframe = waypoints[i];
            Waypoint nextKeyframe = waypoints[i + 1];
            bool linearStart = (currentKeyframe.JointType == JointType.Stop);
            bool linearEnd = (nextKeyframe.JointType == JointType.Stop);

            Vector3 startPos = GlobalTransform.Transfomed(currentKeyframe.Position);
            Vector3 endPos = GlobalTransform.Transfomed(nextKeyframe.Position);
            Vector3 startTangent = linearStart ? startPos : GlobalTransform.Transfomed(currentKeyframe.WorldTangent);
            Vector3 endTangent = linearEnd ? endPos : GlobalTransform.Transfomed(nextKeyframe.InverseWorldTangent);

            Handles.DrawBezier(startPos, endPos, startTangent, endTangent, pathColor, null, thiccness);
        }
    }

    public static void DrawTangent(List<Waypoint> waypoints, float scale = 1.0f, double time = -1.0)
    {
        time = (time < 0) ? TimelineUtilities.Director.time : time;
        Vector3 position = KeyframeUtil.GetPosition(waypoints, time, Vector3.zero);
        Vector3 tangentVector = KeyframeUtil.GetTangent(waypoints, time, true, true);

        Color previousColor = Handles.color;
        Handles.color = Color.red;

        float multiplier = (scale / 2.0f);
        Vector3 halfTangent = multiplier * tangentVector;
        Handles.DrawLine(position - halfTangent, position + halfTangent);
        Handles.color = previousColor;
    }



    public static bool SelectableButton(Vector3 position, float size, Color color)
    {
        int controlId = GUIUtility.GetControlID(FocusType.Passive);            
        if (HandleUtility.DistanceToCircle(position, size) <= 0.0f)
        {
            DrawCircle(position, size, color);
            HandleUtility.Repaint();

            if (Event.current.type == EventType.MouseDown)
            {
                GUIUtility.hotControl = controlId;
                return true;
            }
        }

        return false;
    }

    public static void DrawCircle(Vector3 position, float size, Color color)
    {
        Color previousColor = Handles.color;
        Handles.color = color;

        Vector3 normal = GetNormal(position);
        Handles.DrawWireDisc(position, normal, size);

        Handles.color = previousColor;
    }

    public static Vector3 MoveHandle(Vector3 initialPosition, float offset = 0.05f, float size = 0.05f)
    {
        Vector3 currentPosition = initialPosition;
        currentPosition = MoveAxis(CID_Y, currentPosition, Vector3.up, Palette.TranslucentGreen, offset, size);
        currentPosition = MoveAxis(CID_X, currentPosition, Vector3.right, Palette.TranslucentRed, offset, size);
        currentPosition = MoveAxis(CID_Z, currentPosition, Vector3.forward, Palette.TranslucentBlue, offset, size);

        return currentPosition;
    }

    private static Vector3 MoveAxis(int controlId, Vector3 position, Vector3 direction, Color color, float offset = 0.05f, float size = 0.05f)
    {
        Color previousColor = Handles.color;
        Handles.color = color;
        Vector3 offsetVector = (offset * direction);
        Vector3 offsetPositon = position + offsetVector;
        Vector3 newPosition = Handles.Slider(controlId, offsetPositon, direction, size, Handles.ArrowHandleCap, 0.01f);
        Handles.color = previousColor;

        return newPosition - offsetVector;
    }
}