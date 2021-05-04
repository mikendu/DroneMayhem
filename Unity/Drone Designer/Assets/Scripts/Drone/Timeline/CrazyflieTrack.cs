using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEditor;

[TrackColor(232f / 255f, 99f / 255f, 5f / 255f)]
[TrackBindingType(typeof(Crazyflie))]
[TrackClipType(typeof(CrazyflieControlClip))]
public class CrazyflieTrack : TrackAsset 
{
    private Crazyflie drone;
    public Crazyflie Drone
    {
        get
        {
            if (drone == null)
                drone = TimelineUtilities.Director.GetGenericBinding(this) as Crazyflie;

            return drone;
        }
    }    

    private void OnDestroy()
    {
        foreach (IMarker marker in this.GetMarkers())
            this.DeleteMarker(marker);

        if (drone?.gameObject != null)
        {
            drone.ResetReferences();
            Undo.DestroyObjectImmediate(Drone.gameObject);
        }
    }

    public void ResetReferences()
    {
        drone = null;
    }
}