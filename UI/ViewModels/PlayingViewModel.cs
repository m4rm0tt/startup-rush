using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Core.finance;
using Core.progression;
using Core.roguelike;
using Core.simulation;
using Core.states;
using Core.tasks;
using Core.upgrades;
using UI.Audio;
using UI.Pixel;

namespace UI.ViewModels;

public record LogEntry(string Text, IBrush Brush);

/// <summary>
/// Lit l'état de la run et le met en forme pour le HUD. Aucune règle de jeu ici :
/// les clics appellent directement les commandes de GameSession.
/// </summary>
public class PlayingViewModel : ViewModelBase
{
    private const int MaxLogEntries = 7;
    private const double FlashDuration = 2;

    private readonly PlayingState _state;
    private readonly GameSession _session;
    private double _shownMoney;
    private double _flashUntil;
    private string _tab = "equipment";

    // --- Bandeau du haut ---
    public Bitmap Portrait { get; }
    public string PlayerName => _session.PlayerName.ToUpper();
    public string StageTitle { get => Get<string>(); private set => Set(value); }
    public double StageProgress { get => Get<double>(); private set => Set(value); }
    public string NextStageText { get => Get<string>(); private set => Set(value); }
    public string MoneyText { get => Get<string>(); private set => Set(value); }
    public IBrush MoneyBrush { get => Get<IBrush>(); private set => Set(value); }
    public string FlashText { get => Get<string>(); private set => Set(value); }
    public string NetWorthText { get => Get<string>(); private set => Set(value); }
    public string PassiveText { get => Get<string>(); private set => Set(value); }
    public string DateText { get => Get<string>(); private set => Set(value); }
    public string BillAmountText { get => Get<string>(); private set => Set(value); }
    public string BillTimeText { get => Get<string>(); private set => Set(value); }
    public double MonthProgress { get => Get<double>(); private set => Set(value); }
    public IBrush BillBrush { get => Get<IBrush>(); private set => Set(value); }
    public string BillDetail { get => Get<string>(); private set => Set(value); }
    public string ShieldText { get => Get<string>(); private set => Set(value); }
    public bool ShieldVisible { get => Get<bool>(); private set => Set(value); }
    public bool MissedVisible { get => Get<bool>(); private set => Set(value); }
    public Bitmap BillIcon => Icons.Get("bill");
    public Bitmap CalendarIcon => Icons.Get("calendar");
    public Bitmap ShieldIcon => Icons.Get("shield");
    public Bitmap CoinIcon => Icons.Get("coin");
    public Bitmap WarningIcon => Icons.Get("warning");
    public Bitmap StarIcon => Icons.Get("star");

    // --- Crise ---
    public string CrisisText { get => Get<string>(); private set => Set(value); }
    public bool CrisisVisible { get => Get<bool>(); private set => Set(value); }
    public bool CrisisActive { get => Get<bool>(); private set => Set(value); }
    public IBrush CrisisBrush { get => Get<IBrush>(); private set => Set(value); }

    // --- Contrats ---
    public string ReputationText { get => Get<string>(); private set => Set(value); }
    public double ReputationProgress { get => Get<double>(); private set => Set(value); }
    public string RequestsHeader { get => Get<string>(); private set => Set(value); }
    public string AutoAcceptText { get => Get<string>(); private set => Set(value); }
    public ObservableCollection<RequestViewModel> Requests { get; } = new();

    // --- Colonne de droite ---
    public string CapacityText { get => Get<string>(); private set => Set(value); }
    public string SpeedText { get => Get<string>(); private set => Set(value); }
    public ObservableCollection<TaskViewModel> Tasks { get; } = new();
    public ObservableCollection<PerkCardViewModel> Perks { get; } = new();
    public ObservableCollection<EffectViewModel> Effects { get; } = new();
    public ObservableCollection<LogEntry> Log { get; } = new();
    public string PerksHeader { get => Get<string>(); private set => Set(value); }

