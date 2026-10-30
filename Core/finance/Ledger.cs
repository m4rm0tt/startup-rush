namespace Core.finance;

public enum Category
{
    // Recettes
    Contracts, Subscriptions, Ads, Commissions, Sales, Funding, LoanReceived, Resale, DepositRefund,

    // Dépenses
    Salaries, FounderDraw, Rent, Utilities, Equipment, Hosting, Marketing, Training, Severance,
    Interest, LoanRepayment, Deposit, Moving, Fees, Refund, Registration,

    // Lot 4 : services cloud vendus (recette), électricité et réseau des sites (dépense)
    Cloud, Energy,

    // Lot 5 : matières achetées, garanties et retours, recherche, frais des canaux de vente
    Materials, Warranty, Research, ChannelFees,

    // Finition : transport de stock entre sites
    Logistics
}

/// <summary>Un mouvement de trésorerie (argent réellement entré ou sorti).</summary>
public class Entry
{
    public long Minute { get; set; }
    public decimal Amount { get; set; }
    public Category Category { get; set; }
    public string Label { get; set; } = "";
    public int? CompanyId { get; set; }

    /// <summary>Site auquel le mouvement est rattaché (null : commun à l'entreprise).</summary>
    public int? SiteId { get; set; }
}

/// <summary>
/// Une facture émise à un client : c'est une créance, pas de l'argent. Elle est encaissée à une date qui dépend
/// de la fiabilité du client, parfois jamais (créance irrécouvrable).
/// </summary>
public class Invoice
{
    public int Id { get; set; }
    public string Client { get; set; } = "";
    public string Label { get; set; } = "";
    public decimal Amount { get; set; }
    public Category Category { get; set; } = Category.Contracts;
    public int? CompanyId { get; set; }
    public int? ContractId { get; set; }
    public long IssuedAt { get; set; }
    public long DueAt { get; set; }

    /// <summary>Date d'encaissement prévue par la simulation (inconnue du joueur), ou null si le client ne paiera pas.</summary>
    public long? PaysAt { get; set; }

    public long? PaidAt { get; set; }
    public bool WrittenOff { get; set; }

    public bool Open => PaidAt == null && !WrittenOff;
}

/// <summary>Une dette à payer à une date : loyer, salaires, hébergement, remboursements...</summary>
public class Bill
{
    public int Id { get; set; }
    public string Label { get; set; } = "";
    public Category Category { get; set; }
    public decimal Amount { get; set; }
    public int? CompanyId { get; set; }
    public long IssuedAt { get; set; }
    public long DueAt { get; set; }
    public long? PaidAt { get; set; }

    /// <summary>Pour un salaire : la personne concernée.</summary>
    public int? PersonId { get; set; }

    /// <summary>Site auquel la dette est rattachée (null : commune), ou répartition par site (le reste est commun).</summary>
    public int? SiteId { get; set; }

    /// <summary>Poids par site (clé 0 : commun) : le paiement est réparti à proportion.</summary>
    public Dictionary<int, decimal>? SiteSplit { get; set; }

    public bool Open => PaidAt == null;

    /// <summary>Ordre de paiement quand l'argent manque : salaires d'abord, puis loyer, puis le reste.</summary>
    public int Priority => Category switch
    {
        Category.Salaries => 0,
        Category.FounderDraw => 1,
        Category.Rent => 2,
        Category.LoanRepayment => 3,
        _ => 4
    };
}

public enum LoanKind { Bank, RecoveryPlan }

public class Loan
{
    public int Id { get; set; }
    public LoanKind Kind { get; set; }
    public decimal Principal { get; set; }
    public decimal Remaining { get; set; }
    public decimal MonthlyPayment { get; set; }
    public long StartedAt { get; set; }
    public int MonthsLeft { get; set; }
}

/// <summary>Les comptes : trésorerie, créances, dettes, prêts, et l'historique des mouvements.</summary>
public class Ledger
{
    public const int MaxEntries = 800;

