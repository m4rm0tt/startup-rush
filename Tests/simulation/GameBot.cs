using Core.progression;
using Core.roguelike;
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

    /// <summary>Joue « bien » : clique, accepte, achète, embauche, crée des boîtes (politique mixte).</summary>
    Tycoon
}

/// <summary>Ce que le bot achète en priorité.</summary>
public enum SpendCategory { Hiring, Office, Equipment, Housing, Businesses }

/// <summary>
/// Une façon de jouer. Le bot achète l'option la moins chère après pondération :
/// un poids de 0,2 rend une catégorie cinq fois plus attirante, 0 l'interdit.
/// </summary>
public record BotPolicy(
    string Name,
    double ClicksPerSecond,
    double ReserveMonths,
    IReadOnlyDictionary<SpendCategory, double> Weights,
    bool Buys = true)
{
    public static readonly BotPolicy Idle = new("inactif", 0, 0, new Dictionary<SpendCategory, double>(), Buys: false);
    public static readonly BotPolicy TasksOnly = new("contrats seuls", 4, 0, new Dictionary<SpendCategory, double>(), Buys: false);

    public static readonly BotPolicy Mixed = new("mixte", 4, 2, Weigh(1, 1, 1, 1, 1));
    public static readonly BotPolicy Prudent = new("prudent", 4, 5, Weigh(1.5, 1.5, 1, 3, 2));
    public static readonly BotPolicy Equipment = new("matériel", 4, 2, Weigh(4, 4, 0.2, 2, 4));
    public static readonly BotPolicy Team = new("équipe", 4, 2, Weigh(0.2, 0.2, 2, 3, 3));
    public static readonly BotPolicy Empire = new("empire", 4, 2, Weigh(2, 2, 2, 3, 0.2));
    public static readonly BotPolicy Clicker = new("clic", 7, 2, Weigh(3, 3, 0.3, 2, 3));
    public static readonly BotPolicy Spender = new("dépensier", 4, 0.5, Weigh(1, 1, 1, 1, 1));

    public static IReadOnlyList<BotPolicy> Playstyles { get; } = [Prudent, Equipment, Team, Empire, Clicker, Mixed, Spender];

    private static Dictionary<SpendCategory, double> Weigh(double hiring, double office, double equipment, double housing,
        double businesses) => new()
    {
        [SpendCategory.Hiring] = hiring,
        [SpendCategory.Office] = office,
        [SpendCategory.Equipment] = equipment,
        [SpendCategory.Housing] = housing,
        [SpendCategory.Businesses] = businesses,
    };

    public static BotPolicy From(BotStrategy strategy) => strategy switch
    {
        BotStrategy.Idle => Idle,
        BotStrategy.TasksOnly => TasksOnly,
        _ => Mixed
    };
}

/// <summary>
/// Joue une partie sans interface pour vérifier que la progression est possible
/// et ni trop rapide ni trop lente. Le bot clique à un rythme humain fixe et n'a aucun avantage caché.
/// </summary>
public class GameBot
{
    private const double Step = 0.1;

    private readonly BotPolicy _policy;
    private double _clickBudget;

    /// <summary>Moment où chaque statut a été atteint, en secondes de jeu.</summary>
    public Dictionary<CareerStage, double> StageTimes { get; } = new();

    public GameBot(BotStrategy strategy) : this(BotPolicy.From(strategy))
    {
    }

    public GameBot(BotPolicy policy)
    {
        _policy = policy;
    }

    public static GameSession Play(GameSession session, BotStrategy strategy, double maxTime = 3 * 3600) =>
        new GameBot(strategy).Run(session, maxTime);

    public GameSession Run(GameSession session, double maxTime = 3 * 3600)
    {
        if (StageTimes.Count == 0)
            session.Career.StageReached += s => StageTimes[s.Stage] = session.ElapsedTime;

        while (!session.IsOver && session.ElapsedTime < maxTime)
        {
            HandleChoices(session);

            if (_policy.ClicksPerSecond > 0)
            {
                AcceptRequests(session);
                Click(session);
            }

            if (_policy.Buys)
                Spend(session);

            session.Update(Step);
        }

        return session;
    }

