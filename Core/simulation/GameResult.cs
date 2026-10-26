namespace Core.simulation;

/// <summary>Bilan d'une partie terminée, affiché sur l'écran de fin.</summary>
public record GameResult(
    double SurvivedTime,
    double FinalBalance,
    double TotalEarned,
    int CompletedTasks,
    int FailedTasks)
{
    /// <summary>
    /// 1 point par seconde survécue + 1 point par 10 CHF gagnés.
    /// Le solde final n'entre pas dans le score : à la faillite il est toujours proche de zéro.
    /// </summary>
    public int Score => (int)(SurvivedTime + TotalEarned / 10);
}
