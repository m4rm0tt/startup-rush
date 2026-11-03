namespace Core.bots;

/// <summary>Les façons de jouer mesurées par la campagne d'équilibrage (BALANCE_REPORT.md).</summary>
public static class Strategies
{
    public static readonly IReadOnlyList<Strategy> All =
    [
        new("un_projet", "Se concentrer sur une application, l'améliorer d'après les avis, embaucher quand elle rapporte.")
        {
            Projects = [("freelances", null)], Marketing = 300, MaxStaff = 2,
        },
        new("plusieurs_petits", "Plusieurs petits projets l'un après l'autre (application, jeu, application pour PME).")
        {
            Projects = [("freelances", null), ("casual", null), ("smb", null)], ProjectInterval = 75, Marketing = 200, MaxStaff = 1,
        },
        new("ambitieux", "Un jeu ambitieux pour passionnés : beaucoup de composants, une embauche dès le départ, promotion avant la sortie.")
        {
            Projects = [("core", ["gameplay", "content", "graphics", "audio", "ui", "achievements"])],
            PreLaunchMarketing = 800, Marketing = 800, MaxStaff = 1, HireAbove = 0, CarefulHiring = false,
        },
        new("jeu_simple", "Un petit jeu grand public, puis des mises à jour régulières.")
        {
            Projects = [("casual", null)], Marketing = 300, MaxStaff = 1,
        },
        new("delegation", "Comme plusieurs_petits, mais le fondateur arrête de taper dès la première embauche : l'équipe développe, lance et prépare les petites mises à jour.")
        {
            Projects = [("freelances", null), ("casual", null), ("smb", null)], ProjectInterval = 75, Marketing = 200, MaxStaff = 2,
            StopTypingWithStaff = 1, Delegate = true,
            TreeOrder = ["advanced", "delegation", "hiring", "procedures", "cloud_m", "automation", "optimization", "loyalty"],
        },
        new("hebergement_maison", "Comme plusieurs_petits, puis une salle serveur dès 40 000 CHF : les projets passent sur ses serveurs, le stockage et l'hébergement libres sont vendus.")
        {
            Projects = [("freelances", null), ("casual", null), ("smb", null)], ProjectInterval = 75, Marketing = 200, MaxStaff = 1,
            SelfHostAbove = 40_000,
            TreeOrder = ["advanced", "multisite", "server_room", "cloud_m", "cloud_offers", "search_ads", "optimization", "hiring", "loyalty"],
        },
        new("materiel", "Comme plusieurs_petits, puis le matériel dès 50 000 CHF (une carte à 95 CHF met plusieurs mois à couvrir l'atelier) : un atelier, la recherche « Conception de cartes », une carte pour les makers fabriquée sur une ligne d'assemblage, vendue en ligne.")
        {
            Projects = [("freelances", null), ("casual", null), ("smb", null)], ProjectInterval = 75, Marketing = 200, MaxStaff = 2,
            HardwareAbove = 50_000, HardwareSegment = "makers",
            TreeOrder = ["advanced", "multisite", "hardware", "online_shop", "cloud_m", "search_ads", "optimization", "hiring", "loyalty"],
        },
        new("ia", "Comme plusieurs_petits, puis l'IA dès 30 000 CHF : un petit modèle d'analyse (calcul loué), évalué, publié, un produit d'IA pour les analystes, une seconde version sur données achetées.")
        {
            Projects = [("freelances", null), ("casual", null), ("smb", null)], ProjectInterval = 75, Marketing = 200, MaxStaff = 1,
            AiAbove = 30_000,
            TreeOrder = ["advanced", "quality_lab", "ai_models", "cloud_m", "search_ads", "optimization", "hiring", "loyalty"],
        },
        new("studio_xr", "Comme plusieurs_petits, un studio au siège dès 20 000 CHF, puis le matériel et la XR : zone de test au siège et une application XR pour joueurs en quatrième projet ; la compétence qui manque au studio ou à la zone XR (design, tests) s’acquiert par une formation.")
        {
            Projects = [("freelances", null), ("casual", null), ("smb", null), ("xr_gamers", null)], ProjectInterval = 75, Marketing = 200, MaxStaff = 1,
            StudioAbove = 20_000, XrAbove = 25_000,
            TreeOrder = ["advanced", "studio", "hardware", "xr", "cloud_m", "search_ads", "optimization", "hiring", "loyalty"],
        },
        new("quantique", "Comme plusieurs_petits, tout l'arbre vers le quantique (matériel, usine, salle propre, laboratoire avancé, offres de calcul), un laboratoire dès 230 000 CHF (de quoi l’équiper d’un bloc et payer six mois de loyer), équipé au fil de la trésorerie.")
        {
            Projects = [("freelances", null), ("casual", null), ("smb", null)], ProjectInterval = 75, Marketing = 200, MaxStaff = 2,
            QuantumAbove = 230_000,
            TreeOrder = ["advanced", "multisite", "hardware", "industry", "cleanroom", "advanced_lab", "server_room", "cloud_offers", "compute_offers", "quantum_offers", "hiring"],
        },
        // Scénarios ajoutés à la finition : un segment isolé, chaque famille de matériel, l'IA sur ses propres GPU
        new("un_projet_pme", "Comme un_projet, mais une application pour PME : isole l'effet du segment (prix 39 CHF, marché de 30 000).")
        {
            Projects = [("smb", null)], Marketing = 300, MaxStaff = 2,
        },
        new("materiel_industrie", "Comme materiel, mais la carte est vendue à l'industrie (220 CHF, marché de 25 000) plutôt qu'aux makers.")
        {
            Projects = [("freelances", null), ("casual", null), ("smb", null)], ProjectInterval = 75, Marketing = 200, MaxStaff = 2,
            HardwareAbove = 35_000, HardwareSegment = "iot_industry",
            TreeOrder = ["advanced", "multisite", "hardware", "online_shop", "cloud_m", "search_ads", "optimization", "hiring", "loyalty"],
        },
        new("materiel_pc", "Comme materiel, mais des PC pour joueurs : recherche « Validation de PC » (banc de test), PC assemblés sur la ligne.")
        {
            Projects = [("freelances", null), ("casual", null), ("smb", null)], ProjectInterval = 75, Marketing = 200, MaxStaff = 2,
            HardwareAbove = 35_000, HardwareSegment = "gamers_pc",
            TreeOrder = ["advanced", "multisite", "hardware", "online_shop", "cloud_m", "search_ads", "optimization", "hiring", "loyalty"],
        },
        new("materiel_serveur", "Comme materiel_pc, puis « Serveurs et NAS » : des serveurs pour l'auto-hébergement.")
        {
            Projects = [("freelances", null), ("casual", null), ("smb", null)], ProjectInterval = 75, Marketing = 200, MaxStaff = 2,
            HardwareAbove = 35_000, HardwareSegment = "homelab",
            TreeOrder = ["advanced", "multisite", "hardware", "online_shop", "cloud_m", "search_ads", "optimization", "hiring", "loyalty"],
        },
        new("materiel_gpu", "Comme materiel, mais dès 70 000 CHF (la chaîne coûte plus) : l'industrialisation, « Assemblage de cartes en série », « Cartes graphiques », des GPU pour joueurs.")
        {
            Projects = [("freelances", null), ("casual", null), ("smb", null)], ProjectInterval = 75, Marketing = 200, MaxStaff = 2,
            HardwareAbove = 70_000, HardwareSegment = "gpu_gamers",
            TreeOrder = ["advanced", "multisite", "hardware", "industry", "online_shop", "cloud_m", "search_ads", "optimization", "hiring", "loyalty"],
        },
        new("ia_salle_serveur", "Comme ia, avec une salle serveur dès 40 000 CHF et un serveur GPU : l'IA entraîne et sert sur ses propres GPU.")
        {
            Projects = [("freelances", null), ("casual", null), ("smb", null)], ProjectInterval = 75, Marketing = 200, MaxStaff = 1,
            AiAbove = 30_000, SelfHostAbove = 40_000, ExtraRackModules = ["module_gpu_4u"],
            TreeOrder = ["advanced", "quality_lab", "ai_models", "multisite", "server_room", "high_density", "cloud_offers", "cloud_m", "search_ads", "optimization", "hiring"],
        },
        new("marketing_excessif", "Erreur : 6 000 CHF de marketing par mois dès la sortie d'une petite application.")
        {
            Projects = [("freelances", null)], Marketing = 6_000, MaxStaff = 0, Recover = false, Borrow = false,
        },
        new("exces_puis_correction", "Même erreur de marketing, mais corrigée dès que l'argent devient tendu (budget coupé, prêt si possible).")
        {
            Projects = [("freelances", null)], Marketing = 6_000, MaxStaff = 1,
        },
        new("recrutement_precoce", "Erreur : trois embauches dès le départ, avant le moindre revenu.")
        {
            Projects = [("freelances", null)], Marketing = 300, MaxStaff = 3, HireAbove = 0, CarefulHiring = false, Borrow = false,
        },
        new("trop_de_projets", "Erreur : cinq projets ouverts d'un coup, chacun avec 1 500 CHF de promotion par mois avant même sa sortie.")
        {
            Projects = [("freelances", null), ("casual", null), ("smb", null), ("core", null), ("consumers", null)],
            ProjectInterval = 0, PreLaunchMarketing = 1_500, Marketing = 1_500, MaxStaff = 0, Borrow = false, Recover = false,
        },
    ];
}
