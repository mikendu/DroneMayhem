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
        Handles.color = Color.blue;
        Handles.DrawSolidDisc(new Vector3(1, 1, 1), Vector3.up, 0.5f);
    }
}
