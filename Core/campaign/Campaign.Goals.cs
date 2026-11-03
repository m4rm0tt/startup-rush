using Core.finance;

namespace Core.campaign;

/// <summary>Une ligne du bilan : un produit, ses utilisateurs et ce qu'il a rapporté sur 30 jours.</summary>
public record SummaryProduct(string Name, double Users, decimal Revenue30);

/// <summary>Le bilan d'une partie (réussite ou faillite) : durée, finances, utilisateurs, principaux produits.</summary>
public record GameSummary(long Days, decimal Cash, decimal Revenue30, decimal MonthlyCosts, decimal? LowestCash, double Users,
    int People, decimal Valuation, IReadOnlyList<SummaryProduct> Products);

/// <summary>
/// Objectifs (lot 5 de la version Steam) :
/// - objectif final explicite : une entreprise valorisée 2 millions de CHF, avec la valorisation déjà utilisée par les
///   investisseurs (Campaign.Valuation : actif net + 36 mois du bénéfice récurrent des produits), une entreprise saine (pas
///   d'impayés) et au moins un produit en ligne. Pas de bourse. D'après BALANCE_REPORT.md (36 mois) : plusieurs_petits
///   ≈ 39 000 CHF de revenus par mois et 450 000 CHF de trésorerie → ≈ 1,7 à 1,9 M à 3 ans ; materiel_pc (80 000 CHF/mois)
///   au-delà. L'objectif tombe donc vers 3 à 4 ans pour une bonne stratégie logicielle, plus tôt pour le matériel rentable.
/// - objectifs progressifs : les trois prochains jalons non atteints (Campaign.MilestoneDefs), qui donnent déjà leurs points
///   modestes ; aucune récompense nouvelle (l'économie ne change pas).
/// Atteindre l'objectif ne termine pas la partie : le bilan s'affiche une fois, puis on continue.
/// </summary>
public partial class Campaign
{
    public const decimal FinalGoalValuation = 2_000_000m;

    /// <summary>Minute où l'objectif final a été atteint (null : pas encore). Sauvegardé.</summary>
    public long? FinalGoalReachedAt { get; set; }

    /// <summary>Le bilan de réussite a été montré (il ne revient pas). Sauvegardé.</summary>
    public bool FinalGoalAcknowledged { get; set; }

    /// <summary>Le plus bas niveau de trésorerie atteint en fin de journée (null pour une ancienne sauvegarde, jusqu'au lendemain).</summary>
    public decimal? LowestCash { get; set; }

    public bool FinalGoalMet =>
        Status == CampaignStatus.Running && Valuation() >= FinalGoalValuation && Solvency.State == SolvencyState.Healthy && Products.Any(p => p.IsLive);

    /// <summary>Les prochains jalons à viser, dans l'ordre (3 au plus).</summary>
    public IReadOnlyList<(string Id, string Name, string Description)> NextGoals(int count = 3) =>
        MilestoneDefs.Where(m => !Milestones.ContainsKey(m.Id)).Take(count).ToList();

    /// <summary>Chaque soir : trésorerie la plus basse et objectif final.</summary>
    private void CheckGoals()
    {
        LowestCash = LowestCash is { } low ? Math.Min(low, Ledger.Cash) : Ledger.Cash;
        if (FinalGoalReachedAt != null || !FinalGoalMet)
            return;
        FinalGoalReachedAt = Minute;
        Notify(Severity.Good, "entreprise", "Objectif atteint : 2 millions de valorisation",
            $"{MainCompany.Name} vaut {Valuation():N0} CHF. Tu peux continuer la partie aussi longtemps que tu veux.", "finances");
    }

    public GameSummary Summary()
    {
        var products = Products
            .Select(p => new SummaryProduct(p.Name, p.Users, p.History.Skip(Math.Max(0, p.History.Count - 30)).Sum(d => d.Revenue)))
            .OrderByDescending(p => p.Revenue30).ThenByDescending(p => p.Users).Take(5).ToList();
        return new GameSummary(Day, Ledger.Cash, RecurringRevenue30, MonthlyFixedCosts, LowestCash, Products.Sum(p => p.Users),
            People.Count, Valuation(), products);
    }
}
