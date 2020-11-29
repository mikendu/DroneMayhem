using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;

[Serializable]
public class CrazyflieControlClip : PlayableAsset, ITimelineClipAsset
{
    public Color lightColor = Color.black;

    public ClipCaps clipCaps {  get { return ClipCaps.None; } }

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<CrazyflieControlBehaviour>.Create(graph);
        var droneBehaviour = playable.GetBehaviour();
        droneBehaviour.lightColor = lightColor;

        return playable;
    }
}