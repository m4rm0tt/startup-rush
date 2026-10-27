namespace Core.products;

/// <summary>Les besoins des utilisateurs auxquels les composants répondent.</summary>
public enum Need { Discovery, Engagement, Trust, Conversion, Productivity, Liquidity, Fun }

/// <summary>Comment un projet gagne de l'argent.</summary>
public enum RevenueModel { Subscription, Freemium, Ads, Commission, Purchase }

/// <summary>
/// Un type de projet : ses modèles de revenus, ses besoins prioritaires et ses contraintes propres.
/// Tous passent par le même modèle de marché, avec des paramètres différents.
/// </summary>
public record ProjectType(
    string Id,
    string Name,
    string Icon,
    string Pitch,
    RevenueModel[] Models,
    Dictionary<Need, double> Needs,
    double TrialRate,
    double ChurnFactor,
    double WordOfMouthFactor,
    double TicketFactor,
    string[] Starters,
    string? UnlockNode = null);

/// <summary>Un public visé : taille, prix de référence, sensibilité au prix.</summary>
public record Segment(string Id, string Name, string TypeId, int Size, decimal ReferencePrice, double PriceSensitivity, string Pitch);

/// <summary>
/// Un composant de projet (comptes, recherche, gameplay...) : son rôle, ce qu'il apporte par niveau,
/// son coût de développement, et ce qu'il demande ensuite (serveurs, support, maintenance).
/// </summary>
public record ComponentDef(
    string Id,
    string Name,
    string Icon,
    string Role,
    string[] Types,
    Dictionary<Need, double> Needs,
    double Effort,
    double DesignShare,
    double TestShare,
    double Load,
    double Tickets,
    double Maintenance,
    double Complexity,
    string? Requires = null,
    string? UnlockNode = null)
{
    public double CodeShare => 1 - DesignShare - TestShare;

    /// <summary>Le composant Performances ne vaut rien en soi : il réduit la charge des serveurs.</summary>
    public bool IsPerformance => Id == "performance";
}

public record MarketingChannel(string Id, string Name, string Pitch, Dictionary<string, double> ReachPerChf, double Saturation, string? UnlockNode = null);

public record HostingPlan(string Id, string Name, decimal MonthlyCost, double Capacity, string? UnlockNode = null);

public static class ProductCatalog
{
    public const int MaxLevel = 3;

    /// <summary>Valeur apportée par niveau : chaque niveau rapporte moins que le précédent.</summary>
    public static double LevelValue(int level) => level switch { <= 0 => 0, 1 => 1, 2 => 1.5, _ => 1.85 };

    /// <summary>Effort pour atteindre un niveau, en multiple de l'effort de base.</summary>
    public static double LevelEffort(int level) => level switch { <= 1 => 1, 2 => 1.4, _ => 2 };

    public static readonly IReadOnlyList<ProjectType> Types =
    [
        new("saas", "Application", "screen", "Un logiciel que les gens paient chaque mois.",
            [RevenueModel.Subscription, RevenueModel.Freemium],
            new() { [Need.Productivity] = 0.4, [Need.Trust] = 0.25, [Need.Conversion] = 0.2, [Need.Engagement] = 0.15 },
            TrialRate: 0.07, ChurnFactor: 0.5, WordOfMouthFactor: 0.8, TicketFactor: 1.6,
            ["login", "dashboard", "subscribe"]),
        new("game", "Jeu", "dice", "Vendu une fois ; un bon lancement et des mises à jour font tout.",
            [RevenueModel.Purchase],
            new() { [Need.Fun] = 0.35, [Need.Trust] = 0.25, [Need.Discovery] = 0.2, [Need.Engagement] = 0.2 },
            TrialRate: 0.075, ChurnFactor: 1.6, WordOfMouthFactor: 2.5, TicketFactor: 0.6,
            ["gameplay", "content", "graphics"]),
        new("social", "App communautaire", "people", "Gratuite, financée par la publicité ; il faut beaucoup de monde.",
            [RevenueModel.Ads, RevenueModel.Freemium],
            new() { [Need.Engagement] = 0.4, [Need.Discovery] = 0.3, [Need.Trust] = 0.15, [Need.Conversion] = 0.15 },
            TrialRate: 0.2, ChurnFactor: 1.2, WordOfMouthFactor: 6, TicketFactor: 0.5,
            ["login", "feed", "profile"], UnlockNode: "community"),
        new("marketplace", "Place de marché", "tag", "Une commission sur chaque vente ; il faut des vendeurs et des acheteurs.",
            [RevenueModel.Commission],
            new() { [Need.Liquidity] = 0.3, [Need.Discovery] = 0.25, [Need.Trust] = 0.25, [Need.Conversion] = 0.2 },
            TrialRate: 0.12, ChurnFactor: 1.0, WordOfMouthFactor: 1.2, TicketFactor: 1.0,
            ["login", "listings", "checkout", "catalog"], UnlockNode: "marketplace"),
    ];

