namespace Core.simulation;

/// <summary>Bilan d'une run terminée, par la faillite, l'abandon ou la victoire.</summary>
public record GameResult(
    bool IsVictory,
    double SurvivedTime,
    double FinalNetWorth,
    double PeakNetWorth,
    string StageTitle,
    double TotalEarned,
    int CompletedTasks,
    int FailedTasks)
{
    public int StageIndex { get; init; }
    public int Months { get; init; }

    /// <summary>Ce qui a tué la run, null en cas de victoire.</summary>
    public string? CauseOfDeath { get; init; }

    /// <summary>Patrimoine à chaque fin de mois, pour le graphique de fin de run.</summary>
    public IReadOnlyList<double> NetWorthHistory { get; init; } = [];

    public IReadOnlyList<string> PerkIds { get; init; } = [];
    public string CharacterName { get; init; } = "";
    public int Seed { get; init; }
    public int Pressure { get; init; }
    public int CrisesSurvived { get; init; }
}
