namespace PyxelSharp.Editor.Widgets;

// widgets/image_button.py
public class ImageButton : Button
{
    private readonly Image _img;
    private readonly int _u;
    private readonly int _v;

    public ImageButton(Widget? parent, int x, int y, Image img, int u, int v,
        bool isVisible = true, bool isEnabled = true)
        : base(parent, x, y, 7, 7, isVisible, isEnabled)
    {
        _img = img;
        _u = u;
        _v = v;

        Draw += OnDraw;
    }

    private void OnDraw()
    {
        Pyxel.Pal(WidgetSettings.ButtonEnabledColor, ButtonColor);
        Pyxel.Blt(X, Y, _img, _u, _v, Width, Height, colorKey: 0);
        Pyxel.Pal();
    }
}
