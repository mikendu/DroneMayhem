using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
class BezierNodeTest : MonoBehaviour
{
    public List<Vector3> Nodes = new List<Vector3>();
    public float Scale = 1.0f;

    [Range(0.0f, 1.0f)]
    public float Time = 0.0f;
    public float ScaledTime = 0.0f;

    public bool DrawUnscaledPoints = true;
    public bool DrawScaledPoints = true;
    public bool DrawUnscaledLine = true;
    public bool DrawScaledLine = true;
    public bool DrawUnscaledMarker = true;
    public bool DrawScaledMarker = true;
    
    private List<Vector3> unscaledNodes = new List<Vector3>();
    private List<Vector3> scaledNodes = new List<Vector3>();
    private float[][] unscaledCoefficients = new float[][] { new float[8], new float[8], new float[8] };
    private float[][] scaledCoefficients = new float[][] { new float[8], new float[8], new float[8] };

    public void Update()
    {
    }

    private void GenerateNodes()
    {
        unscaledNodes.Clear();
        scaledNodes.Clear();

        if (Nodes.Count == 0)
            return;

        Vector3 q0 = Nodes[0];
        Vector3 q1 = (Nodes.Count >= 2) ? Nodes[1] : q0;
        Vector3 delta = q1 - q0;

        Vector3 q2 = (Nodes.Count >= 3) ? Nodes[2] : (q0 + (2 * delta));
        Vector3 e = (3 * q2) - (2 * q0) - (6 * delta);
        Vector3 q3 = (Nodes.Count >= 4) ? Nodes[3] : e + (3 * delta);

        Vector3 p4 = (2 * e) - q3;
        Vector3 p5 = q2 - (4 * delta);
        Vector3 p6 = q1 - (2 * delta);
        Vector3 p7 = q0;

        unscaledNodes.Add(q0);
        unscaledNodes.Add(q1);
        unscaledNodes.Add(q2);
        unscaledNodes.Add(q3);
        unscaledNodes.Add(p4);
        unscaledNodes.Add(p5);
        unscaledNodes.Add(p6);
        unscaledNodes.Add(p7);

        float oneMinus = 1.0f - Scale;
        scaledNodes.Add(q0);
        scaledNodes.Add((oneMinus *q0) + (Scale * q1));
        scaledNodes.Add((Mathf.Pow(oneMinus, 2) * q0) + (2 * oneMinus * Scale * q1) + (Mathf.Pow(Scale, 2) * q2));
        scaledNodes.Add((Mathf.Pow(oneMinus, 3) * q0) + (3 * Mathf.Pow(oneMinus, 2) * Scale * q1)
            + (3 * oneMinus * Mathf.Pow(Scale, 2) * q2) + (Mathf.Pow(Scale, 3) * q3));


        scaledNodes.Add((Mathf.Pow(oneMinus, 3) * p7) + (3 * Mathf.Pow(oneMinus, 2) * Scale * p6)
            + (3 * oneMinus * Mathf.Pow(Scale, 2) * p5) + (Mathf.Pow(Scale, 3) * p4));
        scaledNodes.Add((Mathf.Pow(oneMinus, 2) * p7) + (2 * oneMinus * Scale * p6) + (Mathf.Pow(Scale, 2) * p5));
        scaledNodes.Add((oneMinus * p7) + (Scale * p6));
        scaledNodes.Add(p7);

        for (int d = 0; d < 3; d++)
        {
            for (int j = 0; j < 8; j++)
            {
                float s = 0.0f;
                for (int i = 0; i < j + 1; i++)
                {
                    s += Mathf.Pow(-1, i + j) * GetDimension(scaledNodes, i, d) / (Factorial(i) * Factorial(j - i));
                }

                float c = s * Factorial(7) / Factorial(7 - j);
                unscaledCoefficients[d][j] = c;
            }
        }

        float reciprocal = 1.0f / Scale;
        for (int d = 0; d < 3; d++)
        {
            float scale = 1.0f;
            for (int j = 0; j < 8; j++)
            {
                scaledCoefficients[d][j] = (scale * unscaledCoefficients[d][j]);
                scale *= reciprocal;
            }
        }

    }

    int Factorial(int i)
    {
        if (i <= 1)
            return 1;
        else
            return i * Factorial(i - 1);
    }

    float GetDimension(List<Vector3> items, int index, int dimension)
    {
        Vector3 item = items[index];
        switch(dimension)
        {
            case 0:
                return item.x;
            case 1:
                return item.y;
            case 2:
                return item.z;
            default:
                return 0;
        }
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


    private void OnValidate()
    {
        ScaledTime = Time * Scale;
        GenerateNodes();
    }

    private void OnDrawGizmos()
    {
        if (DrawUnscaledPoints)
        {
            Gizmos.color = (0.85f * Color.white);
            foreach (Vector3 v in unscaledNodes)
                Gizmos.DrawSphere(v, 0.015f);
        }

        if (DrawScaledPoints)
        {
            Gizmos.color = (0.85f * Color.red);
            foreach (Vector3 v in scaledNodes)
                Gizmos.DrawSphere(v, 0.015f);
        }

        if (DrawUnscaledLine)
        {
            Gizmos.color = (0.85f * Color.white);
            DrawLine(unscaledCoefficients);
        }

        if (DrawScaledLine)
        {
            Gizmos.color = (0.85f * Color.red);
            DrawLine(scaledCoefficients);
        }

        if (DrawUnscaledMarker)
        {
            Gizmos.color = (0.85f * Color.yellow);
            Vector3 maarker = Evaluate(unscaledCoefficients, Time);
            Gizmos.DrawSphere(maarker, 0.03f);
        }

        if (DrawScaledMarker)
        {
            Gizmos.color = (0.85f * Color.cyan);
            Vector3 maarker = Evaluate(scaledCoefficients, ScaledTime);
            Gizmos.DrawSphere(maarker, 0.03f);
        }
    }
}