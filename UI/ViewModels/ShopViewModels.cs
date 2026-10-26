using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Media;
using Core.businesses;
using Core.investments;
using Core.simulation;
using Core.staff;
using Core.upgrades;

namespace UI.ViewModels;

/// <summary>Une piste de matériel, de logement ou de locaux à améliorer.</summary>
public class UpgradeViewModel : ViewModelBase
{
    private readonly UpgradeTrack _track;
    private readonly GameSession _session;
    private string _currentName = "";
    private string _levelText = "";
    private string _nextText = "";
    private string _effectText = "";
    private string _costText = "";
    private bool _canBuy;
    private bool _isMaxed;

    public string Title => _track.Info.Title;
    public string CurrentName { get => _currentName; private set => SetField(ref _currentName, value); }
    public string LevelText { get => _levelText; private set => SetField(ref _levelText, value); }
    public string NextText { get => _nextText; private set => SetField(ref _nextText, value); }
    public string EffectText { get => _effectText; private set => SetField(ref _effectText, value); }
    public string CostText { get => _costText; private set => SetField(ref _costText, value); }
    public bool CanBuy { get => _canBuy; private set => SetField(ref _canBuy, value); }
    public bool IsMaxed { get => _isMaxed; private set => SetField(ref _isMaxed, value); }
    public ICommand BuyCommand { get; }

    public UpgradeViewModel(UpgradeTrack track, GameSession session)
    {
        _track = track;
        _session = session;
        BuyCommand = new RelayCommand(() => session.BuyUpgrade(track.Info.Id));
    }

    public void Refresh()
    {
        var current = _track.Current;
        CurrentName = current.Name;
        LevelText = $"Niv. {_track.Level + 1}/{_track.Info.Levels.Count}";
        IsMaxed = _track.IsMaxed;

        if (_track.Next is not { } next)
        {
            NextText = "Niveau maximum";
            EffectText = "";
            CostText = "";
            CanBuy = false;
            return;
        }

        bool isOffice = _track.Info.Id == UpgradeTrackId.Office;
        string effect = isOffice
            ? $"{current.Effect:0} → {next.Effect:0} {_track.Info.EffectLabel}"
            : $"{Display.Multiplier(current.Effect)} → {Display.Multiplier(next.Effect)} {_track.Info.EffectLabel}";
        if (next.MonthlyCost > 0)
            effect += $" · {Display.Money(next.MonthlyCost)}/mois";

        NextText = $"→ {next.Name}";
        EffectText = effect;
        CostText = Display.Money(next.Cost);

        bool allowed = !isOffice || _session.Company.IsFounded;
        CanBuy = allowed && _session.Company.WalletBalance >= next.Cost;
    }
}

/// <summary>Un type d'employé, avec embauche et licenciement.</summary>
public class EmployeeViewModel : ViewModelBase
{
    private readonly EmployeeRoleInfo _info;
    private readonly GameSession _session;
    private string _countText = "";
    private bool _canHire;
    private bool _canFire;

    public string Title => _info.Title;
    public string Description => _info.Description;
    public string CostText => $"Embauche {Display.Money(_info.HiringCost)} · {Display.Money(_info.MonthlySalary)}/mois";
    public string CountText { get => _countText; private set => SetField(ref _countText, value); }
    public bool CanHire { get => _canHire; private set => SetField(ref _canHire, value); }
    public bool CanFire { get => _canFire; private set => SetField(ref _canFire, value); }
    public ICommand HireCommand { get; }
    public ICommand FireCommand { get; }

    public EmployeeViewModel(EmployeeRoleInfo info, GameSession session)
    {
        _info = info;
        _session = session;
        HireCommand = new RelayCommand(() => session.Hire(info.Role));
        FireCommand = new RelayCommand(() => session.Fire(info.Role));
    }

    public void Refresh(bool hiringOpen)
    {
        int count = _session.Staff.Count(_info.Role);
        CountText = count.ToString();
        CanFire = count > 0;
        CanHire = hiringOpen && _session.HasFreeSeat && _session.Company.WalletBalance >= _info.HiringCost;
    }
}

/// <summary>Une boîte à créer ou à développer.</summary>
public class BusinessViewModel : ViewModelBase
{
    private readonly Business _business;
    private readonly GameSession _session;
    private string _levelText = "";
    private string _incomeText = "";
    private string _costText = "";
    private string _buttonText = "";
    private bool _canBuy;
    private bool _isOwned;

