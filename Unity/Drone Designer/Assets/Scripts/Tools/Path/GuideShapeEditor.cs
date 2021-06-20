using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityEditor.Timeline;

public class GuideShapeEditor<T> : Editor where T : GuideShape
{
    protected T Target { get { return target as T; } }

    protected static void DrawPointHandles(List<AttachmentPoint> points, bool dyanmic)
    {
        AttachmentPoint selectedPoint = points.FirstOrDefault(p => p.Selected);
        if (selectedPoint != null)
        {
            Vector3 position = selectedPoint.Position;
            CustomHandles.DrawCircle(position, 0.035f, Color.white);
            Crazyflie[] drones = GameObject.FindObjectsOfType<Crazyflie>();
            foreach (Crazyflie drone in drones)
            {
                if (CustomHandles.SelectableButton(drone.transform.position, 0.075f, Color.green))
                {
                    if (dyanmic)
                    {
                        drone.Attach(selectedPoint);
                        drone.UpdateView();
                        selectedPoint.Selected = false;
                        return;
                    }
                    else
                    {
                        drone.SetWaypoint(position, drone.Time);
                        selectedPoint.Drone?.Release();
                        selectedPoint.Drone = null;
                        selectedPoint.Selected = false;
                        return;
                    }
                }
            }
        }

        foreach (AttachmentPoint point in points)
        {
            bool hasDrone = point.Drone != null;
            Vector3 position = point.Position;
            CustomHandles.DrawDisc(position, 0.0075f, Color.white);
            Color circleColor = hasDrone ? Color.red : Color.green;

            if (CustomHandles.SelectableButton(position, 0.035f, circleColor))
            {
                if (hasDrone)
                {
                    Crazyflie drone = point.Drone;
                    point.Selected = false;
                    point.Drone.Release();
                    drone.UpdateView();
                    return;
                }
                else
                {
                    bool currentlySelected = point.Selected;
                    foreach (AttachmentPoint otherPoint in points)
                        otherPoint.Selected = false;

                    point.Selected = !currentlySelected;
                    return;
                }
            }
        }
    }
}