using PyxelSharp.Native;

namespace PyxelSharp;

/// <summary>Tone mode (Python: <c>Tone.mode</c> integer values).</summary>
public enum ToneMode
{
    Wavetable = 0,
    ShortPeriodNoise = 1,
    LongPeriodNoise = 2,
}

/// <summary>
/// A tone: waveform, sample bits, and gain (Python: <c>pyxel.Tone</c>).
/// Usually accessed via <see cref="Pyxel.Tones"/>.
/// </summary>
public sealed unsafe class Tone : IDisposable
{
    private void* _handle;
    private readonly bool _isAppLifetime;

    internal Tone(void* handle, bool isAppLifetime = false)
    {
        _handle = handle;
        _isAppLifetime = isAppLifetime;
        if (isAppLifetime)
        {
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>Creates a default tone (Python: <c>pyxel.Tone()</c>).</summary>
    public Tone()
    {
        void* handle;
        Pyxel.Check(NativeMethods.pyxel_tone_new(&handle));
        _handle = handle;
    }

    internal void* Handle =>
        _handle is not null ? _handle : throw new ObjectDisposedException(nameof(Tone));

    /// <summary>Wavetable or noise mode (Python: <c>tone.mode</c>).</summary>
    public ToneMode Mode
    {
        get
        {
            uint value;
            Pyxel.Check(NativeMethods.pyxel_tone_mode(Handle, &value));
            return (ToneMode)value;
        }
        set => Pyxel.Check(NativeMethods.pyxel_tone_set_mode(Handle, (uint)value));
    }

    /// <summary>Bit depth of the wavetable samples (Python: <c>tone.sample_bits</c>).</summary>
    public int SampleBits
    {
        get
        {
            uint value;
            Pyxel.Check(NativeMethods.pyxel_tone_sample_bits(Handle, &value));
            return (int)value;
        }
        set => Pyxel.Check(NativeMethods.pyxel_tone_set_sample_bits(Handle, (uint)value));
    }

    /// <summary>Output gain (Python: <c>tone.gain</c>).</summary>
    public float Gain
    {
        get
        {
            float value;
            Pyxel.Check(NativeMethods.pyxel_tone_gain(Handle, &value));
            return value;
        }
        set => Pyxel.Check(NativeMethods.pyxel_tone_set_gain(Handle, value));
    }

    /// <summary>The waveform samples (Python: <c>tone.wavetable</c>). Copies on get/set.</summary>
    public uint[] Wavetable
    {
        get
        {
            uint length;
            Pyxel.Check(NativeMethods.pyxel_tone_wavetable_len(Handle, &length));
            var values = new uint[length];
            fixed (uint* buffer = values)
            {
                Pyxel.Check(NativeMethods.pyxel_tone_wavetable_read(Handle, buffer, length));
            }
            return values;
        }
        set
        {
            fixed (uint* data = value)
            {
                Pyxel.Check(NativeMethods.pyxel_tone_wavetable_write(
                    Handle, data, (uint)value.Length));
            }
        }
    }

    /// <summary>Releases the native tone (immediately, from any thread). No-op for banks.</summary>
    public void Dispose()
    {
        if (_isAppLifetime || _handle is null)
        {
            return;
        }
        Pyxel.ReleaseHandle((IntPtr)_handle, Pyxel.HandleKind.Tone);
        _handle = null;
        GC.SuppressFinalize(this);
    }

    ~Tone()
    {
        if (_handle is not null)
        {
            Pyxel.ReleaseHandle((IntPtr)_handle, Pyxel.HandleKind.Tone);
        }
    }
}

/// <summary>The tone banks, <c>Pyxel.Tones[0..3]</c> (Python: <c>pyxel.tones</c>).</summary>
public sealed unsafe class ToneBankList
{
    private Tone?[]? _banks;

    internal ToneBankList()
    {
    }

    public int Count
    {
        get
        {
            uint count;
            Pyxel.Check(NativeMethods.pyxel_num_tones(&count));
            return (int)count;
        }
    }

    public Tone this[int index]
    {
        get
        {
            _banks ??= new Tone?[Count];
            var bank = _banks[index];
            if (bank is null)
            {
                void* handle;
                Pyxel.Check(NativeMethods.pyxel_tone_bank((uint)index, &handle));
                bank = new Tone(handle, isAppLifetime: true);
                _banks[index] = bank;
            }
            return bank;
        }
    }
}
