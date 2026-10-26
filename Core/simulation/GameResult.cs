namespace Core.simulation;

/// <summary>Bilan d'une partie terminée, par la faillite ou par la victoire.</summary>
public record GameResult(
    bool IsVictory,
    double SurvivedTime,
    double FinalNetWorth,
    double PeakNetWorth,
    string StageTitle,
    double TotalEarned,
    int CompletedTasks,
    int FailedTasks);
