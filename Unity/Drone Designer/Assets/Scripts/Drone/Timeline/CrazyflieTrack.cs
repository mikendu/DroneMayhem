using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Timeline;

[TrackColor(232f / 255f, 99f / 255f, 5f / 255f)]
[TrackBindingType(typeof(Crazyflie))]
[TrackClipType(typeof(CrazyflieControlClip))]
public class CrazyflieTrack : TrackAsset 
{

    public Crazyflie Drone
    {
        get
        {
            return TimelineUtilities.Director.GetGenericBinding(this) as Crazyflie;
        }
    }
}