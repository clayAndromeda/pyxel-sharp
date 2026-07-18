namespace PyxelSharp.Editor.Widgets;

// widgets/scroll_bar.py
//
// Exactly one of width/height must be given; the other axis is fixed at 7.
public class ScrollBar : Widget
{
    private readonly bool _isVertical;
    private readonly bool _withShadow;
    private int _dragOffset;
    private bool _isDragged;

    public int ScrollAmount { get; set; }
    public int SliderAmount { get; set; }

    public WidgetVar<int> ValueVar { get; }
    public Button DecButton { get; }
    public Button IncButton { get; }

    public event Action<int>? Change;

    public ScrollBar(Widget? parent, int x, int y, int scrollAmount, int sliderAmount,
        int value, int? width = null, int? height = null, bool withShadow = true,
        bool isVisible = true, bool isEnabled = true)
        : base(parent, x, y, width ?? 7, height ?? 7, isVisible, isEnabled)
    {
        if (width is null == height is null)
        {
            throw new ArgumentException("width or height must be specified, but not both");
        }

        _isVertical = height is not null;
        ScrollAmount = scrollAmount;
        SliderAmount = sliderAmount;
        _withShadow = withShadow;

        ValueVar = new WidgetVar<int>(value);
        ValueVar.AddSetFilter(value2 => Math.Clamp(value2, 0, ScrollAmount));
        ValueVar.Changed += value2 => Change?.Invoke(value2);

        int btnW, btnH, incX, incY;
        if (_isVertical)
        {
            (btnW, btnH) = (7, 6);
            (incX, incY) = (0, Height - 6);
        }
        else
        {
            (btnW, btnH) = (6, 7);
            (incX, incY) = (Width - 6, 0);
        }
        DecButton = new Button(this, 0, 0, btnW, btnH);
        DecButton.Press += OnDecButtonPress;
        IncButton = new Button(this, incX, incY, btnW, btnH);
        IncButton.Press += OnIncButtonPress;

        MouseDown += OnMouseDown;
        MouseUp += (_, _, _) => _isDragged = false;
        MouseDrag += OnMouseDrag;
        MouseRepeat += OnMouseRepeat;
        Draw += OnDraw;
    }

    public int Value
    {
        get => ValueVar.Get();
        set => ValueVar.Set(value);
    }

    // Helpers

    private int ScrollSize => (_isVertical ? Height : Width) - 14;

    private int SliderSize => (int)Math.Round((double)ScrollSize * SliderAmount / ScrollAmount);

    private int SliderPos => (int)Math.Round(7 + (double)ScrollSize * Value / ScrollAmount);

    // Event handlers

    private void OnDecButtonPress() => Value = Math.Max(Value - 1, 0);

    private void OnIncButtonPress() => Value = Math.Min(Value + 1, ScrollAmount - SliderAmount);

    private void OnMouseDown(Key key, int x, int y)
    {
        if (key != Key.MouseButtonLeft)
        {
            return;
        }

        x -= X;
        y -= Y;
        _dragOffset = (_isVertical ? y : x) - SliderPos;
        if (_dragOffset < 0)
        {
            OnDecButtonPress();
        }
        else if (_dragOffset >= SliderSize)
        {
            OnIncButtonPress();
        }
        else
        {
            _isDragged = true;
        }
    }

    private void OnMouseDrag(Key key, int x, int y, int dx, int dy)
    {
        if (!_isDragged)
        {
            return;
        }

        x -= X;
        y -= Y;
        var dragPos = _isVertical ? y : x;
        var value = (double)(dragPos - _dragOffset - 6) * ScrollAmount / ScrollSize;
        Value = (int)Math.Clamp(value, 0, ScrollAmount - SliderAmount);
    }

    private void OnMouseRepeat(Key key, int x, int y)
    {
        if (!_isDragged)
        {
            OnMouseDown(key, x, y);
        }
    }

    // Drawing

    private void DrawVertical(int x, int y, int w, int h)
    {
        var decCol = DecButton.IsPressed ? (Color)6 : WidgetSettings.BackgroundColor;
        var incCol = IncButton.IsPressed ? (Color)6 : WidgetSettings.BackgroundColor;

        // Draw button backgrounds
        Pyxel.Rect(x + 1, y + 1, w - 2, 4, decCol);
        Pyxel.Rect(x + 1, y + 6, w - 2, h - 12, WidgetSettings.BackgroundColor);
        Pyxel.Rect(x + 1, y + h - 5, w - 2, 4, incCol);

        // Draw up arrow
        Pyxel.Pset(x + 3, y + 2, WidgetSettings.PanelColor);
        Pyxel.Line(x + 2, y + 3, x + w - 3, y + 3, WidgetSettings.PanelColor);

        // Draw down arrow
        Pyxel.Pset(x + 3, y + h - 3, WidgetSettings.PanelColor);
        Pyxel.Line(x + 2, y + h - 4, x + w - 3, y + h - 4, WidgetSettings.PanelColor);

        // Draw slider
        Pyxel.Rect(x + 2, y + SliderPos, 3, SliderSize, WidgetSettings.PanelColor);
    }

    private void DrawHorizontal(int x, int y, int w, int h)
    {
        var decCol = DecButton.IsPressed ? (Color)6 : WidgetSettings.BackgroundColor;
        var incCol = IncButton.IsPressed ? (Color)6 : WidgetSettings.BackgroundColor;

        // Draw button backgrounds
        Pyxel.Rect(x + 1, y + 1, 4, h - 2, decCol);
        Pyxel.Rect(x + 6, y + 1, w - 12, h - 2, WidgetSettings.BackgroundColor);
        Pyxel.Rect(x + w - 5, y + 1, 4, h - 2, incCol);

        // Draw left arrow
        Pyxel.Pset(x + 2, y + 3, WidgetSettings.PanelColor);
        Pyxel.Line(x + 3, y + 2, x + 3, y + h - 3, WidgetSettings.PanelColor);

        // Draw right arrow
        Pyxel.Pset(x + w - 3, y + h - 4, WidgetSettings.PanelColor);
        Pyxel.Line(x + w - 4, y + 2, x + w - 4, y + h - 3, WidgetSettings.PanelColor);

        // Draw slider
        Pyxel.Rect(x + SliderPos, y + 2, SliderSize, 3, WidgetSettings.PanelColor);
    }

    private void OnDraw()
    {
        var x = X;
        var y = Y;
        var w = Width;
        var h = Height;
        DrawPanel(x, y, w, h, withShadow: _withShadow);
        if (_isVertical)
        {
            DrawVertical(x, y, w, h);
        }
        else
        {
            DrawHorizontal(x, y, w, h);
        }
    }
}
