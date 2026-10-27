namespace Core.products;

/// <summary>Les besoins des utilisateurs auxquels les modules répondent.</summary>
public enum Need { Discovery, Engagement, Trust, Conversion, Productivity, Liquidity }

public enum RevenueModel { Subscription, Freemium, Ads, Commission }

/// <summary>
/// Un archétype de produit : ses modèles de revenus possibles, ses besoins prioritaires et ses contraintes propres
/// (support, abandon, charge serveur, bouche-à-oreille). Ce ne sont pas des boutons à revenu fixe : chacun passe
/// par le même modèle de marché, avec des paramètres et des contraintes différents.
/// </summary>
public record Archetype(
    string Id,
    string Name,
    string Description,
    RevenueModel[] Models,
    Dictionary<Need, double> Needs,
    double TrialRate,
    double ChurnFactor,
    double WordOfMouthFactor,
    double TicketFactor,
    string[] RequiredModules,
    string RequirementText);

/// <summary>Un public cible : taille, prix de référence, sensibilité au prix.</summary>
public record Segment(
    string Id,
    string Name,
    string ArchetypeId,
    int Size,
    decimal ReferencePrice,
    double PriceSensitivity,
    string Description);

public record ModuleChoice(string Value, string Label, double EffortFactor = 1, double ValueFactor = 1,
    double LoadFactor = 1, double TicketFactor = 1, string Note = "");

public record ModuleOption(string Key, string Label, ModuleChoice[] Choices, bool VisualOnly = false);

/// <summary>
/// Un module de page : ce qu'il apporte (besoins), ce qu'il coûte à faire (points par discipline),
/// à maintenir (points par semaine), à faire tourner (charge serveur, tickets de support) et son risque (complexité → bugs).
/// </summary>
public record ModuleDef(
    string Id,
    string Name,
    string Description,
    Dictionary<Need, double> Needs,
    double Design,
    double Code,
    double Test,
    double Complexity,
    double Maintenance,
    double Load,
    double Tickets,
    ModuleOption[] Options,
    string? Requires = null);

public record MarketingChannel(string Id, string Name, string Description, Dictionary<string, double> ReachPerChf, double Saturation);

public record HostingPlan(string Id, string Name, decimal MonthlyCost, double Capacity);

public static class ProductCatalog
{
    public static readonly IReadOnlyList<Archetype> Archetypes =
    [
        new("saas", "Outil SaaS",
            "Un logiciel en ligne pour des professionnels. Peu de clients, qui paient chaque mois, restent longtemps et posent beaucoup de questions.",
            [RevenueModel.Subscription, RevenueModel.Freemium],
            new() { [Need.Productivity] = 0.4, [Need.Trust] = 0.25, [Need.Conversion] = 0.2, [Need.Engagement] = 0.1, [Need.Discovery] = 0.05 },
            TrialRate: 0.07, ChurnFactor: 0.45, WordOfMouthFactor: 0.6, TicketFactor: 1.8,
            ["subscribe"], "Un module Abonnement est indispensable pour encaisser."),
        new("social", "App de contenu / communauté",
            "Des utilisateurs nombreux qui viennent pour le contenu et les autres membres. Bouche-à-oreille fort, abandon rapide, gros besoin de serveurs.",
            [RevenueModel.Ads, RevenueModel.Freemium],
            new() { [Need.Engagement] = 0.4, [Need.Discovery] = 0.3, [Need.Trust] = 0.15, [Need.Conversion] = 0.15 },
            TrialRate: 0.22, ChurnFactor: 1.5, WordOfMouthFactor: 3, TicketFactor: 0.5,
            ["feed"], "Un Fil de contenu est indispensable : sans contenu, personne ne revient."),
        new("marketplace", "Place de marché",
            "Deux publics à faire venir en même temps : des vendeurs pour l'offre, des acheteurs pour la demande. Sans assez de vendeurs, les acheteurs repartent.",
            [RevenueModel.Commission],
            new() { [Need.Liquidity] = 0.3, [Need.Discovery] = 0.25, [Need.Trust] = 0.25, [Need.Conversion] = 0.2 },
            TrialRate: 0.12, ChurnFactor: 1.0, WordOfMouthFactor: 1.2, TicketFactor: 1.0,
            ["listings", "checkout"], "Il faut des Annonces vendeurs et un Paiement pour qu'une vente ait lieu."),
    ];

    public static readonly IReadOnlyList<Segment> Segments =
    [
        new("freelances", "Indépendants", "saas", 7_000, 19m, 1.3, "Prix serré, veulent gagner du temps tout de suite."),
        new("smb", "PME", "saas", 2_500, 59m, 0.8, "Paient plus, attendent fiabilité, exports et support."),
        new("associations", "Associations", "saas", 4_000, 9m, 1.7, "Petits budgets, beaucoup de bénévoles à former."),
        new("students", "Étudiants", "social", 120_000, 4m, 2.0, "Nombreux, volatils, très sensibles au prix."),
        new("creators", "Créateurs", "social", 40_000, 8m, 1.3, "Veulent de l'audience et des outils de publication."),
        new("crafts", "Artisans locaux", "marketplace", 60_000, 8m, 1.0, "Acheteurs de produits régionaux ; les vendeurs sont rares au début."),
        new("secondhand", "Matériel d'occasion", "marketplace", 140_000, 5m, 1.4, "Beaucoup de demande, marges faibles, besoin de confiance."),
    ];

