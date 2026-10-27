using Core.team;

namespace Core.work;

public enum WorkOwner { Contract, Product }

/// <summary>
/// Une tâche de production : une taille en points (1 point = 1 heure d'une personne de niveau 50),
/// une discipline, des dépendances, et une qualité qui dépend de qui l'a faite et dans quel état.
/// </summary>
public class WorkItem
{
    public int Id { get; set; }
    public WorkOwner Owner { get; set; }
    public int OwnerId { get; set; }
    public string Title { get; set; } = "";
    public Discipline Discipline { get; set; }
    public double Effort { get; set; }
    public double Progress { get; set; }

    /// <summary>Somme qualité × points produits : la qualité moyenne est QualityPoints / Progress.</summary>
    public double QualityPoints { get; set; }

    public List<int> DependsOn { get; set; } = new();

    /// <summary>Module de produit concerné, s'il y en a un (identifiant d'instance).</summary>
    public int? ModuleId { get; set; }

    /// <summary>module, fix, optimize, rework, scope</summary>
    public string Kind { get; set; } = "scope";

    public bool Done => Progress >= Effort - 1e-9;

    public double Remaining => Math.Max(0, Effort - Progress);

    public double Quality => Progress > 0 ? QualityPoints / Progress : 0;

    public double Fraction => Effort <= 0 ? 1 : Math.Min(1, Progress / Effort);
}
