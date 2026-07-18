using System.Runtime.InteropServices;
using PyxelSharp.Native;

namespace PyxelSharp;

/// <summary>
/// An image (canvas of palette color indices). Mirrors Python's
/// <c>pyxel.Image</c>: the image banks (<see cref="Pyxel.Images"/>), the screen
/// (<see cref="Pyxel.Screen"/>), and user-created images all share this class.
/// </summary>
/// <remarks>
/// The native image is reference-counted on the pyxel thread, so handles are
/// released there: <see cref="Dispose"/> on that thread frees immediately,
/// while finalizers and other threads defer the release to the next frame.
/// Image banks and the screen live for the app lifetime; disposing them is a
/// no-op.
/// </remarks>
public sealed unsafe class Image : IDisposable
{
    private void* _handle;
    private readonly bool _isAppLifetime;

    internal Image(void* handle, bool isAppLifetime = false)
    {
        _handle = handle;
        _isAppLifetime = isAppLifetime;
        if (isAppLifetime)
        {
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>Creates a blank image (Python: <c>pyxel.Image(w, h)</c>).</summary>
    public Image(int width, int height)
    {
        void* handle;
        Pyxel.Check(NativeMethods.pyxel_image_new((uint)width, (uint)height, &handle));
        _handle = handle;
    }

    /// <summary>Loads an image file into a new image (Python: <c>pyxel.Image.from_image</c>).</summary>
    public static Image FromImage(string filename, bool? includeColors = null)
    {
        fixed (byte* filenamePtr = Pyxel.ToUtf8Required(filename))
        {
            void* handle;
            Pyxel.Check(NativeMethods.pyxel_image_from_image(
                filenamePtr, Pyxel.ToSentinel(includeColors), &handle));
            return new Image(handle);
        }
    }

    internal void* Handle =>
        _handle is not null ? _handle : throw new ObjectDisposedException(nameof(Image));

    /// <summary>Image width in pixels (Python: <c>image.width</c>).</summary>
    public int Width
    {
        get
        {
            uint value;
            Pyxel.Check(NativeMethods.pyxel_image_width(Handle, &value));
            return (int)value;
        }
    }

    /// <summary>Image height in pixels (Python: <c>image.height</c>).</summary>
    public int Height
    {
        get
        {
            uint value;
            Pyxel.Check(NativeMethods.pyxel_image_height(Handle, &value));
            return (int)value;
        }
    }

    /// <summary>
    /// The raw pixel buffer (row-major <see cref="Width"/>×<see cref="Height"/>
    /// palette indices; Python: <c>image.data_ptr</c>). Valid while the image
    /// is alive and its size unchanged.
    /// </summary>
    public Span<byte> Data
    {
        get
        {
            byte* ptr;
            Pyxel.Check(NativeMethods.pyxel_image_data_ptr(Handle, &ptr));
            return new Span<byte>(ptr, Width * Height);
        }
    }

    /// <summary>Writes rows of hex color digits at (x, y) (Python: <c>image.set</c>).</summary>
    public void Set(int x, int y, params string[] data)
    {
        var pointers = new IntPtr[data.Length];
        try
        {
            for (var i = 0; i < data.Length; i++)
            {
                pointers[i] = Marshal.StringToCoTaskMemUTF8(data[i]);
            }
            fixed (IntPtr* pointersPtr = pointers)
            {
                Pyxel.Check(NativeMethods.pyxel_image_set(
                    Handle, x, y, (byte**)pointersPtr, (uint)data.Length));
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

    /// <summary>Loads an image file at (x, y) (Python: <c>image.load</c>).</summary>
    public void Load(int x, int y, string filename, bool? includeColors = null)
    {
        fixed (byte* filenamePtr = Pyxel.ToUtf8Required(filename))
        {
            Pyxel.Check(NativeMethods.pyxel_image_load(
                Handle, x, y, filenamePtr, Pyxel.ToSentinel(includeColors)));
        }
    }

    /// <summary>Saves the image as a PNG file (Python: <c>image.save</c>).</summary>
    public void Save(string filename, int scale = 1)
    {
        fixed (byte* filenamePtr = Pyxel.ToUtf8Required(filename))
        {
            Pyxel.Check(NativeMethods.pyxel_image_save(Handle, filenamePtr, (uint)scale));
        }
    }

    /// <summary>Sets the clipping region (Python: <c>image.clip(x, y, w, h)</c>).</summary>
    public void Clip(float x, float y, float width, float height) =>
        Pyxel.Check(NativeMethods.pyxel_image_clip(Handle, x, y, width, height));

    /// <summary>Resets the clipping region (Python: <c>image.clip()</c>).</summary>
    public void Clip() => Pyxel.Check(NativeMethods.pyxel_image_clip_reset(Handle));

    /// <summary>Sets the camera offset (Python: <c>image.camera(x, y)</c>).</summary>
    public void Camera(float x, float y) =>
        Pyxel.Check(NativeMethods.pyxel_image_camera(Handle, x, y));

    /// <summary>Resets the camera offset (Python: <c>image.camera()</c>).</summary>
    public void Camera() => Pyxel.Check(NativeMethods.pyxel_image_camera_reset(Handle));

    /// <summary>Replaces a palette color for subsequent draws (Python: <c>image.pal(src, dst)</c>).</summary>
    public void Pal(Color srcColor, Color dstColor) =>
        Pyxel.Check(NativeMethods.pyxel_image_pal(Handle, srcColor.Value, dstColor.Value));

    /// <summary>Resets the palette mapping (Python: <c>image.pal()</c>).</summary>
    public void Pal() => Pyxel.Check(NativeMethods.pyxel_image_pal_reset(Handle));

    /// <summary>Sets the dithering alpha (Python: <c>image.dither</c>).</summary>
    public void Dither(float alpha) =>
        Pyxel.Check(NativeMethods.pyxel_image_dither(Handle, alpha));

    /// <summary>Clears the image (Python: <c>image.cls</c>).</summary>
    public void Cls(Color color) => Pyxel.Check(NativeMethods.pyxel_image_cls(Handle, color.Value));

    /// <summary>Reads the color of a pixel (Python: <c>image.pget</c>).</summary>
    public Color Pget(float x, float y)
    {
        byte color;
        Pyxel.Check(NativeMethods.pyxel_image_pget(Handle, x, y, &color));
        return new Color(color);
    }

    /// <summary>Draws a pixel (Python: <c>image.pset</c>).</summary>
    public void Pset(float x, float y, Color color) =>
        Pyxel.Check(NativeMethods.pyxel_image_pset(Handle, x, y, color.Value));

    /// <summary>Draws a line (Python: <c>image.line</c>).</summary>
    public void Line(float x1, float y1, float x2, float y2, Color color) =>
        Pyxel.Check(NativeMethods.pyxel_image_line(Handle, x1, y1, x2, y2, color.Value));

    /// <summary>Draws a filled rectangle (Python: <c>image.rect</c>).</summary>
    public void Rect(float x, float y, float width, float height, Color color) =>
        Pyxel.Check(NativeMethods.pyxel_image_rect(Handle, x, y, width, height, color.Value));

    /// <summary>Draws a rectangle outline (Python: <c>image.rectb</c>).</summary>
    public void Rectb(float x, float y, float width, float height, Color color) =>
        Pyxel.Check(NativeMethods.pyxel_image_rectb(Handle, x, y, width, height, color.Value));

    /// <summary>Draws a filled circle (Python: <c>image.circ</c>).</summary>
    public void Circ(float x, float y, float radius, Color color) =>
        Pyxel.Check(NativeMethods.pyxel_image_circ(Handle, x, y, radius, color.Value));

    /// <summary>Draws a circle outline (Python: <c>image.circb</c>).</summary>
    public void Circb(float x, float y, float radius, Color color) =>
        Pyxel.Check(NativeMethods.pyxel_image_circb(Handle, x, y, radius, color.Value));

    /// <summary>Draws a filled ellipse (Python: <c>image.elli</c>).</summary>
    public void Elli(float x, float y, float width, float height, Color color) =>
        Pyxel.Check(NativeMethods.pyxel_image_elli(Handle, x, y, width, height, color.Value));

    /// <summary>Draws an ellipse outline (Python: <c>image.ellib</c>).</summary>
    public void Ellib(float x, float y, float width, float height, Color color) =>
        Pyxel.Check(NativeMethods.pyxel_image_ellib(Handle, x, y, width, height, color.Value));

    /// <summary>Draws a filled triangle (Python: <c>image.tri</c>).</summary>
    public void Tri(float x1, float y1, float x2, float y2, float x3, float y3, Color color) =>
        Pyxel.Check(NativeMethods.pyxel_image_tri(Handle, x1, y1, x2, y2, x3, y3, color.Value));

    /// <summary>Draws a triangle outline (Python: <c>image.trib</c>).</summary>
    public void Trib(float x1, float y1, float x2, float y2, float x3, float y3, Color color) =>
        Pyxel.Check(NativeMethods.pyxel_image_trib(Handle, x1, y1, x2, y2, x3, y3, color.Value));

    /// <summary>Flood-fills from (x, y) (Python: <c>image.fill</c>).</summary>
    public void Fill(float x, float y, Color color) =>
        Pyxel.Check(NativeMethods.pyxel_image_fill(Handle, x, y, color.Value));

    /// <summary>Draws text (Python: <c>image.text</c>).</summary>
    public void Text(float x, float y, string text, Color color, Font? font = null)
    {
        fixed (byte* textPtr = Pyxel.ToUtf8Required(text))
        {
            Pyxel.Check(NativeMethods.pyxel_image_text(
                Handle, x, y, textPtr, color.Value, Font.HandleOrNull(font)));
        }
    }

    /// <summary>Copies a region of another image onto this one (Python: <c>image.blt</c>).</summary>
    public void Blt(
        float x,
        float y,
        Image image,
        float u,
        float v,
        float width,
        float height,
        Color? colorKey = null,
        float? rotate = null,
        float? scale = null) =>
        Pyxel.Check(NativeMethods.pyxel_image_blt(
            Handle,
            x,
            y,
            image.Handle,
            u,
            v,
            width,
            height,
            Pyxel.ToSentinel(colorKey),
            Pyxel.ToSentinel(rotate),
            Pyxel.ToSentinel(scale)));

    /// <summary>Copies a region of an image bank onto this one (Python: <c>image.blt</c>).</summary>
    public void Blt(
        float x,
        float y,
        int imageBank,
        float u,
        float v,
        float width,
        float height,
        Color? colorKey = null,
        float? rotate = null,
        float? scale = null) =>
        Blt(x, y, Pyxel.Images[imageBank], u, v, width, height, colorKey, rotate, scale);

    /// <summary>Draws a region of a tilemap onto this image (Python: <c>image.bltm</c>).</summary>
    public void Bltm(
        float x,
        float y,
        Tilemap tilemap,
        float u,
        float v,
        float width,
        float height,
        Color? colorKey = null,
        float? rotate = null,
        float? scale = null) =>
        Pyxel.Check(NativeMethods.pyxel_image_bltm(
            Handle,
            x,
            y,
            tilemap.Handle,
            u,
            v,
            width,
            height,
            Pyxel.ToSentinel(colorKey),
            Pyxel.ToSentinel(rotate),
            Pyxel.ToSentinel(scale)));

    /// <summary>Draws a region of a tilemap bank onto this image (Python: <c>image.bltm</c>).</summary>
    public void Bltm(
        float x,
        float y,
        int tilemapBank,
        float u,
        float v,
        float width,
        float height,
        Color? colorKey = null,
        float? rotate = null,
        float? scale = null) =>
        Bltm(x, y, Pyxel.Tilemaps[tilemapBank], u, v, width, height, colorKey, rotate, scale);

    /// <summary>Perspective-projects a region of an image onto this one (Python: <c>image.blt3d</c>).</summary>
    public void Blt3d(
        float x,
        float y,
        float width,
        float height,
        Image image,
        (float X, float Y, float Z) pos,
        (float X, float Y, float Z) rot,
        float? fov = null,
        Color? colorKey = null) =>
        Pyxel.Check(NativeMethods.pyxel_image_blt3d(
            Handle,
            x,
            y,
            width,
            height,
            image.Handle,
            pos.X,
            pos.Y,
            pos.Z,
            rot.X,
            rot.Y,
            rot.Z,
            Pyxel.ToSentinel(fov),
            Pyxel.ToSentinel(colorKey)));

    /// <summary>Perspective-projects a region of an image bank onto this one (Python: <c>image.blt3d</c>).</summary>
    public void Blt3d(
        float x,
        float y,
        float width,
        float height,
        int imageBank,
        (float X, float Y, float Z) pos,
        (float X, float Y, float Z) rot,
        float? fov = null,
        Color? colorKey = null) =>
        Blt3d(x, y, width, height, Pyxel.Images[imageBank], pos, rot, fov, colorKey);

    /// <summary>Perspective-projects a region of a tilemap onto this image (Python: <c>image.bltm3d</c>).</summary>
    public void Bltm3d(
        float x,
        float y,
        float width,
        float height,
        Tilemap tilemap,
        (float X, float Y, float Z) pos,
        (float X, float Y, float Z) rot,
        float? fov = null,
        Color? colorKey = null) =>
        Pyxel.Check(NativeMethods.pyxel_image_bltm3d(
            Handle,
            x,
            y,
            width,
            height,
            tilemap.Handle,
            pos.X,
            pos.Y,
            pos.Z,
            rot.X,
            rot.Y,
            rot.Z,
            Pyxel.ToSentinel(fov),
            Pyxel.ToSentinel(colorKey)));

    /// <summary>Perspective-projects a region of a tilemap bank onto this image (Python: <c>image.bltm3d</c>).</summary>
    public void Bltm3d(
        float x,
        float y,
        float width,
        float height,
        int tilemapBank,
        (float X, float Y, float Z) pos,
        (float X, float Y, float Z) rot,
        float? fov = null,
        Color? colorKey = null) =>
        Bltm3d(x, y, width, height, Pyxel.Tilemaps[tilemapBank], pos, rot, fov, colorKey);

    /// <summary>
    /// Releases the native image. Safe to call from any thread; the actual
    /// release may be deferred to the next frame. No-op for image banks and
    /// the screen.
    /// </summary>
    public void Dispose()
    {
        if (_isAppLifetime || _handle is null)
        {
            return;
        }
        Pyxel.ReleaseHandle((IntPtr)_handle, Pyxel.HandleKind.Image);
        _handle = null;
        GC.SuppressFinalize(this);
    }

    ~Image()
    {
        if (_handle is not null)
        {
            Pyxel.ReleaseHandle((IntPtr)_handle, Pyxel.HandleKind.Image);
        }
    }
}

/// <summary>The image banks, <c>Pyxel.Images[0..2]</c> (Python: <c>pyxel.images</c>).</summary>
public sealed unsafe class ImageBankList
{
    private Image?[]? _banks;

    internal ImageBankList()
    {
    }

    public int Count
    {
        get
        {
            uint count;
            Pyxel.Check(NativeMethods.pyxel_num_images(&count));
            return (int)count;
        }
    }

    public Image this[int index]
    {
        get
        {
            _banks ??= new Image?[Count];
            var bank = _banks[index];
            if (bank is null)
            {
                void* handle;
                Pyxel.Check(NativeMethods.pyxel_image_bank((uint)index, &handle));
                bank = new Image(handle, isAppLifetime: true);
                _banks[index] = bank;
            }
            return bank;
        }
    }
}
