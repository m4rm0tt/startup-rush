using Core.finance;

namespace Core.campaign;

public enum CloudKind { Hosting, Storage, Compute, Gpu, Quantum }

/// <summary>
/// Une offre de services vendue à d'autres entreprises : un type de capacité, un prix par unité et par mois, et la part de
/// ta capacité que tu y réserves. La demande est agrégée (pas de contrats) : des clients arrivent ou partent selon le
/// prix, la fiabilité et la sécurité ; on ne vend que ce qui est réellement disponible et demandé.
/// </summary>
public class CloudOffer
{
    public int Id { get; set; }
    public CloudKind Kind { get; set; }
    public decimal Price { get; set; }

    /// <summary>Unités réservées à cette offre (au plus ce qui reste après tes projets et les autres offres).</summary>
    public double Reserved { get; set; }

    /// <summary>Unités que les clients voudraient utiliser.</summary>
    public double Demand { get; set; }

    /// <summary>Unités réellement servies hier.</summary>
    public double Served { get; set; }

    /// <summary>Recettes des 30 derniers jours, jour par jour.</summary>
    public List<decimal> Revenue { get; set; } = new();

    public bool Paused { get; set; }

    public long OpenedAt { get; set; }

    public decimal Revenue30 => Revenue.Sum();
}

/// <summary>Une cause lisible de l'évolution d'une offre, et le réglage qui y répond.</summary>
public record CloudTrend(string Text, bool Positive, string Lever);

public partial class Campaign
{
    public List<CloudOffer> CloudOffers { get; set; } = new();

    /// <summary>Nom, unité, prix de référence par unité et par mois, et taille du marché accessible (en unités).</summary>
    public static (string Name, string Unit, decimal Reference, double Market, string Node) CloudInfo(CloudKind kind) => kind switch
    {
        CloudKind.Hosting => ("Hébergement d'applications", "unité", 14m, 1_500, "cloud_offers"),
        CloudKind.Storage => ("Stockage", "To", 9m, 4_000, "cloud_offers"),
        CloudKind.Compute => ("Calcul à la demande", "unité", 9m, 3_000, "compute_offers"),
        CloudKind.Gpu => ("Calcul GPU", "unité GPU", 120m, 400, "compute_offers"),
        _ => ("Calcul quantique", "unité quantique", 900m, 600, "quantum_offers"),
    };

    /// <summary>« 3 unités », « 57 To ».</summary>
    public static string Units(CloudKind kind, double amount)
    {
        string unit = CloudInfo(kind).Unit;
        return $"{amount:0} {(amount >= 2 && unit != "To" ? unit.Replace("unité", "unités") : unit)}";
    }

    /// <summary>Capacité totale de ce type (tous sites), et ce qu'il en reste pour les offres (après tes projets).</summary>
    public double CloudCapacity(CloudKind kind, InfraTotals totals) => kind switch
    {
        CloudKind.Hosting => HostingLeft,
        CloudKind.Storage => totals.Storage,
        // L'IA passe avant les offres : l'inférence et l'entraînement de la nuit ne se revendent pas
        CloudKind.Compute => Math.Max(0, totals.Compute - AiComputeUsed),
        CloudKind.Gpu => Math.Max(0, totals.Gpu - AiGpuUsed),
        _ => totals.Quantum,
    };

    /// <summary>Ce qui peut encore être réservé pour cette offre (sans promettre deux fois la même unité).</summary>
    public double CloudAvailable(CloudOffer offer) =>
        Math.Max(0, CloudCapacity(offer.Kind, Infra()) - CloudOffers.Where(o => o.Kind == offer.Kind && o.Id != offer.Id && !o.Paused).Sum(o => o.Reserved));

    public CommandResult OpenCloudOffer(CloudKind kind)
    {
        var info = CloudInfo(kind);
        if (!IsUnlocked(info.Node))
            return CommandResult.Fail($"À débloquer dans Développement : {progress.DevTree.Node(info.Node).Name}.");
        if (CloudOffers.Any(o => o.Kind == kind))
            return CommandResult.Fail("Cette offre existe déjà : règle-la.");
        var offer = new CloudOffer { Id = NewId(), Kind = kind, Price = info.Reference, OpenedAt = Minute };
        CloudOffers.Add(offer);
        offer.Reserved = Math.Floor(CloudAvailable(offer) * 0.8);
        Notify(Severity.Good, "cloud", $"Nouvelle offre : {info.Name}", $"{Units(kind, offer.Reserved)} en réserve, à {offer.Price:N0} CHF par {info.Unit} et par mois.", $"cloud:{offer.Id}");
        return CommandResult.Success();
    }

