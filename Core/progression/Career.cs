namespace Core.progression;

/// <summary>
/// Fait avancer le joueur dans l'histoire. Un statut atteint n'est jamais perdu.
/// </summary>
public class Career
{
    private readonly IReadOnlyList<CareerStageInfo> _stages;
    private readonly int _employeesForBoss;
    private int _index;

    public CareerStageInfo Current => _stages[_index];
    public CareerStageInfo? Next => _index + 1 < _stages.Count ? _stages[_index + 1] : null;
    public CareerStage Stage => Current.Stage;

    public event Action<CareerStageInfo>? StageReached;

    public Career(IReadOnlyList<CareerStageInfo> stages, int employeesForBoss)
    {
        _stages = stages;
        _employeesForBoss = employeesForBoss;
    }

    public bool IsUnlocked(CareerStage stage) => Stage >= stage;

    public bool CanReach(CareerStageInfo info, CareerSnapshot snapshot)
    {
        if (snapshot.NetWorth < info.NetWorthRequired)
            return false;

        return info.Stage switch
        {
            CareerStage.Founder => snapshot.CompanyFounded,
            CareerStage.Boss => snapshot.EmployeeCount >= _employeesForBoss,
            CareerStage.KingOfTheWorld => snapshot.HasHolding,
            _ => true
        };
    }

    /// <summary>Progression du patrimoine vers le statut suivant, de 0 à 1.</summary>
    public double ProgressToNext(double netWorth)
    {
        if (Next == null)
            return 1;

        double from = Current.NetWorthRequired;
        double to = Next.NetWorthRequired;
        if (to <= from)
            return netWorth >= to ? 1 : 0;

        return Math.Clamp((netWorth - from) / (to - from), 0, 1);
    }

    public void Update(CareerSnapshot snapshot)
    {
        while (Next != null && CanReach(Next, snapshot))
        {
            _index++;
            StageReached?.Invoke(Current);
        }
    }
}