    public static readonly IReadOnlyList<Segment> Segments =
    [
        new("freelances", "Indépendants", "saas", 80_000, 12m, 1.3, "Petit prix, besoin de gagner du temps."),
        new("smb", "PME", "saas", 30_000, 39m, 0.8, "Paient plus, veulent du solide."),
        new("consumers", "Grand public", "saas", 300_000, 4m, 1.8, "Nombreux, mais paient peu."),
        new("casual", "Joueurs occasionnels", "game", 400_000, 6m, 1.6, "Beaucoup de monde, petits prix."),
        new("core", "Passionnés", "game", 150_000, 20m, 1.0, "Exigeants, prêts à payer un bon jeu."),
        new("students", "Étudiants", "social", 400_000, 3m, 2.0, "Nombreux et volatils."),
        new("creators", "Créateurs", "social", 200_000, 6m, 1.3, "Veulent de l'audience."),
        new("crafts", "Artisans locaux", "marketplace", 150_000, 8m, 1.0, "Peu de vendeurs au début."),
        new("secondhand", "Occasion", "marketplace", 300_000, 5m, 1.4, "Beaucoup de demande, faibles marges."),
    ];

    private static Dictionary<Need, double> N(params (Need Need, double Value)[] needs) => needs.ToDictionary(n => n.Need, n => n.Value);

