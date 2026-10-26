namespace Core.simulation;

/// <summary>
/// Toutes les valeurs d'équilibrage du jeu. Une instance est passée aux systèmes
/// pour que les tests puissent utiliser leurs propres réglages.
/// </summary>
public class GameConfig
{
    // Temps
    public double MaxDeltaTime { get; init; } = 0.25; // secondes, évite les sauts quand la fenêtre est figée

    // Économie
    public double StartingCapital { get; init; } = 1000;
    public double BaseOperatingCost { get; init; } = 100; // par seconde

    // Demandes
    public double FirstRequestDelay { get; init; } = 2; // secondes avant la première demande
    public double MinRequestInterval { get; init; } = 4; // secondes entre deux demandes
    public double MaxRequestInterval { get; init; } = 9;
    public double OfferDuration { get; init; } = 15; // secondes avant qu'une offre non traitée expire
    public int MinDifficulty { get; init; } = 1;
    public int MaxDifficulty { get; init; } = 5;
    public double ValuePerDifficulty { get; init; } = 60; // gain moyen par point de difficulté
    public double ValueVariation { get; init; } = 0.25; // ±25 % autour du gain moyen
    public double DeadlineChance { get; init; } = 0.6; // probabilité qu'une demande ait une échéance
    public double MinDeadlineMargin { get; init; } = 1.3; // échéance = durée de travail × marge
    public double MaxDeadlineMargin { get; init; } = 2.0;

    // Tâches
    public double WorkSecondsPerDifficulty { get; init; } = 6;

    public double WorkDurationFor(int difficulty) => difficulty * WorkSecondsPerDifficulty;
}