    // --- Boutique ---
    public ObservableCollection<UpgradeViewModel> Equipment { get; } = new();
    public ObservableCollection<UpgradeViewModel> Home { get; } = new();
    public ObservableCollection<UpgradeViewModel> Office { get; } = new();
    public ObservableCollection<EmployeeViewModel> Employees { get; } = new();
    public ObservableCollection<BusinessViewModel> Businesses { get; } = new();
    public ObservableCollection<AssetViewModel> Assets { get; } = new();

    public bool IsEquipmentTab => _tab == "equipment";
    public bool IsHomeTab => _tab == "home";
    public bool IsTeamTab => _tab == "team";
    public bool IsEmpireTab => _tab == "empire";
    public bool IsMarketTab => _tab == "market";
    public ICommand ShowEquipmentCommand { get; }
    public ICommand ShowHomeCommand { get; }
    public ICommand ShowTeamCommand { get; }
    public ICommand ShowEmpireCommand { get; }
    public ICommand ShowMarketCommand { get; }

    public string CompanyText { get => Get<string>(); private set => Set(value); }
    public string NewCompanyName { get => Get<string>(); set => Set(value); }
    public string FoundText { get => Get<string>(); private set => Set(value); }
    public bool CanFound { get => Get<bool>(); private set => Set(value); }
    public bool ShowFounding { get => Get<bool>(); private set => Set(value); }
    public bool HiringUnlocked { get => Get<bool>(); private set => Set(value); }
    public bool BusinessesUnlocked { get => Get<bool>(); private set => Set(value); }
    public bool MarketUnlocked { get => Get<bool>(); private set => Set(value); }
    public string StaffText { get => Get<string>(); private set => Set(value); }
    public string HoldingText { get => Get<string>(); private set => Set(value); }
    public bool CanBuyHolding { get => Get<bool>(); private set => Set(value); }
    public bool HoldingVisible { get => Get<bool>(); private set => Set(value); }
    public string PortfolioText { get => Get<string>(); private set => Set(value); }
    public string HiringLockText => LockText(Feature.Hiring);
    public string BusinessesLockText => LockText(Feature.Businesses);
    public string MarketLockText => LockText(Feature.Market);
    public Bitmap LockIcon => Icons.Get("lock");
    public Bitmap CrownIcon => Icons.Get("crown");
    public ICommand FoundCommand { get; }
    public ICommand BuyHoldingCommand { get; }

    // --- Fenêtres par-dessus le jeu ---
    public DraftViewModel? Draft { get => Get<DraftViewModel?>(); private set => Set(value); }
    public EventViewModel? Event { get => Get<EventViewModel?>(); private set => Set(value); }
    public bool DraftVisible { get => Get<bool>(); private set => Set(value); }
    public bool EventVisible { get => Get<bool>(); private set => Set(value); }
    public bool PauseVisible { get => Get<bool>(); private set => Set(value); }
    public bool IntroVisible { get => Get<bool>(); private set => Set(value); }
    public string IntroTitle { get; }
    public string IntroText { get; }
    public string IntroTrait { get; }
    public string RunInfo { get; }
    public string MusicText { get => Get<string>(); private set => Set(value); }
    public string SoundText { get => Get<string>(); private set => Set(value); }
    public string EffectsText { get => Get<string>(); private set => Set(value); }
    public ICommand StartCommand { get; }
    public ICommand ResumeCommand { get; }
    public ICommand AbandonCommand { get; }
    public ICommand ToggleMusicCommand { get; }
    public ICommand ToggleSoundCommand { get; }
    public ICommand ToggleEffectsCommand { get; }

    // --- Pour la scène ---
    public bool IsWorking => _session.Company.ActiveTasks.Count > 0;
    public double DayPhase => _session.ElapsedTime % _session.Config.MonthDuration / _session.Config.MonthDuration;
    public bool InCrisis => _session.ActiveCrisis != null;
    public bool InDebt => _session.Company.WalletBalance < 0;

