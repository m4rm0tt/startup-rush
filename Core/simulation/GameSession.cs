using Core.businesses;
using Core.company;
using Core.finance;
using Core.investments;
using Core.progression;
using Core.requests;
using Core.roguelike;
using Core.staff;
using Core.tasks;
using Core.upgrades;

namespace Core.simulation;

/// <summary>
/// Une run : rassemble tous les systèmes, les fait avancer dans un ordre fixe
/// et expose les commandes du joueur. Chaque commande vérifie qu'elle est débloquée.
/// La run se met en pause d'elle-même quand un choix attend le joueur (atout, événement).
/// </summary>
public class GameSession
{
    private readonly GameClock _gameClock;
    private readonly RequestGenerator _requestGenerator;
    private readonly Random _random;
    private readonly RunSetup? _setup;
    private readonly PerkDeck _perkDeck;
    private readonly List<Perk> _perks = new();
    private readonly List<TimedEffect> _timedEffects = new();
    private readonly Queue<IPendingChoice> _choices = new();
    private readonly List<double> _netWorthHistory = new();
    private TimedEffect? _crisisEffect;
    private string? _lastEventId;
    private double _timeUntilEvent;
    private int _monthsCompleted;
    private int _crisesSurvived;
    private string? _crisisAtLastBill; // pour dire au joueur ce qui l'a achevé

    public GameConfig Config { get; }
    public string PlayerName { get; }
    public int Seed { get; }
    public int Pressure => _setup?.Pressure ?? 0;
    public Character? Character => _setup?.Character;

    public Company Company { get; }
    public PlayerStats Stats => Company.Stats;
    public Upgrades Upgrades { get; }
    public Staff Staff { get; }
    public BusinessGroup Businesses { get; }
    public Market Market { get; }
    public Bills Bills { get; }
    public Career Career { get; }

    /// <summary>Bonus et malus actuels (personnage, pression, atouts, crise, événements).</summary>
    public Modifiers Modifiers { get; private set; } = new();

    public IReadOnlyList<Perk> Perks => _perks;
    public IReadOnlyList<TimedEffect> TimedEffects => _timedEffects;

    public CrisisInfo? ActiveCrisis { get; private set; }
    public CrisisInfo? NextCrisis { get; private set; }
    public int NextCrisisMonth { get; private set; }
    public double TimeUntilNextCrisis => (NextCrisisMonth - 1) * Config.MonthDuration - ElapsedTime;

    /// <summary>Le choix qui attend le joueur, null si la run tourne.</summary>
    public IPendingChoice? PendingChoice => _choices.Count > 0 ? _choices.Peek() : null;
    public bool IsPaused => PendingChoice != null;

    public double ElapsedTime => _gameClock.Time;
    public int Month => (int)(ElapsedTime / Config.MonthDuration) + 1;
    public int Day => (int)(ElapsedTime % Config.MonthDuration / (Config.MonthDuration / Config.DaysPerMonth)) + 1;

    /// <summary>Argent + bourse + ce qui a été investi dans les boîtes.</summary>
    public double NetWorth => Company.WalletBalance + Market.TotalValue + Businesses.Valuation;
    public double PeakNetWorth { get; private set; }
    public IReadOnlyList<double> NetWorthHistory => _netWorthHistory;

    /// <summary>Ce que coûterait la facture si elle tombait maintenant.</summary>
    public BillBreakdown CurrentBill
    {
        get
        {
            var office = Upgrades[UpgradeTrackId.Office].Current.MonthlyCost;
            double housing = Upgrades[UpgradeTrackId.Housing].Current.MonthlyCost;
            double subscriptions = Upgrades.MonthlyCost - housing - office;
            double salaries = Staff.MonthlySalaries + (Company.IsFounded ? office : 0);
            double bills = Modifiers.Bills;
            double taxRate = Config.WealthTaxRate + Modifiers.ExtraWealthTax;
            double taxes = Math.Max(0, NetWorth - Config.WealthTaxAllowance) * taxRate;
            return new BillBreakdown(housing * Modifiers.Rent * bills, salaries * bills, subscriptions * bills, taxes * bills);
        }
    }

    public int EmployeeSeats => Upgrades.EmployeeSeats + Modifiers.ExtraSeats;

