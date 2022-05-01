using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class BezierSegment
{

    private List<float[]> coefficients;
    public BezierSegment(
                            int degree,
                            float duration,
                            List<float[]> coefficients,
                            List<float[]> controlPoints)
    {
        this.coefficients = coefficients;

        Degree = degree;
        Duration = duration;
        ControlPoints = new List<Vector3>();
        YawTrajectory = new List<float>();

        for (int i = 0; i <= Degree; i++)
        {
            ControlPoints.Add(new Vector3(controlPoints[0][i], controlPoints[1][i], controlPoints[2][i]));
            YawTrajectory.Add(controlPoints[3][i]);
        }
    }

    public int Degree { get; private set; }
    public float Duration { get; private set; }
    public List<Vector3> ControlPoints { get; private set; }
    public List<float> YawTrajectory { get; private set; }

    public Vector3 Evaluate(float t)
    {
        float x = 0.0f;
        float y = 0.0f;
        float z = 0.0f;

        float power = 1.0f;
        for(int i = 0; i <= Degree; i++)
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

    public void Draw()
    {
        Vector3 startPoint = Evaluate(0.0f);
        float division = 0.03125f;
        for (float t = division; t <= 1.0f; t += division)
        {
            float time = Mathf.Clamp01(t);
            Vector3 currentPoint = Evaluate(time);
            Gizmos.DrawLine(startPoint, currentPoint);
            startPoint = currentPoint;
        }
    }


}