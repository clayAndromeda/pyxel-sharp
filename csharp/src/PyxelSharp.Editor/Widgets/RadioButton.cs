namespace PyxelSharp.Editor.Widgets;

// widgets/radio_button.py
public class RadioButton : Widget
{
    private readonly Image _img;
    private readonly int _u;
    private readonly int _v;
    private readonly int _numButtons;

    public WidgetVar<int> ValueVar { get; }

    public event Action<int>? Change;

    public RadioButton(Widget? parent, int x, int y, Image img, int u, int v,
        int numButtons, int value, bool isVisible = true, bool isEnabled = true)
        : base(parent, x, y, numButtons * 9 - 2, 7, isVisible, isEnabled)
    {
        _img = img;
        _u = u;
        _v = v;
        _numButtons = numButtons;

        ValueVar = new WidgetVar<int>(value);
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
        x -= X;
        y -= Y;
        var index = Math.Clamp(EditorMath.FloorDiv(x, 9), 0, _numButtons - 1);
        var x1 = index * 9;
        return x1 <= x && x < x1 + 7 && y >= 0 && y < 7 ? index : null;
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

    private void OnDraw()
    {
        Pyxel.Blt(X, Y, _img, _u, _v, Width, Height);

        Pyxel.Pal(WidgetSettings.ButtonEnabledColor, WidgetSettings.ButtonPressedColor);
        Pyxel.Blt(X + Value * 9, Y, _img, _u + Value * 9, _v, 7, 7);
        Pyxel.Pal();
    }
}
