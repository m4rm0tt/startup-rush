using Core.simulation;

namespace Core.company;

/// <summary>
/// Les multiplicateurs actuels du joueur, recalculés par GameSession à partir
/// du matériel, des employés et de la réputation. Les systèmes les lisent sans
/// avoir besoin de connaître leur origine.
/// </summary>
public class PlayerStats
{
    public double WorkSpeed { get; set; } = 1;
    public int TaskCapacity { get; set; }
    public double RequestRateMultiplier { get; set; } = 1;
    public double ClickPower { get; set; } = 1;
    public int MaxDifficulty { get; set; }
    public double ValueMultiplier { get; set; } = 1;
    public bool AutoAccept { get; set; }
    public double GigMultiplier { get; set; } = 1;
    public double ReputationGain { get; set; } = 1;
    public double OfferDurationMultiplier { get; set; } = 1;
    public double DeadlineMultiplier { get; set; } = 1;

    /// <summary>Les clics rapportent un petit boulot même quand une tâche est en cours.</summary>
    public bool ClicksAlwaysPay { get; set; }

    public PlayerStats(GameConfig config)
    {
        TaskCapacity = config.MaxActiveTasks;
        MaxDifficulty = config.ReputationLevels[0].MaxDifficulty;
        ValueMultiplier = config.ReputationLevels[0].ValueMultiplier;
    }
}
