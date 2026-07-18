namespace PyxelSharp.Editor;

// Python's canvas_panel duck-types over Image and Tilemap (pget/pset/line/
// rect2/fill/blt/get_slice with color-or-tile values); ICanvas<TValue> makes
// that contract explicit so CanvasPanel can stay generic.
internal interface ICanvas<TValue>
{
    TValue Pget(int x, int y);
    void Pset(int x, int y, TValue value);
    void Line(int x1, int y1, int x2, int y2, TValue value);
    void Rect(int x, int y, int width, int height, TValue value);
    void Rect2(int x1, int y1, int x2, int y2, TValue value);
    void Rectb2(int x1, int y1, int x2, int y2, TValue value);
    void Elli2(int x1, int y1, int x2, int y2, TValue value);
    void Ellib2(int x1, int y1, int x2, int y2, TValue value);
    void Fill(int x, int y, TValue value);
    void Blt(int x, int y, ICanvas<TValue> source, int u, int v, int width, int height);

    /// <summary>Copies source metadata (tilemap: imgsrc; image: nothing).</summary>
    void CopySourceFrom(ICanvas<TValue> other);

    TValue[,] GetSlice(int x, int y, int width, int height);
    void SetSlice(int x, int y, TValue[,] data);
}

internal sealed class ImageCanvas(Image image) : ICanvas<Color>
{
    public Image Image { get; } = image;

    public Color Pget(int x, int y) => Image.Pget(x, y);
    public void Pset(int x, int y, Color value) => Image.Pset(x, y, value);
    public void Line(int x1, int y1, int x2, int y2, Color value) =>
        Image.Line(x1, y1, x2, y2, value);
    public void Rect(int x, int y, int width, int height, Color value) =>
        Image.Rect(x, y, width, height, value);
    public void Rect2(int x1, int y1, int x2, int y2, Color value) =>
        Image.Rect2(x1, y1, x2, y2, value);
    public void Rectb2(int x1, int y1, int x2, int y2, Color value) =>
        Image.Rectb2(x1, y1, x2, y2, value);
    public void Elli2(int x1, int y1, int x2, int y2, Color value) =>
        Image.Elli2(x1, y1, x2, y2, value);
    public void Ellib2(int x1, int y1, int x2, int y2, Color value) =>
        Image.Ellib2(x1, y1, x2, y2, value);
    public void Fill(int x, int y, Color value) => Image.Fill(x, y, value);
    public void Blt(int x, int y, ICanvas<Color> source, int u, int v, int width, int height) =>
        Image.Blt(x, y, ((ImageCanvas)source).Image, u, v, width, height);
    public void CopySourceFrom(ICanvas<Color> other)
    {
    }
    public Color[,] GetSlice(int x, int y, int width, int height) =>
        Image.GetSlice(x, y, width, height);
    public void SetSlice(int x, int y, Color[,] data) => Image.SetSlice(x, y, data);
}

internal sealed class TilemapCanvas(Tilemap tilemap) : ICanvas<Tile>
{
    public Tilemap Tilemap { get; } = tilemap;

    public Tile Pget(int x, int y) => Tilemap.Pget(x, y);
    public void Pset(int x, int y, Tile value) => Tilemap.Pset(x, y, value);
    public void Line(int x1, int y1, int x2, int y2, Tile value) =>
        Tilemap.Line(x1, y1, x2, y2, value);
    public void Rect(int x, int y, int width, int height, Tile value) =>
        Tilemap.Rect(x, y, width, height, value);
    public void Rect2(int x1, int y1, int x2, int y2, Tile value) =>
        Tilemap.Rect2(x1, y1, x2, y2, value);
    public void Rectb2(int x1, int y1, int x2, int y2, Tile value) =>
        Tilemap.Rectb2(x1, y1, x2, y2, value);
    public void Elli2(int x1, int y1, int x2, int y2, Tile value) =>
        Tilemap.Elli2(x1, y1, x2, y2, value);
    public void Ellib2(int x1, int y1, int x2, int y2, Tile value) =>
        Tilemap.Ellib2(x1, y1, x2, y2, value);
    public void Fill(int x, int y, Tile value) => Tilemap.Fill(x, y, value);
    public void Blt(int x, int y, ICanvas<Tile> source, int u, int v, int width, int height) =>
        Tilemap.Blt(x, y, ((TilemapCanvas)source).Tilemap, u, v, width, height);
    public void CopySourceFrom(ICanvas<Tile> other) =>
        Tilemap.SetImageSource(((TilemapCanvas)other).Tilemap.ImageSourceIndex ?? 0);
    public Tile[,] GetSlice(int x, int y, int width, int height) =>
        Tilemap.GetSlice(x, y, width, height);
    public void SetSlice(int x, int y, Tile[,] data) => Tilemap.SetSlice(x, y, data);
}

internal static class CanvasData
{
    public static bool SliceEquals<T>(T[,] a, T[,] b)
    {
        if (a.GetLength(0) != b.GetLength(0) || a.GetLength(1) != b.GetLength(1))
        {
            return false;
        }
        for (var yi = 0; yi < a.GetLength(0); yi++)
        {
            for (var xi = 0; xi < a.GetLength(1); xi++)
            {
                if (!EqualityComparer<T>.Default.Equals(a[yi, xi], b[yi, xi]))
                {
                    return false;
                }
            }
        }
        return true;
    }
}