    private static readonly ModuleChoice[] Columns =
    [
        new("2", "2 colonnes"), new("3", "3 colonnes"), new("4", "4 colonnes"),
    ];

    public static readonly IReadOnlyList<ModuleDef> Modules =
    [
        new("landing", "Présentation", "Page d'accueil qui explique le produit.",
            new() { [Need.Trust] = 0.2, [Need.Conversion] = 0.25 }, 5, 3, 1, 0.5, 0.2, 0.2, 0,
            [
                new("cta", "Appel à l'action", [new("inscription", "« Créer un compte »"), new("demo", "« Voir la démo »", 1.3, 1.15, Note: "Démo vidéo : plus convaincante, plus longue à produire")]),
                new("style", "Illustration", [new("photo", "Photo"), new("illustration", "Illustration"), new("aucune", "Aucune")], VisualOnly: true),
            ]),
        new("login", "Connexion", "Comptes utilisateurs. Indispensable au profil, à la messagerie et au paiement.",
            new() { [Need.Trust] = 0.25 }, 2, 6, 3, 1.0, 0.4, 0.3, 0.4,
            [new("methods", "Méthodes", [new("email", "E-mail"), new("social", "E-mail + réseaux sociaux", 1.4, 1.25, Note: "Inscription plus facile, plus de code à maintenir")])]),
        new("profile", "Profil", "Page personnelle de chaque membre.",
            new() { [Need.Engagement] = 0.2, [Need.Trust] = 0.1 }, 3, 4, 2, 0.7, 0.3, 0.4, 0.2,
            [new("depth", "Personnalisation", [new("simple", "Simple"), new("riche", "Riche (avatar, bio, liens)", 1.5, 1.3, 1.2)])],
            Requires: "login"),
        new("catalog", "Catalogue", "Liste d'éléments à parcourir : articles, offres, modèles.",
            new() { [Need.Discovery] = 0.35, [Need.Liquidity] = 0.1 }, 4, 6, 2, 0.8, 0.4, 0.6, 0.1,
            [
                new("columns", "Colonnes", Columns, VisualOnly: true),
                new("pageSize", "Éléments par page", [new("12", "12", 1, 0.9, 0.8), new("24", "24"), new("48", "48", 1, 1.05, 1.4, Note: "Un peu plus pratique, nettement plus lourd pour les serveurs")]),
            ]),
        new("search", "Recherche", "Trouver un élément précis.",
            new() { [Need.Discovery] = 0.3, [Need.Productivity] = 0.1 }, 2, 8, 3, 1.1, 0.5, 0.9, 0.1,
            [new("filters", "Filtres", [new("aucun", "Aucun", 0.6, 0.6, 0.7), new("basiques", "Basiques"), new("avances", "Avancés", 1.6, 1.35, 1.5)])]),
        new("feed", "Fil de contenu", "Publications des membres, mises à jour.",
            new() { [Need.Engagement] = 0.4, [Need.Discovery] = 0.15 }, 4, 10, 3, 1.2, 0.6, 1.4, 0.3,
            [new("paging", "Défilement", [new("pages", "Par pages"), new("infini", "Infini", 1.2, 1.2, 1.6, Note: "Plus d'engagement, plus de charge serveur")])]),
        new("subscribe", "Abonnement", "Offres payantes et facturation récurrente.",
            new() { [Need.Conversion] = 0.45 }, 3, 8, 4, 1.3, 0.5, 0.2, 0.5,
            [
                new("plans", "Offres", [new("1", "1 offre", 0.8, 0.85), new("2", "2 offres"), new("3", "3 offres", 1.2, 1.1, Note: "Mieux adapté à chacun, plus de questions au support")]),
                new("trial", "Essai gratuit", [new("non", "Non", 1, 0.85), new("oui", "14 jours", 1.1, 1.15, 1, 1.2)]),
            ],
            Requires: "login"),
        new("checkout", "Paiement", "Achat d'un article ou d'un service.",
            new() { [Need.Conversion] = 0.4, [Need.Trust] = 0.1 }, 2, 9, 5, 1.4, 0.6, 0.3, 0.6,
            [new("methods", "Moyens", [new("carte", "Carte"), new("mobile", "Carte + paiement mobile", 1.3, 1.2)])],
            Requires: "login"),
        new("reviews", "Avis", "Notes et commentaires des utilisateurs.",
            new() { [Need.Trust] = 0.3, [Need.Discovery] = 0.1 }, 2, 5, 2, 0.8, 0.4, 0.3, 0.3,
            [new("moderation", "Modération", [new("aucune", "Aucune", 1, 0.8, 1, 0.8), new("manuelle", "Manuelle", 1.1, 1.1, 1, 1.8, Note: "Avis plus fiables, travail de support en plus")])]),
        new("messaging", "Messagerie", "Messages entre membres.",
            new() { [Need.Engagement] = 0.35, [Need.Liquidity] = 0.2 }, 3, 12, 4, 1.5, 0.8, 1.2, 0.8,
            [new("realtime", "Temps réel", [new("non", "Non"), new("oui", "Oui", 1.4, 1.2, 1.8, 1.2)])],
            Requires: "login"),
        new("support", "Aide & support", "FAQ et contact. Réduit les tickets.",
            new() { [Need.Trust] = 0.2 }, 2, 3, 1, 0.4, 0.3, 0.1, -0.3,
            [new("mode", "Mode", [new("faq", "FAQ"), new("chat", "FAQ + chat", 1.5, 1.3, 1, 1.3, Note: "Rassurant, mais chaque discussion occupe le support")])]),
        new("dashboard", "Tableau de bord", "L'écran de travail des clients professionnels.",
            new() { [Need.Productivity] = 0.45, [Need.Engagement] = 0.1 }, 5, 10, 4, 1.3, 0.7, 0.8, 0.5,
            [new("widgets", "Widgets", [new("3", "3", 0.8, 0.85), new("6", "6"), new("9", "9", 1.3, 1.1, 1.2)])],
            Requires: "login"),
        new("export", "Exports & intégrations", "Exports comptables, connexion à d'autres outils.",
            new() { [Need.Productivity] = 0.3, [Need.Trust] = 0.1 }, 1, 9, 3, 1.2, 0.6, 0.4, 0.4,
            [new("formats", "Formats", [new("csv", "CSV"), new("api", "CSV + API publique", 1.6, 1.3, 1.3, 1.3)])]),
        new("notifications", "Notifications", "Rappels et alertes par e-mail.",
            new() { [Need.Engagement] = 0.2, [Need.Conversion] = 0.05 }, 1, 5, 2, 0.7, 0.3, 0.4, 0.1,
            [new("frequency", "Fréquence", [new("hebdo", "Hebdomadaire"), new("quotidien", "Quotidienne", 1, 1.1, 1.2, 1.3, Note: "Plus d'engagement, plus de désinscriptions agacées")])]),
        new("listings", "Annonces vendeurs", "Les vendeurs publient leurs offres.",
            new() { [Need.Liquidity] = 0.5 }, 3, 9, 3, 1.2, 0.6, 0.6, 0.4,
            [new("validation", "Validation", [new("auto", "Automatique", 1, 0.95, 1, 1.3), new("manuelle", "Manuelle", 1.1, 1.05, 1, 1.6)])],
            Requires: "login"),
        new("stats", "Statistiques", "Chiffres pour les utilisateurs avancés ou les vendeurs.",
            new() { [Need.Productivity] = 0.2, [Need.Liquidity] = 0.05 }, 3, 6, 2, 0.8, 0.4, 0.5, 0.1,
            []),
    ];

