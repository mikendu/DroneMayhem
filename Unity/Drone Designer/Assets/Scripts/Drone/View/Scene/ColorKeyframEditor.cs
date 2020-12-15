using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ColorKeyframe))]
public class ColorKeyframEditor : CustomEditor<ColorKeyframe>
{
    protected override void OnDrawScene(SceneView scene)
    {
        ColorKeyframe keyframe = Target;
        Crazyflie drone = keyframe.Drone;
        CrazyflieEditor.Draw(drone);

    }
}
