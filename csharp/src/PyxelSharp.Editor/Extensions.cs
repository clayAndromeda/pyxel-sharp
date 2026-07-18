namespace PyxelSharp.Editor;

// extensions.py
//
// Python monkey-patches these onto the pyxel module / Image / Tilemap;
// here they are a static state holder plus extension methods.

/// <summary>Editor-only palette state (Python: <c>pyxel.num_user_colors</c> /
/// <c>pyxel.user_pal()</c>).</summary>
public static class PyxelUser
{
    /// <summary>Number of user palette colors, set at App startup from the
    /// loaded resource palette.</summary>
    public static int NumUserColors { get; set; }

    /// <summary>Maps drawing colors 0..N-1 to the user palette entries stored
    /// after the 16 system colors.</summary>
    public static void UserPal()
    {
        for (var i = 0; i < NumUserColors; i++)
        {
            Pyxel.Pal(i, Pyxel.NumColors + i);
        }
    }
}

public static class ImageExtensions
{
    private static (int X1, int Y1, int X2, int Y2) NormalizeRect(int x1, int y1, int x2, int y2) =>
        (Math.Min(x1, x2), Math.Min(y1, y2), Math.Max(x1, x2), Math.Max(y1, y2));

    public static void Rect2(this Image image, int x1, int y1, int x2, int y2, Color color)
    {
        (x1, y1, x2, y2) = NormalizeRect(x1, y1, x2, y2);
        image.Rect(x1, y1, x2 - x1 + 1, y2 - y1 + 1, color);
    }

    public static void Rectb2(this Image image, int x1, int y1, int x2, int y2, Color color)
    {
        (x1, y1, x2, y2) = NormalizeRect(x1, y1, x2, y2);
        image.Rectb(x1, y1, x2 - x1 + 1, y2 - y1 + 1, color);
    }

    public static void Elli2(this Image image, int x1, int y1, int x2, int y2, Color color)
    {
        (x1, y1, x2, y2) = NormalizeRect(x1, y1, x2, y2);
        image.Elli(x1, y1, x2 - x1 + 1, y2 - y1 + 1, color);
    }

    public static void Ellib2(this Image image, int x1, int y1, int x2, int y2, Color color)
    {
        (x1, y1, x2, y2) = NormalizeRect(x1, y1, x2, y2);
        image.Ellib(x1, y1, x2 - x1 + 1, y2 - y1 + 1, color);
    }

    /// <summary>Reads a rectangular region as [height, width] color values.</summary>
    public static Color[,] GetSlice(this Image image, int x, int y, int width, int height)
    {
        var data = new Color[height, width];
        for (var yi = 0; yi < height; yi++)
        {
            for (var xi = 0; xi < width; xi++)
            {
                data[yi, xi] = image.Pget(x + xi, y + yi);
            }
        }
        return data;
    }

    public static void SetSlice(this Image image, int x, int y, Color[,] data)
    {
        var height = data.GetLength(0);
        var width = data.GetLength(1);
        for (var yi = 0; yi < height; yi++)
        {
            for (var xi = 0; xi < width; xi++)
            {
                image.Pset(x + xi, y + yi, data[yi, xi]);
            }
        }
    }

}

public static class TilemapExtensions
{
    private static (int X1, int Y1, int X2, int Y2) NormalizeRect(int x1, int y1, int x2, int y2) =>
        (Math.Min(x1, x2), Math.Min(y1, y2), Math.Max(x1, x2), Math.Max(y1, y2));

    public static void Rect2(this Tilemap tilemap, int x1, int y1, int x2, int y2, Tile tile)
    {
        (x1, y1, x2, y2) = NormalizeRect(x1, y1, x2, y2);
        tilemap.Rect(x1, y1, x2 - x1 + 1, y2 - y1 + 1, tile);
    }

    public static void Rectb2(this Tilemap tilemap, int x1, int y1, int x2, int y2, Tile tile)
    {
        (x1, y1, x2, y2) = NormalizeRect(x1, y1, x2, y2);
        tilemap.Rectb(x1, y1, x2 - x1 + 1, y2 - y1 + 1, tile);
    }

    public static void Elli2(this Tilemap tilemap, int x1, int y1, int x2, int y2, Tile tile)
    {
        (x1, y1, x2, y2) = NormalizeRect(x1, y1, x2, y2);
        tilemap.Elli(x1, y1, x2 - x1 + 1, y2 - y1 + 1, tile);
    }

    public static void Ellib2(this Tilemap tilemap, int x1, int y1, int x2, int y2, Tile tile)
    {
        (x1, y1, x2, y2) = NormalizeRect(x1, y1, x2, y2);
        tilemap.Ellib(x1, y1, x2 - x1 + 1, y2 - y1 + 1, tile);
    }

    /// <summary>Reads a rectangular region as [height, width] tiles.</summary>
    public static Tile[,] GetSlice(this Tilemap tilemap, int x, int y, int width, int height)
    {
        var data = new Tile[height, width];
        for (var yi = 0; yi < height; yi++)
        {
            for (var xi = 0; xi < width; xi++)
            {
                data[yi, xi] = tilemap.Pget(x + xi, y + yi);
            }
        }
        return data;
    }

    public static void SetSlice(this Tilemap tilemap, int x, int y, Tile[,] data)
    {
        var height = data.GetLength(0);
        var width = data.GetLength(1);
        for (var yi = 0; yi < height; yi++)
        {
            for (var xi = 0; xi < width; xi++)
            {
                tilemap.Pset(x + xi, y + yi, data[yi, xi]);
            }
        }
    }
}
