namespace Core.finance;

public enum Category
{
    // Recettes
    Contracts, Subscriptions, Ads, Commissions, Sales, Funding, LoanReceived, Resale, DepositRefund,

    // Dépenses
    Salaries, FounderDraw, Rent, Utilities, Equipment, Hosting, Marketing, Training, Severance,
    Interest, LoanRepayment, Deposit, Moving, Fees, Refund, Registration
}

/// <summary>Un mouvement de trésorerie (argent réellement entré ou sorti).</summary>
public class Entry
{
    public long Minute { get; set; }
    public decimal Amount { get; set; }
    public Category Category { get; set; }
    public string Label { get; set; } = "";
    public int? CompanyId { get; set; }
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

    /// <summary>Garantie de loyer bloquée (rendue au départ du local).</summary>
    public decimal DepositHeld { get; set; }

    public bool CanSpend(decimal amount) => Cash - amount >= -Overdraft;

    public decimal OpenReceivables => Invoices.Where(i => i.Open).Sum(i => i.Amount);

    public decimal OpenBills => Bills.Where(b => b.Open).Sum(b => b.Amount);

    public decimal LoanDebt => Loans.Sum(l => l.Remaining);

    public IEnumerable<Bill> Overdue(long now) => Bills.Where(b => b.Open && b.DueAt < now);

    public static bool IsIncome(Category category) => category <= Category.DepositRefund;

    /// <summary>Enregistre un mouvement réel de trésorerie.</summary>
    public void Post(long minute, string month, decimal amount, Category category, string label, int? companyId = null)
    {
        Cash += amount;
        Entries.Add(new Entry { Minute = minute, Amount = amount, Category = category, Label = label, CompanyId = companyId });
        if (Entries.Count > MaxEntries)
            Entries.RemoveRange(0, Entries.Count - MaxEntries);
        if (!Months.TryGetValue(month, out var totals))
            Months[month] = totals = new Dictionary<Category, decimal>();
        totals[category] = totals.GetValueOrDefault(category) + amount;
    }

    public void Accrue(Category category, decimal amount) => Accrued[category] = Accrued.GetValueOrDefault(category) + amount;
}
