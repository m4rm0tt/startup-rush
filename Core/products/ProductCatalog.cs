namespace Core.products;

/// <summary>Les besoins des utilisateurs auxquels les composants répondent.</summary>
public enum Need { Discovery, Engagement, Trust, Conversion, Productivity, Liquidity, Fun, Power, Durability, Portability, Immersion, Intelligence }

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
    string? UnlockNode = null)
{
    /// <summary>Un produit matériel : fabriqué, stocké, vendu à l'unité par des canaux (Core/industry).</summary>
    public bool Hardware { get; init; }

    /// <summary>Nomenclature de base d'une unité (matières du stock), avant ce qu'ajoutent les composants.</summary>
    public Dictionary<string, int> Bom { get; init; } = new();
}

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

    /// <summary>Matières ajoutées à chaque unité fabriquée (composants matériels), quel que soit le niveau.</summary>
    public Dictionary<string, int> Bom { get; init; } = new();

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

        // Lot 6 : produits d'IA (propulsés par un modèle entraîné dans le jeu) et applications XR
        new("ai", "Produit d'IA", "ai", "Un assistant, un outil de génération, d'analyse ou une API, propulsé par ton propre modèle.",
            [RevenueModel.Subscription, RevenueModel.Freemium],
            new() { [Need.Intelligence] = 0.4, [Need.Trust] = 0.25, [Need.Productivity] = 0.2, [Need.Conversion] = 0.15 },
            TrialRate: 0.08, ChurnFactor: 0.6, WordOfMouthFactor: 1.4, TicketFactor: 1.2,
            ["ai_core", "ui"], UnlockNode: "ai_models"),
        new("xr_app", "Application XR", "screen", "Une expérience immersive : se teste avec un casque, des balises et un tapis.",
            [RevenueModel.Purchase, RevenueModel.Subscription],
            new() { [Need.Immersion] = 0.4, [Need.Fun] = 0.2, [Need.Trust] = 0.2, [Need.Engagement] = 0.2 },
            TrialRate: 0.05, ChurnFactor: 1.1, WordOfMouthFactor: 1.8, TicketFactor: 0.8,
            ["immersion", "graphics"], UnlockNode: "xr"),

        // Matériel (lot 5) : conçu par l'équipe comme un logiciel, puis fabriqué, stocké et vendu à l'unité
        new("board", "Cartes et kits", "computer", "Des cartes électroniques et kits embarqués, vendus à l'unité.",
            [RevenueModel.Purchase],
            new() { [Need.Productivity] = 0.35, [Need.Durability] = 0.25, [Need.Trust] = 0.2, [Need.Power] = 0.2 },
            TrialRate: 0.06, ChurnFactor: 0.6, WordOfMouthFactor: 1.4, TicketFactor: 0.7, ["firmware", "sensors"], UnlockNode: "res_boards")
        { Hardware = true, Bom = new() { ["carte"] = 1, ["kit"] = 2 } },
        new("pc", "PC assemblés", "computer", "Des ordinateurs montés, configurés et testés chez toi.",
            [RevenueModel.Purchase],
            new() { [Need.Power] = 0.4, [Need.Durability] = 0.25, [Need.Trust] = 0.2, [Need.Fun] = 0.15 },
            TrialRate: 0.025, ChurnFactor: 0.5, WordOfMouthFactor: 1.2, TicketFactor: 0.8, ["cpu_tier", "cooling"], UnlockNode: "res_pc")
        { Hardware = true, Bom = new() { ["boitier"] = 1, ["carte"] = 1, ["puce"] = 1, ["memoire"] = 2 } },
        new("laptop", "Ordinateurs portables", "computer", "Des portables : autonomie, finesse et puissance à arbitrer.",
            [RevenueModel.Purchase],
            new() { [Need.Portability] = 0.4, [Need.Power] = 0.25, [Need.Durability] = 0.2, [Need.Trust] = 0.15 },
            TrialRate: 0.02, ChurnFactor: 0.5, WordOfMouthFactor: 1.2, TicketFactor: 0.9, ["battery", "cpu_tier"], UnlockNode: "res_laptop")
        { Hardware = true, Bom = new() { ["boitier"] = 1, ["carte"] = 1, ["puce"] = 1, ["memoire"] = 1, ["kit"] = 4 } },
        new("server_hw", "Serveurs et NAS", "computer", "Des serveurs et stockages pour les entreprises.",
            [RevenueModel.Purchase],
            new() { [Need.Durability] = 0.4, [Need.Power] = 0.3, [Need.Productivity] = 0.3 },
            TrialRate: 0.02, ChurnFactor: 0.4, WordOfMouthFactor: 0.8, TicketFactor: 1.0, ["redundancy", "cpu_tier"], UnlockNode: "res_server")
        { Hardware = true, Bom = new() { ["boitier"] = 1, ["carte"] = 1, ["puce"] = 2, ["memoire"] = 4 } },
        new("gpu_card", "Cartes graphiques", "bolt", "Des GPU : puce, mémoire et carte, assemblés et testés.",
            [RevenueModel.Purchase],
            new() { [Need.Power] = 0.5, [Need.Durability] = 0.25, [Need.Trust] = 0.25 },
            TrialRate: 0.025, ChurnFactor: 0.6, WordOfMouthFactor: 1.5, TicketFactor: 0.7, ["cpu_tier", "cooling"], UnlockNode: "res_gpu")
        { Hardware = true, Bom = new() { ["gpu"] = 1 } },
        new("printer3d", "Imprimantes 3D", "gem", "Des imprimantes 3D pour les makers et les écoles.",
            [RevenueModel.Purchase],
            new() { [Need.Productivity] = 0.4, [Need.Durability] = 0.3, [Need.Trust] = 0.3 },
            TrialRate: 0.03, ChurnFactor: 0.5, WordOfMouthFactor: 1.6, TicketFactor: 1.0, ["print_head", "enclosure"], UnlockNode: "res_printer")
        { Hardware = true, Bom = new() { ["boitier"] = 1, ["carte"] = 1, ["kit"] = 6 } },
        new("network", "Équipements réseau", "wifi", "Routeurs et switches pour les petites entreprises.",
            [RevenueModel.Purchase],
            new() { [Need.Durability] = 0.35, [Need.Productivity] = 0.35, [Need.Power] = 0.3 },
            TrialRate: 0.04, ChurnFactor: 0.4, WordOfMouthFactor: 1.0, TicketFactor: 0.9, ["connectivity", "firmware"], UnlockNode: "res_network")
        { Hardware = true, Bom = new() { ["boitier"] = 1, ["carte"] = 1, ["kit"] = 2 } },
        new("xr_kit", "Kits XR", "computer", "Casques et balises de suivi vendus en kit.",
            [RevenueModel.Purchase],
            new() { [Need.Immersion] = 0.35, [Need.Trust] = 0.25, [Need.Power] = 0.2, [Need.Portability] = 0.2 },
            TrialRate: 0.025, ChurnFactor: 0.6, WordOfMouthFactor: 1.4, TicketFactor: 0.9, ["tracking", "firmware"], UnlockNode: "res_xr")
        { Hardware = true, Bom = new() { ["boitier"] = 1, ["carte"] = 1, ["puce"] = 1, ["kit"] = 3 } },
        new("quantum", "Systèmes quantiques", "quantum", "Des machines de calcul quantique pour laboratoires et grands comptes, en fin de progression.",
            [RevenueModel.Purchase],
            new() { [Need.Power] = 0.4, [Need.Durability] = 0.3, [Need.Trust] = 0.3 },
            TrialRate: 0.01, ChurnFactor: 0.4, WordOfMouthFactor: 0.8, TicketFactor: 1.2, ["quantum_core", "cryo_module"], UnlockNode: "res_qsystems")
        { Hardware = true, Bom = new() { ["boitier"] = 6, ["carte"] = 10, ["puce"] = 20, ["memoire"] = 20, ["kit"] = 60 } },
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
        new("makers", "Makers", "board", 120_000, 65m, 1.3, "Bricoleurs et écoles : petits prix, gros volumes."),
        new("iot_industry", "Industrie", "board", 25_000, 220m, 0.7, "Veulent du robuste, paient plus."),
        new("gamers_pc", "Joueurs PC", "pc", 60_000, 1_100m, 1.0, "Veulent de la puissance et du silence."),
        new("office_pc", "Bureaux", "pc", 40_000, 750m, 0.8, "Des machines fiables, sans surprise."),
        new("students_laptop", "Étudiants", "laptop", 90_000, 900m, 1.4, "Légers, endurants, pas trop chers."),
        new("pros_laptop", "Professionnels", "laptop", 50_000, 1_700m, 0.7, "Paient pour la qualité."),
        new("it_smb", "Informatique de PME", "server_hw", 12_000, 2_800m, 0.7, "Ne veulent jamais de panne."),
        new("homelab", "Auto-hébergement", "server_hw", 20_000, 900m, 1.2, "Des passionnés qui hébergent chez eux."),
        new("gpu_gamers", "Joueurs", "gpu_card", 80_000, 650m, 1.1, "Chaque image par seconde compte."),
        new("gpu_compute", "Calcul et IA", "gpu_card", 15_000, 1_800m, 0.6, "Achètent par lots, veulent de la puissance."),
        new("printer_makers", "Makers", "printer3d", 50_000, 600m, 1.2, "Bricolent, comparent, partagent."),
        new("printer_schools", "Écoles", "printer3d", 15_000, 1_100m, 0.8, "Veulent du simple et du solide."),
        new("net_smb", "PME", "network", 30_000, 350m, 0.9, "Un réseau qui tient, sans informaticien."),
        new("ai_assist", "Grand public", "ai", 200_000, 12m, 1.4, "Un assistant pour tous les jours : rapide et bavard."),
        new("ai_creative", "Créatifs", "ai", 120_000, 20m, 1.0, "Veulent des textes et des images de qualité."),
        new("ai_analytics", "Analystes en PME", "ai", 25_000, 90m, 0.7, "Un domaine étroit, une réponse juste."),
        new("ai_api", "Développeurs (API)", "ai", 30_000, 49m, 0.9, "Intègrent ton modèle à leurs produits."),
        new("xr_gamers", "Joueurs XR", "xr_app", 60_000, 25m, 1.3, "Équipés d'un casque, avides de nouveautés."),
        new("xr_pros", "Formation et industrie", "xr_app", 12_000, 180m, 0.7, "Simulations de formation, très exigeantes."),
        new("xr_consumers", "Grand public", "xr_kit", 40_000, 450m, 1.2, "Un casque abordable et fiable."),
        new("xr_training", "Entreprises", "xr_kit", 8_000, 1_400m, 0.7, "Des kits de formation par lots."),
        new("quantum_labs", "Laboratoires et grands comptes", "quantum", 300, 45_000m, 0.5, "Peu d'acheteurs, des machines très chères."),
    ];

    private static Dictionary<Need, double> N(params (Need Need, double Value)[] needs) => needs.ToDictionary(n => n.Need, n => n.Value);

    public static readonly IReadOnlyList<ComponentDef> Components =
    [
        // Communs
        new("ui", "Interface soignée", "gem", "Plus agréable et plus clair.", ["saas", "game", "social", "marketplace", "ai", "xr_app"],
            N((Need.Trust, 0.15), (Need.Engagement, 0.1), (Need.Fun, 0.1)), 10, 0.7, 0.1, 0.1, 0, 0.2, 0.4),
        new("performance", "Performances", "bolt", "Moins de charge sur les serveurs (−25 % par niveau).", ["saas", "game", "social", "marketplace", "ai", "xr_app"],
            N(), 12, 0.05, 0.25, 0, 0, 0.2, 0.6),
        new("support", "Aide intégrée", "heart", "Moins de questions au support.", ["saas", "social", "marketplace", "ai", "xr_app"],
            N((Need.Trust, 0.2)), 6, 0.3, 0.1, 0.1, -0.3, 0.2, 0.3),
        new("login", "Comptes", "user", "Chacun a son compte.", ["saas", "social", "marketplace", "ai"],
            N((Need.Trust, 0.2)), 10, 0.2, 0.2, 0.3, 0.3, 0.4, 0.9),
        new("notifications", "Notifications", "megaphone", "Fait revenir les utilisateurs.", ["saas", "social", "game"],
            N((Need.Engagement, 0.2)), 8, 0.1, 0.2, 0.4, 0.1, 0.3, 0.7),
        new("cloudsave", "Sauvegarde en ligne", "shield", "Rien ne se perd.", ["saas", "game"],
            N((Need.Trust, 0.2), (Need.Engagement, 0.1)), 10, 0.1, 0.25, 0.3, 0.2, 0.3, 0.9),
        // Applications
        new("dashboard", "Tableau de bord", "chart", "L'écran de travail principal.", ["saas"],
            N((Need.Productivity, 0.45), (Need.Engagement, 0.1)), 18, 0.3, 0.2, 0.8, 0.5, 0.7, 1.3, Requires: "login"),
        new("subscribe", "Abonnements", "piggy", "Des offres payantes simples à choisir.", ["saas", "ai"],
            N((Need.Conversion, 0.45)), 14, 0.2, 0.25, 0.2, 0.5, 0.5, 1.3, Requires: "login"),
        new("search", "Recherche", "link", "Trouver vite ce qu'on cherche.", ["saas", "social", "marketplace"],
            N((Need.Discovery, 0.3), (Need.Productivity, 0.1)), 12, 0.15, 0.25, 0.9, 0.1, 0.5, 1.1),
        new("messaging", "Messagerie", "mail", "Les utilisateurs se parlent.", ["saas", "social", "marketplace"],
            N((Need.Engagement, 0.35), (Need.Liquidity, 0.2)), 18, 0.15, 0.2, 1.2, 0.8, 0.8, 1.5, Requires: "login"),
        new("export", "Exports", "briefcase", "Les données sortent vers d'autres outils.", ["saas", "ai"],
            N((Need.Productivity, 0.3), (Need.Trust, 0.1)), 13, 0.05, 0.25, 0.4, 0.4, 0.6, 1.2),
        new("stats", "Statistiques", "chart", "Des chiffres pour les utilisateurs avancés.", ["saas", "marketplace", "ai"],
            N((Need.Productivity, 0.2), (Need.Liquidity, 0.05)), 11, 0.3, 0.2, 0.5, 0.1, 0.4, 0.8),
        // Jeux
        new("gameplay", "Gameplay", "dice", "Le cœur du jeu : ce qu'on y fait.", ["game"],
            N((Need.Fun, 0.5)), 20, 0.4, 0.15, 0.3, 0.2, 0.4, 1.2),
        new("content", "Niveaux et contenu", "leaf", "De quoi jouer longtemps.", ["game", "xr_app"],
            N((Need.Discovery, 0.35), (Need.Fun, 0.15)), 18, 0.6, 0.15, 0.2, 0.1, 0.3, 0.6),
        new("graphics", "Graphismes", "star", "Un jeu qui donne envie.", ["game", "xr_app"],
            N((Need.Trust, 0.25), (Need.Fun, 0.1)), 16, 0.75, 0.05, 0.2, 0, 0.2, 0.4),
        new("audio", "Musique et sons", "fire", "L'ambiance.", ["game", "xr_app"],
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

        // Matériel : la conception se fait comme un logiciel ; chaque composant ajoute des matières à chaque unité
        new("cpu_tier", "Processeur rapide", "bolt", "Plus de puissance, une puce plus chère.", ["pc", "laptop", "server_hw", "gpu_card", "xr_kit"],
            N((Need.Power, 0.4)), 14, 0.2, 0.3, 0, 0.1, 0.3, 0.8) { Bom = new() { ["kit"] = 1 } },
        new("gpu_slot", "Carte graphique intégrée", "star", "Une vraie carte graphique dans la machine.", ["pc", "laptop"],
            N((Need.Power, 0.3), (Need.Fun, 0.25)), 12, 0.2, 0.3, 0, 0.1, 0.3, 0.8) { Bom = new() { ["gpu"] = 1 } },
        new("ram_more", "Mémoire étendue", "gem", "Deux modules de mémoire de plus.", ["pc", "laptop", "server_hw"],
            N((Need.Power, 0.15), (Need.Productivity, 0.1)), 6, 0.1, 0.4, 0, 0, 0.1, 0.3) { Bom = new() { ["memoire"] = 2 } },
        new("cooling", "Refroidissement silencieux", "leaf", "Moins de bruit, des pièces qui durent.", ["pc", "laptop", "server_hw", "gpu_card"],
            N((Need.Durability, 0.3), (Need.Trust, 0.1)), 10, 0.5, 0.3, 0, 0, 0.1, 0.4) { Bom = new() { ["kit"] = 1 } },
        new("case_premium", "Boîtier soigné", "gem", "Un objet qu'on a envie de montrer.", ["pc", "laptop", "server_hw", "printer3d", "network", "xr_kit", "quantum"],
            N((Need.Trust, 0.35)), 10, 0.8, 0.1, 0, 0, 0.1, 0.3),
        new("battery", "Autonomie", "bolt", "Une batterie qui tient la journée.", ["laptop"],
            N((Need.Portability, 0.45)), 14, 0.3, 0.4, 0, 0.1, 0.2, 0.7) { Bom = new() { ["kit"] = 2 } },
        new("slim", "Finesse", "leaf", "Plus léger, plus fin.", ["laptop"],
            N((Need.Portability, 0.3), (Need.Trust, 0.1)), 16, 0.7, 0.2, 0, 0, 0.1, 0.6),
        new("firmware", "Logiciel embarqué", "screen", "Le programme livré avec l'appareil, et ses mises à jour.", ["board", "network", "printer3d", "server_hw", "xr_kit"],
            N((Need.Productivity, 0.3), (Need.Durability, 0.1)), 14, 0.1, 0.25, 0, 0.3, 0.5, 1.0),
        new("connectivity", "Connectique", "link", "Plus de ports, plus de protocoles.", ["board", "network", "server_hw"],
            N((Need.Productivity, 0.25), (Need.Power, 0.1)), 10, 0.2, 0.3, 0, 0.1, 0.2, 0.5) { Bom = new() { ["kit"] = 1 } },
        new("redundancy", "Redondance", "shield", "Tout en double : rien ne s'arrête.", ["server_hw", "network"],
            N((Need.Durability, 0.45)), 16, 0.2, 0.4, 0, 0, 0.2, 0.8) { Bom = new() { ["carte"] = 1, ["kit"] = 2 } },
        new("sensors", "Capteurs", "chart", "Mesurer le monde : température, mouvement, lumière.", ["board"],
            N((Need.Productivity, 0.3), (Need.Power, 0.1)), 10, 0.3, 0.3, 0, 0.1, 0.2, 0.5) { Bom = new() { ["kit"] = 2 } },
        new("print_head", "Tête d'impression précise", "gem", "Des pièces plus fines, plus vite.", ["printer3d"],
            N((Need.Productivity, 0.35), (Need.Power, 0.1)), 14, 0.3, 0.4, 0, 0.1, 0.3, 0.7) { Bom = new() { ["kit"] = 2 } },
        new("enclosure", "Châssis rigide", "shield", "Robuste, stable, sûr.", ["printer3d", "board", "network"],
            N((Need.Durability, 0.35)), 10, 0.6, 0.2, 0, 0, 0.1, 0.4) { Bom = new() { ["boitier"] = 1 } },
        // Lot 6 : IA, XR, quantique
        new("ai_core", "Modèle d'IA", "bolt", "Branche un de tes modèles publiés : sa qualité devient celle du produit.", ["ai"],
            N((Need.Intelligence, 0.55)), 14, 0.15, 0.3, 1.5, 0.4, 0.5, 1.2),
        new("ai_feature", "Fonctions d'IA", "bolt", "Une fonction propulsée par l'un de tes modèles publiés (elle partage sa capacité).", ["saas", "game", "social", "marketplace", "xr_app"],
            N((Need.Productivity, 0.2), (Need.Engagement, 0.15), (Need.Discovery, 0.1)), 12, 0.2, 0.3, 1.0, 0.4, 0.5, 1.1, UnlockNode: "ai_models"),
        new("immersion", "Immersion", "star", "Le cœur de l'expérience XR : présence, échelle, mouvement.", ["xr_app"],
            N((Need.Immersion, 0.5), (Need.Fun, 0.1)), 22, 0.35, 0.2, 0.6, 0.3, 0.5, 1.3),
        new("tracking", "Suivi de mouvement", "link", "Précision du suivi : moins de nausée, plus de présence.", ["xr_app", "xr_kit"],
            N((Need.Immersion, 0.3), (Need.Trust, 0.1)), 16, 0.2, 0.3, 0.3, 0.1, 0.3, 0.8) { Bom = new() { ["kit"] = 2 } },
        new("quantum_core", "Cœur quantique", "bolt", "Le processeur et son électronique de contrôle.", ["quantum"],
            N((Need.Power, 0.5)), 30, 0.2, 0.3, 0, 0.2, 0.4, 1.4) { Bom = new() { ["puce"] = 4, ["carte"] = 2 } },
        new("cryo_module", "Module cryogénique", "shield", "Le refroidissement qui rend la machine stable et fiable.", ["quantum"],
            N((Need.Durability, 0.5), (Need.Trust, 0.1)), 24, 0.3, 0.4, 0, 0.1, 0.3, 1.0) { Bom = new() { ["boitier"] = 2, ["kit"] = 10 } },
        new("hw_support", "Guide et garantie claire", "heart", "Moins de questions et de retours.", ["board", "pc", "laptop", "server_hw", "gpu_card", "printer3d", "network", "xr_kit", "quantum"],
            N((Need.Trust, 0.2)), 6, 0.4, 0.1, 0, -0.3, 0.1, 0.2),
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
        Need.Power => "Puissance",
        Need.Durability => "Robustesse",
        Need.Portability => "Mobilité",
        Need.Immersion => "Immersion",
        Need.Intelligence => "Qualité des réponses",
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
