using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GuideTrack))]
public class GuideTrackEditor : CustomEditor<GuideTrack>
{
    protected override void OnDrawScene(SceneView scene)
    {
        DynamicGuide guide = Target.Guide;
        DynamicGuideEditor.Draw(guide);

    }
}