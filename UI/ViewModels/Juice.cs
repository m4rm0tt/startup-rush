using System;
using Avalonia.Media;

namespace UI.ViewModels;

/// <summary>
/// Les petits effets qui donnent du « feeling » : secousse d'écran et flash de couleur.
/// La fenêtre principale les écoute et les joue.
/// </summary>
public static class Juice
{
    public static event Action<double>? ShakeRequested;
    public static event Action<Color>? FlashRequested;

    /// <param name="strength">Amplitude de départ en pixels.</param>
    public static void Shake(double strength) => ShakeRequested?.Invoke(Settings.ReduceEffects ? strength * 0.25 : strength);

    /// <summary>Un flash bref de couleur, très atténué si le joueur a réduit les effets.</summary>
    public static void Flash(Color color) => FlashRequested?.Invoke(color);
}
