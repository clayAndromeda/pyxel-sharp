using PyxelSharp.Editor.Widgets;

namespace PyxelSharp.Editor;

// sound_editor.py
//
// Undo history entry (Python dict): bank snapshots carry speed + all four
// fields, single edits carry cursor positions + one field.
internal sealed class SoundEditHistory
{
    public int SoundIndex;
    public int OldSpeed;
    public int NewSpeed;
    public int[][]? OldData;
    public int[][]? NewData;
    public (int X, int Y) OldCursorPos;
    public (int X, int Y) NewCursorPos;
    public int[]? OldField;
    public int[]? NewField;
}

public sealed class SoundEditor : EditorBase
{
    private static readonly string[] FieldHelp =
    [
        "NOTE:CLICK/PIANO_KEY+ENTER/BS/DEL",
        "TONE:T/S/P/N/BS/DEL",
        "VOLUME:0-7/BS/DEL",
        "EFFECT:N/S/V/F/H/Q/BS/DEL",
    ];

    private readonly NumberPicker _soundPicker;
    private readonly NumberPicker _speedPicker;
    private readonly ImageButton _playButton;
    private readonly ImageButton _stopButton;
    private readonly ImageToggleButton _loopButton;
    private readonly PianoKeyboard _pianoKeyboard;
    private readonly PianoRoll _pianoRoll;
    private readonly SoundField _soundField;
    private readonly OctaveBar _leftOctaveBar;
    private readonly OctaveBar _rightOctaveBar;

    private SoundEditHistory? _historyData;

    public FieldCursor FieldCursor { get; }
    public WidgetVar<int> SoundIndexVar { get; }
    public WidgetVar<int> SpeedVar { get; }
    public WidgetVar<int> OctaveVar { get; }
    public WidgetVar<int?> NoteVar { get; }
    public WidgetVar<bool> ShouldLoopVar { get; }
    public WidgetVar<bool> IsPlayingVar { get; }

    public SoundEditor(App parent)
        : base(parent)
    {
        FieldCursor = new FieldCursor(
            maxFieldLength: EditorSettings.MaxSoundLength,
            fieldWrapLength: EditorSettings.MaxSoundLength,
            maxFieldValues: [59, 3, 7, 5],
            getField: GetField,
            addPreHistory: AddPreHistory,
            addPostHistory: AddPostHistory,
            enableCrossFieldCopy: false);

        OctaveVar = new WidgetVar<int>(2);

        IsPlayingVar = new WidgetVar<bool>(false);
        IsPlayingVar.AddGetFilter(_ => Pyxel.PlayPos(0) is not null);

        _soundPicker = new NumberPicker(this, 45, 17, 0, Pyxel.NumSounds - 1, 0);
        _soundPicker.Change += value => _speedPicker!.ValueVar.Set(Pyxel.Sounds[value].Speed);
        _soundPicker.MouseHover += (_, _) => HelpMessage = "COPY_ALL:CTRL+SHIFT+C/X/V";
        AddNumberPickerHelp(_soundPicker);
        SoundIndexVar = _soundPicker.ValueVar;

        _speedPicker = new NumberPicker(this, 105, 17, 1, 99, Pyxel.Sounds[0].Speed);
        _speedPicker.Change += value => Pyxel.Sounds[SoundIndexVar.Get()].Speed = value;
        AddNumberPickerHelp(_speedPicker);
        SpeedVar = _speedPicker.ValueVar;
        FieldCursor.SetSpeedVar(SpeedVar);

        _playButton = new ImageButton(this, 185, 17, EditorSettings.EditorImage, 126, 0);
        _playButton.Press += () => PlaySound(Pyxel.Btn(Key.Shift));
        _playButton.MouseHover += (_, _) => HelpMessage = "PLAY:SPACE PART-PLAY:SHIFT+SPACE";

        _stopButton = new ImageButton(this, 195, 17, EditorSettings.EditorImage, 135, 0,
            isEnabled: false);
        _stopButton.Press += StopSound;
        _stopButton.MouseHover += (_, _) => HelpMessage = "STOP:SPACE";

        _loopButton = new ImageToggleButton(this, 205, 17, EditorSettings.EditorImage,
            144, 0, isChecked: false);
        _loopButton.MouseHover += (_, _) => HelpMessage = "LOOP:L";
        ShouldLoopVar = _loopButton.IsCheckedVar;

        _pianoKeyboard = new PianoKeyboard(this);
        NoteVar = _pianoKeyboard.NoteVar;

        _pianoRoll = new PianoRoll(this);

        _soundField = new SoundField(this);

        _leftOctaveBar = new OctaveBar(this, 12, 25);
        _rightOctaveBar = new OctaveBar(this, 224, 25);

        // Set event listeners
        UndoPerformed += data => RestoreState((SoundEditHistory)data, old: true);
        RedoPerformed += data => RestoreState((SoundEditHistory)data, old: false);
        Hidden += StopSound;
        Update += OnUpdate;
        Draw += OnDraw;
    }

    // Public methods

    private Sound Sound => Pyxel.Sounds[SoundIndexVar.Get()];

