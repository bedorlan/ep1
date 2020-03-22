using UnityEngine;

// http://www.sc.ehu.es/sbweb/fisica/cinematica/parabolico/alcance/alcance.htm

public static class MyMath
{
    public static float g = Mathf.Abs(Physics2D.gravity.y);

    public static float CalcAngle(float v, float x, float y)
    {
        var a = (g * x * x) / (2 * v * v);
        var b = -x;
        var c = a + y;
        var (x1, x2) = Quadratic(a, b, c);
        var angle1 = Mathf.Atan(x1);
        var angle2 = Mathf.Atan(x2);
        return Mathf.Min(angle1, angle2);
    }

    public static float CalcBestAngle(float v, float y)
    {
        return Mathf.Atan(v / Mathf.Sqrt(v * v + 2 * g * y));
    }

    public static float CalcMaxReach(float v, float y)
    {
        var bestAngle = CalcBestAngle(v, y);
        return y * Mathf.Tan(2 * bestAngle);
    }

    public static float CalcMinVelocity(float x, float y)
    {
        return Mathf.Sqrt(g * (Mathf.Sqrt(x * x + y * y) + y));
    }

    public static (float x1, float x2) Quadratic(float a, float b, float c)
    {
        var x1 = (-b + Mathf.Sqrt(b * b - 4 * a * c)) / (2 * a);
        var x2 = (-b - Mathf.Sqrt(b * b - 4 * a * c)) / (2 * a);
        return (x1, x2);
    }
}