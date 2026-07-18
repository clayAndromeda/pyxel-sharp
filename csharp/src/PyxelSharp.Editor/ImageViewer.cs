using PyxelSharp.Editor.Widgets;

namespace PyxelSharp.Editor;

// image_viewer.py
public sealed class ImageViewer : Widget
{
    private const int GridSize = 8;
    private const int MaxTileIndex = 31; // 256 / GridSize - 1

    private readonly bool _isTilemapMode;
    private readonly WidgetVar<int> _imageIndexVar;
    private readonly WidgetVar<string> _helpMessageVar;
    private readonly ScrollBar _hScrollBar;
    private readonly ScrollBar _vScrollBar;
    private int _pressX;
    private int _pressY;
    private int _dragOffsetX;
    private int _dragOffsetY;

    public WidgetVar<int> FocusXVar { get; }
    public WidgetVar<int> FocusYVar { get; }
    public WidgetVar<int> FocusWVar { get; }
    public WidgetVar<int> FocusHVar { get; }
    public WidgetVar<int> ViewportXVar { get; }
    public WidgetVar<int> ViewportYVar { get; }

    public ImageViewer(EditorBase parent)
        : base(parent, 157, parent is ITilemapHost ? 80 : 16, 66,
            parent is ITilemapHost ? 66 : 130)
    {
        _isTilemapMode = parent is ITilemapHost;
        var sliderAmount = _isTilemapMode ? 8 : 16;

        _imageIndexVar = ((IImageViewerHost)parent).ImageIndexVar;
        _helpMessageVar = parent.HelpMessageVar;

        FocusXVar = new WidgetVar<int>(0);
        FocusXVar.AddSetFilter(value => Math.Clamp(value, 0, MaxTileIndex + 1 - FocusWVar!.Get()));
        FocusXVar.Changed += OnFocusXChange;

        FocusYVar = new WidgetVar<int>(0);
        FocusYVar.AddSetFilter(value => Math.Clamp(value, 0, MaxTileIndex + 1 - FocusHVar!.Get()));
        FocusYVar.Changed += OnFocusYChange;

        FocusWVar = new WidgetVar<int>(_isTilemapMode ? 1 : 2);
        FocusHVar = new WidgetVar<int>(_isTilemapMode ? 1 : 2);

        _hScrollBar = new ScrollBar(this, 0, Height - 1, scrollAmount: 32,
            sliderAmount: 8, value: 0, width: 66);
        ViewportXVar = _hScrollBar.ValueVar;

        _vScrollBar = new ScrollBar(this, 65, 0, scrollAmount: 32,
            sliderAmount: sliderAmount, value: 0, height: Height);
        ViewportYVar = _vScrollBar.ValueVar;

        MouseDown += OnMouseDown;
        MouseDrag += OnMouseDrag;
        MouseHover += OnMouseHover;
        Draw += OnDraw;
    }

    public int FocusX
    {
        get => FocusXVar.Get();
        set => FocusXVar.Set(value);
    }

    public int FocusY
    {
        get => FocusYVar.Get();
        set => FocusYVar.Set(value);
    }

    // Helpers

    private (int X, int Y) ScreenToFocus(int x, int y)
    {
        var fx = Math.Clamp(
            ViewportXVar.Get() + EditorMath.FloorDiv(x - X - 1, GridSize), 0, MaxTileIndex);
        var fy = Math.Clamp(
            ViewportYVar.Get() + EditorMath.FloorDiv(y - Y - 1, GridSize), 0, MaxTileIndex);
        return (fx, fy);
    }

    // Event handlers

    private void OnFocusXChange(int value)
    {
        var fx = FocusXVar.Get();
        var fw = FocusWVar.Get();
        var vx = ViewportXVar.Get();
        const int vw = 8;
        ViewportXVar.Set(vx + Math.Min(fx - vx, 0) + Math.Max(fx + fw - vx - vw, 0));
    }

    private void OnFocusYChange(int value)
    {
        var fy = FocusYVar.Get();
        var fh = FocusHVar.Get();
        var vy = ViewportYVar.Get();
        var vh = _isTilemapMode ? 8 : 16;
        ViewportYVar.Set(vy + Math.Min(fy - vy, 0) + Math.Max(fy + fh - vy - vh, 0));
    }

    private void OnMouseDown(Key key, int x, int y)
    {
        if (key == Key.MouseButtonLeft)
        {
            (FocusX, FocusY) = ScreenToFocus(x, y);
            _pressX = FocusX;
            _pressY = FocusY;
        }
        else if (key == Key.MouseButtonRight)
        {
            _dragOffsetX = 0;
            _dragOffsetY = 0;
        }
    }

    private void OnMouseDrag(Key key, int x, int y, int dx, int dy)
    {
        if (key == Key.MouseButtonLeft)
        {
            if (_isTilemapMode)
            {
                var (newX, newY) = ScreenToFocus(x, y);
                FocusWVar.Set(Math.Min(Math.Abs(newX - _pressX) + 1, 8));
                FocusHVar.Set(Math.Min(Math.Abs(newY - _pressY) + 1, 8));
                FocusX = Math.Min(newX, _pressX);
                FocusY = Math.Min(newY, _pressY);
            }
            else
            {
                OnMouseDown(key, x, y);
            }
        }
        else if (key == Key.MouseButtonRight)
        {
            _dragOffsetX -= dx;
            _dragOffsetY -= dy;
            if (Math.Abs(_dragOffsetX) >= GridSize)
            {
                var offset = EditorMath.FloorDiv(_dragOffsetX, GridSize);
                ViewportXVar.Set(ViewportXVar.Get() + offset);
                _dragOffsetX -= offset * GridSize;
            }
            if (Math.Abs(_dragOffsetY) >= GridSize)
            {
                var offset = EditorMath.FloorDiv(_dragOffsetY, GridSize);
                ViewportYVar.Set(ViewportYVar.Get() + offset);
                _dragOffsetY -= offset * GridSize;
            }
        }
    }

    private void OnMouseHover(int x, int y)
    {
        var (fx, fy) = ScreenToFocus(x, y);
        _helpMessageVar.Set(_isTilemapMode
            ? $"TILE:SHIFT+CURSOR ({fx},{fy})"
            : $"TARGET:CURSOR ({fx * GridSize},{fy * GridSize})");
    }

    private void OnDraw()
    {
        DrawPanel(X, Y, Width, Height);

        // Draw image preview
        PyxelUser.UserPal();
        Pyxel.Blt(X + 1, Y + 1, _imageIndexVar.Get(),
            ViewportXVar.Get() * GridSize, ViewportYVar.Get() * GridSize,
            Width - 2, Height - 2);
        Pyxel.Pal();

        // Draw focus outline
        var x = X + (FocusX - ViewportXVar.Get()) * GridSize + 1;
        var y = Y + (FocusY - ViewportYVar.Get()) * GridSize + 1;
        var w = FocusWVar.Get() * GridSize;
        var h = FocusHVar.Get() * GridSize;
        Pyxel.Clip(X + 1, Y + 1, Width - 2, Height - 2);
        Pyxel.Rectb(x, y, w, h, EditorSettings.PanelFocusColor);
        Pyxel.Rectb(x + 1, y + 1, w - 2, h - 2, EditorSettings.PanelFocusBorderColor);
        Pyxel.Rectb(x - 1, y - 1, w + 2, h + 2, EditorSettings.PanelFocusBorderColor);
        Pyxel.Clip();
    }
}
