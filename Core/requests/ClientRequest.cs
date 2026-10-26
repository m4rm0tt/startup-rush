namespace Core.requests;

/// <summary>
/// Une demande de travail d'un client, en attente de décision du joueur.
/// Les durées sont des temps restants qui diminuent à chaque Update.
/// </summary>
public class ClientRequest
{
    public int Id { get; }
    public string ClientName { get; }
    public RequestType Type { get; }
    public double Value { get; }
    public int Difficulty { get; }

    /// <summary>Temps restant avant l'échéance du travail, null si la demande n'a pas d'échéance.</summary>
    public double? DeadlineTimeLeft { get; private set; }

    /// <summary>Temps restant avant que l'offre disparaisse si le joueur ne décide rien.</summary>
    public double OfferTimeLeft { get; private set; }

    public bool HasDeadline => DeadlineTimeLeft.HasValue;
    /// <summary>L'offre disparaît à la fin de son délai, ou plus tôt si son échéance est déjà passée.</summary>
    public bool IsExpired => OfferTimeLeft <= 0 || DeadlineTimeLeft <= 0;

    public ClientRequest(int id, string clientName, RequestType type, double value, int difficulty,
        double? deadlineTimeLeft, double offerTimeLeft)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(nameof(value));
        if (difficulty < 1)
            throw new ArgumentOutOfRangeException(nameof(difficulty));

        Id = id;
        ClientName = clientName;
        Type = type;
        Value = value;
        Difficulty = difficulty;
        DeadlineTimeLeft = deadlineTimeLeft;
        OfferTimeLeft = offerTimeLeft;
    }

    public void Update(double deltaTime)
    {
        OfferTimeLeft -= deltaTime;

        if (DeadlineTimeLeft.HasValue)
            DeadlineTimeLeft -= deltaTime;
    }
}
