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
    public override bool Dynamic => false;

    public void DrawShapeGUI()
    {
        GuideShape shape = GetComponent<GuideShape>();
        shape.DrawSceneGUI();
    }
}


[CustomEditor(typeof(StaticGuide))]
public class StaticGuideEditor: GuideEditor
{
    private StaticGuide Guide { get { return target as StaticGuide; } }

    private void OnSceneGUI()
    {
        Draw(Guide);
    }

    public static void Draw(StaticGuide guide)
    {
        guide.DrawShapeGUI();
    }

    public override void OnInspectorGUI()
    {
        DrawGuideSelector();
    }
}