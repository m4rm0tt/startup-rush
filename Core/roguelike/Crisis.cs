using Core.progression;
using Core.simulation;

namespace Core.roguelike;

/// <summary>
/// Un mois de crise, annoncé à l'avance. Ses effets durent tout le mois.
/// </summary>
/// <param name="OnStart">Effet immédiat au début de la crise (un krach, par exemple).</param>
/// <param name="MinStage">La crise ne tombe qu'à partir de ce statut.</param>
public record CrisisInfo(
    string Id,
    string Name,
    string Description,
    Action<Modifiers> Apply,
    Action<GameSession>? OnStart = null,
    CareerStage MinStage = CareerStage.Broke);

/// <summary>Un effet qui dure quelques mois (crise, conséquence d'un événement).</summary>
public class TimedEffect
{
    public string Name { get; }
    public string Description { get; }
    public bool IsPositive { get; }
    public Action<Modifiers> Apply { get; }
    public int MonthsLeft { get; internal set; }

    public TimedEffect(string name, string description, bool isPositive, int months, Action<Modifiers> apply)
    {
        Name = name;
        Description = description;
        IsPositive = isPositive;
        MonthsLeft = months;
        Apply = apply;
    }
}

public static class CrisisCatalog
{
    public static IReadOnlyList<CrisisInfo> All { get; } =
    [
        new("tax", "Contrôle fiscal", "Factures ×1,6 ce mois-ci.", m => m.Bills *= 1.6),
        new("blackout", "Panne de courant", "Vitesse de travail −35 %.", m => m.WorkSpeed *= 0.65),
        new("competitor", "Concurrent agressif", "Demandes −40 %.", m => m.RequestRate *= 0.6),
        new("stingy", "Clients radins", "Valeur des contrats −30 %.", m => m.RequestValue *= 0.7),
        new("landlord", "Proprio véreux", "Loyer ×2.", m => m.Rent *= 2),
        new("prodbug", "Bug en prod", "Offres et échéances raccourcies de 35 %.",
            m => { m.OfferDuration *= 0.65; m.DeadlineMargin *= 0.65; }),
        new("strike", "Grève", "L'équipe ne prend que la moitié des tâches.", m => m.StaffCapacity *= 0.5,
            MinStage: CareerStage.Founder),
        new("recession", "Récession", "Revenus des boîtes −40 %, contrats −20 %.",
            m => { m.BusinessIncome *= 0.6; m.RequestValue *= 0.8; }, MinStage: CareerStage.Boss),
        new("crash", "Krach boursier", "Tous les cours chutent de 30 % d'un coup.", _ => { },
            OnStart: s => s.Market.Shock(-0.3), MinStage: CareerStage.Investor),
    ];
}