    public string Name => _business.Info.Name;
    public string Description => _business.Info.Description;
    public string LevelText { get => _levelText; private set => SetField(ref _levelText, value); }
    public string IncomeText { get => _incomeText; private set => SetField(ref _incomeText, value); }
    public string CostText { get => _costText; private set => SetField(ref _costText, value); }
    public string ButtonText { get => _buttonText; private set => SetField(ref _buttonText, value); }
    public bool CanBuy { get => _canBuy; private set => SetField(ref _canBuy, value); }
    public bool IsOwned { get => _isOwned; private set => SetField(ref _isOwned, value); }
    public ICommand BuyCommand { get; }

    public BusinessViewModel(Business business, int index, GameSession session)
    {
        _business = business;
        _session = session;
        BuyCommand = new RelayCommand(() => session.InvestInBusiness(index));
    }

    public void Refresh(bool unlocked)
    {
        double multiplier = _session.Businesses.HasHolding ? _session.Businesses.HoldingMultiplier : 1;
        double nextIncome = _business.Info.BaseIncomePerSecond * multiplier;

        IsOwned = _business.IsOwned;
        LevelText = _business.IsOwned ? $"Niveau {_business.Level}" : "Pas encore créée";
        IncomeText = _business.IsOwned
            ? $"{Display.Money(_business.IncomePerSecond * multiplier)}/s (+{Display.Money(nextIncome)}/s au niveau suivant)"
            : $"Rapporterait {Display.Money(nextIncome)}/s";
        CostText = Display.Money(_business.NextCost);
        ButtonText = _business.IsOwned ? "Développer" : "Créer";
        CanBuy = unlocked && _session.Company.WalletBalance >= _business.NextCost;
    }
}

/// <summary>Un actif de la bourse, avec son petit graphique de cours.</summary>
public class AssetViewModel : ViewModelBase
{
    public const double ChartWidth = 300;
    public const double ChartHeight = 44;

    private readonly MarketAsset _asset;
    private readonly GameSession _session;
    private string _priceText = "";
    private string _changeText = "";
    private IBrush _changeBrush = Display.Muted;
    private string _holdingText = "";
    private IList<Point> _chart = new List<Point>();
    private bool _canBuy;
    private bool _canSell;
    private double _chartPrice = double.NaN;

    public string Name => _asset.Info.Name;
    public string Description => _asset.Info.Description;
    public string PriceText { get => _priceText; private set => SetField(ref _priceText, value); }
    public string ChangeText { get => _changeText; private set => SetField(ref _changeText, value); }
    public IBrush ChangeBrush { get => _changeBrush; private set => SetField(ref _changeBrush, value); }
    public string HoldingText { get => _holdingText; private set => SetField(ref _holdingText, value); }
    public IList<Point> Chart { get => _chart; private set => SetField(ref _chart, value); }
    public bool CanBuy { get => _canBuy; private set => SetField(ref _canBuy, value); }
    public bool CanSell { get => _canSell; private set => SetField(ref _canSell, value); }

    public ICommand BuyTenPercentCommand { get; }
    public ICommand BuyHalfCommand { get; }
    public ICommand SellAllCommand { get; }

    public AssetViewModel(MarketAsset asset, int index, GameSession session)
    {
        _asset = asset;
        _session = session;
        BuyTenPercentCommand = new RelayCommand(() => Buy(index, 0.1));
        BuyHalfCommand = new RelayCommand(() => Buy(index, 0.5));
        SellAllCommand = new RelayCommand(() => session.SellAsset(index));
    }

    private void Buy(int index, double fraction)
    {
        double amount = Math.Floor(_session.Company.WalletBalance * fraction);
        if (amount > 0)
            _session.BuyAsset(index, amount);
    }

    public void Refresh(bool unlocked)
    {
        PriceText = Display.Money(_asset.Price);
        ChangeText = $"{_asset.Change:+0.0 %;−0.0 %;0.0 %} sur 2 mois";
        ChangeBrush = _asset.Change >= 0 ? Display.Good : Display.Danger;
        HoldingText = _asset.Units > 0 ? $"Tu en détiens pour {Display.Money(_asset.Value)}" : "Tu n'en as pas";
        CanBuy = unlocked && _session.Company.WalletBalance >= 1;
        CanSell = _asset.Units > 0;

        // Le graphique n'est recalculé que quand un nouveau cours arrive
        if (_asset.Price.Equals(_chartPrice))
            return;
        _chartPrice = _asset.Price;
        Chart = BuildChart(_asset.History);
    }

    private static IList<Point> BuildChart(IReadOnlyList<double> history)
    {
        double min = history.Min();
        double max = history.Max();
        double range = Math.Max(max - min, max * 0.001);
        double step = ChartWidth / Math.Max(1, MarketAsset.HistoryLength - 1);
        double offset = ChartWidth - step * (history.Count - 1);

        return history
            .Select((price, i) => new Point(offset + i * step, ChartHeight - (price - min) / range * ChartHeight))
            .ToList();
    }
}