    public PlayingViewModel(PlayingState state)
    {
        _state = state;
        _session = state.Session;
        _shownMoney = _session.Company.WalletBalance;

        var character = state.Setup.Character;
        Portrait = Portraits.Get(character.Id);
        IntroTitle = $"{character.Name.ToUpper()} — {character.Title.ToUpper()}";
        IntroText = $"{character.Description}\n\n{_session.Career.Current.Story}";
        IntroTrait = character.TraitText;
        var pressure = PressureCatalog.All[state.Setup.Pressure];
        RunInfo = $"Graine {state.Setup.Seed} · Pression {pressure.Level} « {pressure.Name} »";

        ShowEquipmentCommand = new RelayCommand(() => SelectTab("equipment"));
        ShowHomeCommand = new RelayCommand(() => SelectTab("home"));
        ShowTeamCommand = new RelayCommand(() => SelectTab("team"));
        ShowEmpireCommand = new RelayCommand(() => SelectTab("empire"));
        ShowMarketCommand = new RelayCommand(() => SelectTab("market"));
        FoundCommand = new RelayCommand(() => Purchase.Try(() => _session.FoundCompany(NewCompanyName)));
        BuyHoldingCommand = new RelayCommand(() => Purchase.Try(_session.BuyHolding));
        StartCommand = new RelayCommand(StartRun);
        ResumeCommand = new RelayCommand(TogglePause);
        AbandonCommand = new RelayCommand(() => _state.Abandon());
        ToggleMusicCommand = new RelayCommand(Settings.CycleMusic);
        ToggleSoundCommand = new RelayCommand(Settings.CycleSound);
        ToggleEffectsCommand = new RelayCommand(Settings.ToggleEffects);

        foreach (var track in _session.Upgrades.Tracks)
        {
            var vm = new UpgradeViewModel(track, _session);
            (track.Info.Id switch
            {
                UpgradeTrackId.Housing => Home,
                UpgradeTrackId.Office => Office,
                _ => Equipment
            }).Add(vm);
        }
        foreach (var role in _session.Staff.Roles)
            Employees.Add(new EmployeeViewModel(role, _session));
        for (int i = 0; i < _session.Businesses.Businesses.Count; i++)
            Businesses.Add(new BusinessViewModel(_session.Businesses.Businesses[i], i, _session));
        for (int i = 0; i < _session.Market.Assets.Count; i++)
            Assets.Add(new AssetViewModel(_session.Market.Assets[i], i, _session));

        SubscribeToEvents();

        // La run attend que le joueur ait lu l'intro
        IntroVisible = true;
        _state.TogglePause();
        Refresh(0);
    }

    // --- Actions du joueur (clavier ou souris) ---

    public void TogglePause()
    {
        if (IntroVisible || _session.IsOver)
            return;

        _state.TogglePause();
        Sfx.Play(_state.IsPaused ? Sound.PauseIn : Sound.PauseOut);
    }

    /// <summary>Touches 1 à 4 : carte d'atout ou choix d'événement.</summary>
    public void PickChoice(int index)
    {
        if (Draft != null && index < Draft.Cards.Count)
            Draft.Cards[index].ChooseCommand?.Execute(null);
        else if (Event != null && !Event.IsResolved && index < Event.Options.Count && Event.Options[index].IsEnabled)
            Event.Options[index].ChooseCommand.Execute(null);
    }

    public void Reroll() => Draft?.Reroll();

    /// <summary>Entrée : continuer après un événement ou lancer la run.</summary>
    public void Confirm()
    {
        if (IntroVisible)
            StartRun();
        else if (Event?.IsResolved == true)
            Event.ContinueCommand.Execute(null);
    }

    private void StartRun()
    {
        if (!IntroVisible)
            return;

        IntroVisible = false;
        _state.TogglePause();
        Sfx.Play(Sound.LevelUp);
    }

    private void SelectTab(string tab)
    {
        _tab = tab;
        Sfx.Play(Sound.Click, 0.6f);
        OnPropertyChanged(nameof(IsEquipmentTab));
        OnPropertyChanged(nameof(IsHomeTab));
        OnPropertyChanged(nameof(IsTeamTab));
        OnPropertyChanged(nameof(IsEmpireTab));
        OnPropertyChanged(nameof(IsMarketTab));
    }

    // --- Mise à jour à chaque image ---

