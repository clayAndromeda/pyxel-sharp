namespace PyxelSharp.Editor.Widgets;

// widgets/text_button.py
public class TextButton : Button
{
    private readonly string _text;

    public TextButton(Widget? parent, int x, int y, string text,
        bool isVisible = true, bool isEnabled = true)
        : base(parent, x, y, text.Length * Pyxel.FontWidth + 3, Pyxel.FontHeight + 1,
            isVisible, isEnabled)
    {
        _text = text;

        Draw += OnDraw;
    }

    private void OnDraw()
    {
        var x = X;
        var y = Y;
        var w = Width;
        var h = Height;
        var col = ButtonColor;

        Pyxel.Line(x + 1, y, x + w - 2, y, col);
        Pyxel.Rect(x, y + 1, w, h - 2, col);
        Pyxel.Line(x + 1, y + h - 1, x + w - 2, y + h - 1, col);
        Pyxel.Text(x + 2, y + 1, _text, WidgetSettings.ButtonTextColor);
    }
}
