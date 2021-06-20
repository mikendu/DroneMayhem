using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;


[Serializable]
[CustomStyle("Waypoint")]
public class GuideKeyframe : Marker, INotification
{
    public PropertyName id => new PropertyName();
    public JointType JointType = JointType.Continuous;
    public Vector3 Position = Vector3.zero;
    public Vector3 Scale = Vector3.one; 
    public Quaternion Rotation = Quaternion.identity;

    public void SetPosition(Vector3 position)
    {
        this.Position = position;
    }

    public void SetScale(Vector3 scale)
    {
        this.Scale = scale;
    }

    public void SetRotation(Quaternion rotation)
    {
        this.Rotation = rotation;
    }

    public void Set(Transform transform)
    {
        this.Position = transform.position;
        this.Rotation = transform.rotation;
        this.Scale = transform.lossyScale;
    }


    public DynamicGuide Guide
    {
        get
        {
            GuideTrack track = this.parent as GuideTrack;
            return track?.Guide;
        }
    }
}