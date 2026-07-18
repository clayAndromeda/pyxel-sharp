using PyxelSharp.Native;

namespace PyxelSharp;

public static unsafe partial class Pyxel
{
    private static Image? _screen;

    /// <summary>The image banks (Python: <c>pyxel.images</c>).</summary>
    public static ImageBankList Images { get; } = new();

    /// <summary>The tilemap banks (Python: <c>pyxel.tilemaps</c>).</summary>
    public static TilemapBankList Tilemaps { get; } = new();

    /// <summary>The screen image (Python: <c>pyxel.screen</c>). Valid after <see cref="Init"/>.</summary>
    public static Image Screen
    {
        get
        {
            if (_screen is null)
            {
                void* handle;
                Check(NativeMethods.pyxel_screen(&handle));
                _screen = new Image(handle, isAppLifetime: true);
            }
            return _screen;
        }
    }

    /// <summary>Clears the screen (Python: <c>pyxel.cls</c>).</summary>
    public static void Cls(Color color) => Check(NativeMethods.pyxel_cls(color.Value));

    /// <summary>Reads the color of a pixel (Python: <c>pyxel.pget</c>).</summary>
    public static Color Pget(float x, float y)
    {
        byte color;
        Check(NativeMethods.pyxel_pget(x, y, &color));
        return new Color(color);
    }

    /// <summary>Draws a pixel (Python: <c>pyxel.pset</c>).</summary>
    public static void Pset(float x, float y, Color color) =>
        Check(NativeMethods.pyxel_pset(x, y, color.Value));

    /// <summary>Draws a line (Python: <c>pyxel.line</c>).</summary>
    public static void Line(float x1, float y1, float x2, float y2, Color color) =>
        Check(NativeMethods.pyxel_line(x1, y1, x2, y2, color.Value));

    /// <summary>Draws a filled rectangle (Python: <c>pyxel.rect</c>).</summary>
    public static void Rect(float x, float y, float width, float height, Color color) =>
        Check(NativeMethods.pyxel_rect(x, y, width, height, color.Value));

    /// <summary>Draws a rectangle outline (Python: <c>pyxel.rectb</c>).</summary>
    public static void Rectb(float x, float y, float width, float height, Color color) =>
        Check(NativeMethods.pyxel_rectb(x, y, width, height, color.Value));

    /// <summary>Draws a filled circle (Python: <c>pyxel.circ</c>).</summary>
    public static void Circ(float x, float y, float radius, Color color) =>
        Check(NativeMethods.pyxel_circ(x, y, radius, color.Value));

    /// <summary>Draws a circle outline (Python: <c>pyxel.circb</c>).</summary>
    public static void Circb(float x, float y, float radius, Color color) =>
        Check(NativeMethods.pyxel_circb(x, y, radius, color.Value));

    /// <summary>Draws a filled ellipse (Python: <c>pyxel.elli</c>).</summary>
    public static void Elli(float x, float y, float width, float height, Color color) =>
        Check(NativeMethods.pyxel_elli(x, y, width, height, color.Value));

    /// <summary>Draws an ellipse outline (Python: <c>pyxel.ellib</c>).</summary>
    public static void Ellib(float x, float y, float width, float height, Color color) =>
        Check(NativeMethods.pyxel_ellib(x, y, width, height, color.Value));

    /// <summary>Draws a filled triangle (Python: <c>pyxel.tri</c>).</summary>
    public static void Tri(float x1, float y1, float x2, float y2, float x3, float y3, Color color) =>
        Check(NativeMethods.pyxel_tri(x1, y1, x2, y2, x3, y3, color.Value));

    /// <summary>Draws a triangle outline (Python: <c>pyxel.trib</c>).</summary>
    public static void Trib(float x1, float y1, float x2, float y2, float x3, float y3, Color color) =>
        Check(NativeMethods.pyxel_trib(x1, y1, x2, y2, x3, y3, color.Value));

    /// <summary>Flood-fills from (x, y) (Python: <c>pyxel.fill</c>).</summary>
    public static void Fill(float x, float y, Color color) =>
        Check(NativeMethods.pyxel_fill(x, y, color.Value));

    /// <summary>Draws text (Python: <c>pyxel.text</c>).</summary>
    public static void Text(float x, float y, string text, Color color, Font? font = null)
    {
        fixed (byte* textPtr = ToUtf8Required(text))
        {
            Check(NativeMethods.pyxel_text(x, y, textPtr, color.Value, Font.HandleOrNull(font)));
        }
    }

