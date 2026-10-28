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
        new("materiel", "Comme plusieurs_petits, puis le matériel dès 35 000 CHF : un atelier, la recherche « Conception de cartes », une carte pour les makers fabriquée sur une ligne d'assemblage, vendue en ligne.")
        {
            Projects = [("freelances", null), ("casual", null), ("smb", null)], ProjectInterval = 75, Marketing = 200, MaxStaff = 2,
            HardwareAbove = 35_000, HardwareSegment = "makers",
            TreeOrder = ["advanced", "multisite", "hardware", "online_shop", "cloud_m", "search_ads", "optimization", "hiring", "loyalty"],
        },
        new("ia", "Comme plusieurs_petits, puis l'IA dès 30 000 CHF : un petit modèle d'analyse (calcul loué), évalué, publié, un produit d'IA pour les analystes, une seconde version sur données achetées.")
        {
            Projects = [("freelances", null), ("casual", null), ("smb", null)], ProjectInterval = 75, Marketing = 200, MaxStaff = 1,
            AiAbove = 30_000,
            TreeOrder = ["advanced", "quality_lab", "ai_models", "cloud_m", "search_ads", "optimization", "hiring", "loyalty"],
        },
        new("studio_xr", "Comme plusieurs_petits, un studio au siège dès 20 000 CHF, puis le matériel et la XR : zone de test au siège et une application XR pour joueurs en quatrième projet.")
        {
            Projects = [("freelances", null), ("casual", null), ("smb", null), ("xr_gamers", null)], ProjectInterval = 75, Marketing = 200, MaxStaff = 1,
            StudioAbove = 20_000, XrAbove = 25_000,
            TreeOrder = ["advanced", "studio", "hardware", "xr", "cloud_m", "search_ads", "optimization", "hiring", "loyalty"],
        },
        new("quantique", "Comme plusieurs_petits, tout l'arbre vers le quantique (matériel, usine, salle propre, laboratoire avancé, offres de calcul), un laboratoire dès 300 000 CHF, équipé au fil de la trésorerie.")
        {
            Projects = [("freelances", null), ("casual", null), ("smb", null)], ProjectInterval = 75, Marketing = 200, MaxStaff = 2,
            QuantumAbove = 300_000,
            TreeOrder = ["advanced", "multisite", "hardware", "industry", "cleanroom", "advanced_lab", "server_room", "cloud_offers", "compute_offers", "quantum_offers", "hiring"],
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
