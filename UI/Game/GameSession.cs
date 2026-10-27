using System;
using System.Collections.Generic;
using Core.campaign;
using Core.persistence;
using UI.Audio;

namespace UI.Game;

/// <summary>Ce qui est sélectionné dans la scène.</summary>
public enum SelectionKind { None, Item, Person }

/// <summary>L'outil d'aménagement actif : acheter un modèle, poser un objet des cartons, ou déplacer un objet.</summary>
public record BuildTool(string DefId, int? StoredId = null, int? MovingItemId = null);

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

    public void TogglePause()
    {
        PausedByPlayer = !PausedByPlayer;
        Sfx.Play(PausedByPlayer ? Sound.PauseIn : Sound.PauseOut);
    }

    /// <summary>Fait avancer le temps de jeu selon le temps réel écoulé (plafonné pour ne jamais rattraper une longue absence).</summary>
    public void Update(double realSeconds)
    {
        realSeconds = Math.Min(realSeconds, 0.1);
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
        SelectionChanged?.Invoke();
    }

    public void ClearSelection() => Select(SelectionKind.None, 0);

    public void SetTool(BuildTool? tool)
    {
        Tool = tool;
        if (tool != null)
            BuildMode = true;
        SelectionChanged?.Invoke();
    }
}