    /// <summary>Vrai s'il reste une place libre dans les locaux.</summary>
    public bool HasFreeSeat => Staff.TotalCount < EmployeeSeats;

    public bool IsOver => Result != null;

    /// <summary>Bilan de la run, null tant qu'elle n'est pas terminée.</summary>
    public GameResult? Result { get; private set; }

    public event Action<GameResult>? GameOver;
    public event Action<IPendingChoice>? ChoiceQueued;
    public event Action<Perk>? PerkAdded;
    public event Action<CrisisInfo>? CrisisStarted;
    public event Action<double>? InterestPaid;

    public GameSession(GameConfig? config = null, Random? random = null, string playerName = "Alex", RunSetup? setup = null)
    {
        Config = config ?? new GameConfig();
        _setup = setup;
        Seed = setup?.Seed ?? 0;
        _random = random ?? (setup != null ? new Random(setup.Seed) : new Random());
        PlayerName = setup?.Character.Name ?? playerName;

        _gameClock = new GameClock(Config.MaxDeltaTime);
        Company = new Company(Config);
        Upgrades = new Upgrades(Config.UpgradeTracks);
        Staff = new Staff(Config.EmployeeRoles);
        Businesses = new BusinessGroup(Config.BusinessCatalog, Config.BusinessCostGrowth,
            Config.HoldingCost, Config.HoldingIncomeMultiplier);
        Market = new Market(Config.MarketAssets, _random, Config.MonthDuration, Config.DaysPerMonth);
        Bills = new Bills(Config.MonthDuration, Config.MaxMissedPayments);
        Career = new Career(Config.CareerStages, Config.EmployeesForBoss);
        _requestGenerator = new RequestGenerator(_random, Config, Company.Stats);
        _perkDeck = new PerkDeck(Config.Perks, _random, setup?.Bonuses.LegendaryPerksUnlocked ?? false);

        Bills.BillPaid += OnMonthEnd;
        Company.TaskCompleted += OnTaskCompleted;
        Career.StageReached += stage =>
        {
            if (stage.Stage != CareerStage.KingOfTheWorld)
                QueueDraft($"Nouveau statut : {stage.Title}", Rarity.Rare);
        };

        if (setup != null)
            ApplySetup(setup);

        if (Config.RoguelikeEnabled)
        {
            NextCrisisMonth = Config.FirstCrisisMonth;
            NextCrisis = PickCrisis();
            _timeUntilEvent = Config.FirstEventDelay;
        }

        RecalculateStats();
        PeakNetWorth = NetWorth;
    }

    /// <summary>
    /// Fait avancer la run. L'ordre des systèmes est fixe :
    /// 1. temps, 2. multiplicateurs, 3. génération des demandes, 4. expiration des offres,
    /// 5. commerciaux, 6. avancement des tâches (et paiements), 7. revenus passifs (boîtes, bourse),
    /// 8. factures et fin de mois, 9. événements, 10. carrière, 11. vérification de fin.
    /// </summary>
    public void Update(double deltaTime)
    {
        if (IsOver || IsPaused)
            return;

        double delta = _gameClock.Tick(deltaTime);
        if (delta <= 0)
            return;

        RecalculateStats();

        foreach (var request in _requestGenerator.Update(delta))
            Company.AddRequest(request);

        Company.UpdateRequests(delta);
        Company.AutoAcceptRequests();
        Company.UpdateTasks(delta);

        Businesses.Update(delta, Company.Wallet, Modifiers.BusinessIncome);
        Market.Update(delta, Company.Wallet);

        Bills.Update(delta, Company.Wallet, CurrentBill);
        UpdateEvents(delta);

        PeakNetWorth = Math.Max(PeakNetWorth, NetWorth);
        Career.Update(new CareerSnapshot(NetWorth, Company.IsFounded, Staff.TotalCount, Businesses.HasHolding));

        CheckGameOver();
    }

    // --- Commandes du joueur ---

    public bool IsUnlocked(Feature feature) => Career.IsUnlocked(feature.RequiredStage());

    public HustleResult Hustle()
    {
        if (IsOver || IsPaused)
            return new HustleResult(0, 0);

        bool critical = Modifiers.ClickCritChance > 0 && _random.NextDouble() < Modifiers.ClickCritChance;
        var result = Company.Hustle(critical ? Modifiers.ClickCritMultiplier : 1);
        return result with { IsCritical = critical };
    }