    public CommandResult SetCloudOffer(int offerId, decimal price, double reserved, bool paused = false)
    {
        var offer = CloudOffers.FirstOrDefault(o => o.Id == offerId);
        if (offer == null)
            return CommandResult.Fail("Offre inconnue.");
        offer.Price = Math.Max(1, Math.Round(price));
        offer.Paused = paused;
        double available = CloudAvailable(offer);
        offer.Reserved = Math.Max(0, Math.Min(reserved, available));
        return CommandResult.Success(reserved > available + 0.01 ? $"Réservé : {offer.Reserved:0} (c'est tout ce qui reste de libre)." : "");
    }

    /// <summary>
    /// Chaque nuit : chaque offre sert au plus ce qui est réservé et réellement disponible ; la demande va vers une cible
    /// qui dépend du prix, de la fiabilité (incidents des 30 derniers jours) et de la sécurité des sites.
    /// </summary>
    private void AllocateCloud(InfraTotals totals)
    {
        if (CloudOffers.Count == 0)
            return;
        double security = Sites.Select(s => SiteInfra(s)).Where(i => i.Any).Select(i => i.Security).DefaultIfEmpty(0).Max();
        double reliability = Math.Max(0.4, 1 - IncidentDays.Count * 0.06);
        decimal total = 0;
        foreach (var kind in Enum.GetValues<CloudKind>())
        {
            double capacity = CloudCapacity(kind, totals);
            foreach (var offer in CloudOffers.Where(o => o.Kind == kind))
            {
                var info = CloudInfo(kind);
                // Tes projets passent d'abord : une réservation qui ne tient plus est réduite, et on le dit
                if (!offer.Paused && offer.Reserved > capacity + 0.01)
                {
                    Notify(Severity.Warning, "cloud", $"{info.Name} : réservation réduite",
                        $"{Units(kind, offer.Reserved)} en réserve, {Units(kind, capacity)} encore libres (tes projets passent d'abord). Ajoute des serveurs ou baisse la réserve.",
                        $"cloud:{offer.Id}", key: $"cloudcut:{offer.Id}");
                    offer.Reserved = Math.Floor(capacity);
                }
                else
                    Resolve($"cloudcut:{offer.Id}");
                double usable = offer.Paused ? 0 : offer.Reserved;
                capacity -= usable;
                double priceFactor = Math.Clamp(Math.Pow((double)(info.Reference / Math.Max(1, offer.Price)), 1.6), 0, 2);
                double target = offer.Paused ? 0 : info.Market * (1 + Renown / 50) * 0.12 * reliability * (1 + 0.25 * security) * priceFactor;
                // Une offre saturée refuse des clients : ils se lassent
                if (offer.Demand > usable * 1.05)
                    target = Math.Min(target, usable * 1.1);
                offer.Demand += (target - offer.Demand) * (target > offer.Demand ? 0.04 : 0.08);
                offer.Served = Math.Min(offer.Demand, usable);
                decimal revenue = Math.Round((decimal)offer.Served * offer.Price / 30, 2);
                offer.Revenue.Add(revenue);
                if (offer.Revenue.Count > 30)
                    offer.Revenue.RemoveAt(0);
                if (revenue > 0)
                    Post(revenue, Category.Cloud, $"{info.Name} : clients du jour");
                total += offer.Revenue30;
            }
        }
        if (total >= 1_000)
            Award("cloud:1k", 2, "Tes offres cloud rapportent 1 000 CHF sur 30 jours.");
    }

    /// <summary>Pourquoi une offre gagne ou perd des clients, avec le bon levier (prix, capacité, fiabilité).</summary>
    public IEnumerable<CloudTrend> CloudTrends(CloudOffer offer)
    {
        var info = CloudInfo(offer.Kind);
        if (offer.Price > info.Reference * 1.3m)
            yield return new CloudTrend("« Trop cher pour ce que c'est. »", false, "price");
        if (offer.Demand > offer.Served * 1.05 && offer.Demand > 1)
            yield return new CloudTrend("« Plus de place : on va voir ailleurs. »", false, "capacity");
        if (IncidentDays.Count > 0)
            yield return new CloudTrend($"« Des coupures ce mois-ci ({IncidentDays.Count} jour(s)). »", false, "sites");
        if (offer.Served > 0 && IncidentDays.Count == 0)
            yield return new CloudTrend("« Fiable, rien à signaler. »", true, "");
        if (offer.Price < info.Reference * 0.8m)
            yield return new CloudTrend("« Imbattable sur le prix. »", true, "price");
    }

    /// <summary>Recettes des offres cloud sur 30 jours.</summary>
    public decimal CloudRevenue30 => CloudOffers.Sum(o => o.Revenue30);
}
