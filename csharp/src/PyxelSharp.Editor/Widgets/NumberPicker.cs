namespace PyxelSharp.Editor.Widgets;

// widgets/number_picker.py
public class NumberPicker : Widget
{
    private readonly int _numberLen;
    private readonly int _minValue;
    private readonly int _maxValue;

    public WidgetVar<int> ValueVar { get; }
    public TextButton DecButton { get; }
    public TextButton IncButton { get; }

    public event Action<int>? Change;

    public NumberPicker(Widget? parent, int x, int y, int minValue, int maxValue, int value,
        bool isVisible = true, bool isEnabled = true)
        : base(parent, x, y,
            Math.Max(minValue.ToString().Length, maxValue.ToString().Length) * 4 + 21, 7,
            isVisible, isEnabled)
    {
        _numberLen = Math.Max(minValue.ToString().Length, maxValue.ToString().Length);
        _minValue = minValue;
        _maxValue = maxValue;

        ValueVar = new WidgetVar<int>(value);
        ValueVar.AddSetFilter(value2 => Math.Clamp(value2, _minValue, _maxValue));
        ValueVar.Changed += OnValueChange;

        DecButton = new TextButton(this, 0, 0, "-");
        DecButton.Press += () => Value -= StepDelta;
        IncButton = new TextButton(this, Width - 7, 0, "+");
        IncButton.Press += () => Value += StepDelta;

        Draw += OnDraw;
    }

    public int Value
    {
        get => ValueVar.Get();
        set => ValueVar.Set(value);
    }

    private static int StepDelta => Pyxel.Btn(Key.Shift) ? 10 : 1;

    private void OnValueChange(int value)
    {
        DecButton.IsEnabled = value > _minValue;
        IncButton.IsEnabled = value < _maxValue;
        Change?.Invoke(value);
    }

    private void OnDraw()
    {
        Pyxel.Rect(X + 9, Y, Width - 18, Height, WidgetSettings.InputFieldColor);
        Pyxel.Text(X + 11, Y + 1, Value.ToString().PadLeft(_numberLen),
            WidgetSettings.InputTextColor);
    }
}
