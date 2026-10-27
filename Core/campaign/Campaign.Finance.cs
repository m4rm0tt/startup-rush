using Core.finance;
using Core.products;

namespace Core.campaign;

/// <summary>Financement et redressement : prêt bancaire, investisseur, plan de redressement. Aucun n'est illimité ni gratuit.</summary>
public partial class Campaign
{
    /// <summary>Revenus d'exploitation moyens par mois sur les 3 derniers mois complets.</summary>
    public decimal AverageMonthlyRevenue()
    {
        var months = Enumerable.Range(1, 3).Select(i => GameTime.ToDate(Minute).AddMonths(-i).ToString("yyyy-MM")).ToList();
        decimal total = 0;
        foreach (var month in months)
            if (Ledger.Months.TryGetValue(month, out var totals))
                total += totals.Where(t => t.Key is Category.Contracts or Category.Subscriptions or Category.Ads or Category.Commissions).Sum(t => t.Value);
        return total / 3;
    }

    /// <summary>Conditions du prêt bancaire : ce qui manque, ou le montant maximal.</summary>
    public (decimal Max, string Condition) LoanTerms()
    {
        if (Ledger.Loans.Any(l => l.Kind == LoanKind.Bank))
            return (0, "Un prêt bancaire est déjà en cours.");
        if (Ledger.Overdue(Minute).Any())
            return (0, "La banque refuse tant qu'il y a des impayés.");
        if (Day < 60)
            return (0, "La banque veut au moins 60 jours d'historique.");
        decimal max = Math.Floor(AverageMonthlyRevenue() * Tuning.Finance.LoanMonthsOfRevenue / 500m) * 500m;
        if (max < 2_000)
            return (0, "Revenus des 3 derniers mois trop faibles (le plafond est de 3 mois de revenus).");
        return (max, $"Jusqu'à {max:N0} CHF sur {Tuning.Finance.LoanMonths} mois à {Tuning.Finance.LoanYearlyRate:P0} par an.");
    }

    public CommandResult TakeLoan(decimal amount)
    {
        var (max, condition) = LoanTerms();
        if (max <= 0)
            return CommandResult.Fail(condition);
        if (amount <= 0 || amount > max)
            return CommandResult.Fail($"Montant entre 1 et {max:N0} CHF.");
        decimal total = amount * (1 + Tuning.Finance.LoanYearlyRate * Tuning.Finance.LoanMonths / 12m);
        Ledger.Loans.Add(new Loan
        {
            Id = NewId(),
            Kind = LoanKind.Bank,
            Principal = amount,
            Remaining = Math.Round(total, 2),
            MonthlyPayment = Math.Round(total / Tuning.Finance.LoanMonths, 2),
            MonthsLeft = Tuning.Finance.LoanMonths,
            StartedAt = Minute,
        });
        Post(amount, Category.LoanReceived, "Prêt bancaire");
        return CommandResult.Success();
    }

    /// <summary>
    /// Un investisseur se manifeste (une seule levée possible) si un produit a au moins 1 500 utilisateurs actifs
    /// ou 100 clients payants, et a grandi d'au moins 20 % en 30 jours.
    /// </summary>
    private void OfferInvestment()
    {
        var candidate = Products.Where(p => p.IsLive && p.History.Count >= 30 && (p.Users >= 1_500 || p.Paying >= 100))
            .FirstOrDefault(p => p.History[^1].Users >= p.History[^30].Users * 1.2);
        if (candidate == null)
            return;
        decimal valuation = Valuation();
        decimal amount = Math.Clamp(Math.Round(valuation * 0.2m / 5_000m) * 5_000m, 50_000m, 400_000m);
        double equity = Math.Round(Math.Clamp((double)(amount / Math.Max(1, valuation + amount)), 0.1, 0.3), 3);
        Investor = new InvestorOffer
        {
            Investor = Random.Pick(["Léman Ventures", "Alpine Angels", "Romandie Seed", "Fonds Innovation Vaud"]),
            Amount = amount,
            Equity = equity,
            ExpiresAt = Minute + 21L * GameTime.MinutesPerDay,
            ProductId = candidate.Id,
        };
        Notify(Severity.Good, "finance", $"{Investor.Investor} veut investir",
            $"{amount:N0} CHF contre {equity:P0} du capital, grâce à la croissance de {candidate.Name}. Offre valable 21 jours.", "finance");
    }

