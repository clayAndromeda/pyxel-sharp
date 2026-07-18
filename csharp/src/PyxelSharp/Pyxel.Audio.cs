using PyxelSharp.Native;

namespace PyxelSharp;

public static unsafe partial class Pyxel
{
    /// <summary>The sound banks (Python: <c>pyxel.sounds</c>).</summary>
    public static SoundBankList Sounds { get; } = new();

    /// <summary>The music banks (Python: <c>pyxel.musics</c>).</summary>
    public static MusicBankList Musics { get; } = new();

    /// <summary>The mixer channels (Python: <c>pyxel.channels</c>).</summary>
    public static ChannelBankList Channels { get; } = new();

    /// <summary>The tone banks (Python: <c>pyxel.tones</c>).</summary>
    public static ToneBankList Tones { get; } = new();

    /// <summary>Plays sound banks in order on a channel (Python: <c>pyxel.play</c>).</summary>
    public static void Play(
        int channel,
        int[] soundBanks,
        float? sec = null,
        bool loop = false,
        bool resume = false)
    {
        var indices = new uint[soundBanks.Length];
        for (var i = 0; i < soundBanks.Length; i++)
        {
            indices[i] = (uint)soundBanks[i];
        }
        fixed (uint* indicesPtr = indices)
        {
            Check(NativeMethods.pyxel_play(
                (uint)channel, indicesPtr, (uint)indices.Length, ToSentinel(sec), loop, resume));
        }
    }

    /// <summary>Plays a sound bank on a channel (Python: <c>pyxel.play</c>).</summary>
    public static void Play(
        int channel,
        int soundBank,
        float? sec = null,
        bool loop = false,
        bool resume = false) =>
        Play(channel, [soundBank], sec, loop, resume);

    /// <summary>Plays sounds in order on a channel (Python: <c>pyxel.play</c>).</summary>
    public static void Play(
        int channel,
        Sound[] sounds,
        float? sec = null,
        bool loop = false,
        bool resume = false)
    {
        var handles = new void*[sounds.Length];
        for (var i = 0; i < sounds.Length; i++)
        {
            handles[i] = sounds[i].Handle;
        }
        fixed (void** handlesPtr = handles)
        {
            Check(NativeMethods.pyxel_play_handles(
                (uint)channel, handlesPtr, (uint)sounds.Length, ToSentinel(sec), loop, resume));
        }
        GC.KeepAlive(sounds);
    }

    /// <summary>Plays a sound on a channel (Python: <c>pyxel.play</c>).</summary>
    public static void Play(
        int channel,
        Sound sound,
        float? sec = null,
        bool loop = false,
        bool resume = false) =>
        Play(channel, [sound], sec, loop, resume);

    /// <summary>Plays MML code on a channel (Python: <c>pyxel.play</c> with a string).</summary>
    public static void Play(
        int channel,
        string mml,
        float? sec = null,
        bool loop = false,
        bool resume = false)
    {
        fixed (byte* mmlPtr = ToUtf8Required(mml))
        {
            Check(NativeMethods.pyxel_play_mml(
                (uint)channel, mmlPtr, ToSentinel(sec), loop, resume));
        }
    }

    /// <summary>Plays a music bank (Python: <c>pyxel.playm</c>).</summary>
    public static void Playm(int music, float? sec = null, bool loop = false) =>
        Check(NativeMethods.pyxel_playm((uint)music, ToSentinel(sec), loop));

    /// <summary>Stops a channel, or all channels when omitted (Python: <c>pyxel.stop</c>).</summary>
    public static void Stop(int? channel = null) =>
        Check(NativeMethods.pyxel_stop(channel ?? -1));

    /// <summary>
    /// The (sound index, seconds) position of a channel's playback, or null
    /// when nothing is playing (Python: <c>pyxel.play_pos</c>).
    /// </summary>
    public static (int SoundIndex, float Sec)? PlayPos(int channel)
    {
        uint soundIndex;
        float sec;
        bool hasValue;
        Check(NativeMethods.pyxel_play_pos((uint)channel, &soundIndex, &sec, &hasValue));
        return hasValue ? ((int)soundIndex, sec) : null;
    }
}
