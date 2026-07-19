using PyxelSharp.Editor.Widgets;

namespace PyxelSharp.Editor;

// music_editor.py
//
// Undo history entry (Python dict): bank snapshots carry all four sequences,
// single edits carry cursor positions + one sequence.
internal sealed class MusicEditHistory
{
    public int MusicIndex;
    public int[][]? OldData;
    public int[][]? NewData;
    public (int X, int Y) OldCursorPos;
    public (int X, int Y) NewCursorPos;
    public int[]? OldField;
    public int[]? NewField;
}

public sealed class MusicEditor : EditorBase
{
    private readonly NumberPicker _musicPicker;
    private readonly ImageButton _playButton;
    private readonly ImageButton _stopButton;
    private readonly ImageToggleButton _loopButton;
    private readonly MusicField[] _musicFields;
    private readonly SoundSelector _soundSelector;

    private MusicEditHistory? _historyData;

    public FieldCursor FieldCursor { get; }
    public WidgetVar<int> MusicIndexVar { get; }
    public WidgetVar<bool> ShouldLoopVar { get; }
    public WidgetVar<bool> IsPlayingVar { get; }

    public MusicEditor(App parent)
        : base(parent)
    {
        IsPlayingVar = new WidgetVar<bool>(false);

        var maxFieldValues = new int[4];
        Array.Fill(maxFieldValues, Pyxel.NumSounds - 1);
        FieldCursor = new FieldCursor(
            maxFieldLength: EditorSettings.MaxMusicLength,
            fieldWrapLength: 16,
            maxFieldValues: maxFieldValues,
            getField: GetField,
            addPreHistory: AddPreHistory,
            addPostHistory: AddPostHistory,
            enableCrossFieldCopy: true);

        _musicPicker = new NumberPicker(this, 45, 17, 0, Pyxel.NumMusics - 1, 0);
        _musicPicker.MouseHover += (_, _) => HelpMessage = "COPY_ALL:CTRL+SHIFT+C/X/V";
        AddNumberPickerHelp(_musicPicker);
        MusicIndexVar = _musicPicker.ValueVar;

        _playButton = new ImageButton(this, 185, 17, EditorSettings.EditorImage, 126, 0);
        _playButton.Press += () => PlayMusic(Pyxel.Btn(Key.Shift));
        _playButton.MouseHover += (_, _) => HelpMessage = "PLAY:SPACE PART-PLAY:SHIFT+SPACE";

        _stopButton = new ImageButton(this, 195, 17, EditorSettings.EditorImage, 135, 0,
            isEnabled: false);
        _stopButton.Press += StopMusic;
        _stopButton.MouseHover += (_, _) => HelpMessage = "STOP:SPACE";

        _loopButton = new ImageToggleButton(this, 205, 17, EditorSettings.EditorImage,
            144, 0, isChecked: false);
        _loopButton.MouseHover += (_, _) => HelpMessage = "LOOP:L";
        ShouldLoopVar = _loopButton.IsCheckedVar;

        _musicFields = [.. Enumerable.Range(0, 4)
            .Select(i => new MusicField(this, 11, 29 + i * 25, i))];

        _soundSelector = new SoundSelector(this);

        // Set event listeners
        UndoPerformed += data => RestoreState((MusicEditHistory)data, old: true);
        RedoPerformed += data => RestoreState((MusicEditHistory)data, old: false);
        Hidden += StopMusic;
        Update += OnUpdate;
        Draw += OnDraw;
    }

    // Public methods

    private Music Music => Pyxel.Musics[MusicIndexVar.Get()];

    public FieldView? GetField(int index)
    {
        if (index >= Pyxel.NumChannels)
        {
            return null;
        }
        return new FieldView(
            () => NormalizedSeqs()[index],
            values =>
            {
                var music = Music;
                var seqs = NormalizedSeqs();
                seqs[index] = values;
                music.Seqs = seqs;
            });
    }

    // Resource load may leave music.seqs shorter than NUM_CHANNELS,
    // so normalize on every access.
    private int[][] NormalizedSeqs()
    {
        var music = Music;
        var seqs = music.Seqs;
        if (seqs.Length != Pyxel.NumChannels)
        {
            var normalized = new int[Pyxel.NumChannels][];
            for (var i = 0; i < normalized.Length; i++)
            {
                normalized[i] = i < seqs.Length ? seqs[i] : [];
            }
            music.Seqs = normalized;
            seqs = normalized;
        }
        return seqs;
    }

    public void AddPreHistory(int? x, int? y, bool bankCopy = false)
    {
        var data = new MusicEditHistory { MusicIndex = MusicIndexVar.Get() };
        _historyData = data;
        if (bankCopy)
        {
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
            data.NewData = [.. Enumerable.Range(0, 4).Select(i => GetField(i)!.ToArray())];
            if (!data.NewData.Zip(data.OldData!).All(pair => pair.First.SequenceEqual(pair.Second)))
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

    // Helpers

    private void PlayMusic(bool isPartial)
    {
        IsPlayingVar.Set(true);
        _musicPicker.IsEnabled = false;
        _playButton.IsEnabled = false;
        _stopButton.IsEnabled = true;
        _loopButton.IsEnabled = false;

        var tick = 0;
        if (isPartial)
        {
            var seqs = NormalizedSeqs();
            for (var i = 0; i < FieldCursor.X; i++)
            {
                var sound = Pyxel.Sounds[seqs[FieldCursor.Y][i]];
                tick += sound.Notes.Length * sound.Speed;
            }
        }
        Pyxel.Playm(MusicIndexVar.Get(), sec: tick / 120f, loop: ShouldLoopVar.Get());
    }

    private void StopMusic()
    {
        IsPlayingVar.Set(false);
        _musicPicker.IsEnabled = true;
        _playButton.IsEnabled = true;
        _stopButton.IsEnabled = false;
        _loopButton.IsEnabled = true;
        Pyxel.Stop();
    }

    private void RestoreState(MusicEditHistory data, bool old)
    {
        StopMusic();
        MusicIndexVar.Set(data.MusicIndex);
        if ((old ? data.OldData : data.NewData) is { } fields)
        {
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
        if (IsPlayingVar.Get())
        {
            IsPlayingVar.Set(Enumerable.Range(0, Pyxel.NumChannels)
                .Any(i => Pyxel.PlayPos(i) is not null));
        }

        if (Pyxel.Btnp(Key.Space))
        {
            if (IsPlayingVar.Get())
            {
                _stopButton.IsPressed = true;
            }
            else
            {
                _playButton.IsPressed = true;
            }
        }

        if (IsPlayingVar.Get())
        {
            return;
        }

        if (!_playButton.IsEnabled)
        {
            StopMusic();
        }

        if (_loopButton.IsEnabled && Pyxel.Btnp(Key.L))
        {
            ShouldLoopVar.Set(!ShouldLoopVar.Get());
        }

        FieldCursor.ProcessInput();
    }

    private void OnDraw()
    {
        DrawPanel(11, 16, 218, 9);
        Pyxel.Text(23, 18, "MUSIC", EditorSettings.TextLabelColor);
    }
}
