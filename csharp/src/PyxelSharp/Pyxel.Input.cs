using PyxelSharp.Native;

namespace PyxelSharp;

public static unsafe partial class Pyxel
{
    /// <summary>True while the key is held down (Python: <c>pyxel.btn</c>).</summary>
    public static bool Btn(Key key) => NativeMethods.pyxel_btn((uint)key);

    /// <summary>
    /// True on the frame the key was pressed; with <paramref name="hold"/> /
    /// <paramref name="repeat"/> it also fires on key repeat (Python: <c>pyxel.btnp</c>).
    /// </summary>
    public static bool Btnp(Key key, int? hold = null, int? repeat = null) =>
        NativeMethods.pyxel_btnp((uint)key, ToSentinel(hold), ToSentinel(repeat));

    /// <summary>True on the frame the key was released (Python: <c>pyxel.btnr</c>).</summary>
    public static bool Btnr(Key key) => NativeMethods.pyxel_btnr((uint)key);

    /// <summary>Analog value of the key (gamepad axes etc.; Python: <c>pyxel.btnv</c>).</summary>
    public static int Btnv(Key key) => NativeMethods.pyxel_btnv((uint)key);

    /// <summary>Mouse cursor X in screen coordinates (Python: <c>pyxel.mouse_x</c>).</summary>
    public static int MouseX => NativeMethods.pyxel_mouse_x();

    /// <summary>Mouse cursor Y in screen coordinates (Python: <c>pyxel.mouse_y</c>).</summary>
    public static int MouseY => NativeMethods.pyxel_mouse_y();

    /// <summary>Mouse wheel delta this frame (Python: <c>pyxel.mouse_wheel</c>).</summary>
    public static int MouseWheel => NativeMethods.pyxel_mouse_wheel();

    /// <summary>Shows or hides the mouse cursor (Python: <c>pyxel.mouse</c>).</summary>
    public static void Mouse(bool visible) => NativeMethods.pyxel_mouse(visible);

    /// <summary>Moves the mouse cursor (Python: <c>pyxel.warp_mouse</c>).</summary>
    public static void WarpMouse(float x, float y) => NativeMethods.pyxel_warp_mouse(x, y);
}
