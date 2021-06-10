using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using Random = UnityEngine.Random;



public class TargetMatcher: MonoBehaviour
{
    public GuideShape Shape;
    
    public void Match()
    {
        if (Shape == null)
            return;

        int index = 0;
        List<AttachmentPoint> points = Shape.AttachmentPoints;
        foreach(Transform child in transform)
        {
            Vector3 position = Random.insideUnitSphere;
            if (index < points.Count)
                position = points[index].Position;

            child.position = position;
            index += 1;
        }

        
    }
}


[CustomEditor(typeof(TargetMatcher))]
public class TargetMatcherEditor : Editor
{

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(30.0f);

        if (GUILayout.Button("Match"))
            ((TargetMatcher)target).Match();

        EditorGUILayout.Space(30.0f);
    }
}