using System.Collections.Concurrent;
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

    // Native image handles wrap Rc<RefCell<_>>, which is not thread-safe, so
    // they may only be dropped on the thread that drives pyxel. Dispose on
    // that thread drops immediately; finalizers and other threads enqueue the
    // handle here, and the queue is drained once per frame (and on Flip/Show).
    private static int _pyxelThreadId = -1;
    private static readonly ConcurrentQueue<IntPtr> _pendingHandleDrops = new();

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
        _pyxelThreadId = Environment.CurrentManagedThreadId;
        fixed (byte* titlePtr = ToUtf8(title))
        {
            Check(NativeMethods.pyxel_init(
                (uint)width,
                (uint)height,
                titlePtr,
                ToSentinel(fps),
                quitKey.HasValue ? (uint)quitKey.Value : NoneU32,
                ToSentinel(displayScale),
                ToSentinel(captureScale),
                ToSentinel(captureSec),
                headless switch { null => -1, false => 0, true => 1 }));
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
        var result = NativeMethods.pyxel_run(&UpdateThunk, &DrawThunk);
        DrainPendingHandleDrops();
        _callbackException?.Throw();
        Check(result);
    }

    public static void Show() => Check(NativeMethods.pyxel_show());

    public static void Flip()
    {
        DrainPendingHandleDrops();
        Check(NativeMethods.pyxel_flip());
    }

    public static void Quit() => Check(NativeMethods.pyxel_quit());

    public static void Title(string title)
    {
        fixed (byte* titlePtr = ToUtf8(title))
        {
            Check(NativeMethods.pyxel_title(titlePtr));
        }
    }

    public static void Fullscreen(bool enabled) => Check(NativeMethods.pyxel_fullscreen(enabled));

    public static void PerfMonitor(bool enabled) => Check(NativeMethods.pyxel_perf_monitor(enabled));

    /// <summary>Frames elapsed since <see cref="Init"/> (Python: <c>pyxel.frame_count</c>).</summary>
    public static int FrameCount
    {
        get
        {
            uint value;
            Check(NativeMethods.pyxel_frame_count(&value));
            return (int)value;
        }
    }

    /// <summary>Screen width in pixels (Python: <c>pyxel.width</c>).</summary>
    public static int Width
    {
        get
        {
            uint value;
            Check(NativeMethods.pyxel_width(&value));
            return (int)value;
        }
    }

    /// <summary>Screen height in pixels (Python: <c>pyxel.height</c>).</summary>
    public static int Height
    {
        get
        {
            uint value;
            Check(NativeMethods.pyxel_height(&value));
            return (int)value;
        }
    }

    // C# exceptions must not unwind across the native frame loop; capture the
    // first one, stop the loop, and rethrow after pyxel_run returns.
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void UpdateThunk()
    {
        DrainPendingHandleDrops();
        InvokeCallback(_update);
    }

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

    internal static void ReleaseImageHandle(IntPtr handle)
    {
        if (Environment.CurrentManagedThreadId == _pyxelThreadId)
        {
            NativeMethods.pyxel_image_drop((void*)handle);
        }
        else
        {
            _pendingHandleDrops.Enqueue(handle);
        }
    }

    private static void DrainPendingHandleDrops()
    {
        while (_pendingHandleDrops.TryDequeue(out var handle))
        {
            NativeMethods.pyxel_image_drop((void*)handle);
        }
    }

    private static uint ToSentinel(int? value) => value.HasValue ? (uint)value.Value : NoneU32;

    internal static float ToSentinel(float? value) => value ?? float.NaN;

    internal static int ToSentinel(Color? value) => value.HasValue ? value.Value.Value : -1;

    internal static int ToSentinel(bool? value) => value switch { null => -1, false => 0, true => 1 };

    private static byte[]? ToUtf8(string? value) =>
        value is null ? null : Encoding.UTF8.GetBytes(value + "\0");

    internal static byte[] ToUtf8Required(string value) => Encoding.UTF8.GetBytes(value + "\0");

    internal static void Check(int result)
    {
        if (result != 0)
        {
            ThrowLastError();
        }
    }

    private static void ThrowLastError()
    {
        var message = Marshal.PtrToStringUTF8((IntPtr)NativeMethods.pyxel_last_error());
        throw new PyxelException(string.IsNullOrEmpty(message) ? "Unknown Pyxel error" : message);
    }
}
