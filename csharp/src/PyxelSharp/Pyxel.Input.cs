using PyxelSharp.Native;

namespace PyxelSharp;

public static unsafe partial class Pyxel
{
    /// <summary>True while the key is held down (Python: <c>pyxel.btn</c>).</summary>
    public static bool Btn(Key key)
    {
        bool value;
        Check(NativeMethods.pyxel_btn((uint)key, &value));
        return value;
    }

    /// <summary>
    /// True on the frame the key was pressed; with <paramref name="hold"/> /
    /// <paramref name="repeat"/> it also fires on key repeat (Python: <c>pyxel.btnp</c>).
    /// </summary>
    public static bool Btnp(Key key, int? hold = null, int? repeat = null)
    {
        bool value;
        Check(NativeMethods.pyxel_btnp((uint)key, ToSentinel(hold), ToSentinel(repeat), &value));
        return value;
    }

    /// <summary>True on the frame the key was released (Python: <c>pyxel.btnr</c>).</summary>
    public static bool Btnr(Key key)
    {
        bool value;
        Check(NativeMethods.pyxel_btnr((uint)key, &value));
        return value;
    }

    /// <summary>Analog value of the key (gamepad axes etc.; Python: <c>pyxel.btnv</c>).</summary>
    public static int Btnv(Key key)
    {
        int value;
        Check(NativeMethods.pyxel_btnv((uint)key, &value));
        return value;
    }

    /// <summary>Mouse cursor X in screen coordinates (Python: <c>pyxel.mouse_x</c>).</summary>
    public static int MouseX
    {
        get
        {
            int value;
            Check(NativeMethods.pyxel_mouse_x(&value));
            return value;
        }
    }

    /// <summary>Mouse cursor Y in screen coordinates (Python: <c>pyxel.mouse_y</c>).</summary>
    public static int MouseY
    {
        get
        {
            int value;
            Check(NativeMethods.pyxel_mouse_y(&value));
            return value;
        }
    }

    /// <summary>Mouse wheel delta this frame (Python: <c>pyxel.mouse_wheel</c>).</summary>
    public static int MouseWheel
    {
        get
        {
            int value;
            Check(NativeMethods.pyxel_mouse_wheel(&value));
            return value;
        }
    }

    /// <summary>Shows or hides the mouse cursor (Python: <c>pyxel.mouse</c>).</summary>
    public static void Mouse(bool visible) => Check(NativeMethods.pyxel_mouse(visible));

    /// <summary>Moves the mouse cursor (Python: <c>pyxel.warp_mouse</c>).</summary>
    public static void WarpMouse(float x, float y) => Check(NativeMethods.pyxel_warp_mouse(x, y));
}
