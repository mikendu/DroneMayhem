using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;

public enum JointType
{
    Linear,
    Continuous
}

[Serializable]
[CustomStyle("Waypoint")]
public class PositionKeyframe : DroneKeyframe
{
    public JointType JointType = JointType.Linear;
    public Vector3 Position = Vector3.zero;
    public Vector3 Tangent = 0.25f * Vector3.right;
}