    public CommandResult AcceptInvestor()
    {
        if (Investor == null)
            return CommandResult.Fail("Aucune offre.");
        Post(Investor.Amount, Category.Funding, $"Levée de fonds : {Investor.Investor}");
        FounderEquity *= 1 - Investor.Equity;
        InvestorRoundDone = true;
        Investor = null;
        return CommandResult.Success();
    }

    public void DeclineInvestor() => Investor = null;

    /// <summary>
    /// Plan de redressement : les dettes échues sont regroupées et étalées sur 6 mois avec un surcoût de 10 %.
    /// Possible seulement en impayés ou mise en demeure, si les revenus récurrents couvrent au moins la moitié
    /// des charges fixes, et une seule fois tous les deux ans.
    /// </summary>
    public (bool Allowed, string Condition) RecoveryPlanTerms()
    {
        var overdue = Ledger.Overdue(Minute).ToList();
        if (overdue.Count == 0)
            return (false, "Aucune dette échue à étaler.");
        if (Solvency.LastRecoveryPlanAt is long last && Minute - last < Tuning.Finance.RecoveryPlanCooldownDays * (long)GameTime.MinutesPerDay)
            return (false, $"Un plan a déjà été accordé le {GameTime.FormatDate(last)} (un tous les deux ans).");
        decimal coverage = MonthlyFixedCosts <= 0 ? 1 : RecurringRevenue30 / MonthlyFixedCosts;
        if (coverage < Tuning.Finance.RecoveryPlanMinCoverage)
            return (false, $"Les revenus récurrents ne couvrent que {coverage:P0} des charges fixes (50 % exigés). Réduis les charges ou fais grandir un produit.");
        decimal total = overdue.Sum(b => b.Amount);
        return (true, $"{total:N0} CHF étalés sur {Tuning.Finance.RecoveryPlanMonths} mois, +{Tuning.Finance.RecoveryPlanSurcharge:P0} ({total * (1 + Tuning.Finance.RecoveryPlanSurcharge) / Tuning.Finance.RecoveryPlanMonths:N0} CHF par mois).");
    }

    public CommandResult RequestRecoveryPlan()
    {
        var (allowed, condition) = RecoveryPlanTerms();
        if (!allowed)
            return CommandResult.Fail(condition);
        var overdue = Ledger.Overdue(Minute).ToList();
        decimal total = overdue.Sum(b => b.Amount) * (1 + Tuning.Finance.RecoveryPlanSurcharge);
        foreach (var bill in overdue)
        {
            bill.PaidAt = Minute;
            if (bill.PersonId is int id && FindPerson(id) is { } person)
                person.UnpaidDays = 0;
        }
        Ledger.Loans.Add(new Loan
        {
            Id = NewId(),
            Kind = LoanKind.RecoveryPlan,
            Principal = total,
            Remaining = Math.Round(total, 2),
            MonthlyPayment = Math.Round(total / Tuning.Finance.RecoveryPlanMonths, 2),
            MonthsLeft = Tuning.Finance.RecoveryPlanMonths,
            StartedAt = Minute,
        });
        Solvency.LastRecoveryPlanAt = Minute;
        Solvency.NoticeUntil = null;
        Solvency.State = SolvencyState.Tight;
        Notify(Severity.Warning, "finance", "Plan de redressement accordé", condition, "finance");
        return CommandResult.Success();
    }

    /// <summary>
    /// Valeur estimée de l'entreprise : actif net (trésorerie, créances à 90 %, matériel à la revente, moins dettes et prêts)
    /// plus trois années du bénéfice récurrent actuel des produits s'il est positif. Une estimation, pas un prix de vente.
    /// </summary>
    public decimal Valuation()
    {
        decimal equipment = Premises.Items.Where(i => i.Def.Movable).Sum(i => ResaleValue(i.Def))
                            + Storage.Sum(s => ResaleValue(space.FurnitureCatalog.Get(s.DefId)));
        decimal net = Ledger.Cash + Ledger.OpenReceivables * 0.9m + equipment + Ledger.DepositHeld - Ledger.OpenBills - Ledger.LoanDebt;
        decimal productProfit = Products.Sum(p => p.History.Skip(Math.Max(0, p.History.Count - 30)).Sum(d => d.Revenue - d.Costs));
        return Math.Round(net + Math.Max(0, productProfit) * 36);
    }
}
