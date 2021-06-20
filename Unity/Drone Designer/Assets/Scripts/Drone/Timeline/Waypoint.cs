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
    Stop,
    Continuous
}

[Serializable]
[CustomStyle("Waypoint")]
public class Waypoint : DroneKeyframe
{
    public JointType JointType = JointType.Stop;
    public Vector3 Position = Vector3.zero;
    public Vector3 Tangent = 0.25f * Vector3.right;

    public void SetPosition(Vector3 position)
    {
        this.Position = position;
    }

    public void SetTangent(Vector3 tangentWorldPos)
    {
        this.Tangent = (tangentWorldPos - Position);
    }

    public void SetInverseTangent(Vector3 tangentWorldPos)
    {
        this.Tangent = (Position - tangentWorldPos);
    }

    public Vector3 WorldTangent { get { return TangentVector(Position + Tangent); } }
    public Vector3 InverseWorldTangent { get { return TangentVector(Position - Tangent); } }

    private Vector3 TangentVector(Vector3 tangentVector)
    {
        if (this.JointType == JointType.Stop)
            return Position;

        return tangentVector;
    }
}