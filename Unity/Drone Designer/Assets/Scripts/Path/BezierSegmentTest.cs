using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
class BezierSegmentTest : MonoBehaviour
{
    private static readonly Color[] colors = new Color[] 
    { 
        Color.white,
        Color.red, 
        Color.cyan, 
        Color.yellow, 
        Color.green, 
        Color.magenta
    };

    private static readonly Vector3[] directions = new Vector3[]
    {
        Vector3.up,
        Vector3.right,
        Vector3.down,
        Vector3.left,
        Vector3.forward,
        Vector3.back,
        Vector3.up + Vector3.left,
        Vector3.up + Vector3.right,
        Vector3.up + Vector3.forward,
        Vector3.down + Vector3.left,
        Vector3.down + Vector3.right,
        Vector3.down + Vector3.back,
    };

    public bool DrawPositions = false;
    public bool DrawLabels = false;
    public bool DrawTimes = false;
    public List<BezierSegment> Segments { get; set; }

    public void Update()
    {
    }

    private void OnDrawGizmos()
    {
        if (Segments == null || Segments.Count == 0)
            return;

        int directionIndex = 0;
        int segmentIndex = 0;

        foreach (BezierSegment segment in Segments)
        {
            Vector3 direction = directions[directionIndex];
            directionIndex = (directionIndex + 1) % directions.Length;

            for (int i = 0; i < 8; i++)
            {
                Vector3 point = segment.ControlPoints[i];
                bool shouldDraw = (i == 0) || ((point - segment.ControlPoints[i - 1]).magnitude >= 0.01f);                
                if (shouldDraw)
                {
                    if (i < 4)
                        Gizmos.color = Color.green;
                    else
                        Gizmos.color = new Color(1, 0.5f, 0);
                    Gizmos.DrawSphere(point, 0.015f);
                    if (DrawPositions)
                    {
                        string label = $"{segmentIndex}-{i}: {point}";
                        Handles.Label(point + 0.05f * direction, new GUIContent(label));
                    }
                    else if (DrawLabels)
                    {
                        float offset = 0.05f * (0.005f * segmentIndex);
                        Handles.Label(point + offset * Vector3.up, new GUIContent($"{segmentIndex}-{i}"));
                    }
                }
            }

            if (DrawTimes)
            {
                Vector3 point = segment.ControlPoints.Last();
                string label = $"{segmentIndex} duration: {segment.Duration}";
                Handles.Label(point + 0.125f * direction, new GUIContent(label));
            }

            Gizmos.color = Color.white;
            segment.Draw();
            segmentIndex += 1;
        }
    }
}