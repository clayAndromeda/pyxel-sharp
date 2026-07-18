namespace PyxelSharp.Editor.Widgets;

// widgets/color_picker.py
public class ColorPicker : Widget
{
    private readonly bool _withShadow;
    private readonly int _colorWidth;
    private readonly int _colorHeight;
    private readonly int _numCols;
    private readonly int _numRows;

    public WidgetVar<int> ValueVar { get; }

    public event Action<int>? Change;

    public ColorPicker(Widget? parent, int x, int y, int value, bool withShadow = false,
        bool isVisible = true, bool isEnabled = true)
        : base(parent, x, y, 65, 17, isVisible, isEnabled)
    {
        _withShadow = withShadow;
        _colorWidth = PyxelUser.NumUserColors > 16 ? 4 : 8;
        _colorHeight = PyxelUser.NumUserColors > 32 ? 4 : 8;
        _numCols = 64 / _colorWidth;
        _numRows = 16 / _colorHeight;

        ValueVar = new WidgetVar<int>(value);
        ValueVar.AddSetFilter(value2 => Math.Min(value2, PyxelUser.NumUserColors - 1));
        ValueVar.Changed += value2 => Change?.Invoke(value2);

        MouseDown += OnMouseDown;
        MouseDrag += (key, x2, y2, _, _) => OnMouseDown(key, x2, y2);
        Draw += OnDraw;
    }

    public int Value
    {
        get => ValueVar.Get();
        set => ValueVar.Set(value);
    }

    public int? CheckValue(int x, int y)
    {
        x -= X + 1;
        y -= Y + 1;
        var cw = _colorWidth;
        var ch = _colorHeight;
        if (x >= 0 && x < Width - 1 && y >= 0 && y < Height - 1)
        {
            var col = EditorMath.FloorDiv(y, ch) * _numCols + EditorMath.FloorDiv(x, cw);
            return col < PyxelUser.NumUserColors ? col : null;
        }
        return null;
    }

    private void OnMouseDown(Key key, int x, int y)
    {
        if (key != Key.MouseButtonLeft)
        {
            return;
        }

        if (CheckValue(x, y) is { } value)
        {
            Value = value;
        }
    }

    // Drawing

    private void DrawColors()
    {
        var cw = _colorWidth;
        var ch = _colorHeight;
        PyxelUser.UserPal();
        for (var yi = 0; yi < _numRows; yi++)
        {
            for (var xi = 0; xi < _numCols; xi++)
            {
                var col = yi * _numCols + xi;
                if (col < PyxelUser.NumUserColors)
                {
                    Pyxel.Rect(X + xi * cw + 1, Y + yi * ch + 1, cw - 1, ch - 1, col);
                }
            }
        }
        Pyxel.Pal();
    }

    private void DrawCursor()
    {
        var col = Value;
        if (col >= PyxelUser.NumUserColors)
        {
            return;
        }

        var cw = _colorWidth;
        var ch = _colorHeight;
        var x = X + cw * (col % _numCols) + cw / 2;
        var y = Y + ch * (col / _numCols) + ch / 2;
        var rgb = Pyxel.Colors[Pyxel.NumColors + col];
        // ITU-R BT.601 luma
        var brightness = (int)(
            ((rgb >> 16) & 0xFF) * 0.299
            + ((rgb >> 8) & 0xFF) * 0.587
            + (rgb & 0xFF) * 0.114);
        // Cursor scales with cell size:
        //   cell 8x8 -> 3x3 cross, cell 4x8 -> 1x3 vertical, cell 4x4 -> 1x1 dot
        var cursorW = cw / 2 - 1;
        var cursorH = ch / 2 - 1;
        Pyxel.Elli(x - cursorW / 2, y - cursorH / 2, cursorW, cursorH,
            brightness < 140 ? 7 : 0);
    }

    private void OnDraw()
    {
        DrawPanel(X, Y, Width, Height, withShadow: _withShadow);
        DrawColors();
        DrawCursor();
    }
}
