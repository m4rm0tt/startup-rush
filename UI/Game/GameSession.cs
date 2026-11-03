using System;
using System.Collections.Generic;
using System.Linq;
using Core.campaign;
using Core.persistence;
using UI.Audio;

namespace UI.Game;

/// <summary>Ce qui est sélectionné dans la scène.</summary>
public enum SelectionKind { None, Item, Person }

/// <summary>L'outil d'aménagement actif : acheter un modèle, poser un objet des cartons, ou déplacer un objet, dans une orientation.</summary>
public record BuildTool(string DefId, int? StoredId = null, int? MovingItemId = null, string? Facing = null);

/// <summary>
/// Outil de finitions en cours : la finition choisie, sa couleur, et où la poser. Sol : « all » (tout le local, aperçu tout de
/// suite), « zone » ou « tile » (aperçu sur la case survolée, clic pour poser). Mur : « both » (aperçu tout de suite) ou
/// « pick » (survoler un mur du fond, clic pour poser).
/// </summary>
public record FinishTool(string FinishId, string Mode, string? Color = null)
{
    public Core.space.FinishDef Def => Core.space.Finishes.Get(FinishId);

    /// <summary>L'aperçu se fait sur toute la cible sans survol (tout le sol, les deux murs).</summary>
    public bool Immediate => Mode is "all" or "both";

    /// <summary>La cible de l'outil pour la case survolée (null : rien à montrer ni à poser).</summary>
    public Core.space.FinishTarget? Target(Core.space.Premises premises, Core.space.Cell? hover)
    {
        switch (Mode)
        {
            case "all" or "both":
                return Core.space.FinishTarget.Everywhere;
            case "left" or "right":
                return new Core.space.FinishTarget(Mode);
        }
        if (hover is not { } cell || !premises.InBounds(cell))
            return null;
        return Mode switch
        {
            "zone" => Core.space.Finishes.ZoneAt(premises, cell),
            "tile" => Core.space.FinishTarget.Rect(cell.X, cell.Y, 1, 1),
            // Un mur du fond se vise en survolant une case qui le longe (comme pour accrocher un objet au mur)
            "pick" when cell.X == 0 && cell.Y == 0 => null,
            "pick" when cell.X == 0 => new Core.space.FinishTarget("left"),
            "pick" when cell.Y == 0 => new Core.space.FinishTarget("right"),
            _ => null,
        };
    }
}

/// <summary>
/// La partie en cours côté interface : la campagne, le rythme du temps, les sauvegardes automatiques,
/// la sélection et l'outil d'aménagement. Le temps avance au rythme réel mesuré, jamais au rythme des images.
/// </summary>
public class GameSession
{
    /// <summary>Minutes de jeu par seconde réelle, pour chaque vitesse (0 = pause).</summary>
    public static readonly double[] Speeds = [0, 5, 20, 60, 180];

    /// <summary>Accélération des nuits et week-ends quand personne n'est au bureau.</summary>
    public const double NightBoost = 8;

    public Campaign Campaign { get; }
    public SaveStore Store { get; }
    public Settings Settings { get; }

    /// <summary>Vitesse choisie par le joueur (1 à 4) ; la pause est séparée pour pouvoir la reprendre.</summary>
    public int Speed { get; private set; } = 1;

    public bool PausedByPlayer { get; private set; }

    /// <summary>Raison d'une pause automatique (menu de pause, grand menu, confirmation), ou null.</summary>
    public string? AutoPause { get; set; }

    public bool Running => !PausedByPlayer && AutoPause == null && Campaign.Status == CampaignStatus.Running;

    /// <summary>Fraction de la minute de jeu en cours (pour faire glisser les personnages entre deux cases).</summary>
    public double SubMinute { get; private set; }

    public SelectionKind SelectionKind { get; private set; }
    public int SelectionId { get; private set; }
    public BuildTool? Tool { get; private set; }

    /// <summary>L'outil de finitions (sol, murs) en cours, ou null. Exclusif avec <see cref="Tool"/>.</summary>
    public FinishTool? Finish { get; private set; }

    public void SetFinish(FinishTool? tool)
    {
        Finish = tool;
        if (tool != null)
            Tool = null;
    }

    public bool BuildMode { get; set; }

    /// <summary>Notifications arrivées depuis la dernière image (affichées en bulles).</summary>
    public Queue<Notice> NewNotices { get; } = new();

    public string? LastSavePath { get; private set; }
    public string LastSaveMessage { get; private set; } = "";

    public event Action? SelectionChanged;

    private double _accumulator;