    public FieldView? GetField(int index) => index switch
    {
        0 => new FieldView(
            () => Array.ConvertAll(Sound.Notes, v => (int)v),
            values => Sound.Notes = Array.ConvertAll(values, v => (sbyte)v)),
        1 => new FieldView(
            () => Array.ConvertAll(Sound.Tones, v => (int)v),
            values => Sound.Tones = Array.ConvertAll(values, v => (byte)v)),
        2 => new FieldView(
            () => Array.ConvertAll(Sound.Volumes, v => (int)v),
            values => Sound.Volumes = Array.ConvertAll(values, v => (byte)v)),
        3 => new FieldView(
            () => Array.ConvertAll(Sound.Effects, v => (int)v),
            values => Sound.Effects = Array.ConvertAll(values, v => (byte)v)),
        _ => null,
    };

    public void AddPreHistory(int? x, int? y, bool bankCopy = false)
    {
        var data = new SoundEditHistory { SoundIndex = SoundIndexVar.Get() };
        _historyData = data;
        if (bankCopy)
        {
            data.OldSpeed = SpeedVar.Get();
            data.OldData = [.. Enumerable.Range(0, 4).Select(i => GetField(i)!.ToArray())];
        }
        else
        {
            data.OldCursorPos = (x!.Value, y!.Value);
            data.OldField = FieldCursor.Field.ToArray();
        }
    }

    public void AddPostHistory(int? x, int? y, bool bankCopy = false)
    {
        var data = _historyData!;
        if (bankCopy)
        {
            data.NewSpeed = SpeedVar.Get();
            data.NewData = [.. Enumerable.Range(0, 4).Select(i => GetField(i)!.ToArray())];
            if (data.NewSpeed != data.OldSpeed || !FieldsEqual(data.NewData, data.OldData!))
            {
                AddHistory(data);
            }
        }
        else
        {
            data.NewCursorPos = (x!.Value, y!.Value);
            data.NewField = FieldCursor.Field.ToArray();
            if (!data.NewField.SequenceEqual(data.OldField!))
            {
                AddHistory(data);
            }
        }
    }

    public string GetFieldHelpMessage()
    {
        if (FieldCursor.IsSelecting)
        {
            return "COPY:CTRL+A/C/X/V SHIFT:CTRL+U/D";
        }
        var cursorY = FieldCursor.Y;
        return cursorY < FieldHelp.Length ? FieldHelp[cursorY] : "";
    }

    // Helpers

    private static bool FieldsEqual(int[][] a, int[][] b) =>
        a.Length == b.Length && a.Zip(b).All(pair => pair.First.SequenceEqual(pair.Second));

    private void PlaySound(bool isPartial)
    {
        _soundPicker.IsEnabled = false;
        _speedPicker.IsEnabled = false;
        _playButton.IsEnabled = false;
        _stopButton.IsEnabled = true;
        _loopButton.IsEnabled = false;

        var tick = isPartial ? FieldCursor.X * SpeedVar.Get() : 0;
        Pyxel.Play(0, SoundIndexVar.Get(), sec: tick / 120f, loop: ShouldLoopVar.Get());
    }

    private void StopSound()
    {
        _soundPicker.IsEnabled = true;
        _speedPicker.IsEnabled = true;
        _playButton.IsEnabled = true;
        _stopButton.IsEnabled = false;
        _loopButton.IsEnabled = true;
        Pyxel.Stop(0);
    }

    private void RestoreState(SoundEditHistory data, bool old)
    {
        StopSound();
        SoundIndexVar.Set(data.SoundIndex);
        if ((old ? data.OldData : data.NewData) is { } fields)
        {
            Sound.Speed = old ? data.OldSpeed : data.NewSpeed;
            for (var i = 0; i < 4; i++)
            {
                GetField(i)!.ReplaceAll(fields[i]);
            }
        }
        else
        {
            var (cursorX, cursorY) = old ? data.OldCursorPos : data.NewCursorPos;
            FieldCursor.MoveTo(cursorX, cursorY, false);
            FieldCursor.Field.ReplaceAll(old ? data.OldField! : data.NewField!);
        }
    }

    // Event handlers

    private void OnUpdate()
    {
        var sound = Sound;
        if (SpeedVar.Get() != sound.Speed)
        {
            SpeedVar.Set(sound.Speed);
        }

        if (Pyxel.Btnp(Key.Space))
        {
            if (IsPlayingVar.Get())
            {
                _stopButton.IsPressed = true;
                return;
            }
            _playButton.IsPressed = true;
        }

        if (!_playButton.IsEnabled && !IsPlayingVar.Get())
        {
            StopSound();
        }

        if (_loopButton.IsEnabled && Pyxel.Btnp(Key.L))
        {
            ShouldLoopVar.Set(!ShouldLoopVar.Get());
        }

        if (Pyxel.Btnp(Key.Pageup))
        {
            OctaveVar.Set(Math.Min(OctaveVar.Get() + 1, 3));
        }
        if (Pyxel.Btnp(Key.Pagedown))
        {
            OctaveVar.Set(Math.Max(OctaveVar.Get() - 1, 0));
        }

        if (!IsPlayingVar.Get())
        {
            FieldCursor.ProcessInput();
        }
    }

    private void OnDraw()
    {
        DrawPanel(11, 16, 218, 157);
        Pyxel.Text(23, 18, "SOUND", EditorSettings.TextLabelColor);
        Pyxel.Text(83, 18, "SPEED", EditorSettings.TextLabelColor);
    }
}