    public double UpgradeCost(UpgradeTrack track) => (track.Next?.Cost ?? 0) * Modifiers.PurchaseCost;
    public double HiringCost(EmployeeRole role) => Staff.Info(role).HiringCost * Modifiers.HiringCost;

    /// <summary>Achète le niveau suivant d'une piste. Les locaux n'existent qu'une fois la boîte fondée.</summary>
    public bool BuyUpgrade(UpgradeTrackId id) =>
        CanAct && (id != UpgradeTrackId.Office || Company.IsFounded) &&
        Upgrades.TryBuy(id, Company.Wallet, Modifiers.PurchaseCost) && Recalculated();

    public bool FoundCompany(string name) => CanAct && IsUnlocked(Feature.FoundCompany) && Company.Found(name);

    public bool Hire(EmployeeRole role) =>
        CanAct && IsUnlocked(Feature.Hiring) && Company.IsFounded && HasFreeSeat &&
        Staff.Hire(role, Company.Wallet, Modifiers.HiringCost) && Recalculated();

    public bool Fire(EmployeeRole role) => CanAct && Staff.Fire(role) && Recalculated();

    public bool InvestInBusiness(int index) =>
        CanAct && IsUnlocked(Feature.Businesses) && Businesses.TryInvest(index, Company.Wallet);

    public bool BuyHolding() => CanAct && IsUnlocked(Feature.Holding) && Businesses.TryBuyHolding(Company.Wallet);

    public bool BuyAsset(int index, double amount) =>
        CanAct && IsUnlocked(Feature.Market) && Market.Buy(index, amount, Company.Wallet);

    public double SellAsset(int index) => CanAct ? Market.SellAll(index, Company.Wallet) : 0;

    /// <summary>Le joueur jette l'éponge : la run se termine comme une défaite.</summary>
    public void Abandon()
    {
        if (!IsOver)
            EndRun(false, "Abandon");
    }

    // --- Choix roguelike ---

    public bool ChoosePerk(int index)
    {
        if (PendingChoice is not Draft draft || index < 0 || index >= draft.Options.Count)
            return false;

        _choices.Dequeue();
        AddPerk(draft.Options[index]);
        return true;
    }

    public bool RerollDraft()
    {
        if (PendingChoice is not Draft draft || !Company.Wallet.TrySpend(draft.RerollCost))
            return false;

        draft.Reroll();
        return true;
    }

    public bool SkipDraft()
    {
        if (PendingChoice is not Draft draft)
            return false;

        _choices.Dequeue();
        Company.Wallet.Earn(draft.SkipReward);
        return true;
    }

    public bool CanChooseOption(GameEvent gameEvent, int index) =>
        index >= 0 && index < gameEvent.Info.Options.Count &&
        (gameEvent.Info.Options[index].IsAvailable?.Invoke(this) ?? true);

    /// <summary>Applique le choix d'un événement. Le joueur lit ensuite le résultat et confirme.</summary>
    public bool ChooseEventOption(int index)
    {
        if (PendingChoice is not GameEvent gameEvent || gameEvent.IsResolved || !CanChooseOption(gameEvent, index))
            return false;

        gameEvent.Resolve(gameEvent.Info.Options[index].Resolve(this, _random));
        RecalculateStats();
        return true;
    }

    public bool ConfirmEvent()
    {
        if (PendingChoice is not GameEvent { IsResolved: true })
            return false;

        _choices.Dequeue();
        return true;
    }

    // --- Utilisé par les événements et les crises ---

    public void AddPerk(Perk perk)
    {
        _perks.Add(perk);
        _perkDeck.Remove(perk);
        Bills.Shields += perk.Shields;
        RecalculateStats();
        PerkAdded?.Invoke(perk);
    }

    public void AddTimedEffect(TimedEffect effect)
    {
        _timedEffects.Add(effect);
        RecalculateStats();
    }