    /// <summary>Copies a region of an image onto the screen (Python: <c>pyxel.blt</c>).</summary>
    public static void Blt(
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
        Check(NativeMethods.pyxel_blt(
            x,
            y,
            image.Handle,
            u,
            v,
            width,
            height,
            ToSentinel(colorKey),
            ToSentinel(rotate),
            ToSentinel(scale)));

    /// <summary>Copies a region of an image bank onto the screen (Python: <c>pyxel.blt</c>).</summary>
    public static void Blt(
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
        Blt(x, y, Images[imageBank], u, v, width, height, colorKey, rotate, scale);

    /// <summary>Draws a region of a tilemap onto the screen (Python: <c>pyxel.bltm</c>).</summary>
    public static void Bltm(
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
        Check(NativeMethods.pyxel_bltm(
            x,
            y,
            tilemap.Handle,
            u,
            v,
            width,
            height,
            ToSentinel(colorKey),
            ToSentinel(rotate),
            ToSentinel(scale)));

    /// <summary>Draws a region of a tilemap bank onto the screen (Python: <c>pyxel.bltm</c>).</summary>
    public static void Bltm(
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
        Bltm(x, y, Tilemaps[tilemapBank], u, v, width, height, colorKey, rotate, scale);

    /// <summary>Perspective-projects a region of an image onto the screen (Python: <c>pyxel.blt3d</c>).</summary>
    public static void Blt3d(
        float x,
        float y,
        float width,
        float height,
        Image image,
        (float X, float Y, float Z) pos,
        (float X, float Y, float Z) rot,
        float? fov = null,
        Color? colorKey = null) =>
        Check(NativeMethods.pyxel_blt3d(
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
            ToSentinel(fov),
            ToSentinel(colorKey)));

    /// <summary>Perspective-projects a region of an image bank onto the screen (Python: <c>pyxel.blt3d</c>).</summary>
    public static void Blt3d(
        float x,
        float y,
        float width,
        float height,
        int imageBank,
        (float X, float Y, float Z) pos,
        (float X, float Y, float Z) rot,
        float? fov = null,
        Color? colorKey = null) =>
        Blt3d(x, y, width, height, Images[imageBank], pos, rot, fov, colorKey);

    /// <summary>Perspective-projects a region of a tilemap onto the screen (Python: <c>pyxel.bltm3d</c>).</summary>
    public static void Bltm3d(
        float x,
        float y,
        float width,
        float height,
        Tilemap tilemap,
        (float X, float Y, float Z) pos,
        (float X, float Y, float Z) rot,
        float? fov = null,
        Color? colorKey = null) =>
        Check(NativeMethods.pyxel_bltm3d(
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
            ToSentinel(fov),
            ToSentinel(colorKey)));

    /// <summary>Perspective-projects a region of a tilemap bank onto the screen (Python: <c>pyxel.bltm3d</c>).</summary>
    public static void Bltm3d(
        float x,
        float y,
        float width,
        float height,
        int tilemapBank,
        (float X, float Y, float Z) pos,
        (float X, float Y, float Z) rot,
        float? fov = null,
        Color? colorKey = null) =>
        Bltm3d(x, y, width, height, Tilemaps[tilemapBank], pos, rot, fov, colorKey);

    /// <summary>Sets the clipping region (Python: <c>pyxel.clip(x, y, w, h)</c>).</summary>
    public static void Clip(float x, float y, float width, float height) =>
        Check(NativeMethods.pyxel_clip(x, y, width, height));

    /// <summary>Resets the clipping region (Python: <c>pyxel.clip()</c>).</summary>
    public static void Clip() => Check(NativeMethods.pyxel_clip_reset());

    /// <summary>Sets the camera offset (Python: <c>pyxel.camera(x, y)</c>).</summary>
    public static void Camera(float x, float y) => Check(NativeMethods.pyxel_camera(x, y));

    /// <summary>Resets the camera offset (Python: <c>pyxel.camera()</c>).</summary>
    public static void Camera() => Check(NativeMethods.pyxel_camera_reset());

    /// <summary>Sets the dithering alpha (Python: <c>pyxel.dither</c>).</summary>
    public static void Dither(float alpha) => Check(NativeMethods.pyxel_dither(alpha));

    /// <summary>Replaces a palette color for subsequent draws (Python: <c>pyxel.pal(src, dst)</c>).</summary>
    public static void Pal(Color srcColor, Color dstColor) =>
        Check(NativeMethods.pyxel_pal(srcColor.Value, dstColor.Value));

    /// <summary>Resets the palette mapping (Python: <c>pyxel.pal()</c>).</summary>
    public static void Pal() => Check(NativeMethods.pyxel_pal_reset());
}
