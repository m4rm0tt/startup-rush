using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Avalonia.Media;
using Core.finance;
using Core.progression;
using Core.requests;
using Core.simulation;
using Core.tasks;
using Core.upgrades;

namespace UI.ViewModels;

public record LogEntry(string Text, IBrush Brush);

/// <summary>
/// Lit l'état de la partie et le met en forme pour l'écran. Aucune règle de jeu ici :
/// les clics appellent directement les commandes de GameSession.
/// </summary>
public class PlayingViewModel : ViewModelBase
{
    private const int MaxLogEntries = 14;
    private const double FlashDuration = 2; // secondes d'affichage d'un gain

    private readonly GameSession _session;
    private string _stageTitle = "";
    private double _stageProgress;
    private string _nextStageText = "";
    private string _balanceText = "";
    private IBrush _balanceBrush = Display.Normal;
    private string _netWorthText = "";
    private string _passiveText = "";
    private string _dateText = "";
    private string _billText = "";
    private IBrush _billBrush = Display.Muted;
    private string _missedText = "";
    private string _reputationText = "";
    private double _reputationProgress;
    private string _speedText = "";
    private string _capacityText = "";
    private string _requestsHeader = "";
    private string _autoAcceptText = "";
    private string _flashText = "";
    private double _flashUntil;
    private string _companyText = "";
    private string _newCompanyName = "";
    private string _foundText = "";
    private bool _canFound;
    private bool _showFounding;
    private bool _hiringUnlocked;
    private bool _businessesUnlocked;
    private bool _marketUnlocked;
    private string _staffText = "";
    private string _holdingText = "";
    private bool _canBuyHolding;
    private bool _holdingVisible;
    private string _portfolioText = "";
    private bool _storyVisible;
    private string _storyTitle = "";
    private string _storyText = "";

    // --- Bandeau du haut ---
    public string PlayerName => _session.PlayerName;
    public string StageTitle { get => _stageTitle; private set => SetField(ref _stageTitle, value); }
    public double StageProgress { get => _stageProgress; private set => SetField(ref _stageProgress, value); }
    public string NextStageText { get => _nextStageText; private set => SetField(ref _nextStageText, value); }
    public string BalanceText { get => _balanceText; private set => SetField(ref _balanceText, value); }
    public IBrush BalanceBrush { get => _balanceBrush; private set => SetField(ref _balanceBrush, value); }
    public string NetWorthText { get => _netWorthText; private set => SetField(ref _netWorthText, value); }
    public string PassiveText { get => _passiveText; private set => SetField(ref _passiveText, value); }
    public string DateText { get => _dateText; private set => SetField(ref _dateText, value); }
    public string BillText { get => _billText; private set => SetField(ref _billText, value); }
    public IBrush BillBrush { get => _billBrush; private set => SetField(ref _billBrush, value); }
    public string MissedText { get => _missedText; private set => SetField(ref _missedText, value); }

    /// <summary>Gain récent affiché quelques secondes à côté du solde.</summary>
    public string FlashText { get => _flashText; private set => SetField(ref _flashText, value); }

    // --- Travail ---
    public string ReputationText { get => _reputationText; private set => SetField(ref _reputationText, value); }
    public double ReputationProgress { get => _reputationProgress; private set => SetField(ref _reputationProgress, value); }
    public string SpeedText { get => _speedText; private set => SetField(ref _speedText, value); }
    public string CapacityText { get => _capacityText; private set => SetField(ref _capacityText, value); }
    public string RequestsHeader { get => _requestsHeader; private set => SetField(ref _requestsHeader, value); }
    public string AutoAcceptText { get => _autoAcceptText; private set => SetField(ref _autoAcceptText, value); }

    public ObservableCollection<RequestViewModel> Requests { get; } = new();
    public ObservableCollection<TaskViewModel> Tasks { get; } = new();
    public ObservableCollection<LogEntry> Log { get; } = new();

