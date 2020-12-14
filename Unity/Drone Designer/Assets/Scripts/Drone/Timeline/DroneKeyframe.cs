using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;


public abstract class DroneKeyframe : Marker, INotification
{
    public PropertyName id => new PropertyName();

    public Crazyflie Drone
    {
        get
        {
            CrazyflieTrack track = this.parent as CrazyflieTrack;
            return track?.Drone;
        }
    }
}