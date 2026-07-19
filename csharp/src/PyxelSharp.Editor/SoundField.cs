using PyxelSharp.Editor.Widgets;

namespace PyxelSharp.Editor;

// sound_field.py
public sealed class SoundField : Widget
{
    // Shortcut keys that insert a value into each field row (indexed by cursor_y).
    private static readonly Dictionary<int, Key[]> FieldKeyTables = new()
    {
        [1] = [Key.T, Key.S, Key.P, Key.N],
        [2] = [Key.D0, Key.D1, Key.D2, Key.D3, Key.D4, Key.D5, Key.D6, Key.D7],
        [3] = [Key.N, Key.S, Key.V, Key.F, Key.H, Key.Q],
    };

    private static readonly string[] FieldChars = ["TSPN", "01234567", "NSVFHQ"];

    private readonly SoundEditor _editor;

    public SoundField(SoundEditor parent)
        : base(parent, 30, 149, 193, 23)
    {
        _editor = parent;

        // Set event listeners
        MouseDown += OnMouseDown;
        MouseHover += (_, _) => _editor.HelpMessage = _editor.GetFieldHelpMessage();
        Update += OnUpdate;
        Draw += OnDraw;
    }

    // Helpers

    private (int X, int Y) ScreenToView(int x, int y)
    {
        var vx = Math.Clamp(EditorMath.FloorDiv(x - X - 1, 4), 0,
            EditorSettings.MaxSoundLength - 1);
        var vy = Math.Clamp(EditorMath.FloorDiv(y - Y, 8), 0, 2);
        return (vx, vy);
    }

    // Event handlers

    private void OnMouseDown(Key key, int x, int y)
    {
        if (key != Key.MouseButtonLeft || _editor.IsPlayingVar.Get())
        {
            return;
        }
        var (vx, vy) = ScreenToView(x, y);
        _editor.FieldCursor.MoveTo(vx, vy + 1, Pyxel.Btn(Key.Shift));
    }

    private void OnUpdate()
    {
        var cursorY = _editor.FieldCursor.Y;
        if (cursorY < 1 || _editor.IsPlayingVar.Get() || EditorSettings.IsModifierPressed())
        {
            return;
        }

        if (!FieldKeyTables.TryGetValue(cursorY, out var keyTable))
        {
            return;
        }
        for (var i = 0; i < keyTable.Length; i++)
        {
            if (Pyxel.Btnp(keyTable[i],
                hold: WidgetSettings.HoldTime, repeat: WidgetSettings.RepeatTime))
            {
                _editor.FieldCursor.Insert(i);
                return;
            }
        }
    }

    private void OnDraw()
    {
        // Draw field labels
        Pyxel.Text(X - 13, Y + 1, "TON", EditorSettings.TextLabelColor);
        Pyxel.Text(X - 13, Y + 9, "VOL", EditorSettings.TextLabelColor);
        Pyxel.Text(X - 13, Y + 17, "EFX", EditorSettings.TextLabelColor);
        Pyxel.Blt(X, Y, EditorSettings.EditorImage, 0, 79, 193, 23);

        // Draw field data
        var dataStr = new string[3];
        for (var i = 0; i < 3; i++)
        {
            dataStr[i] = string.Concat(
                _editor.GetField(i + 1)!.ToArray().Select(v => FieldChars[i][v]));
            Pyxel.Text(31, 150 + i * 8, dataStr[i], EditorSettings.SoundFieldDataNormalColor);
        }

        // Draw cursor
        var cursorY = _editor.FieldCursor.Y;
        var cursorX = _editor.FieldCursor.X;
        if (_editor.IsPlayingVar.Get() || cursorY == 0)
        {
            return;
        }

        var x = cursorX * 4 + 31;
        var y = cursorY * 8 + 142;
        var w = _editor.FieldCursor.Width * 4;
        var col = _editor.FieldCursor.IsSelecting
            ? EditorSettings.SoundFieldCursorSelectColor
            : EditorSettings.SoundFieldCursorEditColor;
        Pyxel.Rect(x, y - 1, w, 7, col);
        var rowStr = dataStr[cursorY - 1];
        if (cursorX < rowStr.Length)
        {
            var end = Math.Min(cursorX + _editor.FieldCursor.Width, rowStr.Length);
            Pyxel.Text(x, y, rowStr[cursorX..end], EditorSettings.SoundFieldDataSelectColor);
        }
    }
}
