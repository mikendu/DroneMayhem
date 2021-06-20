using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;

public struct CubicBezier
{
    public Vector3 anchor1;
    public Vector3 anchor2;
    public Vector3 control1;
    public Vector3 control2;

    public float startTime;
    public float endTime;

    public void SetAnchor1(Vector3 anchor1) { this.anchor1 = anchor1; }
    public void SetAnchor2(Vector3 anchor2) { this.anchor2 = anchor2; }
    public void SetControl1(Vector3 control1) { this.control1 = control1; }
    public void SetControl2(Vector3 control2) { this.control2 = control2; }
}

public class BezierInterpolator
{
    public static List<CubicBezier> Interpolate(List<Vector3> points)
    {
        if (points.Count == 2)
        {
            CubicBezier bezier = new CubicBezier();
            bezier.anchor1 = points[0];
            bezier.control1 = points[0];
            bezier.control2 = points[1];
            bezier.anchor2 = points[1];
            return new List<CubicBezier>() { bezier };
        }

        int n = points.Count - 1;
        Matrix<float> coefficientMatrix = GetCoefficientsMatrix(n);

        float[] pointsX = Extract(points, GridAxis.X);
        float[] pointsY = Extract(points, GridAxis.Y);
        float[] pointsZ = Extract(points, GridAxis.Z);

        Vector<float> rx = GetRightSide(pointsX);
        Vector<float> ry = GetRightSide(pointsY);
        Vector<float> rz = GetRightSide(pointsZ);

        Vector<float> ax = coefficientMatrix.Solve(rx);
        Vector<float> ay = coefficientMatrix.Solve(ry);
        Vector<float> az = coefficientMatrix.Solve(rz);

        Vector<float> bx = GetSecondControlPoint(ax, pointsX);
        Vector<float> by = GetSecondControlPoint(ay, pointsY);
        Vector<float> bz = GetSecondControlPoint(az, pointsZ);

        List<Vector3> controlA = Stitch(ax, ay, az);
        List<Vector3> controlB = Stitch(bx, by, bz);

        List<CubicBezier> curves = new List<CubicBezier>();
        float timeInterval = 1.0f / n;
        float time = 0.0f;

        for(int i = 0; i < n; i++)
        {
            CubicBezier curve = new CubicBezier();
            curve.anchor1 = points[i];
            curve.control1 = controlA[i];
            curve.control2 = controlB[i];
            curve.anchor2 = points[i + 1];
            curve.startTime = time;
            curve.endTime = time + timeInterval;

            time = curve.endTime;
            curves.Add(curve);
        }

        return curves;
    }

    private static Matrix<float> GetCoefficientsMatrix(int n)
    {
        Matrix<float> matrix = Matrix<float>.Build.SparseIdentity(n);
        for(int i = 0; i < n; i++)
        {
            for(int j = 0; j < n; j++)
            {
                if (i == (j + 1) || (j == (i + 1)))
                    matrix[i, j] = 1;

                if (i == j)
                    matrix[i, j] = 4;
            }
        }

        matrix[0, 0] = 2;
        matrix[n - 1, n - 1] = 7;
        matrix[n - 1, n - 2] = 2;

        return matrix;
    }

    private static Vector<float> GetRightSide(float[] points)
    {
        int n = points.Length - 1;
        float[] vector = new float[n];

        for(int i = 1; i < n - 1; i++)
            vector[i] = 2 * (2 * points[i] + points[i + 1]);

        vector[0] = points[0] + (2 * points[1]);
        vector[n - 1] = (8 * points[n - 1]) + points[n];
        return Vector<float>.Build.Dense(vector);
    }

    private static Vector<float> GetSecondControlPoint(Vector<float> a, float[] points)
    {
        int n = a.Count;
        Vector<float> b = a.Clone();
        for (int i = 0; i < n - 1; i++)
            b[i] = (2 * points[i + 1]) - a[i + 1];

        b[n - 1] = (a[n - 1] + points[n]) / 2.0f;
        return b;
    }


    private static List<Vector3> Stitch(Vector<float> x, Vector<float> y, Vector<float> z)
    {
        List<Vector3> results = new List<Vector3>();
        for (int i = 0; i < x.Count; i++)
            results.Add(new Vector3(x[i], y[i], z[i]));

        return results;
    }

    private static float[] Extract(List<Vector3> points, GridAxis axis)
    {
        switch(axis)
        {
            case GridAxis.X:
                return points.Select(p => p.x).ToArray();

            case GridAxis.Y:
                return points.Select(p => p.y).ToArray();

            case GridAxis.Z:
                return points.Select(p => p.z).ToArray();

            default:
                return null;
        }
    }
}