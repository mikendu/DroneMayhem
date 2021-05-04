using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

[Serializable]
public class AttachmentPoint
{
    public bool Selected;
    public Vector3 LocalPosition;
    public Transform Parent;
    public Crazyflie Drone;


    public Vector3 Position { get { return Parent.TransformPoint(LocalPosition);  } }

    public AttachmentPoint(Transform parent, Vector3 position)
    {
        this.Parent = parent;
        this.LocalPosition = position;
    }

    public void Apply()
    {
        if (Drone != null)
            Drone.SetWaypoint(Position, TimelineUtilities.CurrentTime);
    }
}