    public void Refresh(double deltaTime)
    {
        RefreshTopBar(deltaTime);
        RefreshCrisis();
        RefreshWork();
        RefreshShop();
        RefreshChoices();

        PauseVisible = _state.IsPaused && !IntroVisible;
        MusicText = Settings.MusicText;
        SoundText = Settings.SoundText;
        EffectsText = Settings.EffectsText;
        Sfx.PlayMusic(_session.ActiveCrisis != null ? Music.Crisis : Music.Run);
    }

    private void RefreshTopBar(double deltaTime)
    {
        var company = _session.Company;
        var career = _session.Career;

        StageTitle = career.Current.Title.ToUpper();
        StageProgress = career.ProgressToNext(_session.NetWorth) * 100;
        NextStageText = career.Next is { } next ? $"PROCHAIN : {next.Title.ToUpper()} — {next.Requirement}" : "TU ES AU SOMMET DU MONDE";

        // Le compteur d'argent rattrape la vraie valeur en douceur, comme dans un jeu d'arcade
        double actual = company.WalletBalance;
        _shownMoney += (actual - _shownMoney) * Math.Min(1, deltaTime * 10);
        if (Math.Abs(actual - _shownMoney) < 1 || deltaTime == 0)
            _shownMoney = actual;
        MoneyText = Display.Money(_shownMoney);
        MoneyBrush = actual < 0 ? Palette.Blood : Palette.Snow;
        if (_session.ElapsedTime > _flashUntil)
            FlashText = "";

        NetWorthText = $"PATRIMOINE {Display.Money(_session.NetWorth)}";
        double passive = _session.Businesses.IncomePerSecond * _session.Modifiers.BusinessIncome;
        PassiveText = passive > 0 ? $"+{Display.Money(passive)}/s" : "";
        DateText = $"MOIS {_session.Month:00} · J{_session.Day:00}";

        var bill = _session.CurrentBill;
        BillAmountText = Display.Money(bill.Total);
        BillTimeText = Display.Seconds(_session.Bills.TimeUntilDue);
        MonthProgress = (1 - _session.Bills.TimeUntilDue / _session.Config.MonthDuration) * 100;
        BillBrush = company.WalletBalance < bill.Total ? Palette.Blood : Palette.Gold;
        BillDetail = $"Logement {Display.Money(bill.Housing)}\nÉquipe et locaux {Display.Money(bill.Salaries)}\n" +
                     $"Abonnements {Display.Money(bill.Subscriptions)}\nImpôt sur la fortune {Display.Money(bill.Taxes)}";
        ShieldVisible = _session.Bills.Shields > 0;
        ShieldText = $"×{_session.Bills.Shields}";
        MissedVisible = _session.Bills.MissedPayments > 0;
    }

    private void RefreshCrisis()
    {
        if (_session.ActiveCrisis is { } active)
        {
            CrisisVisible = true;
            CrisisActive = true;
            CrisisBrush = Palette.Blood;
            CrisisText = $"CRISE : {active.Name.ToUpper()} — {active.Description}";
        }
        else if (_session.NextCrisis is { } next && _session.TimeUntilNextCrisis < _session.Config.MonthDuration * 1.5)
        {
            CrisisVisible = true;
            CrisisActive = false;
            CrisisBrush = Palette.Slate;
            CrisisText = $"CRISE DANS {Display.Seconds(_session.TimeUntilNextCrisis)} : {next.Name.ToUpper()} — {next.Description}";
        }
        else
        {
            CrisisVisible = false;
        }
    }

