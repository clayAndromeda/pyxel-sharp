namespace PyxelSharp.Editor.Widgets;

// widgets/widget.py
//
// Widget tree with mouse capture. Python's string-keyed events
// (add_event_listener("mouse_down", ...)) become C# events.
public class Widget
{
    private sealed class MouseCaptureInfo
    {
        public Widget? Widget;
        public Key Key;
        public int Time;
        public (int X, int Y) PressPos;
        public (int X, int Y) LastPos;
    }

    private static readonly MouseCaptureInfo Capture = new();

    private static readonly Key[] CaptureButtons =
        [Key.MouseButtonLeft, Key.MouseButtonRight, Key.MouseButtonMiddle];

    private readonly List<Widget> _children = [];
    private int _x;
    private int _y;
    private int _width;
    private int _height;

    public Widget? Parent { get; }
    public IReadOnlyList<Widget> Children => _children;

    public WidgetVar<bool> IsVisibleVar { get; }
    public WidgetVar<bool> IsEnabledVar { get; }

    public event Action? Shown;
    public event Action? Hidden;
    public event Action? Enabled;
    public event Action? Disabled;
    public event Action<Key, int, int>? MouseDown;
    public event Action<Key, int, int>? MouseUp;
    public event Action<Key, int, int, int, int>? MouseDrag;
    public event Action<Key, int, int>? MouseRepeat;
    public event Action<Key, int, int>? MouseClick;
    public event Action<int, int>? MouseHover;
    public event Action? Update;
    public event Action? Draw;

    public Widget(Widget? parent, int x, int y, int width, int height,
        bool isVisible = true, bool isEnabled = true)
    {
        parent?._children.Add(this);
        Parent = parent;
        _x = x;
        _y = y;
        _width = width;
        _height = height;

        IsVisibleVar = new WidgetVar<bool>(isVisible);
        IsVisibleVar.AddGetFilter(value => Parent is null ? value : Parent.IsVisible && value);
        IsVisibleVar.Changed += TriggerVisibleEvent;

        IsEnabledVar = new WidgetVar<bool>(isEnabled);
        IsEnabledVar.AddGetFilter(value => Parent is null ? value : Parent.IsEnabled && value);
        IsEnabledVar.Changed += TriggerEnabledEvent;
    }

    public bool IsVisible
    {
        get => IsVisibleVar.Get();
        set => IsVisibleVar.Set(value);
    }

    public bool IsEnabled
    {
        get => IsEnabledVar.Get();
        set => IsEnabledVar.Set(value);
    }

    // Geometry

    public int X => Parent is null ? _x : Parent.X + _x;
    public int Y => Parent is null ? _y : Parent.Y + _y;
    public int Width => _width;
    public int Height => _height;

    public bool IsHit(int x, int y)
    {
        x -= X;
        y -= Y;
        return x >= 0 && x < Width && y >= 0 && y < Height;
    }

    public void SetPos(int x, int y)
    {
        _x = x;
        _y = y;
    }

    public void SetSize(int width, int height)
    {
        _width = width;
        _height = height;
    }

    // Update pipeline

    public void UpdateAll()
    {
        if (Capture.Widget is { } captureWidget)
        {
            captureWidget.ProcessCapture();
        }
        else
        {
            ProcessInput();
        }

        UpdateWidget();
    }

    private bool ProcessInput()
    {
        if (!IsVisible || !IsEnabled)
        {
            return false;
        }

        for (var i = _children.Count - 1; i >= 0; i--)
        {
            if (_children[i].ProcessInput())
            {
                return true;
            }
        }

        var x = Pyxel.MouseX;
        var y = Pyxel.MouseY;
        if (IsHit(x, y))
        {
            foreach (var button in CaptureButtons)
            {
                if (Pyxel.Btnp(button))
                {
                    StartCapture(button);
                    MouseDown?.Invoke(button, x, y);
                    break;
                }
            }

            MouseHover?.Invoke(x, y);
            return true;
        }

        return false;
    }

    private void StartCapture(Key key)
    {
        Capture.Widget = this;
        Capture.Key = key;
        Capture.Time = Pyxel.FrameCount;
        Capture.PressPos = (Pyxel.MouseX, Pyxel.MouseY);
        Capture.LastPos = Capture.PressPos;
    }

    private static void EndCapture() => Capture.Widget = null;

    private void ProcessCapture()
    {
        // Resolve drag, hover, repeat, and release before clearing capture state.
        var (lastX, lastY) = Capture.LastPos;
        var x = Pyxel.MouseX;
        var y = Pyxel.MouseY;
        if (x != lastX || y != lastY)
        {
            MouseDrag?.Invoke(Capture.Key, x, y, x - lastX, y - lastY);
            Capture.LastPos = (x, y);
        }

        if (IsHit(x, y))
        {
            MouseHover?.Invoke(x, y);
        }

        if (Pyxel.Btnp(Capture.Key, hold: WidgetSettings.HoldTime, repeat: WidgetSettings.RepeatTime))
        {
            MouseRepeat?.Invoke(Capture.Key, x, y);
        }

        if (Pyxel.Btnr(Capture.Key))
        {
            MouseUp?.Invoke(Capture.Key, x, y);
            var (pressX, pressY) = Capture.PressPos;
            if (Pyxel.FrameCount <= Capture.Time + WidgetSettings.ClickTime
                && Math.Abs(x - pressX) <= WidgetSettings.ClickDist
                && Math.Abs(y - pressY) <= WidgetSettings.ClickDist)
            {
                MouseClick?.Invoke(Capture.Key, x, y);
            }

            EndCapture();
        }
    }

    private void UpdateWidget()
    {
        if (!IsVisible)
        {
            return;
        }

        Update?.Invoke();
        foreach (var child in _children)
        {
            child.UpdateWidget();
        }
    }

    // Drawing

    public void DrawAll()
    {
        if (!IsVisible)
        {
            return;
        }

        Draw?.Invoke();
        foreach (var child in _children)
        {
            child.DrawAll();
        }
    }

    public static void DrawPanel(int x, int y, int width, int height, bool withShadow = true)
    {
        var w = width;
        var h = height;
        Pyxel.Line(x + 1, y, x + w - 2, y, WidgetSettings.PanelColor);
        Pyxel.Rect(x, y + 1, w, h - 2, WidgetSettings.PanelColor);
        Pyxel.Line(x + 1, y + h - 1, x + w - 2, y + h - 1, WidgetSettings.PanelColor);
        if (withShadow)
        {
            Pyxel.Line(x + 2, y + h, x + w - 1, y + h, WidgetSettings.ShadowColor);
            Pyxel.Line(x + w, y + 2, x + w, y + h - 1, WidgetSettings.ShadowColor);
            Pyxel.Pset(x + w - 1, y + h - 1, WidgetSettings.ShadowColor);
        }
    }

    // Visibility / enablement callbacks

    private void TriggerVisibleEvent(bool isVisible)
    {
        if (isVisible)
        {
            Shown?.Invoke();
        }
        else
        {
            Hidden?.Invoke();
        }

        foreach (var child in _children)
        {
            if (child.IsVisible == isVisible)
            {
                child.TriggerVisibleEvent(isVisible);
            }
        }
    }

    private void TriggerEnabledEvent(bool isEnabled)
    {
        if (isEnabled)
        {
            Enabled?.Invoke();
        }
        else
        {
            Disabled?.Invoke();
        }

        foreach (var child in _children)
        {
            if (child.IsEnabled == isEnabled)
            {
                child.TriggerEnabledEvent(isEnabled);
            }
        }
    }
}
