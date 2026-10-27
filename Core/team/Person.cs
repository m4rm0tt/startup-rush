using Core.space;

namespace Core.team;

public enum Discipline { Design, Code, Test, Support }

/// <summary>Ce sur quoi une personne travaille. Une personne n'a qu'une seule affectation à la fois.</summary>
public enum AssignmentKind { None, Contract, Product, Maintenance, Support }

public enum WorkSchedule { Normal, Extended }

/// <summary>Ce que la personne fait en ce moment dans le local (affiché et animé dans la scène).</summary>
public enum Activity { Away, Walking, Working, Idle, Break, Training }

public class Assignment
{
    public AssignmentKind Kind { get; set; }

    /// <summary>Contrat ou produit visé (0 si aucun).</summary>
    public int TargetId { get; set; }

    public static Assignment None => new();

    public bool Is(AssignmentKind kind, int targetId) => Kind == kind && TargetId == targetId;
}

public class Person
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public bool IsFounder { get; set; }

    /// <summary>Apparence (un des personnages de la planche Pixel Office).</summary>
    public int Look { get; set; }

    public Dictionary<Discipline, double> Skills { get; set; } = new();
    public decimal Salary { get; set; }
    public long HiredAt { get; set; }

    /// <summary>Énergie de 0 à 100 : baisse en travaillant, remonte la nuit, le week-end et en pause.</summary>
    public double Energy { get; set; } = 100;

    /// <summary>Moral de 0 à 100 : tend vers une cible qui dépend des conditions de travail.</summary>
    public double Morale { get; set; } = 70;

    public Assignment Assignment { get; set; } = Assignment.None;

    /// <summary>Responsable d'équipe : sa cible supporte plus de monde sans perte de coordination.</summary>
    public bool IsLead { get; set; }

    public WorkSchedule Schedule { get; set; } = WorkSchedule.Normal;

    public Discipline? TrainingDiscipline { get; set; }
    public long TrainingUntil { get; set; }

    public int UnpaidDays { get; set; }
    public int LowMoraleDays { get; set; }

    /// <summary>Date de départ annoncée (démission), ou null.</summary>
    public long? LeavesAt { get; set; }

    // Présence dans le local
    public Activity Activity { get; set; } = Activity.Away;
    public Cell Position { get; set; }
    public List<Cell> Path { get; set; } = new();
    public int PathIndex { get; set; }
    public long BreakUntil { get; set; }
    public long NextBreakAt { get; set; }
    public int? CurrentWorkItemId { get; set; }

    /// <summary>Ce que la personne fera en arrivant au bout de son chemin.</summary>
    public Activity WalkGoal { get; set; }

    public long LastArrivalDay { get; set; } = -1;

    /// <summary>Premier jour de travail (préavis d'une nouvelle recrue).</summary>
    public long StartsAt { get; set; }

    /// <summary>Ce qui occupe ou bloque la personne, en clair (« Attend la fin du design »).</summary>
    public string Status { get; set; } = "";

    /// <summary>Points produits depuis lundi (pour repérer les goulots).</summary>
    public double PointsThisWeek { get; set; }

    public double Skill(Discipline discipline) => Skills.TryGetValue(discipline, out double value) ? value : 0;

    public Discipline BestDiscipline => Skills.OrderByDescending(s => s.Value).First().Key;

    public string Title => IsFounder ? "Fondateur·rice" : $"{Level} {SpecialtyName(BestDiscipline)}";

    public string Level => Skill(BestDiscipline) switch
    {
        >= 70 => "Senior",
        >= 45 => "Confirmé·e",
        _ => "Junior"
    };

    public static string SpecialtyName(Discipline discipline) => discipline switch
    {
        Discipline.Design => "designer",
        Discipline.Code => "développeur·se",
        Discipline.Test => "testeur·se",
        _ => "support / ops"
    };

    public static string DisciplineName(Discipline discipline) => discipline switch
    {
        Discipline.Design => "Design",
        Discipline.Code => "Développement",
        Discipline.Test => "Tests",
        _ => "Support"
    };
}
