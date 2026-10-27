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
        new("marketing_excessif", "Erreur : 6 000 CHF de marketing par mois dès la sortie d'une petite application.")
        {
            Projects = [("freelances", null)], Marketing = 6_000, MaxStaff = 0, Recover = false, Borrow = false,
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
