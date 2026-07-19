using PyxelSharp.Editor.Widgets;

namespace PyxelSharp.Editor;

// piano_keyboard.py
public sealed class PianoKeyboard : Widget
{
    private static readonly Key[] KeyTable =
    [
        Key.Z, Key.S, Key.X, Key.D, Key.C, Key.V, Key.G, Key.B, Key.H, Key.N,
        Key.J, Key.M, Key.Q, Key.D2, Key.W, Key.D3, Key.E, Key.R, Key.D5,
        Key.T, Key.D6, Key.Y, Key.D7, Key.U,
    ];

    // (y_start, y_end, note_offset) within each 24px octave block
    private static readonly (int YStart, int YEnd, int Offset)[] BlackKeyRanges =
        [(2, 4, 10), (6, 8, 8), (10, 12, 6), (16, 18, 3), (20, 22, 1)];
    private static readonly (int YStart, int YEnd, int Offset)[] WhiteKeyRanges =
        [(0, 2, 11), (2, 6, 9), (6, 10, 7), (10, 13, 5), (13, 16, 4), (16, 20, 2), (20, 24, 0)];

    // Classification of white keys for the playback highlight shape
    private static readonly int[] BottomWhiteKeys = [0, 5]; // C, F - below a black key
    private static readonly int[] TopWhiteKeys = [4, 11];   // E, B - above a black key
    private static readonly int[] FullWhiteKeys = [2, 7, 9]; // D, G, A - between two black keys

    private readonly SoundEditor _editor;
    private readonly Sound _previewSound = new();
    private int _previewTone;
    private int? _mouseNote;

    public WidgetVar<int?> NoteVar { get; }

    public PianoKeyboard(SoundEditor parent)
        : base(parent, 17, 25, 12, 123)
    {
        _editor = parent;
        _previewSound.Set("g2", "p", "3", "n", 30);

        NoteVar = new WidgetVar<int?>(null);

        // Set event listeners
        MouseDown += OnMouseDown;
        MouseUp += (_, _, _) => _mouseNote = null;
        MouseDrag += (key, x, y, _, _) => OnMouseDown(key, x, y);
        MouseHover += (_, _) => _editor.HelpMessage = "NOTE:Z/S/X..Q/2/W..A+ENTER TONE:1";
        Update += OnUpdate;
        Draw += OnDraw;
    }

    // Helpers

    private int ScreenToNote(int x, int y)
    {
        x -= X;
        y -= Y;
        var octave = (4 - EditorMath.FloorDiv(y, 24)) * 12;
        y = EditorMath.Mod(y, 24);
        if (octave > 59)
        {
            return 59;
        }
        if (octave < 0)
        {
            return -1;
        }

        // Check black keys first (narrower region, x <= 6)
        if (x <= 6)
        {
            foreach (var (yStart, yEnd, offset) in BlackKeyRanges)
            {
                if (yStart <= y && y <= yEnd)
                {
                    return octave + offset;
                }
            }
        }

        foreach (var (yStart, yEnd, offset) in WhiteKeyRanges)
        {
            if (yStart <= y && y < yEnd)
            {
                return octave + offset;
            }
        }
        return octave;
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
        _mouseNote = ScreenToNote(x, y);
    }

    private void OnUpdate()
    {
        if (_editor.FieldCursor.Y > 0 || _editor.IsPlayingVar.Get()
            || EditorSettings.IsModifierPressed())
        {
            return;
        }

        if (Pyxel.Btnp(Key.D1))
        {
            _previewTone = (_previewTone + 1) % 4;
        }

        var note = _mouseNote;
        for (var i = 0; i < KeyTable.Length; i++)
        {
            if (Pyxel.Btn(KeyTable[i]))
            {
                note = _editor.OctaveVar.Get() * 12 + i;
                break;
            }
        }

        if (Pyxel.Btn(Key.A))
        {
            note = -1;
        }

        NoteVar.Set(note);
        if (NoteVar.Get() is { } noteValue)
        {
            var notes = _previewSound.Notes;
            notes[0] = (sbyte)noteValue;
            _previewSound.Notes = notes;
            var tones = _previewSound.Tones;
            tones[0] = (byte)_previewTone;
            _previewSound.Tones = tones;
            Pyxel.Play(1, _previewSound);
        }
        else
        {
            Pyxel.Stop(1);
        }
    }

    private void OnDraw()
    {
        Pyxel.Blt(X, Y, EditorSettings.EditorImage, 208, 0, 12, 123);

        var playPos = Pyxel.PlayPos(0);
        var notes = _editor.GetField(0)!.ToArray();

        int note;
        if (playPos is { } pos && notes.Length > 0)
        {
            var index = Math.Min(
                (int)Math.Round(pos.Sec * 120 / _editor.SpeedVar.Get()), notes.Length - 1);
            note = notes[index];
        }
        else if (playPos is null && NoteVar.Get() is { } noteValue)
        {
            note = noteValue;
        }
        else
        {
            return;
        }

        var key = EditorMath.Mod(note, 12);
        var x = X;
        var y = Y + (59 - note) * 2;

        if (note == -1)
        {
            Pyxel.Rect(x, y + 1, 12, 2, EditorSettings.PianoKeyboardRestColor);
        }
        else if (BottomWhiteKeys.Contains(key))
        {
            Pyxel.Rect(x, y + 1, 7, 1, EditorSettings.PianoKeyboardPlayColor);
            Pyxel.Rect(x + 7, y, 5, 2, EditorSettings.PianoKeyboardPlayColor);
        }
        else if (TopWhiteKeys.Contains(key))
        {
            Pyxel.Rect(x, y + 1, 7, 1, EditorSettings.PianoKeyboardPlayColor);
            Pyxel.Rect(x + 7, y + 1, 5, 2, EditorSettings.PianoKeyboardPlayColor);
        }
        else if (FullWhiteKeys.Contains(key))
        {
            Pyxel.Rect(x, y + 1, 7, 1, EditorSettings.PianoKeyboardPlayColor);
            Pyxel.Rect(x + 7, y, 5, 3, EditorSettings.PianoKeyboardPlayColor);
        }
        else
        {
            Pyxel.Rect(x, y + 1, 6, 1, EditorSettings.PianoKeyboardPlayColor);
        }
    }
}