    public void QueueDraft(string title, Rarity minRarity)
    {
        if (!Config.RoguelikeEnabled || _perkDeck.Remaining == 0)
            return;

        double bill = Math.Max(300, CurrentBill.Total);
        var bonuses = _setup?.Bonuses ?? new LegacyBonuses();
        var draft = new Draft(title, _perkDeck, Config.DraftChoices + bonuses.ExtraDraftChoices, minRarity,
            bonuses.FreeRerolls, Config.RerollBaseCost * bill, Config.RerollCostGrowth, Config.SkipRewardFraction * bill);
        Enqueue(draft);
    }

    // --- Interne ---

    private bool CanAct => !IsOver && !IsPaused;

    private void ApplySetup(RunSetup setup)
    {
        double startingCash = setup.Character.StartingCash + setup.Bonuses.ExtraStartingCash;
        double difference = startingCash - Company.WalletBalance;
        if (difference > 0)
            Company.Wallet.Earn(difference);
        else if (difference < 0)
            Company.Wallet.Pay(-difference);

        foreach (var (track, levels) in setup.Character.StartingUpgrades ?? new Dictionary<UpgradeTrackId, int>())
            for (int i = 0; i < levels; i++)
                Upgrades.Grant(track);

        Company.Reputation.Add(setup.Character.StartingReputation + setup.Bonuses.StartingReputation);
        Bills.Shields += setup.Bonuses.StartingShields;

        var pressure = PressureCatalog.All[Math.Clamp(setup.Pressure, 0, PressureCatalog.All.Count - 1)];
        if (pressure.StartsCursed)
        {
            var curses = Config.Perks.Where(p => p.Rarity == Rarity.Curse && p.Id != "dilution").ToList();
            if (curses.Count > 0)
                _perks.Add(curses[_random.Next(curses.Count)]);
        }
    }

    /// <summary>Recalcule les bonus puis les multiplicateurs à partir du matériel, de l'équipe et de la réputation.</summary>
    private void RecalculateStats()
    {
        var mods = new Modifiers();
        if (_setup != null)
        {
            _setup.Character.Trait(mods);
            PressureCatalog.All[Math.Clamp(_setup.Pressure, 0, PressureCatalog.All.Count - 1)].Apply(mods);
        }
        foreach (var perk in _perks)
            perk.Apply(mods);
        foreach (var effect in _timedEffects)
            effect.Apply(mods);
        Modifiers = mods;

        var stats = Company.Stats;
        var reputation = Company.Reputation.Current;

        stats.WorkSpeed = Upgrades.WorkSpeedMultiplier * Staff.SpeedMultiplier * mods.WorkSpeed;
        int staffCapacity = (int)Math.Floor(Staff.ExtraCapacity * mods.StaffCapacity);
        stats.TaskCapacity = Config.MaxActiveTasks + staffCapacity + mods.ExtraCapacity;
        stats.ClickPower = Upgrades.ClickMultiplier * mods.ClickPower;
        stats.MaxDifficulty = reputation.MaxDifficulty;
        stats.ValueMultiplier = reputation.ValueMultiplier * mods.RequestValue;
        stats.AutoAccept = Staff.AutoAccept;
        stats.GigMultiplier = mods.GigMoney;
        stats.ReputationGain = mods.ReputationGain;
        stats.OfferDurationMultiplier = mods.OfferDuration;
        stats.DeadlineMultiplier = mods.DeadlineMargin;
        stats.ClicksAlwaysPay = mods.ClicksAlwaysPay;

        // Plus l'équipe est grande, plus il faut de demandes pour l'occuper
        int extraTasks = stats.TaskCapacity - Config.MaxActiveTasks;
        stats.RequestRateMultiplier = Upgrades.RequestRateMultiplier * Staff.RequestRateMultiplier *
                                      (1 + Config.RequestRatePerExtraTask * extraTasks) * mods.RequestRate;
    }

    private bool Recalculated()
    {
        RecalculateStats();
        return true;
    }

    private void OnTaskCompleted(WorkTask task)
    {
        if (!task.IsLate && Modifiers.OnTimeBonus > 0)
            Company.Wallet.Earn(task.Request.Value * Modifiers.OnTimeBonus);
    }

