using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;

public class CrazyflieControlBehaviour : PlayableBehaviour
{
    public Color lightColor = Color.black;

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        Crazyflie drone = playerData as Crazyflie;

        if (drone != null)
            drone.LightColor = lightColor;


    }
}