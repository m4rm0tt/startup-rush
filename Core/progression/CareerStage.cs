namespace Core.progression;

/// <summary>Les étapes de l'histoire, du fauché au roi du monde.</summary>
public enum CareerStage
{
    Broke,
    Freelancer,
    Founder,
    Boss,
    Investor,
    Tycoon,
    KingOfTheWorld
}

/// <param name="NetWorthRequired">Patrimoine à atteindre pour passer à ce statut.</param>
/// <param name="Requirement">Condition affichée au joueur.</param>
/// <param name="Story">Texte de l'histoire affiché quand le statut est atteint.</param>
public record CareerStageInfo(
    CareerStage Stage,
    string Title,
    double NetWorthRequired,
    string Requirement,
    string Story);

/// <summary>Ce que la carrière doit savoir du joueur pour décider d'un passage de statut.</summary>
public record CareerSnapshot(double NetWorth, bool CompanyFounded, int EmployeeCount, bool HasHolding);