    public GameSession(Campaign campaign, SaveStore store, Settings settings, string? savePath = null)
    {
        Campaign = campaign;
        Store = store;
        Settings = settings;
        LastSavePath = savePath;
        campaign.Noticed += OnNotice;
    }

    public void Detach() => Campaign.Noticed -= OnNotice;

    private void OnNotice(Notice notice)
    {
        NewNotices.Enqueue(notice);
        while (NewNotices.Count > 20)
            NewNotices.Dequeue();
        Sfx.Play(notice.Severity switch
        {
            Severity.Critical => notice.Title.StartsWith("Impayé") ? Sound.Unpaid : Sound.Alarm,
            Severity.Warning => Sound.Event,
            Severity.Good => notice.Title.StartsWith("Jalon") ? Sound.Milestone
                : notice.Title.Contains("en ligne") || notice.Title.Contains("publiée") ? Sound.Launch
                : notice.Title.StartsWith("Livré") ? Sound.Coin : Sound.Notify,
            _ => Sound.Notify
        });
        if (notice.Title == "Entreprise liquidée")
            Sfx.Play(Sound.Defeat);
    }

    public void SetSpeed(int speed)
    {
        Speed = Math.Clamp(speed, 1, Speeds.Length - 1);
        PausedByPlayer = false;
    }

    /// <summary>Remet exactement la vitesse et la pause d'avant (fermeture du terminal).</summary>
    public void Restore(int speed, bool paused)
    {
        Speed = Math.Clamp(speed, 1, Speeds.Length - 1);
        PausedByPlayer = paused;
    }

    public void TogglePause()
    {
        PausedByPlayer = !PausedByPlayer;
        Sfx.Play(PausedByPlayer ? Sound.PauseIn : Sound.PauseOut);
    }

    /// <summary>Fait avancer le temps de jeu selon le temps réel écoulé (plafonné pour ne jamais rattraper une longue absence).</summary>
    public void Update(double realSeconds)
    {
        realSeconds = Math.Min(realSeconds, 0.1);
        // Même en pause : un poste qu'on vient de compléter est occupé tout de suite (l'alerte devant la porte disparaît)
        Campaign.SettlePlaces();
        if (!Running)
            return;

        double rate = Speeds[Speed];
        if (Settings.FastNights && !Campaign.AnyoneInOffice)
            rate *= NightBoost;
        _accumulator += realSeconds * rate;
        long minutes = (long)Math.Floor(_accumulator);
        _accumulator -= minutes;
        SubMinute = _accumulator;
        if (minutes > 0)
        {
            decimal cash = Campaign.Ledger.Cash;
            Campaign.Advance(minutes);
            if (Campaign.Ledger.Cash - cash >= 500)
                Sfx.Play(Sound.Cash);
            else if (Campaign.Ledger.Cash - cash <= -500)
                Sfx.Play(Sound.Bill);
        }

        if (Campaign.AutosaveDue)
        {
            Campaign.AutosaveDue = false;
            Save(SaveKind.Auto);
        }
    }

    public bool Save(SaveKind kind, string? label = null)
    {
        try
        {
            LastSavePath = Store.Save(Campaign, kind, label);
            LastSaveMessage = kind == SaveKind.Auto ? $"Sauvegarde automatique ({GameTime.FormatDate(Campaign.Minute)})" : "Partie sauvegardée";
            return true;
        }
        catch (Exception e) when (e is System.IO.IOException or UnauthorizedAccessException)
        {
            LastSaveMessage = $"Sauvegarde impossible : {e.Message}";
            return false;
        }
    }

    public void Select(SelectionKind kind, int id)
    {
        SelectionKind = kind;
        SelectionId = id;
        if (kind == SelectionKind.Person && Campaign.FindPerson(id) is { IsFounder: true })
            Campaign.TutorialEvent("select:founder");
        SelectionChanged?.Invoke();
    }

    public void ClearSelection() => Select(SelectionKind.None, 0);

    /// <summary>Tourne l'objet à poser vers son orientation suivante (touche R).</summary>
    public void RotateTool()
    {
        if (Tool == null)
            return;
        var facings = Core.space.FurnitureCatalog.Get(Tool.DefId).Pack.Facings.Keys.ToList();
        int index = facings.IndexOf(Tool.Facing ?? facings[0]);
        SetTool(Tool with { Facing = facings[(index + 1) % facings.Count] });
    }

    public void SetTool(BuildTool? tool)
    {
        Tool = tool;
        if (tool != null)
        {
            BuildMode = true;
            Finish = null;
        }
        SelectionChanged?.Invoke();
    }
}