    public static readonly IReadOnlyList<ComponentDef> Components =
    [
        // Communs
        new("ui", "Interface soignée", "gem", "Plus agréable et plus clair.", ["saas", "game", "social", "marketplace"],
            N((Need.Trust, 0.15), (Need.Engagement, 0.1), (Need.Fun, 0.1)), 10, 0.7, 0.1, 0.1, 0, 0.2, 0.4),
        new("performance", "Performances", "bolt", "Moins de charge sur les serveurs (−25 % par niveau).", ["saas", "game", "social", "marketplace"],
            N(), 12, 0.05, 0.25, 0, 0, 0.2, 0.6),
        new("support", "Aide intégrée", "heart", "Moins de questions au support.", ["saas", "social", "marketplace"],
            N((Need.Trust, 0.2)), 6, 0.3, 0.1, 0.1, -0.3, 0.2, 0.3),
        new("login", "Comptes", "user", "Chacun a son compte.", ["saas", "social", "marketplace"],
            N((Need.Trust, 0.2)), 10, 0.2, 0.2, 0.3, 0.3, 0.4, 0.9),
        new("notifications", "Notifications", "megaphone", "Fait revenir les utilisateurs.", ["saas", "social", "game"],
            N((Need.Engagement, 0.2)), 8, 0.1, 0.2, 0.4, 0.1, 0.3, 0.7),
        new("cloudsave", "Sauvegarde en ligne", "shield", "Rien ne se perd.", ["saas", "game"],
            N((Need.Trust, 0.2), (Need.Engagement, 0.1)), 10, 0.1, 0.25, 0.3, 0.2, 0.3, 0.9),
        // Applications
        new("dashboard", "Tableau de bord", "chart", "L'écran de travail principal.", ["saas"],
            N((Need.Productivity, 0.45), (Need.Engagement, 0.1)), 18, 0.3, 0.2, 0.8, 0.5, 0.7, 1.3, Requires: "login"),
        new("subscribe", "Abonnements", "piggy", "Des offres payantes simples à choisir.", ["saas"],
            N((Need.Conversion, 0.45)), 14, 0.2, 0.25, 0.2, 0.5, 0.5, 1.3, Requires: "login"),
        new("search", "Recherche", "link", "Trouver vite ce qu'on cherche.", ["saas", "social", "marketplace"],
            N((Need.Discovery, 0.3), (Need.Productivity, 0.1)), 12, 0.15, 0.25, 0.9, 0.1, 0.5, 1.1),
        new("messaging", "Messagerie", "mail", "Les utilisateurs se parlent.", ["saas", "social", "marketplace"],
            N((Need.Engagement, 0.35), (Need.Liquidity, 0.2)), 18, 0.15, 0.2, 1.2, 0.8, 0.8, 1.5, Requires: "login"),
        new("export", "Exports", "briefcase", "Les données sortent vers d'autres outils.", ["saas"],
            N((Need.Productivity, 0.3), (Need.Trust, 0.1)), 13, 0.05, 0.25, 0.4, 0.4, 0.6, 1.2),
        new("stats", "Statistiques", "chart", "Des chiffres pour les utilisateurs avancés.", ["saas", "marketplace"],
            N((Need.Productivity, 0.2), (Need.Liquidity, 0.05)), 11, 0.3, 0.2, 0.5, 0.1, 0.4, 0.8),
        // Jeux
        new("gameplay", "Gameplay", "dice", "Le cœur du jeu : ce qu'on y fait.", ["game"],
            N((Need.Fun, 0.5)), 20, 0.4, 0.15, 0.3, 0.2, 0.4, 1.2),
        new("content", "Niveaux et contenu", "leaf", "De quoi jouer longtemps.", ["game"],
            N((Need.Discovery, 0.35), (Need.Fun, 0.15)), 18, 0.6, 0.15, 0.2, 0.1, 0.3, 0.6),
        new("graphics", "Graphismes", "star", "Un jeu qui donne envie.", ["game"],
            N((Need.Trust, 0.25), (Need.Fun, 0.1)), 16, 0.75, 0.05, 0.2, 0, 0.2, 0.4),
        new("audio", "Musique et sons", "fire", "L'ambiance.", ["game"],
            N((Need.Trust, 0.15), (Need.Fun, 0.1)), 10, 0.7, 0.05, 0, 0, 0.1, 0.3),
        new("leaderboard", "Classements", "trophy", "On se compare aux autres.", ["game"],
            N((Need.Engagement, 0.3)), 10, 0.1, 0.2, 0.3, 0.2, 0.3, 0.8),
        new("achievements", "Succès", "crown", "Des objectifs à débloquer.", ["game"],
            N((Need.Engagement, 0.2), (Need.Fun, 0.05)), 8, 0.4, 0.15, 0.1, 0, 0.2, 0.5),
        new("multiplayer", "Multijoueur", "people", "Jouer ensemble, en ligne.", ["game"],
            N((Need.Engagement, 0.45), (Need.Fun, 0.2)), 30, 0.1, 0.25, 2, 0.5, 1, 1.8, UnlockNode: "multiplayer"),
        // Communauté et place de marché
        new("feed", "Fil de contenu", "screen", "Ce que publient les membres.", ["social"],
            N((Need.Engagement, 0.4), (Need.Discovery, 0.15)), 16, 0.2, 0.2, 1.4, 0.3, 0.6, 1.2),
        new("profile", "Profils", "user", "Une page par membre.", ["social", "marketplace"],
            N((Need.Engagement, 0.2), (Need.Trust, 0.1)), 9, 0.35, 0.2, 0.4, 0.2, 0.3, 0.7, Requires: "login"),
        new("moderation", "Modération", "shield", "Un endroit sûr et propre.", ["social", "marketplace"],
            N((Need.Trust, 0.3)), 10, 0.1, 0.2, 0.2, 0.4, 0.3, 0.6),
        new("listings", "Annonces", "tag", "Les vendeurs publient leurs offres.", ["marketplace"],
            N((Need.Liquidity, 0.5)), 15, 0.2, 0.2, 0.6, 0.4, 0.6, 1.2, Requires: "login"),
        new("catalog", "Catalogue", "building", "Parcourir les offres.", ["marketplace", "social"],
            N((Need.Discovery, 0.35), (Need.Liquidity, 0.1)), 12, 0.35, 0.15, 0.6, 0.1, 0.4, 0.8),
        new("checkout", "Paiement", "coin", "Acheter en deux clics.", ["marketplace"],
            N((Need.Conversion, 0.4), (Need.Trust, 0.1)), 16, 0.1, 0.3, 0.3, 0.6, 0.6, 1.4, Requires: "login"),
        new("reviews", "Avis des membres", "check", "La confiance entre membres.", ["marketplace", "social"],
            N((Need.Trust, 0.3), (Need.Discovery, 0.1)), 9, 0.2, 0.2, 0.3, 0.3, 0.4, 0.8),
    ];