    private void RefreshWork()
    {
        var company = _session.Company;
        var reputation = company.Reputation;
        ReputationText = $"RÉPUTATION : {reputation.Current.Title.ToUpper()}";
        ReputationProgress = reputation.ProgressToNext * 100;
        RequestsHeader = $"CONTRATS ({company.PendingRequests.Count})";
        AutoAcceptText = _session.Stats.AutoAccept ? "Tes commerciaux acceptent à ta place." : "";
        CapacityText = $"EN COURS {company.ActiveTasks.Count}/{company.MaxActiveTasks}";
        SpeedText = $"vitesse {Display.Multiplier(_session.Stats.WorkSpeed)} · clic {Display.Multiplier(_session.Stats.ClickPower)}";

        SyncRequests();
        SyncTasks();

        if (Perks.Count != _session.Perks.Count)
        {
            Perks.Clear();
            foreach (var perk in _session.Perks)
                Perks.Add(new PerkCardViewModel(perk));
        }
        PerksHeader = $"ATOUTS ({_session.Perks.Count})";

        var effects = _session.TimedEffects
            .Select(e => new EffectViewModel(e.Name.ToUpper(), e.Description,
                e.MonthsLeft > 1 ? $"{e.MonthsLeft} mois" : "ce mois", e.IsPositive ? Palette.Leaf : Palette.Blood))
            .ToList();
        if (!effects.SequenceEqual(Effects))
        {
            Effects.Clear();
            foreach (var effect in effects)
                Effects.Add(effect);
        }
    }

    private void RefreshShop()
    {
        var company = _session.Company;

        HiringUnlocked = _session.IsUnlocked(Feature.Hiring);
        BusinessesUnlocked = _session.IsUnlocked(Feature.Businesses);
        MarketUnlocked = _session.IsUnlocked(Feature.Market);

        ShowFounding = !company.IsFounded;
        CanFound = _session.IsUnlocked(Feature.FoundCompany) && company.WalletBalance >= _session.Config.CompanyFoundingCost;
        FoundText = _session.IsUnlocked(Feature.FoundCompany)
            ? $"FONDER MA BOÎTE  {Display.Money(_session.Config.CompanyFoundingCost)}"
            : $"Disponible au statut {StageTitleOf(Feature.FoundCompany)}";
        CompanyText = company.IsFounded ? company.Name.ToUpper() : "TU ES ENCORE FREELANCE";
        StaffText = company.IsFounded
            ? $"{_session.Staff.TotalCount}/{_session.EmployeeSeats} places · {Display.Money(_session.Staff.MonthlySalaries)}/mois"
            : "Fonde ta boîte pour embaucher.";

        var group = _session.Businesses;
        HoldingVisible = _session.IsUnlocked(Feature.Holding) || group.HasHolding;
        CanBuyHolding = !group.HasHolding && _session.IsUnlocked(Feature.Holding) && company.WalletBalance >= group.HoldingCost;
        HoldingText = group.HasHolding
            ? $"Ta holding regroupe tout : revenus des boîtes {Display.Multiplier(group.HoldingMultiplier)}."
            : $"Regroupe tes boîtes : revenus {Display.Multiplier(group.HoldingMultiplier)} pour {Display.Money(group.HoldingCost)}.";
        PortfolioText = $"PORTEFEUILLE {Display.Money(_session.Market.TotalValue)}";

        // Seul l'onglet visible est rafraîchi
        if (IsEquipmentTab)
            foreach (var vm in Equipment) vm.Refresh();
        if (IsHomeTab)
            foreach (var vm in Home) vm.Refresh();
        if (IsTeamTab)
        {
            foreach (var vm in Office) vm.Refresh();
            foreach (var vm in Employees) vm.Refresh(HiringUnlocked && company.IsFounded);
        }
        if (IsEmpireTab)
            foreach (var vm in Businesses) vm.Refresh(BusinessesUnlocked);
        if (IsMarketTab)
            foreach (var vm in Assets) vm.Refresh(MarketUnlocked);
    }

    private void RefreshChoices()
    {
        var pending = _session.PendingChoice;

        if (pending is Draft draft)
        {
            if (Draft?.Draft != draft)
            {
                string subtitle = draft.Title.StartsWith("Nouveau statut")
                    ? _session.Career.Current.Story
                    : "Choisis un atout. Il te suivra jusqu'à la fin de la run.";
                Draft = new DraftViewModel(draft, _session, subtitle);
            }
            Draft.Refresh();
        }
        else
        {
            Draft = null;
        }

        if (pending is GameEvent gameEvent)
        {
            if (Event?.Event != gameEvent)
                Event = new EventViewModel(gameEvent, _session);
            Event.Refresh();
        }
        else
        {
            Event = null;
        }

        // Rien ne passe devant l'intro de la run
        DraftVisible = Draft != null && !IntroVisible;
        EventVisible = Event != null && !IntroVisible;
    }

