using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;

public class GuideEditor<T> : Editor where T : UnityEngine.Object
{
    protected T Target { get { return target as T; } }

    protected static void DrawPointHandles(List<AttachmentPoint> points)
    {
        foreach (AttachmentPoint point in points)
        {
            Vector3 position = point.Position;
            CustomHandles.DrawDisc(position, 0.0075f, Color.white);
            if (CustomHandles.SelectableButton(position, 0.035f, Color.green))
            {
                foreach (AttachmentPoint otherPoint in points)
                    otherPoint.Selected = false;

                point.Selected = true;
            }

            if (point.Selected)
            {
                CustomHandles.DrawCircle(position, 0.035f, Color.white);
                Crazyflie[] drones = GameObject.FindObjectsOfType<Crazyflie>();
                foreach (Crazyflie drone in drones)
                {
                    if (CustomHandles.SelectableButton(drone.transform.position, 0.075f, Color.green))
                    {
                        drone.SetWaypoint(position, drone.Time);
                        point.Drone = drone;
                        point.Selected = false;
                    }
                }
            }
        }
    }
}