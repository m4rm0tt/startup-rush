using Core.space;

namespace Core.team;

/// <summary>
/// Les métiers. Les quatre premiers font avancer les projets ; Production (opérateur·rice) fait tourner les lignes de l'atelier
/// et de l'usine, Recherche (chercheur·se) fait avancer la file de recherche.
/// </summary>
public enum Discipline { Design, Code, Test, Support, Operations, Research }

/// <summary>
/// Ce sur quoi une personne travaille : un projet précis, ou « automatique » (le projet prioritaire du portefeuille).
/// Une personne ne travaille que sur un projet à la fois.
/// </summary>
/// <summary>Projet (TargetId = projet), recherche (file de recherche), production (TargetId = site où l'on est opérateur).</summary>
public enum AssignmentKind { None, Project, Research, Production }

public enum WorkSchedule { Normal, Extended }

/// <summary>Ce que la personne fait en ce moment dans le local (affiché et animé dans la scène).</summary>
public enum Activity { Away, Walking, Working, Idle, Break, Training }

public class Assignment
{
    public AssignmentKind Kind { get; set; }

    /// <summary>Projet visé (0 en automatique).</summary>
    public int TargetId { get; set; }

    public static Assignment None => new();

    public bool Is(AssignmentKind kind, int targetId) => Kind == kind && TargetId == targetId;
}

/// <summary>
/// L'apparence d'une personne, en couches du pack : une base (fondateur ou employé), une coiffure, une teinte de cheveux
/// et de peau. Tirée une fois à la création, puis sauvegardée. Elle ne change rien aux compétences ni à la simulation.
/// </summary>
public class Appearance
{
    public static readonly string[] Hairs = ["hair_short_crop", "hair_side_part", "hair_curly_top", "hair_long_straight", "hair_bun", "hair_bob"];
    public static readonly string[] HairRamps = ["hair_black", "hair_brown", "hair_auburn", "hair_blond", "hair_gray"];
    public static readonly string[] Skins = ["skin_light", "skin_medium", "skin_deep"];

    public string Base { get; set; } = "employee_m_01";
    public string Hair { get; set; } = "hair_short_crop";
    public string HairRamp { get; set; } = "hair_brown";
    public string Skin { get; set; } = "skin_light";

    /// <summary>Une apparence au hasard pour une recrue (cheveux gris rares).</summary>
    public static Appearance Random(campaign.SimRandom random) => new()
    {
        Base = $"employee_{(random.Chance(0.5) ? 'f' : 'm')}_0{random.Next(1, 6)}",
        Hair = random.Pick(Hairs),
        HairRamp = random.Chance(0.1) ? "hair_gray" : random.Pick(HairRamps[..4]),
        Skin = random.Pick(Skins),
    };
}

public class Person
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public bool IsFounder { get; set; }

    public Appearance Appearance { get; set; } = new();

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

    /// <summary>En congé jusqu'à cette date (exclue).</summary>
    public long VacationUntil { get; set; }

    /// <summary>Dates des augmentations (pour en limiter l'effet et le nombre).</summary>
    public List<long> RaiseDates { get; set; } = new();

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
        Discipline.Operations => "opérateur·rice",
        Discipline.Research => "chercheur·se",
        _ => "support / ops"
    };

    public static string DisciplineName(Discipline discipline) => discipline switch
    {
        Discipline.Design => "Design",
        Discipline.Code => "Développement",
        Discipline.Test => "Tests",
        Discipline.Operations => "Production",
        Discipline.Research => "Recherche",
        _ => "Support"
    };

    /// <summary>
    /// Les compétences de production et de recherche de quelqu'un qui n'en a pas fait son métier : une part de ses autres
    /// compétences (la rigueur des tests et du code sert à la ligne ; le code et le design, à la recherche). Sans tirage.
    /// </summary>
    public static void FillSecondary(Dictionary<Discipline, double> skills)
    {
        double S(Discipline d) => skills.GetValueOrDefault(d);
        if (!skills.ContainsKey(Discipline.Operations))
            skills[Discipline.Operations] = Math.Round(0.5 * S(Discipline.Test) + 0.3 * S(Discipline.Code));
        if (!skills.ContainsKey(Discipline.Research))
            skills[Discipline.Research] = Math.Round(0.5 * Math.Max(S(Discipline.Code), S(Discipline.Design)) + 0.2 * S(Discipline.Test));
    }

    /// <summary>Rythme d'un opérateur à sa station selon sa compétence de production (1 vers 30, 1,36 pour un senior) ⚙️.</summary>
    public static double OperatorPace(double skill) => Math.Clamp(0.85 + 0.006 * skill, 0.85, 1.4);
}
