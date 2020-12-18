using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;


[Serializable]
[CustomStyle("ColorKeyframe")]
public class ColorKeyframe : DroneKeyframe
{
    public Color LightColor = Color.white;

    public Vector3 Position { get; set; } = Vector3.zero;
    public Vector3 Tangent { get; set; } = Vector3.zero;
    public float Offset { get; set; } = 0f;
    public int SortedIndex { get; set; } = 0;
}