namespace PyxelSharp.Editor.Widgets;

// widgets/toggle_button.py
public class ToggleButton : Widget
{
    public WidgetVar<bool> IsCheckedVar { get; }

    public event Action? Checked;
    public event Action? Unchecked;

    public ToggleButton(Widget? parent, int x, int y, int width, int height, bool isChecked,
        bool isVisible = true, bool isEnabled = true)
        : base(parent, x, y, width, height, isVisible, isEnabled)
    {
        IsCheckedVar = new WidgetVar<bool>(isChecked);
        IsCheckedVar.Changed += OnIsCheckedChange;

        MouseDown += OnMouseDown;
    }

    public bool IsChecked
    {
        get => IsCheckedVar.Get();
        set => IsCheckedVar.Set(value);
    }

    public Color ButtonColor =>
        !IsEnabled ? WidgetSettings.ButtonDisabledColor
        : IsChecked ? WidgetSettings.ButtonPressedColor
        : WidgetSettings.ButtonEnabledColor;

    private void OnIsCheckedChange(bool value)
    {
        if (value)
        {
            Checked?.Invoke();
        }
        else
        {
            Unchecked?.Invoke();
        }
    }

    private void OnMouseDown(Key key, int x, int y)
    {
        if (key == Key.MouseButtonLeft)
        {
            IsChecked = !IsChecked;
        }
    }
}