    // --- Onglets ---
    public ObservableCollection<UpgradeViewModel> Equipment { get; } = new();
    public ObservableCollection<UpgradeViewModel> Home { get; } = new();
    public ObservableCollection<UpgradeViewModel> Office { get; } = new();
    public ObservableCollection<EmployeeViewModel> Employees { get; } = new();
    public ObservableCollection<BusinessViewModel> Businesses { get; } = new();
    public ObservableCollection<AssetViewModel> Assets { get; } = new();

    public string CompanyText { get => _companyText; private set => SetField(ref _companyText, value); }
    public string NewCompanyName { get => _newCompanyName; set => SetField(ref _newCompanyName, value); }
    public string FoundText { get => _foundText; private set => SetField(ref _foundText, value); }
    public bool CanFound { get => _canFound; private set => SetField(ref _canFound, value); }
    public bool ShowFounding { get => _showFounding; private set => SetField(ref _showFounding, value); }
    public bool HiringUnlocked { get => _hiringUnlocked; private set => SetField(ref _hiringUnlocked, value); }
    public bool BusinessesUnlocked { get => _businessesUnlocked; private set => SetField(ref _businessesUnlocked, value); }
    public bool MarketUnlocked { get => _marketUnlocked; private set => SetField(ref _marketUnlocked, value); }
    public string StaffText { get => _staffText; private set => SetField(ref _staffText, value); }
    public string HoldingText { get => _holdingText; private set => SetField(ref _holdingText, value); }
    public bool CanBuyHolding { get => _canBuyHolding; private set => SetField(ref _canBuyHolding, value); }
    public bool HoldingVisible { get => _holdingVisible; private set => SetField(ref _holdingVisible, value); }
    public string PortfolioText { get => _portfolioText; private set => SetField(ref _portfolioText, value); }

    public string HiringLockText => LockText(Feature.Hiring);
    public string BusinessesLockText => LockText(Feature.Businesses);
    public string MarketLockText => LockText(Feature.Market);

    public ICommand FoundCommand { get; }
    public ICommand BuyHoldingCommand { get; }

    // --- Histoire ---
    public bool StoryVisible { get => _storyVisible; private set => SetField(ref _storyVisible, value); }
    public string StoryTitle { get => _storyTitle; private set => SetField(ref _storyTitle, value); }
    public string StoryText { get => _storyText; private set => SetField(ref _storyText, value); }
    public ICommand CloseStoryCommand { get; }

    public PlayingViewModel(GameSession session)
    {
        _session = session;

        FoundCommand = new RelayCommand(() => session.FoundCompany(NewCompanyName));
        BuyHoldingCommand = new RelayCommand(() => session.BuyHolding());
        CloseStoryCommand = new RelayCommand(() => StoryVisible = false);

        foreach (var track in session.Upgrades.Tracks)
        {
            var vm = new UpgradeViewModel(track, session);
            var list = track.Info.Id switch
            {
                UpgradeTrackId.Housing => Home,
                UpgradeTrackId.Office => Office,
                _ => Equipment
            };
            list.Add(vm);
        }

        foreach (var role in session.Staff.Roles)
            Employees.Add(new EmployeeViewModel(role, session));

        for (int i = 0; i < session.Businesses.Businesses.Count; i++)
            Businesses.Add(new BusinessViewModel(session.Businesses.Businesses[i], i, session));

        for (int i = 0; i < session.Market.Assets.Count; i++)
            Assets.Add(new AssetViewModel(session.Market.Assets[i], i, session));

        SubscribeToEvents();
        ShowStory(session.Career.Current);
        Refresh();
    }

    /// <summary>Raccourci pour la scène : le perso a-t-il du travail en cours ?</summary>
    public bool IsWorking => _session.Company.ActiveTasks.Count > 0;