    public decimal Cash { get; set; }
    public decimal Overdraft { get; set; }
    public List<Entry> Entries { get; set; } = new();
    public List<Invoice> Invoices { get; set; } = new();
    public List<Bill> Bills { get; set; } = new();
    public List<Loan> Loans { get; set; } = new();

    /// <summary>Totaux par mois et par catégorie (gardés en entier, une ligne par mois joué).</summary>
    public Dictionary<string, Dictionary<Category, decimal>> Months { get; set; } = new();

    /// <summary>Coûts accumulés jour après jour, facturés en début de mois (hébergement, marketing).</summary>
    public Dictionary<Category, decimal> Accrued { get; set; } = new();

    /// <summary>Part de ces coûts accumulés rattachée à un site (le reste est commun).</summary>
    public Dictionary<Category, Dictionary<int, decimal>> AccruedSites { get; set; } = new();

    /// <summary>
    /// Les mêmes totaux mensuels, détaillés par site (clé 0 : commun à l'entreprise). Par construction, la somme des sites
    /// d'un mois égale le total du mois : un mouvement n'est compté qu'une fois, sur un seul site ou réparti.
    /// </summary>
    public Dictionary<string, Dictionary<int, Dictionary<Category, decimal>>> SiteMonths { get; set; } = new();

    public const int Common = 0;

    /// <summary>Garantie de loyer bloquée (rendue au départ du local).</summary>
    public decimal DepositHeld { get; set; }

    public bool CanSpend(decimal amount) => Cash - amount >= -Overdraft;

    public decimal OpenReceivables => Invoices.Where(i => i.Open).Sum(i => i.Amount);

    public decimal OpenBills => Bills.Where(b => b.Open).Sum(b => b.Amount);

    public decimal LoanDebt => Loans.Sum(l => l.Remaining);

    public IEnumerable<Bill> Overdue(long now) => Bills.Where(b => b.Open && b.DueAt < now);

    public static bool IsIncome(Category category) => category <= Category.DepositRefund;

    /// <summary>Enregistre un mouvement réel de trésorerie.</summary>
    public void Post(long minute, string month, decimal amount, Category category, string label, int? companyId = null,
        int? siteId = null, IReadOnlyDictionary<int, decimal>? split = null)
    {
        Cash += amount;
        Entries.Add(new Entry { Minute = minute, Amount = amount, Category = category, Label = label, CompanyId = companyId, SiteId = siteId });
        if (Entries.Count > MaxEntries)
            Entries.RemoveRange(0, Entries.Count - MaxEntries);
        if (!Months.TryGetValue(month, out var totals))
            Months[month] = totals = new Dictionary<Category, decimal>();
        totals[category] = totals.GetValueOrDefault(category) + amount;

        // Détail par site : « split » donne des poids par site (clé 0 : commun) et le montant est réparti à proportion ;
        // l'arrondi revient au commun, la somme reste exacte
        decimal left = amount;
        if (split != null)
        {
            decimal total = split.Values.Where(v => v > 0).Sum();
            if (total > 0)
                foreach (var (site, part) in split.Where(s => s.Value > 0 && s.Key != Common).OrderBy(s => s.Key))
                {
                    decimal share = Math.Round(amount * part / total, 2);
                    AddSite(month, site, category, share);
                    left -= share;
                }
        }
        AddSite(month, split == null ? siteId ?? Common : Common, category, left);
    }

    private void AddSite(string month, int site, Category category, decimal amount)
    {
        if (amount == 0)
            return;
        if (!SiteMonths.TryGetValue(month, out var sites))
            SiteMonths[month] = sites = new();
        if (!sites.TryGetValue(site, out var totals))
            sites[site] = totals = new();
        totals[category] = totals.GetValueOrDefault(category) + amount;
    }

    public void Accrue(Category category, decimal amount, int? siteId = null)
    {
        Accrued[category] = Accrued.GetValueOrDefault(category) + amount;
        if (siteId is not { } site || site == Common)
            return;
        if (!AccruedSites.TryGetValue(category, out var sites))
            AccruedSites[category] = sites = new();
        sites[site] = sites.GetValueOrDefault(site) + amount;
    }
}