    private string LockText(Feature feature) => $"VERROUILLÉ — débloqué au statut « {StageTitleOf(feature)} »";

    private string StageTitleOf(Feature feature) =>
        _session.Config.CareerStages.First(s => s.Stage == feature.RequiredStage()).Title;

    // --- Événements du jeu → journal, sons, effets ---

    private void SubscribeToEvents()
    {
        var company = _session.Company;
        company.RequestExpired += r => AddLog($"Offre perdue : {r.ClientName}", Palette.Fog);
        company.RequestReceived += _ => Sfx.Play(Sound.Offer);
        company.TaskCompleted += OnTaskCompleted;
        company.TaskFailed += t =>
        {
            AddLog($"Contrat raté : {t.Request.ClientName}", Palette.Blood);
            Sfx.Play(Sound.Error);
        };
        company.Reputation.LevelChanged += l => AddLog($"Réputation : {l.Title}", Palette.Plum);

        _session.Bills.BillPaid += OnBillPaid;
        _session.Bills.Shielded += () =>
        {
            AddLog("Ton assurance a couvert l'impayé !", Palette.Sky);
            Sfx.Play(Sound.LevelUp);
        };
        _session.Upgrades.Upgraded += t => AddLog($"Nouveau : {t.Current.Name}", Palette.Leaf);
        _session.Businesses.BusinessGrew += b =>
            AddLog(b.Level == 1 ? $"Nouvelle boîte : {b.Info.Name}" : $"{b.Info.Name} niveau {b.Level}", Palette.Gold);
        _session.Career.StageReached += s =>
        {
            AddLog($"NOUVEAU STATUT : {s.Title.ToUpper()}", Palette.Lemon);
            Sfx.Play(Sound.LevelUp);
        };
        _session.PerkAdded += p => AddLog($"Atout : {p.Name}", Palette.Rarity(p.Rarity));
        _session.InterestPaid += i => AddLog($"Intérêts : +{Display.Money(i)}", Palette.Leaf);
        _session.CrisisStarted += c =>
        {
            AddLog($"CRISE : {c.Name}", Palette.Blood);
            Sfx.Play(Sound.Alarm);
            Juice.Shake(10);
            Juice.Flash(Color.Parse("#e43b44"));
        };
        _session.ChoiceQueued += choice => Sfx.Play(choice is Draft ? Sound.Card : Sound.Event);
    }

    private void OnBillPaid(BillBreakdown bill, bool unpaid)
    {
        if (unpaid)
        {
            AddLog($"IMPAYÉ ! Factures de {Display.Money(bill.Total)}", Palette.Blood);
            Sfx.Play(Sound.Unpaid);
            Juice.Shake(14);
            Juice.Flash(Color.Parse("#e43b44"));
        }
        else
        {
            AddLog($"Factures payées : −{Display.Money(bill.Total)}", Palette.Fog);
            Sfx.Play(Sound.Bill);
        }
    }

    private void OnTaskCompleted(WorkTask task)
    {
        string gain = $"+{Display.Money(task.EarnedReward)}";
        if (task.IsLate)
            AddLog($"En retard : {task.Request.ClientName} {gain}", Palette.Gold);
        else
            AddLog($"Livré : {task.Request.ClientName} {gain}", Palette.Leaf);

        Sfx.Play(Sound.Coin);
        FlashText = gain;
        _flashUntil = _session.ElapsedTime + FlashDuration;
    }

    private void AddLog(string text, IBrush brush)
    {
        Log.Insert(0, new LogEntry(text, brush));
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
            Requests.Add(new RequestViewModel(request, company, _session.Config.WorkDurationFor(request.Difficulty),
                request.OfferTimeLeft));

        foreach (var vm in Requests)
            vm.Refresh(company.HasFreeCapacity, _session.Stats.WorkSpeed);
    }

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
