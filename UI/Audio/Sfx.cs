using System;
using System.Collections.Generic;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace UI.Audio;

/// <summary>
/// Joue les sons et la musique via un mélangeur NAudio. Si la carte son est absente
/// ou indisponible, le jeu continue simplement sans son.
/// </summary>
public static class Sfx
{
    private const float SoundVolume = 0.35f;
    private const float MusicVolume = 0.16f;

    private static WaveOutEvent? _output;
    private static MixingSampleProvider? _mixer;
    private static Dictionary<Sound, float[]> _sounds = new();
    private static LoopProvider? _music;

    public static bool SoundOn { get; set; } = true;

    public static bool MusicOn
    {
        get => _music?.Volume > 0;
        set
        {
            if (_music != null)
                _music.Volume = value ? MusicVolume : 0;
        }
    }

    public static void Init()
    {
        try
        {
            _sounds = Synth.BuildSounds();
            _mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(Synth.Rate, 1)) { ReadFully = true };
            _music = new LoopProvider(Synth.BuildMusic(), _mixer.WaveFormat) { Volume = MusicVolume };
            _mixer.AddMixerInput(_music);

            _output = new WaveOutEvent { DesiredLatency = 90 };
            _output.Init(_mixer);
            _output.Play();
        }
        catch (Exception)
        {
            // Pas de son disponible : le jeu reste jouable en silence
            _output = null;
            _mixer = null;
        }
    }

    public static void Play(Sound sound, float volume = 1)
    {
        if (!SoundOn || _mixer == null || !_sounds.TryGetValue(sound, out var samples))
            return;

        _mixer.AddMixerInput(new OneShotProvider(samples, _mixer.WaveFormat, volume * SoundVolume));
    }

    public static void Shutdown()
    {
        _output?.Stop();
        _output?.Dispose();
    }

    /// <summary>Joue un son une fois, puis se retire du mélangeur.</summary>
    private sealed class OneShotProvider(float[] samples, WaveFormat format, float volume) : ISampleProvider
    {
        private int _position;

        public WaveFormat WaveFormat => format;

        public int Read(float[] buffer, int offset, int count)
        {
            int available = Math.Min(count, samples.Length - _position);
            for (int i = 0; i < available; i++)
                buffer[offset + i] = samples[_position + i] * volume;
            _position += available;
            return available;
        }
    }

    /// <summary>Joue la musique en boucle, sans fin.</summary>
    private sealed class LoopProvider(float[] samples, WaveFormat format) : ISampleProvider
    {
        private int _position;

        public float Volume { get; set; }
        public WaveFormat WaveFormat => format;

        public int Read(float[] buffer, int offset, int count)
        {
            for (int i = 0; i < count; i++)
            {
                buffer[offset + i] = samples[_position] * Volume;
                _position = (_position + 1) % samples.Length;
            }
            return count;
        }
    }
}
