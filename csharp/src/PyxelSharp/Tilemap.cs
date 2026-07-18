using System.Runtime.InteropServices;
using PyxelSharp.Native;

namespace PyxelSharp;

/// <summary>
/// A tilemap (canvas of <see cref="Tile"/> cells referencing a source image).
/// Mirrors Python's <c>pyxel.Tilemap</c>: the tilemap banks
/// (<see cref="Pyxel.Tilemaps"/>) and user-created tilemaps share this class.
/// </summary>
/// <remarks>
/// Handles are released on the pyxel thread like <see cref="Image"/>:
/// <see cref="Dispose"/> on that thread frees immediately, finalizers and
/// other threads defer the release to the next frame. Tilemap banks live for
/// the app lifetime; disposing them is a no-op.
/// </remarks>
public sealed unsafe class Tilemap : IDisposable
{
    private void* _handle;
    private readonly bool _isAppLifetime;

    internal Tilemap(void* handle, bool isAppLifetime = false)
    {
        _handle = handle;
        _isAppLifetime = isAppLifetime;
        if (isAppLifetime)
        {
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>Creates a tilemap whose tiles reference an image bank (Python: <c>pyxel.Tilemap(w, h, img)</c>).</summary>
    public Tilemap(int width, int height, int imageBank)
    {
        void* handle;
        Pyxel.Check(NativeMethods.pyxel_tilemap_new(
            (uint)width, (uint)height, null, (uint)imageBank, &handle));
        _handle = handle;
    }

    /// <summary>Creates a tilemap whose tiles reference an image (Python: <c>pyxel.Tilemap(w, h, img)</c>).</summary>
    public Tilemap(int width, int height, Image image)
    {
        void* handle;
        Pyxel.Check(NativeMethods.pyxel_tilemap_new(
            (uint)width, (uint)height, image.Handle, 0, &handle));
        _handle = handle;
    }

    /// <summary>Loads a layer of a TMX file into a new tilemap (Python: <c>pyxel.Tilemap.from_tmx</c>).</summary>
    public static Tilemap FromTmx(string filename, int layer)
    {
        fixed (byte* filenamePtr = Pyxel.ToUtf8Required(filename))
        {
            void* handle;
            Pyxel.Check(NativeMethods.pyxel_tilemap_from_tmx(filenamePtr, (uint)layer, &handle));
            return new Tilemap(handle);
        }
    }

    internal void* Handle =>
        _handle is not null ? _handle : throw new ObjectDisposedException(nameof(Tilemap));

    /// <summary>Tilemap width in tiles (Python: <c>tilemap.width</c>).</summary>
    public int Width
    {
        get
        {
            uint value;
            Pyxel.Check(NativeMethods.pyxel_tilemap_width(Handle, &value));
            return (int)value;
        }
    }

    /// <summary>Tilemap height in tiles (Python: <c>tilemap.height</c>).</summary>
    public int Height
    {
        get
        {
            uint value;
            Pyxel.Check(NativeMethods.pyxel_tilemap_height(Handle, &value));
            return (int)value;
        }
    }

    /// <summary>
    /// The image bank index the tiles reference, or null when the source is a
    /// standalone image (Python: <c>tilemap.imgsrc</c>).
    /// </summary>
    public int? ImageSourceIndex
    {
        get
        {
            int index;
            void* image;
            Pyxel.Check(NativeMethods.pyxel_tilemap_imgsrc(Handle, &index, &image));
            if (image is not null)
            {
                Pyxel.ReleaseHandle((IntPtr)image, Pyxel.HandleKind.Image);
                return null;
            }
            return index;
        }
    }

    /// <summary>
    /// The source image when the tiles reference a standalone image, or null
    /// when they reference an image bank (Python: <c>tilemap.imgsrc</c>).
    /// </summary>
    public Image? ImageSourceImage
    {
        get
        {
            int index;
            void* image;
            Pyxel.Check(NativeMethods.pyxel_tilemap_imgsrc(Handle, &index, &image));
            return image is null ? null : new Image(image);
        }
    }

    /// <summary>Points the tiles at an image bank (Python: <c>tilemap.imgsrc = int</c>).</summary>
    public void SetImageSource(int imageBank) =>
        Pyxel.Check(NativeMethods.pyxel_tilemap_set_imgsrc(Handle, null, (uint)imageBank));

    /// <summary>Points the tiles at an image (Python: <c>tilemap.imgsrc = Image</c>).</summary>
    public void SetImageSource(Image image) =>
        Pyxel.Check(NativeMethods.pyxel_tilemap_set_imgsrc(Handle, image.Handle, 0));

    /// <summary>
    /// The raw tile buffer (row-major <see cref="Width"/>×<see cref="Height"/>
    /// cells, two u16 per tile; Python: <c>tilemap.data_ptr</c>). Valid while
    /// the tilemap is alive and its size unchanged.
    /// </summary>
    public Span<ushort> Data
    {
        get
        {
            ushort* ptr;
            Pyxel.Check(NativeMethods.pyxel_tilemap_data_ptr(Handle, &ptr));
            return new Span<ushort>(ptr, Width * Height * 2);
        }
    }

    /// <summary>Writes rows of tile data at (x, y) (Python: <c>tilemap.set</c>).</summary>
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
                Pyxel.Check(NativeMethods.pyxel_tilemap_set(
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

    /// <summary>Loads a TMX layer at (x, y) (Python: <c>tilemap.load</c>).</summary>
    public void Load(int x, int y, string filename, int layer)
    {
        fixed (byte* filenamePtr = Pyxel.ToUtf8Required(filename))
        {
            Pyxel.Check(NativeMethods.pyxel_tilemap_load(Handle, x, y, filenamePtr, (uint)layer));
        }
    }

    /// <summary>Sets the clipping region (Python: <c>tilemap.clip(x, y, w, h)</c>).</summary>
    public void Clip(float x, float y, float width, float height) =>
        Pyxel.Check(NativeMethods.pyxel_tilemap_clip(Handle, x, y, width, height));

    /// <summary>Resets the clipping region (Python: <c>tilemap.clip()</c>).</summary>
    public void Clip() => Pyxel.Check(NativeMethods.pyxel_tilemap_clip_reset(Handle));

    /// <summary>Sets the camera offset (Python: <c>tilemap.camera(x, y)</c>).</summary>
    public void Camera(float x, float y) =>
        Pyxel.Check(NativeMethods.pyxel_tilemap_camera(Handle, x, y));

    /// <summary>Resets the camera offset (Python: <c>tilemap.camera()</c>).</summary>
    public void Camera() => Pyxel.Check(NativeMethods.pyxel_tilemap_camera_reset(Handle));

    /// <summary>Fills the tilemap with a tile (Python: <c>tilemap.cls</c>).</summary>
    public void Cls(Tile tile) =>
        Pyxel.Check(NativeMethods.pyxel_tilemap_cls(Handle, tile.X, tile.Y));

    /// <summary>Reads the tile at (x, y) (Python: <c>tilemap.pget</c>).</summary>
    public Tile Pget(float x, float y)
    {
        ushort tileX;
        ushort tileY;
        Pyxel.Check(NativeMethods.pyxel_tilemap_pget(Handle, x, y, &tileX, &tileY));
        return new Tile(tileX, tileY);
    }

    /// <summary>Writes the tile at (x, y) (Python: <c>tilemap.pset</c>).</summary>
    public void Pset(float x, float y, Tile tile) =>
        Pyxel.Check(NativeMethods.pyxel_tilemap_pset(Handle, x, y, tile.X, tile.Y));

    /// <summary>Draws a line of tiles (Python: <c>tilemap.line</c>).</summary>
    public void Line(float x1, float y1, float x2, float y2, Tile tile) =>
        Pyxel.Check(NativeMethods.pyxel_tilemap_line(Handle, x1, y1, x2, y2, tile.X, tile.Y));

    /// <summary>Draws a filled rectangle of tiles (Python: <c>tilemap.rect</c>).</summary>
    public void Rect(float x, float y, float width, float height, Tile tile) =>
        Pyxel.Check(NativeMethods.pyxel_tilemap_rect(Handle, x, y, width, height, tile.X, tile.Y));

    /// <summary>Draws a rectangle outline of tiles (Python: <c>tilemap.rectb</c>).</summary>
    public void Rectb(float x, float y, float width, float height, Tile tile) =>
        Pyxel.Check(NativeMethods.pyxel_tilemap_rectb(Handle, x, y, width, height, tile.X, tile.Y));

    /// <summary>Draws a filled circle of tiles (Python: <c>tilemap.circ</c>).</summary>
    public void Circ(float x, float y, float radius, Tile tile) =>
        Pyxel.Check(NativeMethods.pyxel_tilemap_circ(Handle, x, y, radius, tile.X, tile.Y));

    /// <summary>Draws a circle outline of tiles (Python: <c>tilemap.circb</c>).</summary>
    public void Circb(float x, float y, float radius, Tile tile) =>
        Pyxel.Check(NativeMethods.pyxel_tilemap_circb(Handle, x, y, radius, tile.X, tile.Y));

    /// <summary>Draws a filled ellipse of tiles (Python: <c>tilemap.elli</c>).</summary>
    public void Elli(float x, float y, float width, float height, Tile tile) =>
        Pyxel.Check(NativeMethods.pyxel_tilemap_elli(Handle, x, y, width, height, tile.X, tile.Y));

    /// <summary>Draws an ellipse outline of tiles (Python: <c>tilemap.ellib</c>).</summary>
    public void Ellib(float x, float y, float width, float height, Tile tile) =>
        Pyxel.Check(NativeMethods.pyxel_tilemap_ellib(Handle, x, y, width, height, tile.X, tile.Y));

    /// <summary>Draws a filled triangle of tiles (Python: <c>tilemap.tri</c>).</summary>
    public void Tri(float x1, float y1, float x2, float y2, float x3, float y3, Tile tile) =>
        Pyxel.Check(NativeMethods.pyxel_tilemap_tri(
            Handle, x1, y1, x2, y2, x3, y3, tile.X, tile.Y));

    /// <summary>Draws a triangle outline of tiles (Python: <c>tilemap.trib</c>).</summary>
    public void Trib(float x1, float y1, float x2, float y2, float x3, float y3, Tile tile) =>
        Pyxel.Check(NativeMethods.pyxel_tilemap_trib(
            Handle, x1, y1, x2, y2, x3, y3, tile.X, tile.Y));

    /// <summary>Flood-fills from (x, y) (Python: <c>tilemap.fill</c>).</summary>
    public void Fill(float x, float y, Tile tile) =>
        Pyxel.Check(NativeMethods.pyxel_tilemap_fill(Handle, x, y, tile.X, tile.Y));

    /// <summary>
    /// Slides a rect by (dx, dy) against wall tiles, returning the allowed
    /// movement (Python: <c>tilemap.collide</c>).
    /// </summary>
    public (float Dx, float Dy) Collide(
        float x,
        float y,
        float width,
        float height,
        float dx,
        float dy,
        params Tile[] walls)
    {
        var flatWalls = new ushort[walls.Length * 2];
        for (var i = 0; i < walls.Length; i++)
        {
            flatWalls[i * 2] = walls[i].X;
            flatWalls[i * 2 + 1] = walls[i].Y;
        }
        fixed (ushort* wallsPtr = flatWalls)
        {
            float allowedDx;
            float allowedDy;
            Pyxel.Check(NativeMethods.pyxel_tilemap_collide(
                Handle, x, y, width, height, dx, dy,
                wallsPtr, (uint)walls.Length, &allowedDx, &allowedDy));
            return (allowedDx, allowedDy);
        }
    }

    /// <summary>Copies a region of another tilemap onto this one (Python: <c>tilemap.blt</c>).</summary>
    public void Blt(
        float x,
        float y,
        Tilemap tilemap,
        float u,
        float v,
        float width,
        float height,
        Tile? tileKey = null,
        float? rotate = null,
        float? scale = null) =>
        Pyxel.Check(NativeMethods.pyxel_tilemap_blt(
            Handle,
            x,
            y,
            tilemap.Handle,
            u,
            v,
            width,
            height,
            tileKey.HasValue ? tileKey.Value.X : -1,
            tileKey.HasValue ? tileKey.Value.Y : -1,
            Pyxel.ToSentinel(rotate),
            Pyxel.ToSentinel(scale)));

    /// <summary>Copies a region of a tilemap bank onto this one (Python: <c>tilemap.blt</c>).</summary>
    public void Blt(
        float x,
        float y,
        int tilemapBank,
        float u,
        float v,
        float width,
        float height,
        Tile? tileKey = null,
        float? rotate = null,
        float? scale = null) =>
        Blt(x, y, Pyxel.Tilemaps[tilemapBank], u, v, width, height, tileKey, rotate, scale);

    /// <summary>
    /// Releases the native tilemap. Safe to call from any thread; the actual
    /// release may be deferred to the next frame. No-op for tilemap banks.
    /// </summary>
    public void Dispose()
    {
        if (_isAppLifetime || _handle is null)
        {
            return;
        }
        Pyxel.ReleaseHandle((IntPtr)_handle, Pyxel.HandleKind.Tilemap);
        _handle = null;
        GC.SuppressFinalize(this);
    }

    ~Tilemap()
    {
        if (_handle is not null)
        {
            Pyxel.ReleaseHandle((IntPtr)_handle, Pyxel.HandleKind.Tilemap);
        }
    }
}

/// <summary>The tilemap banks, <c>Pyxel.Tilemaps[0..7]</c> (Python: <c>pyxel.tilemaps</c>).</summary>
public sealed unsafe class TilemapBankList
{
    private Tilemap?[]? _banks;

    internal TilemapBankList()
    {
    }

    public int Count
    {
        get
        {
            uint count;
            Pyxel.Check(NativeMethods.pyxel_num_tilemaps(&count));
            return (int)count;
        }
    }

    public Tilemap this[int index]
    {
        get
        {
            _banks ??= new Tilemap?[Count];
            var bank = _banks[index];
            if (bank is null)
            {
                void* handle;
                Pyxel.Check(NativeMethods.pyxel_tilemap_bank((uint)index, &handle));
                bank = new Tilemap(handle, isAppLifetime: true);
                _banks[index] = bank;
            }
            return bank;
        }
    }
}
