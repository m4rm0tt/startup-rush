using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Avalonia.Platform;
using NAudio.Vorbis;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace UI.Audio;

public enum Sound
{
    Click, Hover, Select, Offer, Coin, Cash, Buy, Bill, Error, Card, CardPlace, Shuffle,
    Crit, LevelUp, Alarm, Unpaid, Event, PauseIn, PauseOut, Victory, Defeat
}

public enum Music { None, Menu, Run, Crisis }

/// <summary>
/// Joue les bruitages et la musique (fichiers des packs, voir ASSET_USAGE.md) via un mélangeur NAudio.
/// Tout est décodé une fois au lancement. Si la carte son est absente, le jeu continue en silence.
/// </summary>
public static class Sfx
{
    private const int Rate = 44100;
    private const int MaxVoices = 10;           // au-delà, les nouveaux sons sont ignorés
    private const double MinRepeatDelay = 0.045; // un même son ne se superpose pas à lui-même trop vite
    private const float SoundLevel = 0.5f;
    private const float MusicLevel = 0.32f;

    // Fichier et volume relatif de chaque son, dosés pour que le travail répété reste discret
    private static readonly Dictionary<Sound, (string File, float Volume)> SoundFiles = new()
    {
        [Sound.Click] = ("work.wav", 0.35f),
        [Sound.Hover] = ("hover.wav", 0.3f),
        [Sound.Select] = ("select.wav", 0.7f),
        [Sound.Offer] = ("offer.wav", 0.25f),
        [Sound.Coin] = ("coin.wav", 0.6f),
        [Sound.Cash] = ("cash.wav", 0.7f),
        [Sound.Buy] = ("buy.wav", 0.7f),
        [Sound.Bill] = ("bill.wav", 0.6f),
        [Sound.Error] = ("error.wav", 0.6f),
        [Sound.Card] = ("card_fan.ogg", 0.9f),
        [Sound.CardPlace] = ("card_place.ogg", 0.9f),
        [Sound.Shuffle] = ("card_shuffle.ogg", 0.9f),
        [Sound.Crit] = ("crit.wav", 0.55f),
        [Sound.LevelUp] = ("levelup.wav", 0.8f),
        [Sound.Alarm] = ("alarm.wav", 0.6f),
        [Sound.Unpaid] = ("unpaid.wav", 0.8f),
        [Sound.Event] = ("event.wav", 0.7f),
        [Sound.PauseIn] = ("pause_in.wav", 0.6f),
        [Sound.PauseOut] = ("pause_out.wav", 0.6f),
        [Sound.Victory] = ("victory.wav", 0.9f),
        [Sound.Defeat] = ("defeat.wav", 0.8f),
    };

    private static readonly Dictionary<Music, string[]> MusicFiles = new()
    {
        [Music.Menu] = ["menu.ogg"],
        [Music.Run] = ["run1.ogg", "run2.ogg"], // les deux thèmes alternent pour ne pas lasser
        [Music.Crisis] = ["crisis.ogg"],
    };

    private static WaveOutEvent? _output;
    private static MixingSampleProvider? _mixer;
    private static MusicPlayer? _music;
    private static readonly Dictionary<Sound, float[]> Sounds = new();
    private static readonly Dictionary<string, float[]> Tracks = new();
    private static readonly Dictionary<Sound, DateTime> LastPlayed = new();
    private static int _voices;

    public static double SoundVolume { get; set; } = 0.8;

    public static double MusicVolume
    {
        get => _musicVolume;
        set
        {
            _musicVolume = Math.Clamp(value, 0, 1);
            if (_music != null)
                _music.Volume = (float)_musicVolume * MusicLevel;
        }
    }
    private static double _musicVolume = 0.6;

