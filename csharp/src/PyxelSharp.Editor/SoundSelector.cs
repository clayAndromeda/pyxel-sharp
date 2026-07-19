using PyxelSharp.Editor.Widgets;

namespace PyxelSharp.Editor;

// sound_selector.py
public sealed class SoundSelector : Widget
{
    private readonly MusicEditor _editor;
    private int? _pressedSound;
    private int? _previewSound;
    private int? _lastPreviewSound;

    public SoundSelector(MusicEditor parent)
        : base(parent, 11, 129, 218, 44)
    {
        _editor = parent;

        // Set event listeners
        MouseDown += OnMouseDown;
        MouseUp += (key, _, _) =>
        {
            if (key == Key.MouseButtonLeft)
            {
                _pressedSound = null;
            }
        };
        MouseRepeat += OnMouseDown;
        MouseHover += (_, _) => _editor.HelpMessage = "PREVIEW:HOVER INSERT:CLICK";
        Update += OnUpdate;
        Draw += OnDraw;
    }

    // Helpers

    private int? HitSoundButton(int x, int y)
    {
        x -= X + 6;
        y -= Y + 5;
        if (x < 0 || y < 0 || x > 205 || y > 33 || x % 13 > 10 || y % 9 > 6)
        {
            return null;
        }
        return y / 9 * 16 + x / 13;
    }

    // Event handlers

    private void OnMouseDown(Key key, int x, int y)
    {
        if (key != Key.MouseButtonLeft || _editor.IsPlayingVar.Get())
        {
            return;
        }

        _pressedSound = HitSoundButton(x, y);
        if (_pressedSound is { } sound)
        {
            _editor.FieldCursor.Insert(sound);
        }
    }

    private void OnUpdate()
    {
        if (_editor.IsPlayingVar.Get())
        {
            return;
        }

        var mx = Pyxel.MouseX;
        var my = Pyxel.MouseY;
        if (IsHit(mx, my))
        {
            _previewSound = HitSoundButton(mx, my);
            if (_previewSound is { } sound && _previewSound != _lastPreviewSound)
            {
                Pyxel.Play(0, sound, loop: true);
            }
        }
        else
        {
            _previewSound = null;
        }

        if (_previewSound is null && Pyxel.PlayPos(0) is not null)
        {
            Pyxel.Stop(0);
        }

        _lastPreviewSound = _previewSound;
    }

    // Drawing

    private void DrawSoundButton(int sound, Color col)
    {
        Pyxel.Pal(13, col);
        var x = sound % 16 * 13;
        var y = sound / 16 * 9;
        Pyxel.Blt(X + x + 6, Y + y + 5, EditorSettings.EditorImage, x, y + 121, 11, 7);
        Pyxel.Pal();
    }

    private void OnDraw()
    {
        DrawPanel(X, Y, Width, Height);
        Pyxel.Blt(X + 6, Y + 5, EditorSettings.EditorImage, 0, 121, 206, 34);

        for (var i = 0; i < Pyxel.NumSounds; i++)
        {
            if (Pyxel.Sounds[i].Notes.Length > 0)
            {
                DrawSoundButton(i, WidgetSettings.ButtonEnabledColor);
            }
        }

        if (_pressedSound is { } pressed)
        {
            DrawSoundButton(pressed, WidgetSettings.ButtonPressedColor);
        }
    }
}
