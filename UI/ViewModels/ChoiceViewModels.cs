using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Core.roguelike;
using Core.simulation;
using UI.Audio;
using UI.Pixel;

namespace UI.ViewModels;

/// <summary>Une carte d'atout, à choisir dans un tirage ou affichée dans le HUD.</summary>
public class PerkCardViewModel
{
    public Perk Perk { get; }
    public string Name => Perk.Name;
    public string Description => Perk.Description;
    public string RarityText => Palette.RarityName(Perk.Rarity);
    public IBrush RarityBrush => Palette.Rarity(Perk.Rarity);
    public Bitmap Icon => Icons.Get(Perk.Icon);
    public string KeyHint { get; }
    public string Tooltip => $"{Perk.Name} ({RarityText.ToLower()})\n{Perk.Description}";
    public ICommand? ChooseCommand { get; }

    public PerkCardViewModel(Perk perk, int index = -1, GameSession? session = null)
    {
        Perk = perk;
        KeyHint = index >= 0 ? $"[{index + 1}]" : "";
        if (session != null)
            ChooseCommand = new RelayCommand(() =>
            {
                if (session.ChoosePerk(index))
                    Sfx.Play(Sound.Select);
            });
    }
}

/// <summary>Un tirage d'atouts en cours : le jeu est en pause tant que le joueur n'a pas choisi.</summary>
public class DraftViewModel : ViewModelBase
{
    private readonly Draft _draft;
    private readonly GameSession _session;
    private IReadOnlyList<PerkCardViewModel> _cards = [];
    private string _rerollText = "";
    private bool _canReroll;

    public string Title => _draft.Title.ToUpper();
    public string Subtitle { get; }
    public IReadOnlyList<PerkCardViewModel> Cards { get => _cards; private set => SetField(ref _cards, value); }
    public string RerollText { get => _rerollText; private set => SetField(ref _rerollText, value); }
    public bool CanReroll { get => _canReroll; private set => SetField(ref _canReroll, value); }
    public string SkipText => $"PASSER  +{Display.Money(_draft.SkipReward)}";
    public ICommand RerollCommand { get; }
    public ICommand SkipCommand { get; }
    public Draft Draft => _draft;

    public DraftViewModel(Draft draft, GameSession session, string subtitle)
    {
        _draft = draft;
        _session = session;
        Subtitle = subtitle;
        RerollCommand = new RelayCommand(Reroll);
        SkipCommand = new RelayCommand(() =>
        {
            if (session.SkipDraft())
                Sfx.Play(Sound.Coin);
        });
        RefreshCards();
    }

    public void Reroll()
    {
        if (_session.RerollDraft())
        {
            Sfx.Play(Sound.Card);
            RefreshCards();
        }
        else
        {
            Sfx.Play(Sound.Error);
        }
    }

    public void Refresh()
    {
        RerollText = _draft.RerollCost == 0
            ? $"RELANCER (gratuit, {_draft.FreeRerollsLeft})"
            : $"RELANCER  {Display.Money(_draft.RerollCost)}";
        CanReroll = _session.Company.WalletBalance >= _draft.RerollCost;
    }

    private void RefreshCards()
    {
        Cards = _draft.Options.Select((perk, i) => new PerkCardViewModel(perk, i, _session)).ToList();
        Refresh();
    }
}

public class EventOptionViewModel
{
    public string Label { get; }
    public string Hint { get; }
    public string KeyHint { get; }
    public bool IsEnabled { get; }
    public ICommand ChooseCommand { get; }

    public EventOptionViewModel(EventOption option, int index, bool enabled, GameSession session)
    {
        Label = option.Label;
        Hint = enabled ? option.Hint : $"{option.Hint} (pas assez d'argent)";
        KeyHint = $"[{index + 1}]";
        IsEnabled = enabled;
        ChooseCommand = new RelayCommand(() =>
        {
            if (session.ChooseEventOption(index))
                Sfx.Play(Sound.Select);
        });
    }
}

/// <summary>Un événement à choix : on lit, on choisit, on lit le résultat, on continue.</summary>
public class EventViewModel : ViewModelBase
{
    private readonly GameEvent _event;
    private bool _isResolved;
    private string _outcome = "";

    public GameEvent Event => _event;
    public string Title => _event.Info.Title.ToUpper();
    public string Text => _event.Info.Text;
    public Bitmap Icon => Icons.Get(_event.Info.Icon);
    public IReadOnlyList<EventOptionViewModel> Options { get; }
    public bool IsResolved { get => _isResolved; private set => SetField(ref _isResolved, value); }
    public string Outcome { get => _outcome; private set => SetField(ref _outcome, value); }
    public ICommand ContinueCommand { get; }

    public EventViewModel(GameEvent gameEvent, GameSession session)
    {
        _event = gameEvent;
        Options = gameEvent.Info.Options
            .Select((option, i) => new EventOptionViewModel(option, i, session.CanChooseOption(gameEvent, i), session))
            .ToList();
        ContinueCommand = new RelayCommand(() => session.ConfirmEvent());
    }

    public void Refresh()
    {
        IsResolved = _event.IsResolved;
        Outcome = _event.Outcome ?? "";
    }
}

/// <summary>Un effet temporaire (crise, conséquence d'événement) affiché dans le HUD.</summary>
public record EffectViewModel(string Name, string Description, string MonthsText, IBrush Brush);
