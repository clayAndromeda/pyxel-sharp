using PyxelSharp.Editor.Widgets;

namespace PyxelSharp.Editor;

// octave_bar.py
public sealed class OctaveBar : Widget
{
    private readonly SoundEditor _editor;

    public OctaveBar(SoundEditor parent, int x, int y)
        : base(parent, x, y, 4, 123)
    {
        _editor = parent;

        // Set event listeners
        MouseDown += OnMouseDown;
        MouseDrag += (key, x2, y2, _, _) => OnMouseDown(key, x2, y2);
        MouseHover += (_, _) => _editor.HelpMessage = "OCTAVE:PAGEUP/PAGEDOWN";
        Draw += OnDraw;
    }

    // Event handlers

    private void OnMouseDown(Key key, int x, int y)
    {
        if (key != Key.MouseButtonLeft)
        {
            return;
        }

        if (_editor.FieldCursor.Y > 0)
        {
            _editor.FieldCursor.MoveTo(_editor.FieldCursor.X, 0, false);
        }

        _editor.OctaveVar.Set(Math.Clamp(3 - EditorMath.FloorDiv(y - Y - 12, 24), 0, 3));
    }

    private void OnDraw()
    {
        var x = X + 1;
        var y = Y + 1 + (3 - _editor.OctaveVar.Get()) * 24;

        Pyxel.Rect(X, Y, Width, Height, EditorSettings.OctaveBarBackgroundColor);
        Pyxel.Rect(x, y, 2, 47, EditorSettings.OctaveBarColor);
    }
}
