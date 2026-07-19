namespace PyxelSharp.Editor;

// settings.py
public static class EditorSettings
{
    // Editor image (assets/editor_220x160.png). Python loads it at module
    // import time against the default 16-color palette; here it must be
    // loaded after Pyxel.Init but BEFORE the palette is extended with user
    // colors, or the closest-color mapping would pick user palette entries.
    private static Image? _editorImage;

    public static Image EditorImage =>
        _editorImage ??= Image.FromImage(
            Path.Combine(AppContext.BaseDirectory, "assets", "editor_220x160.png"));

    // App dimensions
    public const int AppWidth = 240;
    public const int AppHeight = 180;

    // Tool constants
    public const int ToolSelect = 0;
    public const int ToolPencil = 1;
    public const int ToolRectb = 2;
    public const int ToolRect = 3;
    public const int ToolCircb = 4;
    public const int ToolCirc = 5;
    public const int ToolBucket = 6;

    // Audio field lengths
    public const int MaxSoundLength = 48;
    public const int MaxMusicLength = 32;

    // Resource file extension (Python: pyxel.RESOURCE_FILE_EXTENSION)
    public const string ResourceFileExtension = ".pyxres";

    // Text colors
    public static readonly Color TextLabelColor = 7;
    public static readonly Color HelpMessageColor = 5;

    // Panel focus/select colors
    public static readonly Color PanelFocusColor = 7;
    public static readonly Color PanelFocusBorderColor = 0;
    public static readonly Color PanelSelectFrameColor = 15;
    public static readonly Color PanelSelectBorderColor = 0;

    // Piano keyboard colors
    public static readonly Color PianoKeyboardRestColor = 12;
    public static readonly Color PianoKeyboardPlayColor = 14;

    // Piano roll colors
    public static readonly Color PianoRollCursorPlayColor = 14;
    public static readonly Color PianoRollCursorEditColor = 6;
    public static readonly Color PianoRollCursorSelectColor = 15;
    public static readonly Color PianoRollBackgroundColor = 7;
    public static readonly Color PianoRollNoteColor = 8;
    public static readonly Color PianoRollRestColor = 5;

    // Octave bar colors
    public static readonly Color OctaveBarBackgroundColor = 7;
    public static readonly Color OctaveBarColor = 13;

    // Sound field colors
    public static readonly Color SoundFieldDataNormalColor = 1;
    public static readonly Color SoundFieldDataSelectColor = 7;
    public static readonly Color SoundFieldCursorEditColor = 1;
    public static readonly Color SoundFieldCursorSelectColor = 2;

    // Music field colors
    public static readonly Color MusicFieldBackgroundColor = 6;
    public static readonly Color MusicFieldSoundNormalColor = 1;
    public static readonly Color MusicFieldSoundSelectColor = 7;
    public static readonly Color MusicFieldCursorPlayColor = 8;
    public static readonly Color MusicFieldCursorEditColor = 1;
    public static readonly Color MusicFieldCursorSelectColor = 2;

    public static bool IsModifierPressed() =>
        Pyxel.Btn(Key.Shift) || Pyxel.Btn(Key.Ctrl) || Pyxel.Btn(Key.Alt) || Pyxel.Btn(Key.Gui);
}

public static class EditorMath
{
    /// <summary>Python-style floor division (<c>//</c>), which rounds toward
    /// negative infinity instead of truncating like C# <c>/</c>.</summary>
    public static int FloorDiv(int a, int b)
    {
        var q = a / b;
        if (a % b != 0 && (a < 0) != (b < 0))
        {
            q--;
        }
        return q;
    }

    /// <summary>Python-style modulo (<c>%</c>), which is never negative for a
    /// positive divisor.</summary>
    public static int Mod(int a, int b)
    {
        var r = a % b;
        return r < 0 ? r + b : r;
    }
}