    public static readonly IReadOnlyList<MarketingChannel> Channels =
    [
        new("social", "Réseaux sociaux", "Large public, peu de professionnels.",
            new() { ["saas"] = 0.2, ["game"] = 1.4, ["social"] = 2.0, ["marketplace"] = 1.1 }, 120),
        new("search", "Moteurs de recherche", "Des gens qui cherchent déjà.",
            new() { ["saas"] = 0.6, ["game"] = 0.4, ["social"] = 0.5, ["marketplace"] = 0.8 }, 80, UnlockNode: "search_ads"),
        new("events", "Salons et presse", "Cher, mais vise juste.",
            new() { ["saas"] = 0.8, ["game"] = 0.9, ["social"] = 0.05, ["marketplace"] = 0.3 }, 40, UnlockNode: "press"),
    ];

    public static readonly IReadOnlyList<HostingPlan> Hosting =
    [
        new("shared", "Mutualisé", 15m, 2),
        new("cloud_s", "Cloud S", 90m, 10),
        new("cloud_m", "Cloud M", 340m, 45, UnlockNode: "cloud_m"),
        new("cloud_l", "Cloud L", 1_150m, 170, UnlockNode: "cloud_l"),
        new("cluster", "Cluster", 3_900m, 650, UnlockNode: "cloud_l"),
    ];

    public static ProjectType Type(string id) => Types.First(a => a.Id == id);
    public static Segment Segment(string id) => Segments.First(s => s.Id == id);
    public static ComponentDef Component(string id) => Components.First(m => m.Id == id);
    public static bool HasComponent(string id) => Components.Any(m => m.Id == id);
    public static MarketingChannel Channel(string id) => Channels.First(c => c.Id == id);
    public static HostingPlan Plan(string id) => Hosting.First(h => h.Id == id);

    public static string NeedName(Need need) => need switch
    {
        Need.Discovery => "Contenu et découverte",
        Need.Engagement => "Envie de revenir",
        Need.Trust => "Confiance et finition",
        Need.Conversion => "Envie de payer",
        Need.Productivity => "Utilité au travail",
        Need.Liquidity => "Offre des vendeurs",
        _ => "Plaisir de jeu"
    };

    public static string ModelName(RevenueModel model) => model switch
    {
        RevenueModel.Subscription => "Abonnement",
        RevenueModel.Freemium => "Gratuit + option payante",
        RevenueModel.Ads => "Publicité",
        RevenueModel.Commission => "Commission",
        _ => "Vente"
    };

    /// <summary>Prix par défaut viable pour un public et un modèle.</summary>
    public static decimal DefaultPrice(Segment segment, RevenueModel model) => model switch
    {
        RevenueModel.Commission => 10m,
        RevenueModel.Ads => 0m,
        RevenueModel.Freemium => Math.Max(2m, Math.Round(segment.ReferencePrice * 1.2m)),
        _ => segment.ReferencePrice
    };
}
