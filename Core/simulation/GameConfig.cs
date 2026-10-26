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
}
