﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;


[Serializable]
[CustomStyle("ColorKeyframe")]
public class ColorKeyframe : Marker, INotification
{
    public Color LightColor = Color.black;

    public PropertyName id => new PropertyName();
}