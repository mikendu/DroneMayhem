using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

[ExecuteInEditMode]
public class IntersectorTest : MonoBehaviour
{
    public Transform Ray;
    public Transform Box;

    private Tuple<Vector3, Vector3> hitPoints;

    public void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawSphere(Ray.position, 0.05f);
        Gizmos.DrawLine(Ray.position, Ray.position + Ray.forward * 5.0f);

        Gizmos.color = (hitPoints != null) ? Color.green : Color.red;
        Gizmos.DrawCube(Box.position, Box.localScale * 1.05f);

        if (hitPoints != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(hitPoints.Item1, 0.05f);
            Gizmos.DrawSphere(hitPoints.Item2, 0.05f);
        }
    }

    public void Update()
    {
        Bounds bounds = new Bounds(Box.position, Box.localScale);
        hitPoints = BoxIntersector.Intersect(Ray.position, Ray.forward, bounds);
    }
}