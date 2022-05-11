using System;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public class BSegment 
{
    public BNode HeadNode;
    public float Time;

    private List<Vector3> unscaledNodes = new List<Vector3>();
    private List<Vector3> scaledNodes = new List<Vector3>();

    private float[][] unscaledCoefficients = new float[][] { new float[8], new float[8], new float[8] };
    private float[][] scaledCoefficients = new float[][] { new float[8], new float[8], new float[8] };


    public void Generate(BNode tailNode, bool continuity = true)
    {
        HeadNode.Generate();
        unscaledNodes.Clear();
        unscaledNodes.AddRange(HeadNode.HeadPoints);
        unscaledNodes.AddRange(tailNode.TailPoints);

        if (continuity)
            scaledNodes = BezierMath.EnforceContinuity(unscaledNodes, Time);
        else
            scaledNodes = unscaledNodes;

        unscaledCoefficients = BezierMath.ToPolynomial(scaledNodes);
        scaledCoefficients = BezierMath.ScalePolynomial(unscaledCoefficients, Time);
    }

    public void DrawUnscaledNodes()
    {
        int index = 0;
        foreach (Vector3 v in unscaledNodes)
        {
            if (index < 4)
                Gizmos.color = (0.85f * Color.white);
            else
                Gizmos.color = Color.yellow;

            Gizmos.DrawSphere(v, 0.015f);
            index += 1;
        }
    }

    public void DrawScaledNodes()
    {
        Gizmos.color = (0.85f * Color.red);
        foreach (Vector3 v in scaledNodes)
            Gizmos.DrawSphere(v, 0.015f);
    }

    public void DrawUnscaledLine()
    {
        Gizmos.color = (0.85f * Color.white);
        DrawLine(unscaledCoefficients);
    }

    public void DrawScaledLine()
    {
        Gizmos.color = (0.85f * Color.red);
        DrawLine(scaledCoefficients);
    }

    public void DrawMarker(float time)
    {
        Gizmos.color = (0.85f * Color.cyan);
        Vector3 maarker = Evaluate(scaledCoefficients, time);
        Gizmos.DrawSphere(maarker, 0.03f);
    }

    public Vector3 Evaluate(float[][] coefficients, float t)
    {
        float x = 0.0f;
        float y = 0.0f;
        float z = 0.0f;

        float power = 1.0f;
        for (int i = 0; i <= 7; i++)
        {
            float xCoeff = coefficients[0][i];
            float yCoeff = coefficients[1][i];
            float zCoeff = coefficients[2][i];

            x += power * xCoeff;
            y += power * yCoeff;
            z += power * zCoeff;
            power *= t;
        }

        return new Vector3(x, y, z);
    }

    public void DrawLine(float[][] coefficients)
    {
        Vector3 startPoint = Evaluate(coefficients, 0.0f);
        float division = 0.03125f;
        for (float t = division; t <= 1.0f; t += division)
        {
            float time = Mathf.Clamp01(t);
            Vector3 currentPoint = Evaluate(coefficients, time);
            Gizmos.DrawLine(startPoint, currentPoint);
            startPoint = currentPoint;
        }
    }

}