    /// <summary>
    /// Fin de mois : intérêts, effets qui s'épuisent, début de crise et bilan trimestriel.
    /// </summary>
    private void OnMonthEnd(BillBreakdown bill, bool unpaid)
    {
        _monthsCompleted++;
        _netWorthHistory.Add(NetWorth);
        _crisisAtLastBill = ActiveCrisis?.Name;
        if (!Config.RoguelikeEnabled)
            return;

        double cash = Company.WalletBalance;
        if (Modifiers.MonthlyInterest > 0 && cash > 0)
        {
            double interest = Math.Min(cash * Modifiers.MonthlyInterest, bill.Total * Config.InterestCapBills + 200);
            Company.Wallet.Earn(interest);
            InterestPaid?.Invoke(interest);
        }
        if (Modifiers.MarketMonthlyBonus > 0 && Market.TotalValue > 0)
            Company.Wallet.Earn(Market.TotalValue * Modifiers.MarketMonthlyBonus);

        bool crisisEnded = false;
        foreach (var effect in _timedEffects.ToList())
        {
            effect.MonthsLeft--;
            if (effect.MonthsLeft > 0)
                continue;

            _timedEffects.Remove(effect);
            if (effect == _crisisEffect)
            {
                crisisEnded = true;
                _crisisEffect = null;
                ActiveCrisis = null;
                if (!unpaid)
                    _crisesSurvived++;
            }
        }

        if (_monthsCompleted + 1 == NextCrisisMonth)
            StartCrisis();

        if (_monthsCompleted % Config.MonthsPerQuarter == 0)
            QueueDraft(crisisEnded ? "Crise surmontée !" : "Bilan du trimestre", crisisEnded ? Rarity.Rare : Rarity.Common);

        RecalculateStats();
    }

    private void StartCrisis()
    {
        if (NextCrisis is not { } crisis)
            return;

        ActiveCrisis = crisis;
        _crisisEffect = new TimedEffect(crisis.Name, crisis.Description, false, 1, crisis.Apply);
        _timedEffects.Add(_crisisEffect);
        crisis.OnStart?.Invoke(this);
        CrisisStarted?.Invoke(crisis);

        NextCrisisMonth += Config.CrisisEvery;
        NextCrisis = PickCrisis();
    }

    private CrisisInfo? PickCrisis()
    {
        var eligible = Config.Crises.Where(c => c.MinStage <= Career.Stage && c.Id != ActiveCrisis?.Id).ToList();
        return eligible.Count == 0 ? null : eligible[_random.Next(eligible.Count)];
    }

    private void UpdateEvents(double delta)
    {
        if (!Config.RoguelikeEnabled || Config.Events.Count == 0)
            return;

        _timeUntilEvent -= delta;
        if (_timeUntilEvent > 0 || IsPaused)
            return;

        _timeUntilEvent = Config.MinEventInterval + _random.NextDouble() * (Config.MaxEventInterval - Config.MinEventInterval);

        var eligible = Config.Events.Where(e => e.Id != _lastEventId && (e.CanHappen?.Invoke(this) ?? true)).ToList();
        if (eligible.Count == 0)
            return;

        var info = eligible[_random.Next(eligible.Count)];
        _lastEventId = info.Id;
        Enqueue(new GameEvent(info));
    }

    private void Enqueue(IPendingChoice choice)
    {
        _choices.Enqueue(choice);
        ChoiceQueued?.Invoke(choice);
    }

    private void CheckGameOver()
    {
        if (Career.Stage == CareerStage.KingOfTheWorld)
            EndRun(true, null);
        else if (Bills.IsBankrupt)
            EndRun(false, _crisisAtLastBill != null
                ? $"Achevé par la crise « {_crisisAtLastBill} »"
                : "Deux factures impayées d'affilée");
    }

    private void EndRun(bool victory, string? cause)
    {
        _choices.Clear();
        Result = new GameResult(
            victory,
            ElapsedTime,
            NetWorth,
            PeakNetWorth,
            Career.Current.Title,
            Company.TotalEarned,
            Company.CompletedTaskCount,
            Company.FailedTaskCount)
        {
            StageIndex = (int)Career.Stage,
            Months = _monthsCompleted,
            CauseOfDeath = victory ? null : cause,
            NetWorthHistory = _netWorthHistory.Append(NetWorth).ToList(),
            PerkIds = _perks.Select(p => p.Id).ToList(),
            CharacterName = PlayerName,
            Seed = Seed,
            Pressure = Pressure,
            CrisesSurvived = _crisesSurvived
        };

        GameOver?.Invoke(Result);
    }
}