    public void Refresh()
    {
        var company = _session.Company;
        var career = _session.Career;

        StageTitle = $"{_session.PlayerName} · {career.Current.Title}";
        StageProgress = career.ProgressToNext(_session.NetWorth) * 100;
        NextStageText = career.Next is { } next
            ? $"Prochain statut : {next.Title} — {next.Requirement}"
            : "Tu es au sommet du monde.";

        BalanceText = Display.Money(company.WalletBalance);
        BalanceBrush = company.WalletBalance < 0 ? Display.Danger : Display.Normal;
        NetWorthText = $"Patrimoine {Display.Money(_session.NetWorth)}";
        double passive = _session.Businesses.IncomePerSecond;
        PassiveText = passive > 0 ? $"Boîtes +{Display.Money(passive)}/s" : "";
        DateText = $"Mois {_session.Month} · Jour {_session.Day}";

        var bill = _session.CurrentBill;
        BillText = $"Factures dans {Display.Seconds(_session.Bills.TimeUntilDue)} : {Display.Money(bill.Total)}";
        BillBrush = company.WalletBalance < bill.Total ? Display.Warning : Display.Muted;
        MissedText = _session.Bills.MissedPayments > 0
            ? "Facture impayée ! Encore une et c'est la faillite."
            : "";

        if (_session.ElapsedTime > _flashUntil)
            FlashText = "";

        var reputation = company.Reputation;
        ReputationText = $"Réputation : {reputation.Current.Title}";
        ReputationProgress = reputation.ProgressToNext * 100;
        SpeedText = $"Vitesse {Display.Multiplier(_session.Stats.WorkSpeed)} · clic {Display.Multiplier(_session.Stats.ClickPower)}";
        CapacityText = $"Tâches en cours ({company.ActiveTasks.Count}/{company.MaxActiveTasks})";
        RequestsHeader = $"Demandes ({company.PendingRequests.Count})";
        AutoAcceptText = _session.Stats.AutoAccept ? "Tes commerciaux acceptent les contrats faisables à ta place." : "";

        RefreshCompany();
        SyncRequests();
        SyncTasks();

        foreach (var vm in Equipment.Concat(Home).Concat(Office))
            vm.Refresh();
        foreach (var vm in Employees)
            vm.Refresh(HiringUnlocked && company.IsFounded);
        foreach (var vm in Businesses)
            vm.Refresh(BusinessesUnlocked);
        foreach (var vm in Assets)
            vm.Refresh(MarketUnlocked);
    }

    private void RefreshCompany()
    {
        var company = _session.Company;

        HiringUnlocked = _session.IsUnlocked(Feature.Hiring);
        BusinessesUnlocked = _session.IsUnlocked(Feature.Businesses);
        MarketUnlocked = _session.IsUnlocked(Feature.Market);

        ShowFounding = !company.IsFounded;
        CanFound = _session.IsUnlocked(Feature.FoundCompany) &&
                   company.WalletBalance >= _session.Config.CompanyFoundingCost;
        FoundText = _session.IsUnlocked(Feature.FoundCompany)
            ? $"Fonder ma boîte ({Display.Money(_session.Config.CompanyFoundingCost)})"
            : $"Disponible au statut {StageTitleOf(Feature.FoundCompany)}";
        CompanyText = company.IsFounded ? company.Name : "Tu es encore freelance.";

        int seats = _session.Upgrades.EmployeeSeats;
        StaffText = company.IsFounded
            ? $"{_session.Staff.TotalCount}/{seats} places occupées · salaires {Display.Money(_session.Staff.MonthlySalaries)}/mois"
            : "";

        var group = _session.Businesses;
        HoldingVisible = _session.IsUnlocked(Feature.Holding) || group.HasHolding;
        CanBuyHolding = !group.HasHolding && _session.IsUnlocked(Feature.Holding) && company.WalletBalance >= group.HoldingCost;
        HoldingText = group.HasHolding
            ? $"Ta holding regroupe toutes tes boîtes : revenus {Display.Multiplier(group.HoldingMultiplier)}."
            : $"Regroupe tes boîtes dans une holding pour {Display.Money(group.HoldingCost)} : revenus {Display.Multiplier(group.HoldingMultiplier)}.";

        PortfolioText = $"Portefeuille : {Display.Money(_session.Market.TotalValue)}";
    }

    private string LockText(Feature feature) => $"Verrouillé — débloqué au statut « {StageTitleOf(feature)} »";

