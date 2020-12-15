using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CrazyflieTrack))]
public class TrackEditor : CustomEditor<CrazyflieTrack>
{
    protected override void OnDrawScene(SceneView scene)
    {
        Crazyflie drone = Target.Drone;
        CrazyflieEditor.Draw(drone);

    }
}