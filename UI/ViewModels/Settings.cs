using System;
using Core.meta;
using UI.Audio;

namespace UI.ViewModels;

/// <summary>
/// Réglages du joueur (volumes, effets), modifiables depuis la pause et sauvegardés avec le profil.
/// </summary>
public static class Settings
{
    private static GameSettings _values = new();
    private static Action? _save;

    public static bool ReduceEffects => _values.ReduceEffects;
    public static string MusicText => $"MUSIQUE : {Percent(_values.MusicVolume)}";
    public static string SoundText => $"BRUITAGES : {Percent(_values.SoundVolume)}";
    public static string EffectsText => _values.ReduceEffects ? "SECOUSSES ET FLASHS : RÉDUITS" : "SECOUSSES ET FLASHS : NORMAUX";

    public static void Init(GameSettings values, Action save)
    {
        _values = values;
        _save = save;
        Apply();
    }

    /// <summary>Volume suivant : 0, 20, 40... 100 %, puis on revient à 0.</summary>
    public static void CycleMusic()
    {
        _values.MusicVolume = Next(_values.MusicVolume);
        Changed();
    }

    public static void CycleSound()
    {
        _values.SoundVolume = Next(_values.SoundVolume);
        Changed();
        Sfx.Play(Sound.Select);
    }

    public static void ToggleEffects()
    {
        _values.ReduceEffects = !_values.ReduceEffects;
        Changed();
    }

    private static void Changed()
    {
        Apply();
        _save?.Invoke();
    }

    private static void Apply()
    {
        Sfx.MusicVolume = _values.MusicVolume;
        Sfx.SoundVolume = _values.SoundVolume;
    }

    private static double Next(double volume) => volume >= 0.99 ? 0 : Math.Min(1, Math.Round(volume + 0.2, 1));

    private static string Percent(double volume) => volume <= 0 ? "COUPÉE" : $"{Math.Round(volume * 100)} %";
}
