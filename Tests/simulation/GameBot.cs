using Core.progression;
using Core.simulation;
using Core.staff;
using Core.upgrades;

namespace Tests.simulation;

public enum BotStrategy
{
    /// <summary>Ne fait rien.</summary>
    Idle,

    /// <summary>Clique et accepte les contrats faisables, mais n'achète jamais rien.</summary>
    TasksOnly,

    /// <summary>Joue « bien » : clique, accepte, achète, embauche, crée des boîtes.</summary>
    Tycoon
}

/// <summary>
/// Joue une partie sans interface pour vérifier que la progression est possible
/// et ni trop rapide ni trop lente.
/// </summary>
public class GameBot
{
    private const double Step = 0.1;
    private const double ClicksPerSecond = 4; // un humain qui charbonne tranquillement

    private readonly BotStrategy _strategy;
    private double _clickBudget;

    /// <summary>Moment où chaque statut a été atteint, en secondes de jeu.</summary>
    public Dictionary<CareerStage, double> StageTimes { get; } = new();

    public GameBot(BotStrategy strategy)
    {
        _strategy = strategy;
    }

    public static GameSession Play(GameSession session, BotStrategy strategy, double maxTime = 3 * 3600) =>
        new GameBot(strategy).Run(session, maxTime);

    public GameSession Run(GameSession session, double maxTime = 3 * 3600)
    {
        session.Career.StageReached += s => StageTimes[s.Stage] = session.ElapsedTime;

        while (!session.IsOver && session.ElapsedTime < maxTime)
        {
            if (_strategy != BotStrategy.Idle)
            {
                AcceptRequests(session);
                Click(session);
            }

            if (_strategy == BotStrategy.Tycoon)
                Spend(session);

            session.Update(Step);
        }

        return session;
    }

    private static void AcceptRequests(GameSession session)
    {
        var company = session.Company;
        var candidates = company.PendingRequests
            .Where(company.CanFinishOnTime)
            .OrderByDescending(r => r.Value / r.Difficulty)
            .ToList();

        foreach (var request in candidates)
            company.AcceptRequest(request.Id);
    }

    private void Click(GameSession session)
    {
        _clickBudget += Step * ClicksPerSecond;
        while (_clickBudget >= 1)
        {
            session.Hustle();
            _clickBudget--;
        }
    }

    private static void Spend(GameSession session)
    {
        for (int i = 0; i < 5 && TryBuySomething(session); i++)
        {
        }
    }

    // Garde toujours de quoi payer deux mois de factures, puis achète l'option la moins chère
    private static bool TryBuySomething(GameSession session)
    {
        double spendable = session.Company.WalletBalance - session.CurrentBill.Total * 2;
        if (spendable <= 0)
            return false;

        if (session.IsUnlocked(Feature.Holding) && !session.Businesses.HasHolding)
            return session.Businesses.HoldingCost <= spendable && session.BuyHolding();

        if (session.IsUnlocked(Feature.FoundCompany) && !session.Company.IsFounded)
            return session.Config.CompanyFoundingCost <= spendable && session.FoundCompany("Bot Corp");

        if (session.Company.IsFounded && session.HasFreeSeat && TryHire(session, spendable))
            return true;

        var housing = session.Upgrades[UpgradeTrackId.Housing];
        if (housing.Next is { } nextHome && nextHome.Cost <= spendable * 0.25 && session.BuyUpgrade(UpgradeTrackId.Housing))
            return true;

        var options = new List<(double Cost, Func<bool> Buy)>();
        foreach (var track in session.Upgrades.Tracks.Where(t => t.Info.Id != UpgradeTrackId.Housing && t.Next != null &&
                                                                 (t.Info.Id != UpgradeTrackId.Office || session.Company.IsFounded)))
            options.Add((track.Next!.Cost, () => session.BuyUpgrade(track.Info.Id)));

        if (session.IsUnlocked(Feature.Businesses))
        {
            for (int i = 0; i < session.Businesses.Businesses.Count; i++)
            {
                int index = i;
                options.Add((session.Businesses.Businesses[i].NextCost, () => session.InvestInBusiness(index)));
            }
        }

        var cheapest = options.Where(o => o.Cost <= spendable).OrderBy(o => o.Cost).FirstOrDefault();
        return cheapest.Buy != null && cheapest.Buy();
    }

    private static bool TryHire(GameSession session, double spendable)
    {
        var staff = session.Staff;
        EmployeeRole role;
        if (staff.TotalCount == 0)
            role = EmployeeRole.Intern;
        else if (staff.Count(EmployeeRole.Salesperson) == 0)
            role = EmployeeRole.Salesperson;
        else if (staff.Count(EmployeeRole.ProjectManager) < staff.TotalCount / 6)
            role = EmployeeRole.ProjectManager;
        else
            role = EmployeeRole.Developer;

        var info = staff.Info(role);
        return info.HiringCost + info.MonthlySalary * 6 <= spendable && session.Hire(role);
    }
}
