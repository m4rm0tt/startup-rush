using Core.businesses;
using Core.company;
using Core.finance;
using Core.investments;
using Core.progression;
using Core.requests;
using Core.staff;
using Core.upgrades;

namespace Core.simulation;

/// <summary>
/// Une partie : rassemble tous les systèmes, les fait avancer dans un ordre fixe
/// et expose les commandes du joueur. Chaque commande vérifie qu'elle est débloquée.
/// </summary>
public class GameSession
{
    private readonly GameClock _gameClock;
    private readonly RequestGenerator _requestGenerator;

    public GameConfig Config { get; }
    public string PlayerName { get; }

    public Company Company { get; }
    public PlayerStats Stats => Company.Stats;
    public Upgrades Upgrades { get; }
    public Staff Staff { get; }
    public BusinessGroup Businesses { get; }
    public Market Market { get; }
    public Bills Bills { get; }
    public Career Career { get; }

    public double ElapsedTime => _gameClock.Time;
    public int Month => (int)(ElapsedTime / Config.MonthDuration) + 1;
    public int Day => (int)(ElapsedTime % Config.MonthDuration / (Config.MonthDuration / Config.DaysPerMonth)) + 1;

    /// <summary>Argent + bourse + ce qui a été investi dans les boîtes.</summary>
    public double NetWorth => Company.WalletBalance + Market.TotalValue + Businesses.Valuation;
    public double PeakNetWorth { get; private set; }

    /// <summary>Ce que coûterait la facture si elle tombait maintenant.</summary>
    public BillBreakdown CurrentBill
    {
        get
        {
            double housing = Upgrades[UpgradeTrackId.Housing].Current.MonthlyCost;
            double office = Company.IsFounded ? Upgrades[UpgradeTrackId.Office].Current.MonthlyCost : 0;
            double subscriptions = Upgrades.MonthlyCost - housing - Upgrades[UpgradeTrackId.Office].Current.MonthlyCost;
            return new BillBreakdown(housing, Staff.MonthlySalaries + office, subscriptions);
        }
    }

    /// <summary>Vrai s'il reste une place libre dans les locaux.</summary>
    public bool HasFreeSeat => Staff.TotalCount < Upgrades.EmployeeSeats;

    public bool IsOver => Result != null;

    /// <summary>Bilan de la partie, null tant qu'elle n'est pas terminée.</summary>
    public GameResult? Result { get; private set; }

    public event Action<GameResult>? GameOver;

    public GameSession(GameConfig? config = null, Random? random = null, string playerName = "Alex")
    {
        Config = config ?? new GameConfig();
        random ??= new Random();
        PlayerName = playerName;

        _gameClock = new GameClock(Config.MaxDeltaTime);
        Company = new Company(Config);
        Upgrades = new Upgrades(Config.UpgradeTracks);
        Staff = new Staff(Config.EmployeeRoles);
        Businesses = new BusinessGroup(Config.BusinessCatalog, Config.BusinessCostGrowth,
            Config.HoldingCost, Config.HoldingIncomeMultiplier);
        Market = new Market(Config.MarketAssets, random, Config.MonthDuration, Config.DaysPerMonth);
        Bills = new Bills(Config.MonthDuration, Config.MaxMissedPayments);
        Career = new Career(Config.CareerStages, Config.EmployeesForBoss);
        _requestGenerator = new RequestGenerator(random, Config, Company.Stats);

        PeakNetWorth = NetWorth;
        RecalculateStats();
    }

    /// <summary>
    /// Fait avancer la partie. L'ordre des systèmes est fixe :
    /// 1. temps, 2. multiplicateurs, 3. génération des demandes, 4. expiration des offres,
    /// 5. commerciaux, 6. avancement des tâches (et paiements), 7. revenus passifs (boîtes, bourse),
    /// 8. factures, 9. carrière, 10. vérification de fin.
    /// </summary>
    public void Update(double deltaTime)
    {
        if (IsOver)
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

        Businesses.Update(delta, Company.Wallet);
        Market.Update(delta, Company.Wallet);

        Bills.Update(delta, Company.Wallet, CurrentBill);

        PeakNetWorth = Math.Max(PeakNetWorth, NetWorth);
        Career.Update(new CareerSnapshot(NetWorth, Company.IsFounded, Staff.TotalCount, Businesses.HasHolding));

        CheckGameOver();
    }

    // --- Commandes du joueur ---

    public bool IsUnlocked(Feature feature) => Career.IsUnlocked(feature.RequiredStage());

    public HustleResult Hustle() => IsOver ? new HustleResult(0, 0) : Company.Hustle();

    /// <summary>Achète le niveau suivant d'une piste. Les locaux n'existent qu'une fois la boîte fondée.</summary>
    public bool BuyUpgrade(UpgradeTrackId id) =>
        !IsOver && (id != UpgradeTrackId.Office || Company.IsFounded) &&
        Upgrades.TryBuy(id, Company.Wallet) && Recalculated();

    public bool FoundCompany(string name) => !IsOver && IsUnlocked(Feature.FoundCompany) && Company.Found(name);

    public bool Hire(EmployeeRole role) =>
        !IsOver && IsUnlocked(Feature.Hiring) && Company.IsFounded && HasFreeSeat &&
        Staff.Hire(role, Company.Wallet) && Recalculated();

    public bool Fire(EmployeeRole role) => !IsOver && Staff.Fire(role) && Recalculated();

    public bool InvestInBusiness(int index) =>
        !IsOver && IsUnlocked(Feature.Businesses) && Businesses.TryInvest(index, Company.Wallet);

    public bool BuyHolding() => !IsOver && IsUnlocked(Feature.Holding) && Businesses.TryBuyHolding(Company.Wallet);

    public bool BuyAsset(int index, double amount) =>
        !IsOver && IsUnlocked(Feature.Market) && Market.Buy(index, amount, Company.Wallet);

    public double SellAsset(int index) => IsOver ? 0 : Market.SellAll(index, Company.Wallet);

    // --- Interne ---

    /// <summary>Recalcule les multiplicateurs à partir du matériel, de l'équipe et de la réputation.</summary>
    private void RecalculateStats()
    {
        var stats = Company.Stats;
        var reputation = Company.Reputation.Current;

        stats.WorkSpeed = Upgrades.WorkSpeedMultiplier * Staff.SpeedMultiplier;
        stats.TaskCapacity = Config.MaxActiveTasks + Staff.ExtraCapacity;
        stats.ClickPower = Upgrades.ClickMultiplier;
        stats.MaxDifficulty = reputation.MaxDifficulty;
        stats.ValueMultiplier = reputation.ValueMultiplier;
        stats.AutoAccept = Staff.AutoAccept;

        // Plus l'équipe est grande, plus il faut de demandes pour l'occuper
        int extraTasks = stats.TaskCapacity - Config.MaxActiveTasks;
        stats.RequestRateMultiplier = Upgrades.RequestRateMultiplier * Staff.RequestRateMultiplier *
                                      (1 + Config.RequestRatePerExtraTask * extraTasks);
    }

    private bool Recalculated()
    {
        RecalculateStats();
        return true;
    }

    private void CheckGameOver()
    {
        bool victory = Career.Stage == CareerStage.KingOfTheWorld;
        if (!victory && !Bills.IsBankrupt)
            return;

        Result = new GameResult(
            victory,
            ElapsedTime,
            NetWorth,
            PeakNetWorth,
            Career.Current.Title,
            Company.TotalEarned,
            Company.CompletedTaskCount,
            Company.FailedTaskCount);

        GameOver?.Invoke(Result);
    }
}
