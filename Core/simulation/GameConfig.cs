namespace Core.simulation;

/// <summary>
/// Toutes les valeurs d'équilibrage du jeu. Une instance est passée aux systèmes
/// pour que les tests puissent utiliser leurs propres réglages.
/// Les catalogues (matériel, employés, boîtes...) sont dans GameConfig.Catalogs.cs.
/// </summary>
public partial record GameConfig
{
    // Temps
    public double MaxDeltaTime { get; init; } = 0.25; // secondes, évite les sauts quand la fenêtre est figée
    public double MonthDuration { get; init; } = 60; // secondes de jeu par mois
    public int DaysPerMonth { get; init; } = 30;

    // Économie
    public double StartingCapital { get; init; } = 150;
    public int MaxMissedPayments { get; init; } = 2; // impayés d'affilée avant la faillite
    public double WealthTaxRate { get; init; } = 0.01; // impôt mensuel sur le patrimoine...
    public double WealthTaxAllowance { get; init; } = 50_000; // ...au-delà de ce montant

    // Demandes
    public double FirstRequestDelay { get; init; } = 2; // secondes avant la première demande
    public double MinRequestInterval { get; init; } = 4; // secondes entre deux demandes
    public double MaxRequestInterval { get; init; } = 9;
    public double OfferDuration { get; init; } = 15; // secondes avant qu'une offre non traitée expire
    public int DifficultySpread { get; init; } = 3; // difficultés proposées : de (max - spread) à max
    public double ValuePerDifficulty { get; init; } = 40; // gain moyen par point de difficulté
    public double ValueVariation { get; init; } = 0.25; // ±25 % autour du gain moyen
    public double DeadlineChance { get; init; } = 0.6; // probabilité qu'une demande ait une échéance
    public double MinDeadlineMargin { get; init; } = 1.3; // échéance = durée de travail × marge
    public double MaxDeadlineMargin { get; init; } = 2.0;
    public double RequestRatePerExtraTask { get; init; } = 0.5; // +50 % de demandes par place de tâche en plus

    // Tâches
    public double WorkSecondsPerDifficulty { get; init; } = 6;
    public int MaxActiveTasks { get; init; } = 1; // capacité du joueur seul, les employés en ajoutent
    public LatePenalty LatePenalty { get; init; } = LatePenalty.ReducedReward;
    public double LateRewardMultiplier { get; init; } = 0.5; // utilisé avec LatePenalty.ReducedReward
    public double FailedReputationPenalty { get; init; } = 2; // points perdus par point de difficulté

    // Charbonner (cliquer sur le personnage)
    public double ClickWork { get; init; } = 0.3; // secondes de travail ajoutées par clic
    public double ClickGigMoney { get; init; } = 1; // CHF gagnés par clic quand il n'y a pas de tâche

    // Entreprise et empire
    public double CompanyFoundingCost { get; init; } = 5_000;
    public int EmployeesForBoss { get; init; } = 3; // le garage a juste assez de places
    public double BusinessCostGrowth { get; init; } = 1.3; // chaque niveau coûte 30 % de plus
    public double HoldingCost { get; init; } = 50_000_000;
    public double HoldingIncomeMultiplier { get; init; } = 2;

    // Roguelike
    public bool RoguelikeEnabled { get; init; } = true; // atouts, crises et événements (désactivables pour les tests)
    public int MonthsPerQuarter { get; init; } = 3; // un choix d'atout à chaque bilan trimestriel
    public int DraftChoices { get; init; } = 3;
    public double RerollBaseCost { get; init; } = 0.25; // part de la facture du mois
    public double RerollCostGrowth { get; init; } = 1.6; // chaque relance coûte 60 % de plus
    public double SkipRewardFraction { get; init; } = 0.5; // passer un choix rapporte une demi-facture
    public int FirstCrisisMonth { get; init; } = 6;
    public int CrisisEvery { get; init; } = 3; // mois entre deux crises
    public double FirstEventDelay { get; init; } = 90; // secondes
    public double MinEventInterval { get; init; } = 75;
    public double MaxEventInterval { get; init; } = 140;
    public double InterestCapBills { get; init; } = 2; // les intérêts mensuels ne dépassent pas 2 factures

    public double WorkDurationFor(int difficulty) => difficulty * WorkSecondsPerDifficulty;
}

/// <summary>Conséquence d'une tâche qui dépasse son échéance.</summary>
public enum LatePenalty
{
    /// <summary>La tâche continue et rapporte une partie de sa valeur.</summary>
    ReducedReward,

    /// <summary>La tâche échoue dès que l'échéance est dépassée, sans gain.</summary>
    Fail
}
