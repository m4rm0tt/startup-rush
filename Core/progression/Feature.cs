namespace Core.progression;

/// <summary>Les mécaniques qui se débloquent au fil de l'histoire.</summary>
public enum Feature
{
    FoundCompany,
    Hiring,
    Businesses,
    Market,
    Holding
}

public static class FeatureExtensions
{
    public static CareerStage RequiredStage(this Feature feature) => feature switch
    {
        Feature.FoundCompany => CareerStage.Freelancer,
        Feature.Hiring => CareerStage.Founder,
        Feature.Businesses => CareerStage.Boss,
        Feature.Market => CareerStage.Investor,
        Feature.Holding => CareerStage.Tycoon,
        _ => CareerStage.KingOfTheWorld
    };
}
