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
public class PositionKeyframe : Marker, INotification
{
    public JointType JointType = JointType.Linear;
    public Vector3 Position = Vector3.zero;
    public Vector3 StartTangent = 0.25f * Vector3.left;
    public Vector3 EndTangent = 0.25f * Vector3.right;

    public PropertyName id => new PropertyName();
}