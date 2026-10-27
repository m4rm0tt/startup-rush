using Core.campaign;
using Core.team;

namespace Core.work;

public enum ContractStatus { Offered, Active, Completed, Cancelled, Expired, Declined }

/// <summary>Le délai promis au client : on peut négocier plus de temps contre un prix plus bas, ou l'inverse.</summary>
public enum DeadlineOption { Standard, Safe, Rush }

/// <summary>
/// Une prestation pour un client : périmètre (points par discipline), prix, acompte, échéance,
/// qualité attendue et pénalités de retard. La facture et son encaissement sont deux étapes distinctes (voir Ledger).
/// </summary>
public class Contract
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public string Client { get; set; } = "";
    public string KindId { get; set; } = "";
    public string Title { get; set; } = "";
    public Dictionary<Discipline, double> Scope { get; set; } = new();
    public double ExpectedQuality { get; set; }
    public decimal Price { get; set; }
    public int DurationWorkdays { get; set; }

    /// <summary>Fiabilité de paiement du client (0,6 à 1) : un client peu fiable paie en retard, parfois jamais.</summary>
    public double Reliability { get; set; }

    public long OfferExpiresAt { get; set; }
    public ContractStatus Status { get; set; }

    public DeadlineOption Option { get; set; }
    public decimal AgreedPrice { get; set; }
    public long AcceptedAt { get; set; }
    public long Deadline { get; set; }
    public long? DeliveredAt { get; set; }

    /// <summary>Code rendu réutilisable : plus d'effort maintenant, moins sur les projets suivants du même type.</summary>
    public bool Capitalize { get; set; }

    public bool ReworkRequested { get; set; }
    public double DeliveredQuality { get; set; }
    public decimal Penalty { get; set; }
    public List<int> InvoiceIds { get; set; } = new();
    public string Outcome { get; set; } = "";

    public double TotalEffort => Scope.Values.Sum();

    public ContractKind Kind => ContractKinds.Get(KindId);

    /// <summary>Prix et délai d'une option de négociation.</summary>
    public (decimal Price, int Workdays) Terms(DeadlineOption option) => option switch
    {
        DeadlineOption.Safe => (Math.Round(Price * 0.9m / 50m) * 50m, (int)Math.Ceiling(DurationWorkdays * 1.4)),
        DeadlineOption.Rush => (Math.Round(Price * 1.15m / 50m) * 50m, Math.Max(2, (int)Math.Floor(DurationWorkdays * 0.75))),
        _ => (Price, DurationWorkdays)
    };
}

public record ContractKind(
    string Id,
    string Name,
    double Design,
    double Code,
    double Test,
    double Quality,
    decimal PricePerPoint);

public static class ContractKinds
{
    public static readonly IReadOnlyList<ContractKind> All =
    [
        new("vitrine", "Site vitrine", 0.35, 0.45, 0.2, 0.55, 95m),
        new("shop", "Boutique en ligne", 0.25, 0.55, 0.2, 0.62, 105m),
        new("mobile", "Application mobile", 0.3, 0.5, 0.2, 0.65, 115m),
        new("tool", "Outil métier", 0.15, 0.6, 0.25, 0.66, 110m),
        new("api", "Intégration d'API", 0.05, 0.7, 0.25, 0.7, 120m),
        new("redesign", "Refonte graphique", 0.65, 0.25, 0.1, 0.6, 100m),
    ];

    public static ContractKind Get(string id) => All.First(k => k.Id == id);
}

/// <summary>Les demandes de clients : leur taille et leur exigence suivent la réputation de l'agence.</summary>
public static class ContractMarket
{
    private static readonly string[] Clients =
    [
        "Boulangerie Pittet", "Garage du Lac", "Fondation Horizon", "Cave de Lavaux", "Cabinet Rochat & Associés",
        "Fitness Léman", "Librairie La Plume", "Commune de Bussigny", "Hôtel des Alpes", "Brasserie du Flon",
        "Atelier Vélo Plus", "Clinique des Pins", "École de musique Arpège", "Agence immobilière Meylan",
        "Fromagerie Bovet", "Startup Alpinova", "Assurances Romandes", "Pharmacie Centrale", "Festival Lumière",
        "Coopérative Agricole Vaudoise", "Transports Riviera", "Musée de l'Horlogerie", "Chocolats Favre",
    ];

    /// <summary>Probabilité d'une nouvelle demande par jour ouvré.</summary>
    public static double DailyOfferChance(double reputation) => 0.35 + reputation / 150;

    public static Contract Generate(SimRandom random, int id, long now, double reputation, double founderSpeed)
    {
        var kind = random.Pick(ContractKinds.All);
        // Trois tailles : petites demandes pour tout le monde, plus grosses avec la réputation
        double size;
        if (reputation >= 55 && random.Chance(0.35))
            size = random.Range(160, 340);
        else if (reputation >= 20 && random.Chance(0.5))
            size = random.Range(60, 130);
        else
            size = random.Range(22, 48);

        var scope = new Dictionary<Discipline, double>();
        void Add(Discipline discipline, double share)
        {
            double points = Math.Round(size * share * random.Range(0.85, 1.15));
            if (points >= 1)
                scope[discipline] = points;
        }
        Add(Discipline.Design, kind.Design);
        Add(Discipline.Code, kind.Code);
        Add(Discipline.Test, kind.Test);
        double effort = scope.Values.Sum();

        double expected = Math.Clamp(kind.Quality + (size > 150 ? 0.06 : 0) + random.Range(-0.05, 0.05), 0.45, 0.85);
        // Les gros clients paient mieux le point (projet mieux cadré, budget plus large) : la croissance d'une agence a du sens
        decimal premium = size > 150 ? 1.3m : size > 55 ? 1.15m : 1m;
        decimal price = Math.Round((decimal)effort * kind.PricePerPoint * premium * (decimal)random.Range(0.9, 1.15) / 50m) * 50m;
        // Délai : environ 6 points par jour ouvré pour une personne seule, avec une marge
        int workdays = (int)Math.Ceiling(effort / Math.Max(4, founderSpeed) * random.Range(1.3, 1.7)) + 2;

        return new Contract
        {
            Id = id,
            Client = random.Pick(Clients),
            KindId = kind.Id,
            Title = kind.Name,
            Scope = scope,
            ExpectedQuality = Math.Round(expected, 2),
            Price = price,
            DurationWorkdays = workdays,
            Reliability = Math.Round(random.Range(0.6, 1.0), 2),
            OfferExpiresAt = GameTime.AddWorkdays(now, random.Next(4, 9)),
            Status = ContractStatus.Offered,
        };
    }
}
