using PyxelSharp.Editor.Widgets;

namespace PyxelSharp.Editor;

// music_field.py
public sealed class MusicField : Widget
{
    private readonly MusicEditor _editor;
    private readonly int _ch;

    public MusicField(MusicEditor parent, int x, int y, int ch)
        : base(parent, x, y, 218, 21)
    {
        _editor = parent;
        _ch = ch;

        // Set event listeners
        MouseDown += OnMouseDown;
        MouseHover += OnMouseHover;
        Draw += OnDraw;
    }

    // Helpers

    private FieldView Data => _editor.GetField(_ch)!;

    // Event handlers

    private void OnMouseDown(Key key, int x, int y)
    {
        if (key != Key.MouseButtonLeft || _editor.IsPlayingVar.Get())
        {
            return;
        }
        x -= X + 21;
        y -= Y + 2;
        if (x < 0 || y < 0 || x > 188 || y > 16 || x % 12 > 8 || y % 10 > 6)
        {
            return;
        }
        _editor.FieldCursor.MoveTo(x / 12 + y / 10 * 16, _ch, Pyxel.Btn(Key.Shift));
    }

    private void OnMouseHover(int x, int y)
    {
        _editor.HelpMessage = _editor.FieldCursor.IsSelecting
            ? "COPY:CTRL+A/C/X/V SHIFT:CTRL+U/D"
            : "SOUND:SOUND_BUTTON/BS/DEL";
    }

    private void OnDraw()
    {
        // Draw frame
        DrawPanel(X, Y, Width, Height);
        Pyxel.Text(X + 5, Y + 8, $"CH{_ch}", EditorSettings.TextLabelColor);
        Pyxel.Blt(X + 20, Y + 1, EditorSettings.EditorImage, 0, 102, 191, 19,
            EditorSettings.MusicFieldBackgroundColor);

        // Determine cursor state
        int cursorX;
        int cursorY;
        var cursorWidth = 1;
        var cursorCol = EditorSettings.MusicFieldCursorPlayColor;
        if (_editor.IsPlayingVar.Get())
        {
            if (Pyxel.PlayPos(_ch) is { } playPos)
            {
                cursorX = playPos.SoundIndex;
                cursorY = _ch;
            }
            else
            {
                cursorX = -1;
                cursorY = -1;
            }
        }
        else
        {
            cursorX = _editor.FieldCursor.X;
            cursorY = _editor.FieldCursor.Y;
            cursorWidth = _editor.FieldCursor.Width;
            cursorCol = _editor.FieldCursor.IsSelecting
                ? EditorSettings.MusicFieldCursorSelectColor
                : EditorSettings.MusicFieldCursorEditColor;
        }

        // Draw cursor highlight
        var data = Data.ToArray();
        if (cursorY == _ch)
        {
            for (var i = 0; i <= data.Length; i++)
            {
                if (cursorX <= i && i < cursorX + cursorWidth)
                {
                    var x = X + i % 16 * 12 + 21;
                    var y = Y + i / 16 * 10 + 2;
                    Pyxel.Rect(x, y, 9, 7, cursorCol);
                }
            }
        }

        // Draw sound indices
        for (var i = 0; i < data.Length; i++)
        {
            var x = X + 22 + i % 16 * 12;
            var y = Y + i / 16 * 10 + 3;
            var isSelected = cursorY == _ch && cursorX <= i && i < cursorX + cursorWidth;
            var col = isSelected
                ? EditorSettings.MusicFieldSoundSelectColor
                : EditorSettings.MusicFieldSoundNormalColor;
            Pyxel.Text(x, y, data[i].ToString().PadLeft(2, '0'), col);
        }
    }
}