    public static readonly IReadOnlyList<MarketingChannel> Channels =
    [
        new("social", "Réseaux sociaux", "Large audience grand public, peu efficace auprès des pros.",
            new() { ["saas"] = 0.2, ["social"] = 2.0, ["marketplace"] = 1.1 }, 120),
        new("search", "Publicité sur moteurs de recherche", "Des gens qui cherchent déjà une solution.",
            new() { ["saas"] = 0.6, ["social"] = 0.5, ["marketplace"] = 0.8 }, 80),
        new("events", "Salons et démarchage", "Cher, mais touche directement les professionnels.",
            new() { ["saas"] = 0.8, ["social"] = 0.05, ["marketplace"] = 0.3 }, 40),
    ];

    public static readonly IReadOnlyList<HostingPlan> Hosting =
    [
        new("shared", "Hébergement mutualisé", 15m, 2),
        new("cloud_s", "Cloud S", 90m, 10),
        new("cloud_m", "Cloud M", 340m, 45),
        new("cloud_l", "Cloud L", 1_150m, 170),
        new("cluster", "Cluster dédié", 3_900m, 650),
    ];

    public static Archetype Archetype(string id) => Archetypes.First(a => a.Id == id);
    public static Segment Segment(string id) => Segments.First(s => s.Id == id);
    public static ModuleDef Module(string id) => Modules.First(m => m.Id == id);
    public static MarketingChannel Channel(string id) => Channels.First(c => c.Id == id);
    public static HostingPlan Plan(string id) => Hosting.First(h => h.Id == id);

    public static string NeedName(Need need) => need switch
    {
        Need.Discovery => "Découverte",
        Need.Engagement => "Engagement",
        Need.Trust => "Confiance",
        Need.Conversion => "Conversion",
        Need.Productivity => "Productivité",
        _ => "Liquidité"
    };

    public static string ModelName(RevenueModel model) => model switch
    {
        RevenueModel.Subscription => "Abonnement",
        RevenueModel.Freemium => "Freemium",
        RevenueModel.Ads => "Publicité",
        _ => "Commission sur les ventes"
    };
}
