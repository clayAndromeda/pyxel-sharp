namespace PyxelSharp.Editor.Widgets;

// widgets/button.py
public class Button : Widget
{
    private int _pressingTime;

    // is_pressed_var: the stored value is never used — get computes from
    // _pressingTime and set only performs side effects (Python parity).
    public WidgetVar<bool> IsPressedVar { get; }

    public event Action? Press;

    public Button(Widget? parent, int x, int y, int width, int height,
        bool isVisible = true, bool isEnabled = true)
        : base(parent, x, y, width, height, isVisible, isEnabled)
    {
        IsPressedVar = new WidgetVar<bool>(false);
        IsPressedVar.AddGetFilter(_ => _pressingTime > 0);
        IsPressedVar.AddSetFilter(value =>
        {
            if (value)
            {
                _pressingTime = WidgetSettings.ButtonPressingTime + 1;
                Press?.Invoke();
            }
            else
            {
                _pressingTime = 0;
            }
            return false;
        });

        MouseDown += OnMouseDown;
        MouseRepeat += OnMouseDown;
        MouseUp += OnMouseUp;
        Update += OnUpdate;
    }

    public bool IsPressed
    {
        get => IsPressedVar.Get();
        set => IsPressedVar.Set(value);
    }

    public Color ButtonColor =>
        !IsEnabled ? WidgetSettings.ButtonDisabledColor
        : IsPressed ? WidgetSettings.ButtonPressedColor
        : WidgetSettings.ButtonEnabledColor;

    private void OnMouseDown(Key key, int x, int y)
    {
        if (key == Key.MouseButtonLeft)
        {
            IsPressed = true;
        }
    }

    private void OnMouseUp(Key key, int x, int y)
    {
        if (key == Key.MouseButtonLeft)
        {
            IsPressed = false;
        }
    }

    private void OnUpdate()
    {
        if (_pressingTime > 0)
        {
            _pressingTime--;
        }
    }
}
