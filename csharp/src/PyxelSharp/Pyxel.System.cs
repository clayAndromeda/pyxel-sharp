using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text;
using PyxelSharp.Native;

namespace PyxelSharp;

/// <summary>
/// The Pyxel API. Mirrors the Python module-level functions
/// (<c>pyxel.init</c> → <see cref="Init"/>, <c>pyxel.run</c> → <see cref="Run"/>, …).
/// </summary>
public static unsafe partial class Pyxel
{
    private const uint NoneU32 = uint.MaxValue;

    private static Action? _update;
    private static Action? _draw;
    private static ExceptionDispatchInfo? _callbackException;

    /// <summary>Initializes the Pyxel window and singleton. Call once, before anything else.</summary>
    public static void Init(
        int width,
        int height,
        string? title = null,
        int? fps = null,
        Key? quitKey = null,
        int? displayScale = null,
        int? captureScale = null,
        int? captureSec = null,
        bool? headless = null)
    {
        fixed (byte* titlePtr = ToUtf8(title))
        {
            var result = NativeMethods.pyxel_init(
                (uint)width,
                (uint)height,
                titlePtr,
                ToSentinel(fps),
                quitKey.HasValue ? (uint)quitKey.Value : NoneU32,
                ToSentinel(displayScale),
                ToSentinel(captureScale),
                ToSentinel(captureSec),
                headless switch { null => -1, false => 0, true => 1 });
            if (result != 0)
            {
                ThrowLastError();
            }
        }
    }

    /// <summary>
    /// Starts the frame loop, calling <paramref name="update"/> then
    /// <paramref name="draw"/> every frame. Blocks until the app quits.
    /// </summary>
    public static void Run(Action update, Action draw)
    {
        _update = update;
        _draw = draw;
        _callbackException = null;
        NativeMethods.pyxel_run(&UpdateThunk, &DrawThunk);
        _callbackException?.Throw();
    }

    public static void Show() => NativeMethods.pyxel_show();

    public static void Flip() => NativeMethods.pyxel_flip();

    public static void Quit() => NativeMethods.pyxel_quit();

    public static void Title(string title)
    {
        fixed (byte* titlePtr = ToUtf8(title))
        {
            NativeMethods.pyxel_title(titlePtr);
        }
    }

    public static void Fullscreen(bool enabled) => NativeMethods.pyxel_fullscreen(enabled);

    public static void PerfMonitor(bool enabled) => NativeMethods.pyxel_perf_monitor(enabled);

    /// <summary>Frames elapsed since <see cref="Init"/> (Python: <c>pyxel.frame_count</c>).</summary>
    public static int FrameCount => (int)NativeMethods.pyxel_frame_count();

    /// <summary>Screen width in pixels (Python: <c>pyxel.width</c>).</summary>
    public static int Width => (int)NativeMethods.pyxel_width();

    /// <summary>Screen height in pixels (Python: <c>pyxel.height</c>).</summary>
    public static int Height => (int)NativeMethods.pyxel_height();

    // C# exceptions must not unwind across the native frame loop; capture the
    // first one, stop the loop, and rethrow after pyxel_run returns.
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void UpdateThunk() => InvokeCallback(_update);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void DrawThunk() => InvokeCallback(_draw);

    private static void InvokeCallback(Action? callback)
    {
        if (_callbackException is not null)
        {
            return;
        }
        try
        {
            callback?.Invoke();
        }
        catch (Exception exception)
        {
            _callbackException = ExceptionDispatchInfo.Capture(exception);
            NativeMethods.pyxel_quit();
        }
    }

    private static uint ToSentinel(int? value) => value.HasValue ? (uint)value.Value : NoneU32;

    private static byte[]? ToUtf8(string? value) =>
        value is null ? null : Encoding.UTF8.GetBytes(value + "\0");

    private static void ThrowLastError()
    {
        var message = Marshal.PtrToStringUTF8((IntPtr)NativeMethods.pyxel_last_error());
        throw new PyxelException(string.IsNullOrEmpty(message) ? "Unknown Pyxel error" : message);
    }
}
