using PyxelSharp.Native;

namespace PyxelSharp;

/// <summary>
/// A music piece: one sound-index sequence per channel
/// (Python: <c>pyxel.Music</c>). The music banks (<see cref="Pyxel.Musics"/>)
/// and user-created music share this class.
/// </summary>
/// <remarks>
/// Audio objects are thread-safe natively, so <see cref="Dispose"/> releases
/// immediately from any thread. <see cref="Seqs"/> copies on get/set.
/// </remarks>
public sealed unsafe class Music : IDisposable
{
    private void* _handle;
    private readonly bool _isAppLifetime;

    internal Music(void* handle, bool isAppLifetime = false)
    {
        _handle = handle;
        _isAppLifetime = isAppLifetime;
        if (isAppLifetime)
        {
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>Creates an empty music piece (Python: <c>pyxel.Music()</c>).</summary>
    public Music()
    {
        void* handle;
        Pyxel.Check(NativeMethods.pyxel_music_new(&handle));
        _handle = handle;
    }

    internal void* Handle =>
        _handle is not null ? _handle : throw new ObjectDisposedException(nameof(Music));

    /// <summary>The per-channel sound-index sequences (Python: <c>music.seqs</c>). Copies on get/set.</summary>
    public int[][] Seqs
    {
        get
        {
            uint seqCount;
            Pyxel.Check(NativeMethods.pyxel_music_seqs_len(Handle, &seqCount));
            var seqs = new int[seqCount][];
            for (var i = 0u; i < seqCount; i++)
            {
                uint length;
                Pyxel.Check(NativeMethods.pyxel_music_seq_len(Handle, i, &length));
                var seq = new int[length];
                fixed (int* buffer = seq)
                {
                    Pyxel.Check(NativeMethods.pyxel_music_seq_read(
                        Handle, i, (uint*)buffer, length));
                }
                seqs[i] = seq;
            }
            return seqs;
        }
        set => Set(value);
    }

    /// <summary>Replaces all sequences (Python: <c>music.set</c>).</summary>
    public void Set(params int[][] seqs)
    {
        Pyxel.Check(NativeMethods.pyxel_music_seqs_clear(Handle));
        foreach (var seq in seqs)
        {
            fixed (int* data = seq)
            {
                Pyxel.Check(NativeMethods.pyxel_music_seqs_append(
                    Handle, (uint*)data, (uint)seq.Length));
            }
        }
    }

    /// <summary>Renders <paramref name="sec"/> seconds to a WAV file (Python: <c>music.save</c>).</summary>
    public void Save(string filename, float sec, bool? ffmpeg = null)
    {
        fixed (byte* filenamePtr = Pyxel.ToUtf8Required(filename))
        {
            Pyxel.Check(NativeMethods.pyxel_music_save(
                Handle, filenamePtr, sec, Pyxel.ToSentinel(ffmpeg)));
        }
    }

    /// <summary>Releases the native music (immediately, from any thread). No-op for banks.</summary>
    public void Dispose()
    {
        if (_isAppLifetime || _handle is null)
        {
            return;
        }
        Pyxel.ReleaseHandle((IntPtr)_handle, Pyxel.HandleKind.Music);
        _handle = null;
        GC.SuppressFinalize(this);
    }

    ~Music()
    {
        if (_handle is not null)
        {
            Pyxel.ReleaseHandle((IntPtr)_handle, Pyxel.HandleKind.Music);
        }
    }
}

/// <summary>The music banks, <c>Pyxel.Musics[0..7]</c> (Python: <c>pyxel.musics</c>).</summary>
public sealed unsafe class MusicBankList
{
    private Music?[]? _banks;

    internal MusicBankList()
    {
    }

    public int Count
    {
        get
        {
            uint count;
            Pyxel.Check(NativeMethods.pyxel_num_musics(&count));
            return (int)count;
        }
    }

    public Music this[int index]
    {
        get
        {
            _banks ??= new Music?[Count];
            var bank = _banks[index];
            if (bank is null)
            {
                void* handle;
                Pyxel.Check(NativeMethods.pyxel_music_bank((uint)index, &handle));
                bank = new Music(handle, isAppLifetime: true);
                _banks[index] = bank;
            }
            return bank;
        }
    }
}
