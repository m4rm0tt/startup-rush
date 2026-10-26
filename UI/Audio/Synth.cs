using System;
using System.Collections.Generic;

namespace UI.Audio;

/// <summary>
/// Synthétiseur chiptune : fabrique tous les sons et la musique du jeu à partir d'ondes simples
/// (carré, triangle, bruit). Aucun fichier audio, donc aucune question de licence.
/// </summary>
public static class Synth
{
    public const int Rate = 44100;

    private enum Wave { Square, Pulse, Triangle, Noise }

    private static readonly Random Noise = new(7);

    /// <summary>Une note : fréquence de départ et d'arrivée (glissando), durée, forme d'onde, volume.</summary>
    private record Note(double From, double To, double Duration, Wave Wave = Wave.Square, double Volume = 1, double Start = 0);

    private static float[] Render(params Note[] notes)
    {
        double length = 0;
        foreach (var note in notes)
            length = Math.Max(length, note.Start + note.Duration);

        var buffer = new float[(int)(length * Rate) + 1];
        foreach (var note in notes)
            AddNote(buffer, note);
        return buffer;
    }

    private static void AddNote(float[] buffer, Note note)
    {
        int start = (int)(note.Start * Rate);
        int count = (int)(note.Duration * Rate);
        double phase = 0;
        for (int i = 0; i < count && start + i < buffer.Length; i++)
        {
            double t = (double)i / count;
            double frequency = note.From + (note.To - note.From) * t;
            phase += frequency / Rate;
            double p = phase - Math.Floor(phase);

            double sample = note.Wave switch
            {
                Wave.Square => p < 0.5 ? 1 : -1,
                Wave.Pulse => p < 0.125 ? 1 : -1,
                Wave.Triangle => 4 * Math.Abs(p - 0.5) - 1,
                _ => Noise.NextDouble() * 2 - 1
            };

            // Enveloppe : attaque très courte, puis décroissance
            double attack = Math.Min(1, i / (0.004 * Rate));
            double envelope = attack * Math.Pow(1 - t, 1.6);
            buffer[start + i] += (float)(sample * envelope * note.Volume * 0.5);
        }
    }

    private static double Midi(int note) => 440 * Math.Pow(2, (note - 69) / 12.0);

    public static Dictionary<Sound, float[]> BuildSounds() => new()
    {
        [Sound.Click] = Render(new Note(900, 500, 0.035, Wave.Square, 0.45)),
        [Sound.Hover] = Render(new Note(2200, 2000, 0.012, Wave.Square, 0.12)),
        [Sound.Select] = Render(new Note(660, 660, 0.05), new Note(990, 990, 0.08, Start: 0.05)),
        [Sound.Coin] = Render(new Note(988, 988, 0.06), new Note(1319, 1319, 0.22, Start: 0.06)),
        [Sound.Cash] = Render(
            new Note(Midi(84), Midi(84), 0.07), new Note(Midi(88), Midi(88), 0.07, Start: 0.06),
            new Note(Midi(91), Midi(91), 0.07, Start: 0.12), new Note(Midi(96), Midi(96), 0.3, Start: 0.18),
            new Note(4000, 1000, 0.08, Wave.Noise, 0.3, 0.18)),
        [Sound.Buy] = Render(new Note(523, 523, 0.06, Wave.Triangle), new Note(784, 784, 0.06, Wave.Triangle, Start: 0.06),
            new Note(1047, 1047, 0.14, Wave.Square, 0.6, 0.12)),
        [Sound.Error] = Render(new Note(160, 140, 0.18, Wave.Square, 0.7)),
        [Sound.Card] = Render(new Note(3000, 600, 0.12, Wave.Noise, 0.5), new Note(400, 800, 0.08, Wave.Triangle, 0.4)),
        [Sound.Crit] = Render(new Note(1800, 2600, 0.06), new Note(2600, 3400, 0.12, Wave.Pulse, 0.7, 0.05)),
        [Sound.LevelUp] = Render(
            new Note(Midi(72), Midi(72), 0.08, Wave.Square, 0.8), new Note(Midi(76), Midi(76), 0.08, Start: 0.08),
            new Note(Midi(79), Midi(79), 0.08, Start: 0.16), new Note(Midi(84), Midi(84), 0.08, Start: 0.24),
            new Note(Midi(88), Midi(88), 0.4, Wave.Square, 0.9, 0.32), new Note(Midi(76), Midi(76), 0.4, Wave.Triangle, 1, 0.32)),
        [Sound.Alarm] = Render(
            new Note(740, 520, 0.22), new Note(740, 520, 0.22, Start: 0.25), new Note(740, 520, 0.22, Start: 0.5),
            new Note(740, 520, 0.3, Start: 0.75)),
        [Sound.Unpaid] = Render(
            new Note(Midi(55), Midi(55), 0.2, Wave.Square, 0.8), new Note(Midi(54), Midi(54), 0.2, Start: 0.22),
            new Note(Midi(53), Midi(53), 0.2, Start: 0.44), new Note(Midi(52), Midi(49), 0.7, Wave.Square, 0.9, 0.66)),
        [Sound.Event] = Render(new Note(Midi(76), Midi(76), 0.1, Wave.Triangle), new Note(Midi(81), Midi(81), 0.25, Wave.Triangle, Start: 0.1)),
        [Sound.Victory] = Render(
            new Note(Midi(72), Midi(72), 0.15), new Note(Midi(72), Midi(72), 0.15, Start: 0.17),
            new Note(Midi(72), Midi(72), 0.15, Start: 0.34), new Note(Midi(77), Midi(77), 0.5, Start: 0.51),
            new Note(Midi(81), Midi(81), 0.25, Start: 1.0), new Note(Midi(84), Midi(84), 0.9, Wave.Square, 1, 1.25),
            new Note(Midi(60), Midi(60), 2.1, Wave.Triangle, 1)),
        [Sound.Defeat] = Render(
            new Note(Midi(67), Midi(67), 0.3, Wave.Triangle), new Note(Midi(63), Midi(63), 0.3, Wave.Triangle, Start: 0.32),
            new Note(Midi(60), Midi(60), 0.3, Wave.Triangle, Start: 0.64), new Note(Midi(55), Midi(43), 1.2, Wave.Square, 0.6, 0.96)),
    };

