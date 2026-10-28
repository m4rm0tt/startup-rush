using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Core.space;
using Core.team;
using UI.Iso;
using UI.Pixel;

namespace UI.Scene;

/// <summary>
/// Effets de scène du pack (vfx_*) et curseurs (cursor_*). Les cases de placement, de survol et de sélection sont des sprites
/// isométriques ; les petites particules naissent d'événements réels (travail en cours, tâche finie, moral en hausse, revenus
/// du jour, objet posé) et ne changent rien à la simulation. « Réduire les effets » coupe les particules et les pulsations.
/// </summary>
public partial class SceneCanvas
{
    private readonly record struct Particle(string Sprite, double X, double Y, double Born, double Life, double Rise, bool Blink);

    private readonly List<Particle> _particles = new();
    private readonly Dictionary<int, (double Morale, int? Work, double NextPuff)> _watched = new();
    private long _effectsDay = -1;
    private HitRegion? _hoverHit;
    private bool _placementOk;
    private string _cursorName = "";

    private bool Effects => !_session.Settings.ReduceEffects;

    /// <summary>Survol et case de placement : opacité 1 ↔ 0,7 en une seconde (fixe si les effets sont réduits).</summary>
    private double Pulse => Effects ? 0.85 + 0.15 * Math.Cos(_clock * Math.PI * 2) : 1;

    private static readonly Dictionary<string, (int X, int Y)> Anchors = new();

    private static (int X, int Y) AnchorOf(string id)
    {
        if (!Anchors.TryGetValue(id, out var anchor))
        {
            var a = PackLibrary.Asset(id).GetProperty("anchor");
            Anchors[id] = anchor = (a[0].GetInt32(), a[1].GetInt32());
        }
        return anchor;
    }

    /// <summary>Un sprite d'effet du pack, son ancre posée sur un point de la pièce, à l'échelle de la scène.</summary>
    private void DrawEffect(DrawingContext context, string id, double x, double y, double opacity = 1, double grow = 1)
    {
        var bitmap = PackSkins.Sprite(id);
        var (ax, ay) = AnchorOf(id);
        double w = bitmap.PixelSize.Width, h = bitmap.PixelSize.Height;
        var at = ToScreen(x - ax * grow, y - ay * grow);
        using (context.PushOpacity(opacity))
            context.DrawImage(bitmap, new Rect(0, 0, w, h), new Rect(at.X, at.Y, w * Scale * grow, h * Scale * grow));
    }

    /// <summary>Une case isométrique du pack (64×32, ancrée sur son coin du haut).</summary>
    private void DrawTile(DrawingContext context, Cell cell, string id, double opacity = 1)
    {
        var (tx, ty) = Tile(cell.X, cell.Y);
        DrawEffect(context, id, tx, ty, opacity);
    }

    /// <summary>Les pieds d'une personne : le bas de sa zone cliquable, au milieu.</summary>
    private static (double X, double Y) Feet(HitRegion hit) => (hit.Polygon.Average(p => p.X), hit.Polygon.Max(p => p.Y));

    private void Spawn(string id, double x, double y, double life, double rise, bool blink = false)
    {
        if (Effects && _particles.Count < 60)
            _particles.Add(new Particle(id, x, y, _clock, life, rise, blink));
    }

    private void DrawParticles(DrawingContext context)
    {
        _particles.RemoveAll(p => _clock - p.Born > p.Life);
        foreach (var p in _particles)
        {
            double t = (_clock - p.Born) / p.Life;
            // Monter puis s'effacer sur le dernier tiers ; les étincelles clignotent (pas de rotation en pixel art)
            double opacity = p.Blink ? ((int)(t * 6) % 2 == 0 ? 1 : 0.3) : t > 0.65 ? (1 - t) / 0.35 : 1;
            DrawEffect(context, p.Sprite, p.X, p.Y - p.Rise * t, opacity);
        }
    }

    /// <summary>
    /// Ce qui arrive à une personne depuis l'image précédente : moral en hausse (cœur), tâche terminée (coche), travail en
    /// cours (bouffée de code, d'idée ou de travail toutes les 2 à 4 s).
    /// </summary>
    private void Watch(Person person, double x, double y)
    {
        double head = y - 24;
        if (!_watched.TryGetValue(person.Id, out var seen))
        {
            _watched[person.Id] = (person.Morale, person.CurrentWorkItemId, _clock + 1 + person.Id % 3);
            return;
        }
        if (person.Morale >= seen.Morale + 1)
            Spawn("vfx_heart_morale", x + 10, head, 1.0, 10);
        if (seen.Work is int done && person.CurrentWorkItemId != done && Campaign.FindWork(done) == null)
            Spawn("vfx_success", x, head - 6, 0.8, 12);
        double next = seen.NextPuff;
        if (person.Activity == Activity.Working && _clock >= next)
        {
            string puff = person.Assignment.Kind == AssignmentKind.Research ? "vfx_idea"
                : CurrentDiscipline(person) is Discipline.Design or Discipline.Test or null ? "vfx_work" : "vfx_code";
            Spawn(puff, x - 12, head, 0.9, 10);
            next = _clock + 2 + person.Id * 7 % 3;
        }
        _watched[person.Id] = (person.Morale, person.CurrentWorkItemId, next);
    }

    /// <summary>Chaque nouveau jour avec des revenus : quelques pièces montent au-dessus de ceux qui travaillent.</summary>
    private void WatchDay(List<(double X, double Y)> heads)
    {
        if (Campaign.Day == _effectsDay)
            return;
        bool first = _effectsDay < 0;
        _effectsDay = Campaign.Day;
        if (first || Campaign.Products.Sum(p => p.History.LastOrDefault()?.Revenue ?? 0) <= 0)
            return;
        foreach (var (x, y) in heads.Take(3))
            Spawn("vfx_coin", x + 6, y - 30, 1.0, 14);
    }

    /// <summary>Un objet vient d'être posé : grande étincelle au centre, deux petites qui clignotent autour.</summary>
    private void Sparkle(Cell cell)
    {
        var (x, y) = Tile(cell.X + 0.5, cell.Y + 0.5);
        Spawn("vfx_sparkle_large", x, y - 18, 0.6, 0);
        Spawn("vfx_sparkle_small", x - 14, y - 26, 0.5, 0, blink: true);
        Spawn("vfx_sparkle_small", x + 12, y - 32, 0.5, 0, blink: true);
    }

    // ------------------------------------------------------------------ Curseurs

    /// <summary>Un curseur du pack (cache commun avec le reste de l'interface).</summary>
    public static Cursor PackCursor(string id) => PackSkins.Cursor(id);

    /// <summary>Le curseur selon ce que ferait un clic : poser (ici c'est possible ou non), déplacer, choisir, ou rien.</summary>
    private void UpdateCursor()
    {
        string id = _session.Tool is { } tool
            ? tool.MovingItemId != null ? "cursor_move" : _placementOk ? "cursor_place_valid" : "cursor_place_invalid"
            : _hoverHit != null ? "cursor_select" : "cursor_normal";
        if (id == _cursorName)
            return;
        _cursorName = id;
        Cursor = PackCursor(id);
    }
}
