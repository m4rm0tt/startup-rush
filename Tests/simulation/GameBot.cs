using Core.simulation;

namespace Tests.simulation;

public enum BotStrategy
{
    /// <summary>Ne fait rien.</summary>
    Idle,

    /// <summary>Accepte toutes les demandes dès qu'il a de la place.</summary>
    AcceptAll,

    /// <summary>Accepte d'abord les demandes les plus rentables qu'il peut finir à temps.</summary>
    AcceptOnTime
}

/// <summary>
/// Joue une partie sans interface pour repérer une économie trop facile ou impossible.
/// </summary>
public static class GameBot
{
    public static GameSession Play(GameSession session, BotStrategy strategy, double maxTime = 3600, double step = 0.1)
    {
        while (!session.IsOver && session.ElapsedTime < maxTime)
        {
            Act(session, strategy);
            session.Update(step);
        }

        return session;
    }

    private static void Act(GameSession session, BotStrategy strategy)
    {
        var company = session.Company;

        switch (strategy)
        {
            case BotStrategy.AcceptAll:
                foreach (var request in company.PendingRequests.ToList())
                    company.AcceptRequest(request.Id);
                break;

            case BotStrategy.AcceptOnTime:
                var candidates = company.PendingRequests
                    .Where(r => !r.HasDeadline || r.DeadlineTimeLeft >= session.Config.WorkDurationFor(r.Difficulty))
                    .OrderByDescending(r => r.Value / session.Config.WorkDurationFor(r.Difficulty))
                    .ToList();
                foreach (var request in candidates)
                    company.AcceptRequest(request.Id);
                break;
        }
    }
}
