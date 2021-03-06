using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;


[ExecuteInEditMode]
[RequireComponent(typeof(GuideShape))]
public class StaticGuide : Guide
{
}


[CustomEditor(typeof(StaticGuide))]
public class StaticGuideEditor: GuideEditor
{

    public override void OnInspectorGUI()
    {
        DrawGuideSelector();
    }
}