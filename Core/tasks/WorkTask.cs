using Core.requests;
using Core.simulation;

namespace Core.tasks;

/// <summary>
/// Le travail lancé quand le joueur accepte une demande. Il avance avec le temps
/// et rapporte la valeur de la demande à la fin, réduite ou perdue en cas de retard.
/// </summary>
public class WorkTask
{
    private readonly LatePenalty _latePenalty;
    private readonly double _lateRewardMultiplier;

    public ClientRequest Request { get; }
    public double Duration { get; }
    public double WorkDone { get; private set; }
    public double? DeadlineTimeLeft { get; private set; }
    public WorkTaskStatus Status { get; private set; } = WorkTaskStatus.InProgress;

    /// <summary>Montant gagné, connu une fois la tâche terminée.</summary>
    public double EarnedReward { get; private set; }

    public double Progress => Math.Min(1, WorkDone / Duration);
    public double TimeRemaining => Math.Max(0, Duration - WorkDone);
    public bool IsLate => DeadlineTimeLeft is < 0;

    /// <summary>Vrai si, au rythme donné, la tâche finira après son échéance.</summary>
    public bool WillBeLate(double workSpeed = 1) =>
        DeadlineTimeLeft.HasValue && TimeRemainingAt(workSpeed) > DeadlineTimeLeft.Value;

    public WorkTask(ClientRequest request, GameConfig config)
    {
        Request = request;
        Duration = config.WorkDurationFor(request.Difficulty);
        DeadlineTimeLeft = request.DeadlineTimeLeft;
        _latePenalty = config.LatePenalty;
        _lateRewardMultiplier = config.LateRewardMultiplier;
    }

    /// <param name="workSpeed">Multiplie le travail fait pendant le delta ; l'échéance, elle, avance au temps réel.</param>
    public void Update(double deltaTime, double workSpeed = 1)
    {
        if (Status != WorkTaskStatus.InProgress)
            return;

        WorkDone += deltaTime * workSpeed;
        if (DeadlineTimeLeft.HasValue)
            DeadlineTimeLeft -= deltaTime;

        if (IsLate && _latePenalty == LatePenalty.Fail)
        {
            Status = WorkTaskStatus.Failed;
            return;
        }

        CheckCompleted();
    }

    /// <summary>Ajoute du travail d'un coup (quand le joueur charbonne en cliquant).</summary>
    public void AddWork(double work)
    {
        if (Status != WorkTaskStatus.InProgress || work <= 0)
            return;

        WorkDone += work;
        CheckCompleted();
    }

    /// <summary>Temps réel restant au rythme donné.</summary>
    public double TimeRemainingAt(double workSpeed) => TimeRemaining / workSpeed;

    private void CheckCompleted()
    {
        if (WorkDone < Duration)
            return;

        Status = WorkTaskStatus.Completed;
        EarnedReward = IsLate ? Request.Value * _lateRewardMultiplier : Request.Value;
    }
}
