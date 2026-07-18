using PyxelSharp.Native;

namespace PyxelSharp;

/// <summary>
/// A sound: note/tone/volume/effect sequences plus playback speed, or MML
/// code (Python: <c>pyxel.Sound</c>). The sound banks
/// (<see cref="Pyxel.Sounds"/>) and user-created sounds share this class.
/// </summary>
/// <remarks>
/// Audio objects are thread-safe natively, so <see cref="Dispose"/> releases
/// immediately from any thread. The component lists are exchanged by copy:
/// mutate via the property setters, not element-wise.
/// </remarks>
public sealed unsafe class Sound : IDisposable
{
    private void* _handle;
    private bool _isAppLifetime;

    internal Sound(void* handle, bool isAppLifetime = false)
    {
        _handle = handle;
        _isAppLifetime = isAppLifetime;
        if (isAppLifetime)
        {
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>Creates an empty sound (Python: <c>pyxel.Sound()</c>).</summary>
    public Sound()
    {
        void* handle;
        Pyxel.Check(NativeMethods.pyxel_sound_new(&handle));
        _handle = handle;
    }

    internal void* Handle =>
        _handle is not null ? _handle : throw new ObjectDisposedException(nameof(Sound));

    /// <summary>The note sequence, -1 for rests (Python: <c>sound.notes</c>). Copies on get/set.</summary>
    public sbyte[] Notes
    {
        get
        {
            uint length;
            Pyxel.Check(NativeMethods.pyxel_sound_notes_len(Handle, &length));
            var values = new sbyte[length];
            fixed (sbyte* buffer = values)
            {
                Pyxel.Check(NativeMethods.pyxel_sound_notes_read(Handle, buffer, length));
            }
            return values;
        }
        set
        {
            fixed (sbyte* data = value)
            {
                Pyxel.Check(NativeMethods.pyxel_sound_notes_write(Handle, data, (uint)value.Length));
            }
        }
    }

    /// <summary>The tone sequence (Python: <c>sound.tones</c>). Copies on get/set.</summary>
    public byte[] Tones
    {
        get
        {
            uint length;
            Pyxel.Check(NativeMethods.pyxel_sound_tones_len(Handle, &length));
            var values = new byte[length];
            fixed (byte* buffer = values)
            {
                Pyxel.Check(NativeMethods.pyxel_sound_tones_read(Handle, buffer, length));
            }
            return values;
        }
        set
        {
            fixed (byte* data = value)
            {
                Pyxel.Check(NativeMethods.pyxel_sound_tones_write(Handle, data, (uint)value.Length));
            }
        }
    }

    /// <summary>The volume sequence (Python: <c>sound.volumes</c>). Copies on get/set.</summary>
    public byte[] Volumes
    {
        get
        {
            uint length;
            Pyxel.Check(NativeMethods.pyxel_sound_volumes_len(Handle, &length));
            var values = new byte[length];
            fixed (byte* buffer = values)
            {
                Pyxel.Check(NativeMethods.pyxel_sound_volumes_read(Handle, buffer, length));
            }
            return values;
        }
        set
        {
            fixed (byte* data = value)
            {
                Pyxel.Check(NativeMethods.pyxel_sound_volumes_write(Handle, data, (uint)value.Length));
            }
        }
    }

    /// <summary>The effect sequence (Python: <c>sound.effects</c>). Copies on get/set.</summary>
    public byte[] Effects
    {
        get
        {
            uint length;
            Pyxel.Check(NativeMethods.pyxel_sound_effects_len(Handle, &length));
            var values = new byte[length];
            fixed (byte* buffer = values)
            {
                Pyxel.Check(NativeMethods.pyxel_sound_effects_read(Handle, buffer, length));
            }
            return values;
        }
        set
        {
            fixed (byte* data = value)
            {
                Pyxel.Check(NativeMethods.pyxel_sound_effects_write(Handle, data, (uint)value.Length));
            }
        }
    }

    /// <summary>Playback speed in ticks per note (Python: <c>sound.speed</c>).</summary>
    public int Speed
    {
        get
        {
            ushort value;
            Pyxel.Check(NativeMethods.pyxel_sound_speed(Handle, &value));
            return value;
        }
        set => Pyxel.Check(NativeMethods.pyxel_sound_set_speed(Handle, (ushort)value));
    }

    /// <summary>Sets all components from their string notations (Python: <c>sound.set</c>).</summary>
    public void Set(string notes, string tones, string volumes, string effects, int speed)
    {
        fixed (byte* notesPtr = Pyxel.ToUtf8Required(notes))
        fixed (byte* tonesPtr = Pyxel.ToUtf8Required(tones))
        fixed (byte* volumesPtr = Pyxel.ToUtf8Required(volumes))
        fixed (byte* effectsPtr = Pyxel.ToUtf8Required(effects))
        {
            Pyxel.Check(NativeMethods.pyxel_sound_set(
                Handle, notesPtr, tonesPtr, volumesPtr, effectsPtr, (ushort)speed));
        }
    }

    /// <summary>Sets the notes from string notation (Python: <c>sound.set_notes</c>).</summary>
    public void SetNotes(string notes)
    {
        fixed (byte* notesPtr = Pyxel.ToUtf8Required(notes))
        {
            Pyxel.Check(NativeMethods.pyxel_sound_set_notes(Handle, notesPtr));
        }
    }

    /// <summary>Sets the tones from string notation (Python: <c>sound.set_tones</c>).</summary>
    public void SetTones(string tones)
    {
        fixed (byte* tonesPtr = Pyxel.ToUtf8Required(tones))
        {
            Pyxel.Check(NativeMethods.pyxel_sound_set_tones(Handle, tonesPtr));
        }
    }

    /// <summary>Sets the volumes from string notation (Python: <c>sound.set_volumes</c>).</summary>
    public void SetVolumes(string volumes)
    {
        fixed (byte* volumesPtr = Pyxel.ToUtf8Required(volumes))
        {
            Pyxel.Check(NativeMethods.pyxel_sound_set_volumes(Handle, volumesPtr));
        }
    }

    /// <summary>Sets the effects from string notation (Python: <c>sound.set_effects</c>).</summary>
    public void SetEffects(string effects)
    {
        fixed (byte* effectsPtr = Pyxel.ToUtf8Required(effects))
        {
            Pyxel.Check(NativeMethods.pyxel_sound_set_effects(Handle, effectsPtr));
        }
    }

    /// <summary>
    /// Sets the MML code, or clears it when null so the component lists take
    /// effect again (Python: <c>sound.mml</c>).
    /// </summary>
    public void Mml(string? code = null)
    {
        fixed (byte* codePtr = Pyxel.ToUtf8Optional(code))
        {
            Pyxel.Check(NativeMethods.pyxel_sound_mml(Handle, codePtr));
        }
    }

    /// <summary>
    /// Loads a PCM file (WAV etc.), or clears the PCM data when null
    /// (Python: <c>sound.pcm</c>).
    /// </summary>
    public void Pcm(string? filename = null)
    {
        fixed (byte* filenamePtr = Pyxel.ToUtf8Optional(filename))
        {
            Pyxel.Check(NativeMethods.pyxel_sound_pcm(Handle, filenamePtr));
        }
    }

    /// <summary>Renders <paramref name="sec"/> seconds to a WAV file (Python: <c>sound.save</c>).</summary>
    public void Save(string filename, float sec, bool? ffmpeg = null)
    {
        fixed (byte* filenamePtr = Pyxel.ToUtf8Required(filename))
        {
            Pyxel.Check(NativeMethods.pyxel_sound_save(
                Handle, filenamePtr, sec, Pyxel.ToSentinel(ffmpeg)));
        }
    }

    /// <summary>Playback length in seconds, or null when unknown (Python: <c>sound.total_sec</c>).</summary>
    public float? TotalSec()
    {
        float value;
        bool hasValue;
        Pyxel.Check(NativeMethods.pyxel_sound_total_sec(Handle, &value, &hasValue));
        return hasValue ? value : null;
    }

    /// <summary>Releases the native sound (immediately, from any thread). No-op for banks.</summary>
    public void Dispose()
    {
        if (_isAppLifetime || _handle is null)
        {
            return;
        }
        Pyxel.ReleaseHandle((IntPtr)_handle, Pyxel.HandleKind.Sound);
        _handle = null;
        GC.SuppressFinalize(this);
    }

    ~Sound()
    {
        if (_handle is not null)
        {
            Pyxel.ReleaseHandle((IntPtr)_handle, Pyxel.HandleKind.Sound);
        }
    }

    // Called when the bank cache is invalidated (pyxel.load replaced the
    // banks): the wrapper keeps working against the old object like a stale
    // Python reference, but is now released by GC when unreferenced.
    internal void Detach()
    {
        if (!_isAppLifetime)
        {
            return;
        }
        _isAppLifetime = false;
        GC.ReRegisterForFinalize(this);
    }
}

/// <summary>The sound banks, <c>Pyxel.Sounds[0..63]</c> (Python: <c>pyxel.sounds</c>).</summary>
public sealed unsafe class SoundBankList
{
    private Sound?[]? _banks;

    internal SoundBankList()
    {
    }

    public int Count
    {
        get
        {
            uint count;
            Pyxel.Check(NativeMethods.pyxel_num_sounds(&count));
            return (int)count;
        }
    }

    public Sound this[int index]
    {
        get
        {
            _banks ??= new Sound?[Count];
            var bank = _banks[index];
            if (bank is null)
            {
                void* handle;
                Pyxel.Check(NativeMethods.pyxel_sound_bank((uint)index, &handle));
                bank = new Sound(handle, isAppLifetime: true);
                _banks[index] = bank;
            }
            return bank;
        }
    }

    // Drops cached wrappers so the next access re-fetches the banks; needed
    // after pyxel.load replaces them (cached handles would go stale).
    internal void Invalidate()
    {
        if (_banks is null)
        {
            return;
        }
        foreach (var bank in _banks)
        {
            bank?.Detach();
        }
        _banks = null;
    }
}