    /// <summary>
    /// Atouts : le plus rare (les témoins qui n'achètent rien passent, pour ne pas fausser leur rôle).
    /// Événements : toujours le dernier choix, le prudent.
    /// </summary>
    private void HandleChoices(GameSession session)
    {
        while (session.PendingChoice is { } choice)
        {
            if (choice is Draft draft)
            {
                if (_policy.Buys)
                {
                    var best = draft.Options.Select((perk, i) => (perk, i)).MaxBy(o => o.perk.Rarity);
                    session.ChoosePerk(best.i);
                }
                else
                {
                    session.SkipDraft();
                }
            }
            else if (choice is GameEvent gameEvent)
            {
                if (!gameEvent.IsResolved)
                    session.ChooseEventOption(gameEvent.Info.Options.Count - 1);
                session.ConfirmEvent();
            }
        }
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
        _clickBudget += Step * _policy.ClicksPerSecond;
        while (_clickBudget >= 1)
        {
            session.Hustle();
            _clickBudget--;
        }
    }

    private void Spend(GameSession session)
    {
        for (int i = 0; i < 5 && TryBuySomething(session); i++)
        {
        }
    }

    // Garde toujours quelques mois de factures, puis achète l'option la moins chère après pondération
    private bool TryBuySomething(GameSession session)
    {
        double spendable = session.Company.WalletBalance - session.CurrentBill.Total * _policy.ReserveMonths;
        if (spendable <= 0)
            return false;

        // Les étapes obligatoires de l'histoire passent avant tout
        if (session.IsUnlocked(Feature.Holding) && !session.Businesses.HasHolding)
            return session.Businesses.HoldingCost <= spendable && session.BuyHolding();

        if (session.IsUnlocked(Feature.FoundCompany) && !session.Company.IsFounded)
            return session.Config.CompanyFoundingCost <= spendable && session.FoundCompany("Bot Corp");

        var options = new List<(double Cost, SpendCategory Category, Func<bool> Buy)>();

        if (session.Company.IsFounded && session.HasFreeSeat && NextHire(session) is { } role)
        {
            var info = session.Staff.Info(role);
            options.Add((info.HiringCost + info.MonthlySalary * 6, SpendCategory.Hiring, () => session.Hire(role)));
        }

        foreach (var track in session.Upgrades.Tracks.Where(t => t.Next != null))
        {
            var category = track.Info.Id switch
            {
                UpgradeTrackId.Housing => SpendCategory.Housing,
                UpgradeTrackId.Office => SpendCategory.Office,
                _ => SpendCategory.Equipment
            };
            if (category == SpendCategory.Office && !session.Company.IsFounded)
                continue;

            // Un logement se paie aussi chaque mois : on attend d'en avoir largement les moyens
            double cost = category == SpendCategory.Housing ? track.Next!.Cost * 4 : track.Next!.Cost;
            options.Add((cost, category, () => session.BuyUpgrade(track.Info.Id)));
        }

        if (session.IsUnlocked(Feature.Businesses))
        {
            for (int i = 0; i < session.Businesses.Businesses.Count; i++)
            {
                int index = i;
                options.Add((session.Businesses.Businesses[i].NextCost, SpendCategory.Businesses,
                    () => session.InvestInBusiness(index)));
            }
        }

        var choice = options
            .Where(o => o.Cost <= spendable && _policy.Weights.GetValueOrDefault(o.Category) > 0)
            .OrderBy(o => o.Cost * _policy.Weights[o.Category])
            .FirstOrDefault();
        return choice.Buy != null && choice.Buy();
    }

    private static EmployeeRole? NextHire(GameSession session)
    {
        var staff = session.Staff;
        if (staff.TotalCount == 0)
            return EmployeeRole.Intern;
        if (staff.Count(EmployeeRole.Salesperson) == 0)
            return EmployeeRole.Salesperson;
        if (staff.Count(EmployeeRole.ProjectManager) < staff.TotalCount / 6)
            return EmployeeRole.ProjectManager;
        return EmployeeRole.Developer;
    }
}
