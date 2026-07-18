using PyxelSharp.Native;

namespace PyxelSharp;

public static unsafe partial class Pyxel
{
    // Python's pyxel.clamp / pyxel.sgn are not bound; use Math.Clamp / Math.Sign.

    /// <summary>Smallest integer at or above x (Python: <c>pyxel.ceil</c>).</summary>
    public static int Ceil(float x)
    {
        int value;
        Check(NativeMethods.pyxel_ceil(x, &value));
        return value;
    }

    /// <summary>Largest integer at or below x (Python: <c>pyxel.floor</c>).</summary>
    public static int Floor(float x)
    {
        int value;
        Check(NativeMethods.pyxel_floor(x, &value));
        return value;
    }

    /// <summary>Square root (Python: <c>pyxel.sqrt</c>).</summary>
    public static float Sqrt(float x)
    {
        float value;
        Check(NativeMethods.pyxel_sqrt(x, &value));
        return value;
    }

    /// <summary>Sine of an angle in degrees (Python: <c>pyxel.sin</c>).</summary>
    public static float Sin(float deg)
    {
        float value;
        Check(NativeMethods.pyxel_sin(deg, &value));
        return value;
    }

    /// <summary>Cosine of an angle in degrees (Python: <c>pyxel.cos</c>).</summary>
    public static float Cos(float deg)
    {
        float value;
        Check(NativeMethods.pyxel_cos(deg, &value));
        return value;
    }

    /// <summary>Arctangent of y/x in degrees (Python: <c>pyxel.atan2</c>).</summary>
    public static float Atan2(float y, float x)
    {
        float value;
        Check(NativeMethods.pyxel_atan2(y, x, &value));
        return value;
    }

    /// <summary>Seeds the random number generator (Python: <c>pyxel.rseed</c>).</summary>
    public static void Rseed(int seed) => Check(NativeMethods.pyxel_rseed((uint)seed));

    /// <summary>Random integer in [a, b] (Python: <c>pyxel.rndi</c>).</summary>
    public static int Rndi(int a, int b)
    {
        int value;
        Check(NativeMethods.pyxel_rndi(a, b, &value));
        return value;
    }

    /// <summary>Random float in [a, b] (Python: <c>pyxel.rndf</c>).</summary>
    public static float Rndf(float a, float b)
    {
        float value;
        Check(NativeMethods.pyxel_rndf(a, b, &value));
        return value;
    }

    /// <summary>Seeds the Perlin noise generator (Python: <c>pyxel.nseed</c>).</summary>
    public static void Nseed(int seed) => Check(NativeMethods.pyxel_nseed((uint)seed));

    /// <summary>Perlin noise at (x, y, z) (Python: <c>pyxel.noise</c>).</summary>
    public static float Noise(float x, float y = 0, float z = 0)
    {
        float value;
        Check(NativeMethods.pyxel_noise(x, y, z, &value));
        return value;
    }
}
