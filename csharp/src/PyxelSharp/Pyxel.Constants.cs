using System.Runtime.InteropServices;
using PyxelSharp.Native;

namespace PyxelSharp;

public static unsafe partial class Pyxel
{
    // Values mirror pyxel/crates/pyxel-core/src/settings.rs; they have been
    // stable across pyxel releases. The version string alone is read from the
    // linked pyxel-core so it can never drift.

    private static string? _version;

    /// <summary>The pyxel-core version, e.g. "2.9.8" (Python: <c>pyxel.VERSION</c>).</summary>
    public static string Version
    {
        get
        {
            if (_version is null)
            {
                byte* version;
                Check(NativeMethods.pyxel_version(&version));
                _version = Marshal.PtrToStringUTF8((IntPtr)version) ?? string.Empty;
            }
            return _version;
        }
    }

    /// <summary>Number of palette colors (Python: <c>pyxel.NUM_COLORS</c>).</summary>
    public const int NumColors = 16;

    /// <summary>Number of image banks (Python: <c>pyxel.NUM_IMAGES</c>).</summary>
    public const int NumImages = 3;

    /// <summary>Width and height of an image bank in pixels (Python: <c>pyxel.IMAGE_SIZE</c>).</summary>
    public const int ImageSize = 256;

    /// <summary>Number of tilemap banks (Python: <c>pyxel.NUM_TILEMAPS</c>).</summary>
    public const int NumTilemaps = 8;

    /// <summary>Width and height of a tilemap bank in tiles (Python: <c>pyxel.TILEMAP_SIZE</c>).</summary>
    public const int TilemapSize = 256;

    /// <summary>Width and height of a tile in pixels (Python: <c>pyxel.TILE_SIZE</c>).</summary>
    public const int TileSize = 8;

    /// <summary>Character width of the built-in font (Python: <c>pyxel.FONT_WIDTH</c>).</summary>
    public const int FontWidth = 4;

    /// <summary>Character height of the built-in font (Python: <c>pyxel.FONT_HEIGHT</c>).</summary>
    public const int FontHeight = 6;

    /// <summary>Number of mixer channels (Python: <c>pyxel.NUM_CHANNELS</c>).</summary>
    public const int NumChannels = 4;

    /// <summary>Number of tone banks (Python: <c>pyxel.NUM_TONES</c>).</summary>
    public const int NumTones = 4;

    /// <summary>Number of sound banks (Python: <c>pyxel.NUM_SOUNDS</c>).</summary>
    public const int NumSounds = 64;

    /// <summary>Number of music banks (Python: <c>pyxel.NUM_MUSICS</c>).</summary>
    public const int NumMusics = 8;

    /// <summary>Triangle tone index for <see cref="Sound.Tones"/> (Python: <c>pyxel.TONE_TRIANGLE</c>).</summary>
    public const byte ToneTriangle = 0;

    /// <summary>Square tone index for <see cref="Sound.Tones"/> (Python: <c>pyxel.TONE_SQUARE</c>).</summary>
    public const byte ToneSquare = 1;

    /// <summary>Pulse tone index for <see cref="Sound.Tones"/> (Python: <c>pyxel.TONE_PULSE</c>).</summary>
    public const byte TonePulse = 2;

    /// <summary>Noise tone index for <see cref="Sound.Tones"/> (Python: <c>pyxel.TONE_NOISE</c>).</summary>
    public const byte ToneNoise = 3;

    /// <summary>No effect, for <see cref="Sound.Effects"/> (Python: <c>pyxel.EFFECT_NONE</c>).</summary>
    public const byte EffectNone = 0;

    /// <summary>Slide effect (Python: <c>pyxel.EFFECT_SLIDE</c>).</summary>
    public const byte EffectSlide = 1;

    /// <summary>Vibrato effect (Python: <c>pyxel.EFFECT_VIBRATO</c>).</summary>
    public const byte EffectVibrato = 2;

    /// <summary>Fadeout effect (Python: <c>pyxel.EFFECT_FADEOUT</c>).</summary>
    public const byte EffectFadeout = 3;

    /// <summary>Half-fadeout effect (Python: <c>pyxel.EFFECT_HALF_FADEOUT</c>).</summary>
    public const byte EffectHalfFadeout = 4;

    /// <summary>Quarter-fadeout effect (Python: <c>pyxel.EFFECT_QUARTER_FADEOUT</c>).</summary>
    public const byte EffectQuarterFadeout = 5;

    private static readonly uint[] _defaultColors =
    [
        // Palette indices 0-7
        0x000000, 0x2b335f, 0x7e2072, 0x19959c, 0x8b4852, 0x395c98, 0xa9c1ff, 0xeeeeee,
        // Palette indices 8-15
        0xd4186c, 0xd38441, 0xe9c35b, 0x70c6a9, 0x7696de, 0xa3a3a3, 0xff9798, 0xedc7b0,
    ];

    /// <summary>The pristine default palette, 0xRRGGBB per entry (Python: <c>pyxel.DEFAULT_COLORS</c>).</summary>
    public static ReadOnlySpan<uint> DefaultColors => _defaultColors;
}
