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
        Color.magenta, 
        new Color(0.9f, 0.6f, 1.0f), 
        new Color(1.0f, 0.9f, 0.6f) 
    };
  
    public List<BezierSegment> Segments { get; set; }

    public void Update()
    {
    }

    private void OnDrawGizmos()
    {
        if (Segments == null || Segments.Count == 0)
            return;

        int colorIndex = 0;
        foreach (BezierSegment segment in Segments)
        {
            Gizmos.color = colors[colorIndex];
            colorIndex = (colorIndex + 1) % colors.Length;
            foreach (Vector3 point in segment.ControlPoints)
            {
                Gizmos.color *= 0.85f;
                Gizmos.DrawSphere(point, 0.035f);
            }

            Gizmos.color = Color.white;
            segment.Draw();
        }
    }
}