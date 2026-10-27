using Core.finance;
using Core.products;
using Core.space;
using Core.team;
using Core.progress;
using Core.work;

namespace Core.campaign;

public enum CampaignStatus { Running, Liquidated }

public enum FounderProfile { Developer, Designer, Generalist }

/// <summary>Un meuble rangé dans les cartons (après un déménagement ou retiré d'un bureau).</summary>
public class StoredItem
{
    public int Id { get; set; }
    public string DefId { get; set; } = "";
}

/// <summary>Résultat d'une commande du joueur : réussie ou non, avec la raison.</summary>
public record CommandResult(bool Ok, string Message)
{
    public static CommandResult Success(string message = "") => new(true, message);
    public static CommandResult Fail(string message) => new(false, message);
}

/// <summary>
/// Une campagne : tout l'état d'une partie, sérialisé tel quel dans la sauvegarde. Le moteur tourne sans interface
/// (les tests et les bots l'utilisent directement) ; l'interface ne fait qu'afficher cet état et appeler ses commandes.
/// </summary>
public partial class Campaign
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public ulong Seed { get; set; }
    public string FounderName { get; set; } = "";
    public FounderProfile Profile { get; set; }
    public CampaignStatus Status { get; set; }

    public long Minute { get; set; } = GameTime.StartMinute;
    public SimRandom Random { get; set; } = new(1);
    public int NextId { get; set; } = 1;

    public List<Company> Companies { get; set; } = new();
    public Premises Premises { get; set; } = new();
    public List<StoredItem> Storage { get; set; } = new();
    public List<Person> People { get; set; } = new();
    public List<Candidate> Candidates { get; set; } = new();
    public List<WorkItem> Work { get; set; } = new();
    public List<Product> Products { get; set; } = new();
    public List<Competitor> Competitors { get; set; } = new();
    public Ledger Ledger { get; set; } = new();
    public Solvency Solvency { get; set; } = new();
    public List<Notice> Notices { get; set; } = new();

    /// <summary>Événements périodiques déjà traités (« rent:2027-02 »...) : un même événement ne se déclenche jamais deux fois.</summary>
    public HashSet<string> Processed { get; set; } = new();

    public InvestorOffer? Investor { get; set; }
    public bool InvestorRoundDone { get; set; }

    /// <summary>Part du capital détenue par le fondateur (baisse après une levée de fonds).</summary>
    public double FounderEquity { get; set; } = 1;

    /// <summary>Jalons atteints et leur date.</summary>
    public Dictionary<string, long> Milestones { get; set; } = new();

    /// <summary>Points de développement à dépenser dans l'arbre.</summary>
    public int DevPoints { get; set; } = DevTree.StartingPoints;

    /// <summary>Nœuds débloqués dans l'arbre de développement.</summary>
    public HashSet<string> Unlocked { get; set; } = new();

    /// <summary>Accomplissements déjà récompensés (un même accomplissement ne rapporte qu'une fois).</summary>
    public HashSet<string> Awarded { get; set; } = new();

    /// <summary>Message de migration d'une ancienne sauvegarde, affiché une fois au chargement.</summary>
    public string MigrationNote { get; set; } = "";

    /// <summary>Travail de support et de maintenance fait aujourd'hui, par produit (points).</summary>
    public Dictionary<int, double> SupportToday { get; set; } = new();

    /// <summary>Une sauvegarde automatique est due (posé chaque lundi à minuit, consommé par l'interface).</summary>
    public bool AutosaveDue { get; set; }

    /// <summary>Prévenu à chaque nouvelle notification (non sauvegardé).</summary>
    public event Action<Notice>? Noticed;

    public const int MaxNotices = 150;

    public static Campaign New(string name, string founderName, string companyName, FounderProfile profile, ulong seed)
    {
        var campaign = new Campaign
        {
            Name = name,
            FounderName = founderName,
            Profile = profile,
            Seed = seed,
            Random = new SimRandom(seed),
        };
        campaign.Ledger.Cash = Tuning.Start.Cash;
        campaign.Ledger.Overdraft = Tuning.Start.Overdraft;
        campaign.Companies.Add(new Company { Id = campaign.NewId(), Name = companyName, Sector = "Agence web et produits numériques", FoundedAt = campaign.Minute });

        // L'appartement, avec le coin de travail du début : une table, une chaise et ton vieux portable
        campaign.Premises = RealEstate.Create(RealEstate.Get(RealEstate.Apartment), campaign.NewId);
        var desk = new PlacedItem { Id = campaign.NewId(), DefId = "desk", X = 8, Y = 2 };
        campaign.Premises.Items.Add(desk);
        campaign.Premises.Items.Add(new PlacedItem { Id = campaign.NewId(), DefId = "chair", X = 8, Y = 1 });
        campaign.Premises.Items.Add(new PlacedItem { Id = campaign.NewId(), DefId = "laptop", X = 8, Y = 2, HostId = desk.Id });
        campaign.Premises.Items.Add(new PlacedItem { Id = campaign.NewId(), DefId = "plant", X = 13, Y = 0 });

        var skills = profile switch
        {
            FounderProfile.Developer => (Design: 30.0, Code: 62.0, Test: 40.0, Support: 35.0),
            FounderProfile.Designer => (Design: 64.0, Code: 38.0, Test: 34.0, Support: 40.0),
            _ => (Design: 46.0, Code: 48.0, Test: 42.0, Support: 46.0),
        };
        var founder = new Person
        {
            Id = campaign.NewId(),
            Name = founderName,
            IsFounder = true,
            Look = 0,
            Skills = new()
            {
                [Discipline.Design] = skills.Design,
                [Discipline.Code] = skills.Code,
                [Discipline.Test] = skills.Test,
                [Discipline.Support] = skills.Support,
            },
            HiredAt = campaign.Minute,
            Morale = 75,
        };
        desk.AssignedPersonId = founder.Id;
        campaign.People.Add(founder);

        campaign.SeedCompetitors();
        for (int i = 0; i < 3; i++)
            campaign.Candidates.Add(Recruitment.Generate(campaign.Random, campaign.Minute, campaign.NewId(), 0));

        campaign.Notify(Severity.Info, "entreprise", $"Bienvenue chez {companyName}",
            "Crée ton premier projet : une application ou un jeu.", "projects");
        return campaign;
    }

    private void SeedCompetitors()
    {
        string[] names = ["Flowly", "Tasko", "Brikk", "Nuvola", "Kanto", "Lumio", "Vendo", "Marketto", "Swapz", "Trova", "Zelia", "Orbit"];
        int n = 0;
        foreach (var segment in ProductCatalog.Segments)
        {
            int count = segment.TypeId == "social" ? 2 : Random.Next(1, 3);
            for (int i = 0; i < count; i++)
                Competitors.Add(new Competitor
                {
                    Id = NewId(),
                    Name = names[n++ % names.Length],
                    SegmentId = segment.Id,
                    Appeal = Math.Round(Random.Range(0.18, 0.34), 2),
                    Price = segment.ReferencePrice,
                });
        }
    }

    public int NewId() => NextId++;

    public Person Founder => People.First(p => p.IsFounder);
    public Company MainCompany => Companies[0];
    public string Month => GameTime.MonthKey(Minute);
    public long Day => GameTime.Day(Minute);

    public Company? FindCompany(int id) => Companies.FirstOrDefault(c => c.Id == id);
    public Person? FindPerson(int id) => People.FirstOrDefault(p => p.Id == id);
    public Product? FindProduct(int id) => Products.FirstOrDefault(p => p.Id == id);
    public WorkItem? FindWork(int id) => Work.FirstOrDefault(w => w.Id == id);

    public IEnumerable<Person> Staff => People.Where(p => !p.IsFounder);

    public IEnumerable<WorkItem> WorkFor(WorkOwner owner, int ownerId) => Work.Where(w => w.Owner == owner && w.OwnerId == ownerId);

    /// <summary>
    /// Ajoute une notification. Un problème avec une clé déjà signalé et pas encore résolu est regroupé
    /// (compteur et date mis à jour) au lieu de s'empiler.
    /// </summary>
    public Notice Notify(Severity severity, string category, string title, string detail = "", string target = "", string key = "")
    {
        if (key != "" && Notices.LastOrDefault(n => n.Key == key && !n.Resolved) is { } existing)
        {
            existing.Count++;
            existing.Minute = Minute;
            existing.Title = title;
            existing.Detail = detail;
            existing.Read = false;
            return existing;
        }
        var notice = new Notice
        {
            Id = NewId(),
            Minute = Minute,
            Severity = severity,
            Category = category,
            Title = title,
            Detail = detail,
            Target = target,
            Key = key,
        };
        Notices.Add(notice);
        if (Notices.Count > MaxNotices)
            Notices.RemoveAt(Notices.FindIndex(n => !n.IsProblem || n.Resolved) is int i and >= 0 ? i : 0);
        Noticed?.Invoke(notice);
        return notice;
    }

    /// <summary>Marque comme résolus les problèmes dont la clé commence ainsi.</summary>
    public void Resolve(string key)
    {
        foreach (var notice in Notices.Where(n => n.Key == key && !n.Resolved))
            notice.Resolved = true;
    }

    public bool IsUnlocked(string node) => Unlocked.Contains(node);

    /// <summary>Accorde des points de développement pour un accomplissement, une seule fois.</summary>
    public void Award(string key, int points, string why)
    {
        if (!Awarded.Add(key))
            return;
        DevPoints += points;
        Notify(Severity.Good, "développement", $"+{points} point{(points > 1 ? "s" : "")} de développement", why, "tree");
    }

    /// <summary>Enregistre un mouvement de trésorerie à la date du jour.</summary>
    public void Post(decimal amount, Category category, string label, int? companyId = null) =>
        Ledger.Post(Minute, Month, amount, category, label, companyId ?? MainCompany.Id);

    /// <summary>Crée une dette à payer (le paiement se fait automatiquement à l'échéance si l'argent est là).</summary>
    public Bill AddBill(string label, Category category, decimal amount, long dueAt, int? personId = null)
    {
        var bill = new Bill
        {
            Id = NewId(),
            Label = label,
            Category = category,
            Amount = Math.Round(amount, 2),
            IssuedAt = Minute,
            DueAt = dueAt,
            PersonId = personId,
            CompanyId = MainCompany.Id,
        };
        Ledger.Bills.Add(bill);
        return bill;
    }

    /// <summary>Émet une facture client. La date réelle de paiement dépend de la fiabilité du client.</summary>
    public Invoice AddInvoice(string client, string label, decimal amount, int paymentDays, double reliability, int? contractId, Category category = Category.Contracts, int? companyId = null)
    {
        long due = Minute + paymentDays * (long)GameTime.MinutesPerDay;
        long? paysAt = due + (long)(Random.Range(-0.2, 1.0) * (1 - reliability) * 40 * GameTime.MinutesPerDay);
        if (Random.Chance((1 - reliability) * 0.08))
            paysAt = null;
        var invoice = new Invoice
        {
            Id = NewId(),
            Client = client,
            Label = label,
            Amount = Math.Round(amount, 2),
            Category = category,
            CompanyId = companyId ?? MainCompany.Id,
            ContractId = contractId,
            IssuedAt = Minute,
            DueAt = due,
            PaysAt = paysAt == null ? null : Math.Max(Minute + GameTime.MinutesPerDay, paysAt.Value),
        };
        Ledger.Invoices.Add(invoice);
        return invoice;
    }

    /// <summary>Paie tout de suite (achat par carte). Refusé si le découvert autorisé serait dépassé.</summary>
    public CommandResult Spend(decimal amount, Category category, string label)
    {
        if (!Ledger.CanSpend(amount))
            return CommandResult.Fail($"Trésorerie insuffisante : il faudrait {amount:N0} CHF (découvert compris).");
        Post(-amount, category, label);
        return CommandResult.Success();
    }
}
