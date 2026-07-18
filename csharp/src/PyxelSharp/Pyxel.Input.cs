using System.Runtime.InteropServices;
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

    /// <summary>Text typed since the last frame (Python: <c>pyxel.input_text</c>).</summary>
    public static string InputText
    {
        get
        {
            byte* text;
            Check(NativeMethods.pyxel_input_text(&text));
            return Marshal.PtrToStringUTF8((IntPtr)text) ?? string.Empty;
        }
    }

    /// <summary>Keys currently held down (Python: <c>pyxel.input_keys</c>). Copies on get.</summary>
    public static Key[] InputKeys
    {
        get
        {
            uint length;
            Check(NativeMethods.pyxel_input_keys_len(&length));
            var keys = new Key[length];
            fixed (Key* buffer = keys)
            {
                Check(NativeMethods.pyxel_input_keys_read((uint*)buffer, length));
            }
            return keys;
        }
    }

    /// <summary>Files dropped onto the window this frame (Python: <c>pyxel.dropped_files</c>). Copies on get.</summary>
    public static string[] DroppedFiles
    {
        get
        {
            uint length;
            Check(NativeMethods.pyxel_dropped_files_len(&length));
            var files = new string[length];
            for (var i = 0u; i < length; i++)
            {
                byte* path;
                Check(NativeMethods.pyxel_dropped_file(i, &path));
                files[i] = Marshal.PtrToStringUTF8((IntPtr)path) ?? string.Empty;
            }
            return files;
        }
    }

    /// <summary>Overrides a key state for the current frame (Python: <c>pyxel.set_btn</c>, mainly for tests).</summary>
    public static void SetBtn(Key key, bool state) =>
        Check(NativeMethods.pyxel_set_btn((uint)key, state));

    /// <summary>Overrides an analog key value for the current frame (Python: <c>pyxel.set_btnv</c>).</summary>
    public static void SetBtnv(Key key, int value) =>
        Check(NativeMethods.pyxel_set_btnv((uint)key, value));

    /// <summary>Overrides the mouse position (Python: <c>pyxel.set_mouse_pos</c>).</summary>
    public static void SetMousePos(float x, float y) =>
        Check(NativeMethods.pyxel_warp_mouse(x, y));

    /// <summary>Overrides the typed text for the current frame (Python: <c>pyxel.set_input_text</c>).</summary>
    public static void SetInputText(string text)
    {
        fixed (byte* textPtr = ToUtf8Required(text))
        {
            Check(NativeMethods.pyxel_set_input_text(textPtr));
        }
    }

    /// <summary>Overrides the dropped-file list for the current frame (Python: <c>pyxel.set_dropped_files</c>).</summary>
    public static void SetDroppedFiles(string[] files)
    {
        var pointers = new IntPtr[files.Length];
        try
        {
            for (var i = 0; i < files.Length; i++)
            {
                pointers[i] = Marshal.StringToCoTaskMemUTF8(files[i]);
            }
            fixed (IntPtr* pointersPtr = pointers)
            {
                Check(NativeMethods.pyxel_set_dropped_files(
                    (byte**)pointersPtr, (uint)files.Length));
            }
        }
        finally
        {
            foreach (var pointer in pointers)
            {
                Marshal.FreeCoTaskMem(pointer);
            }
        }
    }
}
