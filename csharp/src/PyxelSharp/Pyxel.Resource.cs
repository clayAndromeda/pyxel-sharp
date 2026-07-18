using System.Runtime.InteropServices;
using PyxelSharp.Native;

namespace PyxelSharp;

public static unsafe partial class Pyxel
{
    /// <summary>Loads a .pyxres resource file (Python: <c>pyxel.load</c>).</summary>
    public static void Load(
        string filename,
        bool? excludeImages = null,
        bool? excludeTilemaps = null,
        bool? excludeSounds = null,
        bool? excludeMusics = null)
    {
        fixed (byte* filenamePtr = ToUtf8Required(filename))
        {
            Check(NativeMethods.pyxel_load(
                filenamePtr,
                ToSentinel(excludeImages),
                ToSentinel(excludeTilemaps),
                ToSentinel(excludeSounds),
                ToSentinel(excludeMusics)));
        }
    }

    /// <summary>Saves a .pyxres resource file (Python: <c>pyxel.save</c>).</summary>
    public static void Save(
        string filename,
        bool? excludeImages = null,
        bool? excludeTilemaps = null,
        bool? excludeSounds = null,
        bool? excludeMusics = null)
    {
        fixed (byte* filenamePtr = ToUtf8Required(filename))
        {
            Check(NativeMethods.pyxel_save(
                filenamePtr,
                ToSentinel(excludeImages),
                ToSentinel(excludeTilemaps),
                ToSentinel(excludeSounds),
                ToSentinel(excludeMusics)));
        }
    }

    /// <summary>Loads a .pyxpal palette file (Python: <c>pyxel.load_pal</c>).</summary>
    public static void LoadPal(string filename)
    {
        fixed (byte* filenamePtr = ToUtf8Required(filename))
        {
            Check(NativeMethods.pyxel_load_pal(filenamePtr));
        }
    }

    /// <summary>Saves a .pyxpal palette file (Python: <c>pyxel.save_pal</c>).</summary>
    public static void SavePal(string filename)
    {
        fixed (byte* filenamePtr = ToUtf8Required(filename))
        {
            Check(NativeMethods.pyxel_save_pal(filenamePtr));
        }
    }

    /// <summary>
    /// Saves a screenshot; without a filename it goes to the desktop with a
    /// timestamped name (Python: <c>pyxel.screenshot</c>).
    /// </summary>
    public static void Screenshot(string? filename = null, int? scale = null)
    {
        fixed (byte* filenamePtr = ToUtf8(filename))
        {
            Check(NativeMethods.pyxel_screenshot(filenamePtr, ToSentinel(scale)));
        }
    }

    /// <summary>
    /// Saves the recent frames as a GIF; without a filename it goes to the
    /// desktop with a timestamped name (Python: <c>pyxel.screencast</c>).
    /// </summary>
    public static void Screencast(string? filename = null, int? scale = null)
    {
        fixed (byte* filenamePtr = ToUtf8(filename))
        {
            Check(NativeMethods.pyxel_screencast(filenamePtr, ToSentinel(scale)));
        }
    }

    /// <summary>Discards the frames recorded for <see cref="Screencast"/> (Python: <c>pyxel.reset_screencast</c>).</summary>
    public static void ResetScreencast() => Check(NativeMethods.pyxel_reset_screencast());

    /// <summary>Returns the per-user data directory for the app (Python: <c>pyxel.user_data_dir</c>).</summary>
    public static string UserDataDir(string vendorName, string appName)
    {
        fixed (byte* vendorPtr = ToUtf8Required(vendorName))
        fixed (byte* appPtr = ToUtf8Required(appName))
        {
            byte* path;
            Check(NativeMethods.pyxel_user_data_dir(vendorPtr, appPtr, &path));
            return Marshal.PtrToStringUTF8((IntPtr)path) ?? string.Empty;
        }
    }
}