    private string StageTitleOf(Feature feature) =>
        _session.Config.CareerStages.First(s => s.Stage == feature.RequiredStage()).Title;

    private void SubscribeToEvents()
    {
        var company = _session.Company;
        company.RequestExpired += r => AddLog($"Offre expirée : {r.ClientName}", Display.Muted);
        company.TaskCompleted += OnTaskCompleted;
        company.TaskFailed += t => AddLog($"Tâche ratée : {t.Request.ClientName}. Ta réputation en prend un coup.", Display.Danger);
        company.Reputation.LevelChanged += l => AddLog($"Réputation : {l.Title}", Display.Gold);

        _session.Bills.BillPaid += OnBillPaid;
        _session.Upgrades.Upgraded += t => AddLog($"Nouveau : {t.Current.Name}", Display.Good);
        _session.Businesses.BusinessGrew += b =>
            AddLog(b.Level == 1 ? $"Tu crées ta boîte « {b.Info.Name} » !" : $"{b.Info.Name} passe au niveau {b.Level}", Display.Gold);

        _session.Career.StageReached += stage =>
        {
            AddLog($"Nouveau statut : {stage.Title}", Display.Gold);
            ShowStory(stage);
        };
    }

    private void ShowStory(Core.progression.CareerStageInfo stage)
    {
        StoryTitle = stage.Stage == CareerStage.Broke ? "Le début" : $"Nouveau statut : {stage.Title}";
        StoryText = stage.Story;
        StoryVisible = true;
    }

    private void OnBillPaid(BillBreakdown bill, bool unpaid)
    {
        if (unpaid)
            AddLog($"Factures de {Display.Money(bill.Total)} impayées ! Renfloue les caisses avant le mois prochain.", Display.Danger);
        else
            AddLog($"Factures payées : −{Display.Money(bill.Total)}", Display.Muted);
    }

    private void OnTaskCompleted(WorkTask task)
    {
        string gain = $"+{Display.Money(task.EarnedReward)}";

        if (task.IsLate)
            AddLog($"Livré en retard : {task.Request.ClientName} {gain} au lieu de {Display.Money(task.Request.Value)}",
                Display.Warning);
        else
            AddLog($"Livré : {task.Request.ClientName} {gain}", Display.Good);

        FlashText = gain;
        _flashUntil = _session.ElapsedTime + FlashDuration;
    }

    private void AddLog(string text, IBrush brush)
    {
        Log.Insert(0, new LogEntry($"[M{_session.Month}·J{_session.Day}] {text}", brush));

        while (Log.Count > MaxLogEntries)
            Log.RemoveAt(Log.Count - 1);
    }

    // Les listes sont synchronisées par identité plutôt que recréées :
    // recréer les boutons à chaque image empêcherait de cliquer dessus.
    private void SyncRequests()
    {
        var company = _session.Company;
        var pending = company.PendingRequests;

        foreach (var vm in Requests.Where(vm => pending.All(r => r.Id != vm.Id)).ToList())
            Requests.Remove(vm);

        foreach (var request in pending.Where(r => Requests.All(vm => vm.Id != r.Id)))
            Requests.Add(CreateRequestViewModel(request));

        foreach (var vm in Requests)
            vm.Refresh(company.HasFreeCapacity, _session.Stats.WorkSpeed);
    }

    private RequestViewModel CreateRequestViewModel(ClientRequest request) =>
        new(request, _session.Company, _session.Config.WorkDurationFor(request.Difficulty), _session.Config.OfferDuration);

    private void SyncTasks()
    {
        var active = _session.Company.ActiveTasks;

        foreach (var vm in Tasks.Where(vm => !active.Contains(vm.Task)).ToList())
            Tasks.Remove(vm);

        foreach (var task in active.Where(t => Tasks.All(vm => vm.Task != t)))
            Tasks.Add(new TaskViewModel(task));

        foreach (var vm in Tasks)
            vm.Refresh(_session.Stats.WorkSpeed);
    }
}