    public static void Init()
    {
        try
        {
            foreach (var (sound, (file, _)) in SoundFiles)
                Sounds[sound] = Normalize(Decode($"Sfx/{file}"), 0.25f);
            foreach (var files in MusicFiles.Values)
                foreach (var file in files)
                    Tracks[file] = Normalize(Decode($"Music/{file}"), 0.2f);

            _mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(Rate, 1)) { ReadFully = true };
            _music = new MusicPlayer(_mixer.WaveFormat) { Volume = (float)_musicVolume * MusicLevel };
            _mixer.AddMixerInput(_music);

            _output = new WaveOutEvent { DesiredLatency = 90 };
            _output.Init(new SoftClipProvider(_mixer));
            _output.Play();
            Console.Error.WriteLine($"Audio prêt : {Sounds.Count} sons, {Tracks.Count} musiques");
        }
        catch (Exception e)
        {
            // Pas de son disponible : le jeu reste jouable en silence, mais on dit pourquoi
            Console.Error.WriteLine($"Audio indisponible : {e.GetType().Name} {e.Message}");
            _output = null;
            _mixer = null;
            _music = null;
        }
    }

    public static void Play(Sound sound, float volume = 1)
    {
        if (SoundVolume <= 0 || _mixer == null || !Sounds.TryGetValue(sound, out var samples))
            return;

        var now = DateTime.UtcNow;
        if (Volatile.Read(ref _voices) >= MaxVoices || LastPlayed.TryGetValue(sound, out var last) && (now - last).TotalSeconds < MinRepeatDelay)
            return;
        LastPlayed[sound] = now;

        float gain = volume * SoundFiles[sound].Volume * SoundLevel * (float)SoundVolume;
        _mixer.AddMixerInput(new OneShotProvider(samples, _mixer.WaveFormat, gain));
    }

    /// <summary>Change de musique en fondu. Rejouer la même ne fait rien.</summary>
    public static void PlayMusic(Music music)
    {
        if (_music == null)
            return;

        var playlist = new List<float[]>();
        if (MusicFiles.TryGetValue(music, out var files))
            foreach (var file in files)
                playlist.Add(Tracks[file]);
        _music.Switch(music, playlist);
    }

    public static void Shutdown()
    {
        _output?.Stop();
        _output?.Dispose();
    }

    // ---------- Chargement ----------

    /// <summary>Lit un fichier WAV ou OGG des ressources et le convertit en mono 44,1 kHz.</summary>
    private static float[] Decode(string path)
    {
        using var resource = AssetLoader.Open(new Uri($"avares://UI/Assets/Audio/{path}"));
        var memory = new MemoryStream();
        resource.CopyTo(memory);
        memory.Position = 0;

        using WaveStream reader = path.EndsWith(".ogg") ? new VorbisWaveReader(memory) : new WaveFileReader(memory);
        ISampleProvider samples = reader.ToSampleProvider();
        if (samples.WaveFormat.Channels == 2)
            samples = new StereoToMonoSampleProvider(samples);
        if (samples.WaveFormat.SampleRate != Rate)
            samples = new WdlResamplingSampleProvider(samples, Rate);

        var result = new List<float>();
        var buffer = new float[Rate];
        int read;
        while ((read = samples.Read(buffer, 0, buffer.Length)) > 0)
            for (int i = 0; i < read; i++)
                result.Add(buffer[i]);
        return result.ToArray();
    }

    /// <summary>Ramène un son à un niveau moyen commun, sans jamais dépasser la crête.</summary>
    private static float[] Normalize(float[] samples, float targetRms)
    {
        double sum = 0;
        float peak = 0;
        foreach (float s in samples)
        {
            sum += s * s;
            peak = Math.Max(peak, Math.Abs(s));
        }
        if (peak <= 0)
            return samples;

        float rms = (float)Math.Sqrt(sum / samples.Length);
        float gain = Math.Min(targetRms / rms, 0.95f / peak);
        for (int i = 0; i < samples.Length; i++)
            samples[i] *= gain;
        return samples;
    }

    // ---------- Lecture ----------

    /// <summary>Joue un son une fois, puis se retire du mélangeur.</summary>
    private sealed class OneShotProvider : ISampleProvider
    {
        private readonly float[] _samples;
        private readonly float _volume;
        private int _position;
        private bool _done;

        public OneShotProvider(float[] samples, WaveFormat format, float volume)
        {
            _samples = samples;
            _volume = volume;
            WaveFormat = format;
            Interlocked.Increment(ref _voices);
        }

        public WaveFormat WaveFormat { get; }

        public int Read(float[] buffer, int offset, int count)
        {
            int available = Math.Min(count, _samples.Length - _position);
            for (int i = 0; i < available; i++)
                buffer[offset + i] = _samples[_position + i] * _volume;
            _position += available;
            if (available < count && !_done)
            {
                _done = true;
                Interlocked.Decrement(ref _voices);
            }
            return available;
        }
    }

    /// <summary>
    /// Musique en boucle sans coupure, avec fondu enchaîné quand on change de morceau.
    /// Une playlist de plusieurs morceaux passe au suivant toutes les deux boucles.
    /// </summary>
    private sealed class MusicPlayer(WaveFormat format) : ISampleProvider
    {
        private const int FadeSamples = Rate * 3 / 2;
        private const int LoopsPerTrack = 2;

        private readonly object _lock = new();
        private Music _current = Music.None;
        private List<float[]> _playlist = [];
        private int _track;
        private int _position;
        private int _loops;
        private float[]? _fadingOut;
        private int _fadingPosition;
        private int _fade;

        public float Volume { get; set; }
        public WaveFormat WaveFormat => format;

        public void Switch(Music music, List<float[]> playlist)
        {
            lock (_lock)
            {
                if (music == _current)
                    return;

                _fadingOut = _playlist.Count > 0 ? _playlist[_track] : null;
                _fadingPosition = _position;
                _fade = _fadingOut != null ? FadeSamples : 0;
                _current = music;
                _playlist = playlist;
                _track = 0;
                _position = 0;
                _loops = 0;
            }
        }

        public int Read(float[] buffer, int offset, int count)
        {
            lock (_lock)
            {
                for (int i = 0; i < count; i++)
                {
                    float sample = 0;
                    float fadeIn = _fade > 0 ? 1 - (float)_fade / FadeSamples : 1;

                    if (_playlist.Count > 0)
                    {
                        var track = _playlist[_track];
                        sample = track[_position] * fadeIn;
                        if (++_position >= track.Length)
                            NextLoop();
                    }

                    if (_fadingOut != null && _fade > 0)
                    {
                        sample += _fadingOut[_fadingPosition] * (float)_fade / FadeSamples;
                        _fadingPosition = (_fadingPosition + 1) % _fadingOut.Length;
                        if (--_fade == 0)
                            _fadingOut = null;
                    }

                    buffer[offset + i] = sample * Volume;
                }
            }
            return count;
        }

        private void NextLoop()
        {
            _position = 0;
            if (_playlist.Count < 2 || ++_loops < LoopsPerTrack)
                return;

            // Morceau suivant de la playlist, en fondu
            _fadingOut = _playlist[_track];
            _fadingPosition = 0;
            _fade = FadeSamples;
            _loops = 0;
            _track = (_track + 1) % _playlist.Count;
        }
    }

    /// <summary>Adoucit les crêtes quand beaucoup de sons se superposent, au lieu d'écrêter sèchement.</summary>
    private sealed class SoftClipProvider(ISampleProvider source) : ISampleProvider
    {
        public WaveFormat WaveFormat => source.WaveFormat;

        public int Read(float[] buffer, int offset, int count)
        {
            int read = source.Read(buffer, offset, count);
            for (int i = 0; i < read; i++)
                buffer[offset + i] = MathF.Tanh(buffer[offset + i]);
            return read;
        }
    }
}
