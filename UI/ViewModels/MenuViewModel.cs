using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Core.meta;
using Core.roguelike;
using Core.states;
using UI.Audio;
using UI.Pixel;

namespace UI.ViewModels;

public class CharacterCardViewModel : ViewModelBase
{
    private readonly MenuViewModel _menu;

    public Character Character { get; }
    public string Name => Character.Name.ToUpper();
    public string Title => Character.Title;
    public string Description => Character.Description;
    public string Trait => Character.TraitText;
    public Bitmap Portrait => Portraits.Get(Character.Id);
    public bool IsUnlocked { get => Get<bool>(); private set => Set(value); }
    public bool IsLocked => !IsUnlocked;
    public bool IsSelected { get => Get<bool>(); set => Set(value); }
    public IBrush FrameBrush => IsSelected ? Palette.Gold : Palette.Steel;
    public string UnlockText => $"DÉBLOQUER  {Character.UnlockCost} LINGOTS";
    public bool CanUnlock { get => Get<bool>(); private set => Set(value); }
    public ICommand SelectCommand { get; }
    public ICommand UnlockCommand { get; }

    public CharacterCardViewModel(Character character, MenuViewModel menu)
    {
        Character = character;
        _menu = menu;
        SelectCommand = new RelayCommand(() => menu.Select(this));
        UnlockCommand = new RelayCommand(() => menu.Unlock(this));
    }

    public void Refresh(Profile profile)
    {
        IsUnlocked = Legacy.IsUnlocked(profile, Character);
        CanUnlock = !IsUnlocked && profile.Lingots >= Character.UnlockCost;
        OnPropertyChanged(nameof(IsLocked));
        OnPropertyChanged(nameof(FrameBrush));
    }
}

public class LegacyUpgradeViewModel : ViewModelBase
{
    private readonly LegacyUpgrade _upgrade;

    public string Name => _upgrade.Name.ToUpper();
    public string Description => _upgrade.Description;
    public string Pips { get => Get<string>(); private set => Set(value); }
    public string CostText { get => Get<string>(); private set => Set(value); }
    public bool CanBuy { get => Get<bool>(); private set => Set(value); }
    public ICommand BuyCommand { get; }

    public LegacyUpgradeViewModel(LegacyUpgrade upgrade, MenuViewModel menu)
    {
        _upgrade = upgrade;
        BuyCommand = new RelayCommand(() => menu.BuyLegacy(upgrade.Id));
    }

    public void Refresh(Profile profile)
    {
        int level = Legacy.Level(profile, _upgrade.Id);
        Pips = new string('■', level) + new string('□', _upgrade.Costs.Count - level);
        var cost = Legacy.NextCost(profile, _upgrade);
        CostText = cost is { } c ? $"{c} LINGOTS" : "MAXIMUM";
        CanBuy = cost is { } price && profile.Lingots >= price;
    }
}

/// <summary>Écran titre, choix du personnage et Héritage.</summary>
public class MenuViewModel : ViewModelBase
{
    private readonly MenuState _state;
    private CharacterCardViewModel _selected;
    private int _pressure;

    public Bitmap GoldIcon => Icons.Get("gold");
    public IReadOnlyList<CharacterCardViewModel> Characters { get; }
    public IReadOnlyList<LegacyUpgradeViewModel> LegacyUpgrades { get; }

    public string Screen { get => Get<string>(); private set => Set(value); }
    public bool IsTitle => Screen == "title";
    public bool IsSelect => Screen == "select";
    public bool IsLegacy => Screen == "legacy";

    public string LingotsText { get => Get<string>(); private set => Set(value); }
    public string StatsText { get => Get<string>(); private set => Set(value); }
    public string PressureName { get => Get<string>(); private set => Set(value); }
    public string PressureDescription { get => Get<string>(); private set => Set(value); }
    public bool CanStart { get => Get<bool>(); private set => Set(value); }
    public string StartText { get => Get<string>(); private set => Set(value); }

    public ICommand PlayCommand { get; }
    public ICommand LegacyCommand { get; }
    public ICommand BackCommand { get; }
    public ICommand StartCommand { get; }
    public ICommand PressureUpCommand { get; }
    public ICommand PressureDownCommand { get; }

    public MenuViewModel(MenuState state)
    {
        _state = state;
        Characters = CharacterCatalog.All.Select(c => new CharacterCardViewModel(c, this)).ToList();
        LegacyUpgrades = Legacy.Upgrades.Select(u => new LegacyUpgradeViewModel(u, this)).ToList();
        _selected = Characters[0];
        _selected.IsSelected = true;

        PlayCommand = new RelayCommand(() => Show("select"));
        LegacyCommand = new RelayCommand(() => Show("legacy"));
        BackCommand = new RelayCommand(() => Show("title"));
        StartCommand = new RelayCommand(Start);
        PressureUpCommand = new RelayCommand(() => ChangePressure(1));
        PressureDownCommand = new RelayCommand(() => ChangePressure(-1));

        Screen = "title";
        Refresh();
    }

    public void Show(string screen)
    {
        Screen = screen;
        Sfx.Play(Sound.Select);
        OnPropertyChanged(nameof(IsTitle));
        OnPropertyChanged(nameof(IsSelect));
        OnPropertyChanged(nameof(IsLegacy));
        Refresh();
    }

    public void Select(CharacterCardViewModel card)
    {
        _selected.IsSelected = false;
        _selected = card;
        card.IsSelected = true;
        Sfx.Play(Sound.Card);
        Refresh();
    }

    public void Unlock(CharacterCardViewModel card)
    {
        Sfx.Play(_state.UnlockCharacter(card.Character) ? Sound.Cash : Sound.Error);
        Refresh();
    }

    public void BuyLegacy(string id)
    {
        Sfx.Play(_state.BuyLegacyUpgrade(id) ? Sound.Cash : Sound.Error);
        Refresh();
    }

    public void Start()
    {
        if (!_state.StartRun(_selected.Character, _pressure))
            Sfx.Play(Sound.Error);
    }

    private void ChangePressure(int step)
    {
        int next = _pressure + step;
        if (next < 0 || next > _state.Profile.MaxPressureUnlocked)
        {
            Sfx.Play(Sound.Error);
            return;
        }

        _pressure = next;
        Sfx.Play(Sound.Click);
        Refresh();
    }

    public void Refresh()
    {
        var profile = _state.Profile;
        foreach (var card in Characters)
            card.Refresh(profile);
        foreach (var upgrade in LegacyUpgrades)
            upgrade.Refresh(profile);

        LingotsText = $"{profile.Lingots}";
        StatsText = profile.Runs == 0
            ? "Première run. Bonne chance."
            : $"{profile.Runs} runs · {profile.Wins} victoires · record {Display.Money(profile.BestNetWorth)}";

        var pressure = PressureCatalog.All[_pressure];
        PressureName = $"PRESSION {pressure.Level} : {pressure.Name.ToUpper()}";
        PressureDescription = pressure.Description +
                              (profile.MaxPressureUnlocked > _pressure ? "" : "  (gagne une run pour débloquer la suivante)");

        CanStart = _selected.IsUnlocked;
        StartText = _selected.IsUnlocked ? $"LANCER LA RUN AVEC {_selected.Name}" : "PERSONNAGE VERROUILLÉ";
    }
}
