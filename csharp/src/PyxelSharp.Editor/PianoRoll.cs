using PyxelSharp.Editor.Widgets;

namespace PyxelSharp.Editor;

// piano_roll.py
public sealed class PianoRoll : Widget
{
    private readonly SoundEditor _editor;
    private int _pressX;
    private int _pressY;

    public PianoRoll(SoundEditor parent)
        : base(parent, 30, 25, 193, 123)
    {
        _editor = parent;

        // Set event listeners
        MouseDown += OnMouseDown;
        MouseDrag += OnMouseDrag;
        MouseClick += OnMouseClick;
        MouseHover += (_, _) => _editor.HelpMessage = _editor.GetFieldHelpMessage();
        Update += OnUpdate;
        Draw += OnDraw;
    }

    // Helpers

    private (int X, int Y) ScreenToView(int x, int y)
    {
        var vx = Math.Clamp(EditorMath.FloorDiv(x - X - 1, 4), 0,
            EditorSettings.MaxSoundLength - 1);
        var vy = Math.Clamp(59 - EditorMath.FloorDiv(y - Y - 1, 2), -1, 59);
        return (vx, vy);
    }

    private void SetNote(int x, int y)
    {
        _editor.AddPreHistory(x, 0);
        _editor.FieldCursor.MoveTo(x, 0, Pyxel.Btn(Key.Shift));

        var field = _editor.FieldCursor.Field;
        var fieldLen = field.Count;
        if (x < fieldLen)
        {
            field[x] = y;
        }
        else
        {
            var extension = new int[x - fieldLen + 1];
            Array.Fill(extension, -1);
            extension[^1] = y;
            field.InsertRange(fieldLen, extension);
        }

        _editor.AddPostHistory(x, 0);
    }

    // Event handlers

    private void OnMouseDown(Key key, int x, int y)
    {
        if (key != Key.MouseButtonLeft || _editor.IsPlayingVar.Get())
        {
            return;
        }
        (x, y) = ScreenToView(x, y);
        _pressX = x;
        _pressY = y;
        _editor.FieldCursor.MoveTo(x, 0, Pyxel.Btn(Key.Shift));
    }

    private void OnMouseDrag(Key key, int x, int y, int dx, int dy)
    {
        if (key != Key.MouseButtonLeft || _editor.IsPlayingVar.Get())
        {
            return;
        }

        (x, y) = ScreenToView(x, y);
        if (x == _pressX)
        {
            if (y != _pressY)
            {
                SetNote(x, y);
                _pressX = x;
                _pressY = y;
            }
            return;
        }

        // Interpolate notes between press and current positions
        var deltaX = x - _pressX;
        var deltaY = y - _pressY;
        var step = deltaX > 0 ? 1 : -1;
        var alpha = (double)deltaY / deltaX;
        for (var i = 0; i != deltaX + step; i += step)
        {
            SetNote(_pressX + i, (int)Math.Round(_pressY + alpha * i));
        }

        _pressX = x;
        _pressY = y;
    }

    private void OnMouseClick(Key key, int x, int y)
    {
        if (key != Key.MouseButtonLeft || _editor.IsPlayingVar.Get())
        {
            return;
        }
        (x, y) = ScreenToView(x, y);
        SetNote(x, y);
    }

    private void OnUpdate()
    {
        if (_editor.FieldCursor.Y > 0 || _editor.IsPlayingVar.Get())
        {
            return;
        }
        var enterPressed =
            Pyxel.Btnp(Key.Return, hold: WidgetSettings.HoldTime, repeat: WidgetSettings.RepeatTime)
            || Pyxel.Btnp(Key.KpEnter, hold: WidgetSettings.HoldTime, repeat: WidgetSettings.RepeatTime);
        if (enterPressed && _editor.NoteVar.Get() is { } note)
        {
            _editor.FieldCursor.Insert(note);
        }
    }

    private void OnDraw()
    {
        // Draw frame
        Pyxel.Rect(X, Y, Width, Height, EditorSettings.PianoRollBackgroundColor);

        // Draw cursor or playback position
        var playPos = Pyxel.PlayPos(0);
        if (playPos is { } pos)
        {
            var x = (int)Math.Round(pos.Sec * 120 / _editor.SpeedVar.Get()) * 4 + 31;
            Pyxel.Rect(x, Y, 3, Height, EditorSettings.PianoRollCursorPlayColor);
        }
        else if (_editor.FieldCursor.Y == 0)
        {
            var x = _editor.FieldCursor.X * 4 + 31;
            var w = _editor.FieldCursor.Width * 4 - 1;
            var col = _editor.FieldCursor.IsSelecting
                ? EditorSettings.PianoRollCursorSelectColor
                : EditorSettings.PianoRollCursorEditColor;
            Pyxel.Rect(x, Y, w, Height, col);
        }

        // Draw piano roll grid
        Pyxel.Blt(X, Y, EditorSettings.EditorImage, 0, 7, 193, 72,
            EditorSettings.PianoRollBackgroundColor);
        Pyxel.Blt(X, Y + 72, EditorSettings.EditorImage, 0, 7, 193, 51,
            EditorSettings.PianoRollBackgroundColor);

        // Draw notes
        var notes = _editor.GetField(0)!.ToArray();
        for (var i = 0; i < notes.Length; i++)
        {
            var note = notes[i];
            Pyxel.Rect(i * 4 + 31, 143 - note * 2, 3, 3,
                note >= 0 ? EditorSettings.PianoRollNoteColor : EditorSettings.PianoRollRestColor);
        }
    }
}