    /// <summary>
    /// Une boucle lo-fi chiptune de 8 mesures (la mineur, fa, do, sol) à 96 BPM :
    /// basse au triangle, arpèges en impulsion fine, charleston au bruit.
    /// </summary>
    public static float[] BuildMusic()
    {
        const double beat = 60.0 / 96;
        int[][] chords = [[57, 60, 64], [53, 57, 60], [48, 52, 55], [55, 59, 62]]; // Am F C G
        int bars = 8;
        var notes = new List<Note>();

        for (int bar = 0; bar < bars; bar++)
        {
            var chord = chords[bar % chords.Length];
            double barStart = bar * 4 * beat;

            // Basse : la fondamentale en croches, une octave plus bas
            for (int i = 0; i < 8; i++)
            {
                int root = chord[0] - 12 + (i % 4 == 3 ? 7 : 0);
                notes.Add(new Note(Midi(root), Midi(root), beat / 2 * 0.9, Wave.Triangle, 0.9, barStart + i * beat / 2));
            }

            // Arpège en doubles croches, qui monte puis descend
            int[] pattern = [0, 1, 2, 1, 0, 1, 2, 1, 0, 1, 2, 1, 0, 2, 1, 2];
            for (int i = 0; i < 16; i++)
            {
                int pitch = chord[pattern[i]] + 12 + (bar >= 4 && i % 8 == 6 ? 12 : 0);
                notes.Add(new Note(Midi(pitch), Midi(pitch), beat / 4 * 0.8, Wave.Pulse, 0.28, barStart + i * beat / 4));
            }

            // Charleston sur les contretemps, grosse caisse au bruit grave sur les temps forts
            for (int i = 0; i < 4; i++)
            {
                notes.Add(new Note(9000, 7000, 0.03, Wave.Noise, 0.18, barStart + i * beat + beat / 2));
                if (i % 2 == 0)
                    notes.Add(new Note(120, 40, 0.12, Wave.Square, 0.5, barStart + i * beat));
            }
        }

        var buffer = Render(notes.ToArray());
        int loopLength = (int)(bars * 4 * beat * Rate);
        Array.Resize(ref buffer, loopLength);
        return buffer;
    }
}

public enum Sound
{
    Click,
    Hover,
    Select,
    Coin,
    Cash,
    Buy,
    Error,
    Card,
    Crit,
    LevelUp,
    Alarm,
    Unpaid,
    Event,
    Victory,
    Defeat
}
