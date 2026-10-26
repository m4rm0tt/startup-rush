namespace Core.roguelike;

/// <summary>
/// Tous les bonus et malus du moment, cumulés à partir du personnage, de la pression,
/// des atouts, des malédictions, de la crise en cours et des effets d'événements.
/// Les multiplicateurs valent 1 quand ils ne changent rien.
/// </summary>
public class Modifiers
{
    public double WorkSpeed { get; set; } = 1;
    public double ClickPower { get; set; } = 1;
    public double GigMoney { get; set; } = 1;
    public double RequestRate { get; set; } = 1;
    public double RequestValue { get; set; } = 1;
    public double OfferDuration { get; set; } = 1;
    public double DeadlineMargin { get; set; } = 1;
    public double ReputationGain { get; set; } = 1;
    public double Bills { get; set; } = 1;
    public double Rent { get; set; } = 1;
    public double BusinessIncome { get; set; } = 1;
    public double PurchaseCost { get; set; } = 1; // matériel, logement, locaux
    public double HiringCost { get; set; } = 1;
    public double StaffCapacity { get; set; } = 1; // multiplie les places de tâches apportées par l'équipe
    public int ExtraCapacity { get; set; }
    public int ExtraSeats { get; set; }

    /// <summary>Part de la valeur ajoutée en bonus à chaque contrat livré à temps.</summary>
    public double OnTimeBonus { get; set; }

    /// <summary>Part du cash versée en intérêts à chaque fin de mois (plafonnée).</summary>
    public double MonthlyInterest { get; set; }

    /// <summary>Part du portefeuille boursier versée en bonus à chaque fin de mois.</summary>
    public double MarketMonthlyBonus { get; set; }

    /// <summary>Hausse du coût de la vie à chaque mois écoulé (loyers, salaires, abonnements, charges).</summary>
    public double CostInflation { get; set; }

    /// <summary>Impôt mensuel sur la fortune, en plus du taux de base de GameConfig.</summary>
    public double ExtraWealthTax { get; set; }

    public double ClickCritChance { get; set; }
    public double ClickCritMultiplier { get; set; } = 10;

    /// <summary>Les clics rapportent un petit boulot même quand une tâche est en cours.</summary>
    public bool ClicksAlwaysPay { get; set; }
}
