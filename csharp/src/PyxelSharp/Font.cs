using PyxelSharp.Native;

namespace PyxelSharp;

/// <summary>
/// A BDF or TTF font usable with the <c>Text</c> drawing methods
/// (Python: <c>pyxel.Font</c>).
/// </summary>
/// <remarks>
/// Handles are released on the pyxel thread like <see cref="Image"/>:
/// <see cref="Dispose"/> on that thread frees immediately, finalizers and
/// other threads defer the release to the next frame.
/// </remarks>
public sealed unsafe class Font : IDisposable
{
    private void* _handle;

    /// <summary>Loads a font file; <paramref name="fontSize"/> applies to TTF fonts.</summary>
    public Font(string filename, float? fontSize = null)
    {
        fixed (byte* filenamePtr = Pyxel.ToUtf8Required(filename))
        {
            void* handle;
            Pyxel.Check(NativeMethods.pyxel_font_new(
                filenamePtr, Pyxel.ToSentinel(fontSize), &handle));
            _handle = handle;
        }
    }

    internal void* Handle =>
        _handle is not null ? _handle : throw new ObjectDisposedException(nameof(Font));

    internal static void* HandleOrNull(Font? font) => font is null ? null : font.Handle;

    /// <summary>Width in pixels of the text in this font (Python: <c>font.text_width</c>).</summary>
    public int TextWidth(string text)
    {
        fixed (byte* textPtr = Pyxel.ToUtf8Required(text))
        {
            int value;
            Pyxel.Check(NativeMethods.pyxel_font_text_width(Handle, textPtr, &value));
            return value;
        }
    }

    /// <summary>
    /// Releases the native font. Safe to call from any thread; the actual
    /// release may be deferred to the next frame.
    /// </summary>
    public void Dispose()
    {
        if (_handle is null)
        {
            return;
        }
        Pyxel.ReleaseHandle((IntPtr)_handle, Pyxel.HandleKind.Font);
        _handle = null;
        GC.SuppressFinalize(this);
    }

    ~Font()
    {
        if (_handle is not null)
        {
            Pyxel.ReleaseHandle((IntPtr)_handle, Pyxel.HandleKind.Font);
        }
    }
}
