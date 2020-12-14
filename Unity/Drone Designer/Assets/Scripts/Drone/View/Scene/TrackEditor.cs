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
        Handles.color = Color.green;
        Handles.DrawSolidDisc(new Vector3(1, 1, -1), Vector3.up, 0.5f);
    }
}