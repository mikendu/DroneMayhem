using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public enum BezierDegree
{
    Constant = 0,
    Linear = 1,
    Cubic = 2
}

static class BezierDegreeExtensions
{
    public static byte Value(this BezierDegree bezierDegree)
    {
        return (byte)bezierDegree;
    }
}