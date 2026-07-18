using PyxelSharp.Native;

namespace PyxelSharp;

/// <summary>
/// A mixer channel (Python: <c>pyxel.Channel</c>). Usually accessed via
/// <see cref="Pyxel.Channels"/>.
/// </summary>
public sealed unsafe class Channel : IDisposable
{
    private void* _handle;
    private readonly bool _isAppLifetime;

    internal Channel(void* handle, bool isAppLifetime = false)
    {
        _handle = handle;
        _isAppLifetime = isAppLifetime;
        if (isAppLifetime)
        {
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>Creates a standalone channel (Python: <c>pyxel.Channel()</c>).</summary>
    public Channel()
    {
        void* handle;
        Pyxel.Check(NativeMethods.pyxel_channel_new(&handle));
        _handle = handle;
    }

    internal void* Handle =>
        _handle is not null ? _handle : throw new ObjectDisposedException(nameof(Channel));

    /// <summary>Output gain, 0.0–1.0 (Python: <c>channel.gain</c>).</summary>
    public float Gain
    {
        get
        {
            float value;
            Pyxel.Check(NativeMethods.pyxel_channel_gain(Handle, &value));
            return value;
        }
        set => Pyxel.Check(NativeMethods.pyxel_channel_set_gain(Handle, value));
    }

    /// <summary>Pitch offset in cents (Python: <c>channel.detune</c>).</summary>
    public int Detune
    {
        get
        {
            int value;
            Pyxel.Check(NativeMethods.pyxel_channel_detune(Handle, &value));
            return value;
        }
        set => Pyxel.Check(NativeMethods.pyxel_channel_set_detune(Handle, value));
    }

    /// <summary>Plays sounds in order on this channel (Python: <c>channel.play</c>).</summary>
    public void Play(Sound[] sounds, float? sec = null, bool loop = false, bool resume = false)
    {
        var handles = new void*[sounds.Length];
        for (var i = 0; i < sounds.Length; i++)
        {
            handles[i] = sounds[i].Handle;
        }
        fixed (void** handlesPtr = handles)
        {
            Pyxel.Check(NativeMethods.pyxel_channel_play(
                Handle, handlesPtr, (uint)sounds.Length, Pyxel.ToSentinel(sec), loop, resume));
        }
        GC.KeepAlive(sounds);
    }

    /// <summary>Plays a sound on this channel (Python: <c>channel.play</c>).</summary>
    public void Play(Sound sound, float? sec = null, bool loop = false, bool resume = false) =>
        Play([sound], sec, loop, resume);

    /// <summary>Plays sound banks in order on this channel (Python: <c>channel.play</c>).</summary>
    public void Play(int[] soundBanks, float? sec = null, bool loop = false, bool resume = false)
    {
        var sounds = new Sound[soundBanks.Length];
        for (var i = 0; i < soundBanks.Length; i++)
        {
            sounds[i] = Pyxel.Sounds[soundBanks[i]];
        }
        Play(sounds, sec, loop, resume);
    }

    /// <summary>Plays a sound bank on this channel (Python: <c>channel.play</c>).</summary>
    public void Play(int soundBank, float? sec = null, bool loop = false, bool resume = false) =>
        Play(Pyxel.Sounds[soundBank], sec, loop, resume);

    /// <summary>Plays MML code on this channel (Python: <c>channel.play(mml)</c>).</summary>
    public void Play(string mml, float? sec = null, bool loop = false, bool resume = false)
    {
        fixed (byte* mmlPtr = Pyxel.ToUtf8Required(mml))
        {
            Pyxel.Check(NativeMethods.pyxel_channel_play_mml(
                Handle, mmlPtr, Pyxel.ToSentinel(sec), loop, resume));
        }
    }

    /// <summary>Stops playback on this channel (Python: <c>channel.stop</c>).</summary>
    public void Stop() => Pyxel.Check(NativeMethods.pyxel_channel_stop(Handle));

    /// <summary>
    /// The (sound index, seconds) position of current playback, or null when
    /// nothing is playing (Python: <c>channel.play_pos</c>).
    /// </summary>
    public (int SoundIndex, float Sec)? PlayPos()
    {
        uint soundIndex;
        float sec;
        bool hasValue;
        Pyxel.Check(NativeMethods.pyxel_channel_play_pos(Handle, &soundIndex, &sec, &hasValue));
        return hasValue ? ((int)soundIndex, sec) : null;
    }

    /// <summary>Releases the native channel (immediately, from any thread). No-op for the mixer channels.</summary>
    public void Dispose()
    {
        if (_isAppLifetime || _handle is null)
        {
            return;
        }
        Pyxel.ReleaseHandle((IntPtr)_handle, Pyxel.HandleKind.Channel);
        _handle = null;
        GC.SuppressFinalize(this);
    }

    ~Channel()
    {
        if (_handle is not null)
        {
            Pyxel.ReleaseHandle((IntPtr)_handle, Pyxel.HandleKind.Channel);
        }
    }
}

/// <summary>The mixer channels, <c>Pyxel.Channels[0..3]</c> (Python: <c>pyxel.channels</c>).</summary>
public sealed unsafe class ChannelBankList
{
    private Channel?[]? _banks;

    internal ChannelBankList()
    {
    }

    public int Count
    {
        get
        {
            uint count;
            Pyxel.Check(NativeMethods.pyxel_num_channels(&count));
            return (int)count;
        }
    }

    public Channel this[int index]
    {
        get
        {
            _banks ??= new Channel?[Count];
            var bank = _banks[index];
            if (bank is null)
            {
                void* handle;
                Pyxel.Check(NativeMethods.pyxel_channel_bank((uint)index, &handle));
                bank = new Channel(handle, isAppLifetime: true);
                _banks[index] = bank;
            }
            return bank;
        }
    }
}
