using Core.finance;
using Core.space;
using Core.team;

namespace Core.campaign;

/// <summary>
/// Une recherche : ce qu'elle débloque, son effort (points de travail des chercheurs), son budget par jour de travail,
/// les équipements qu'il faut sur le site des chercheurs (un de chaque groupe), et ce qu'il faut avant.
/// </summary>
public record ResearchDef(string Id, string Name, string Field, string Unlocks, double Effort, decimal DailyCost, string[][] Equipment,
    string? Requires = null, string? Node = null);

/// <summary>Une recherche en cours : sa progression est gardée, même suspendue.</summary>
public class ResearchProject
{
    public string DefId { get; set; } = "";
    public double Progress { get; set; }
    public bool Paused { get; set; }
    public long StartedAt { get; set; }

    /// <summary>Points apportés aujourd'hui (le budget du jour n'est payé que si quelqu'un y a travaillé).</summary>
    public double Today { get; set; }

    public ResearchDef Def => Campaign.ResearchCatalog.First(r => r.Id == DefId);
}

public partial class Campaign
{
    public static readonly IReadOnlyList<ResearchDef> ResearchCatalog =
    [
        new("res_boards", "Conception de cartes", "Électronique", "Famille « Cartes et kits ».", 60, 60m,
            [["workbench_electronics"], ["oscilloscope", "soldering_station"]], Node: "hardware"),
        new("res_pc", "Validation de PC", "Matériel", "Famille « PC assemblés ».", 90, 80m,
            [["test_bench_pc"]], Node: "hardware"),
        new("res_printer", "Imprimantes 3D", "Prototypage", "Famille « Imprimantes 3D ».", 100, 80m,
            [["printer_3d"], ["calibration_bench", "cnc_mill_desktop"]], Node: "hardware"),
        new("res_network", "Équipements réseau", "Électronique", "Famille « Équipements réseau ».", 80, 70m,
            [["workbench_electronics"], ["oscilloscope"]], Requires: "res_boards"),
        new("res_laptop", "Portables", "Matériel", "Famille « Ordinateurs portables ».", 140, 110m,
            [["test_bench_embedded"], ["workbench_electronics"]], Requires: "res_pc"),
        new("res_server", "Serveurs et NAS", "Matériel", "Famille « Serveurs et NAS ».", 140, 110m,
            [["test_bench_pc"], ["instrument_rack"]], Requires: "res_pc"),
        new("res_quality", "Contrôle qualité", "Procédés", "−30 % de défauts sur tout ce qui est fabriqué.", 80, 70m,
            [["instrument_rack", "oscilloscope"], ["microscope"]], Node: "hardware"),
        new("res_lean", "Gabarits d'assemblage", "Prototypage", "Postes manuels (assemblage, flash, emballage) 25 % plus rapides.", 80, 60m,
            [["laser_cutter", "cnc_mill_desktop", "printer_3d"]], Node: "hardware"),
        new("res_smt", "Assemblage de cartes en série", "Procédés", "Recette « Cartes électroniques » (placement, refusion, inspection).", 120, 120m,
            [["microscope"], ["instrument_rack"]], Requires: "res_boards", Node: "industry"),
        new("res_cobot", "Cobotique", "Procédés", "Cellules d'assemblage robotisées 30 % plus rapides.", 100, 120m,
            [["cobot_cell"]], Node: "industry"),
        new("res_gpu", "Cartes graphiques", "Électronique", "Famille « Cartes graphiques » et recette du sous-ensemble GPU.", 200, 160m,
            [["test_bench_pc"], ["oscilloscope"], ["instrument_rack"]], Requires: "res_smt"),
        new("res_chips", "Microcomposants", "Procédés", "Recette « Processeurs » : 20 puces par plaquette, en salle propre.", 260, 220m,
            [["microscope"], ["research_station"]], Requires: "res_smt", Node: "cleanroom"),
    ];

    public static ResearchDef ResearchDefOf(string id) => ResearchCatalog.First(r => r.Id == id);

    /// <summary>Ce qui débloque quelque chose, lisible : « Développement : Matériel » ou « Recherche : Validation de PC ».</summary>
    public static string UnlockName(string id) =>
        ResearchCatalog.FirstOrDefault(r => r.Id == id) is { } research ? $"Recherche : {research.Name}" : $"Développement : {progress.DevTree.Node(id).Name}";

    public List<ResearchProject> Research { get; set; } = new();

    /// <summary>Recherches terminées : elles débloquent comme un nœud de l'arbre (IsUnlocked).</summary>
    public HashSet<string> ResearchDone { get; set; } = new();

    /// <summary>Peut-on lancer cette recherche ? Sinon, ce qui manque (déblocage, recherche d'avant).</summary>
    public (bool Ok, string Reason) CanResearch(ResearchDef def)
    {
        if (ResearchDone.Contains(def.Id))
            return (false, "Déjà terminée.");
        if (def.Node is { } node && !IsUnlocked(node))
            return (false, $"À débloquer dans Développement : {progress.DevTree.Node(node).Name}.");
        if (def.Requires is { } before && !ResearchDone.Contains(before))
            return (false, $"Il faut d'abord : {ResearchDefOf(before).Name}.");
        return (true, "");
    }

    /// <summary>Les équipements qui manquent sur un site pour cette recherche (un de chaque groupe), vide si tout y est.</summary>
    public static List<string> MissingEquipment(Premises site, ResearchDef def) =>
        def.Equipment.Where(group => !group.Any(id => site.Items.Any(i => i.DefId == id)))
            .Select(group => string.Join(" ou ", group.Select(id => FurnitureCatalog.Get(id).Name))).ToList();

    public CommandResult StartResearch(string defId)
    {
        var def = ResearchDefOf(defId);
        var (ok, reason) = CanResearch(def);
        if (!ok)
            return CommandResult.Fail(reason);
        if (Research.FirstOrDefault(r => r.DefId == defId) is { } existing)
        {
            existing.Paused = false;
            return CommandResult.Success("La recherche reprend où elle en était.");
        }
        Research.Add(new ResearchProject { DefId = defId, StartedAt = Minute });
        string missing = Sites.Any(s => MissingEquipment(s, def).Count == 0) ? "" : $" Il manque sur tes sites : {string.Join(", ", MissingEquipment(Premises, def))}.";
        return CommandResult.Success($"Recherche lancée : {def.Name}.{missing}");
    }

    public CommandResult PauseResearch(string defId, bool paused)
    {
        var project = Research.FirstOrDefault(r => r.DefId == defId);
        if (project == null)
            return CommandResult.Fail("Recherche inconnue.");
        project.Paused = paused;
        return CommandResult.Success(paused ? "Recherche suspendue : rien de l'acquis n'est perdu." : "");
    }

    /// <summary>Affecte une personne à la recherche, ou comme opérateur de production sur un site.</summary>
    public CommandResult AssignTo(int personId, AssignmentKind kind, int targetId = 0)
    {
        var person = FindPerson(personId);
        if (person == null || person.IsFounder)
            return CommandResult.Fail("Le fondateur travaille au terminal.");
        if (kind == AssignmentKind.Production && FindSite(targetId) == null)
            return CommandResult.Fail("Site inconnu.");
        person.Assignment = new Assignment { Kind = kind, TargetId = targetId };
        person.CurrentWorkItemId = null;
        return CommandResult.Success(kind == AssignmentKind.Production ? $"{person.Name} devient opérateur : {FindSite(targetId)!.Name}." : $"{person.Name} rejoint la recherche.");
    }

    /// <summary>La recherche sur laquelle travaille un chercheur de ce site : la première de la file, non suspendue, équipée ici.</summary>
    public ResearchProject? ResearchFor(Premises site) =>
        Research.FirstOrDefault(r => !r.Paused && !ResearchDone.Contains(r.DefId) && MissingEquipment(site, r.Def).Count == 0);

    /// <summary>Une minute de recherche : le rythme de la personne, plus 10 % par paillasse du site (2 au plus).</summary>
    private void DoResearch(Person person, Premises site)
    {
        var project = ResearchFor(site);
        if (project == null)
        {
            person.Status = Research.Any(r => !r.Paused && !ResearchDone.Contains(r.DefId))
                ? "Recherche : il manque un équipement sur ce site" : "Recherche : aucune recherche en cours";
            return;
        }
        var discipline = new[] { Discipline.Code, Discipline.Test, Discipline.Design }.MaxBy(person.Skill);
        double stations = Math.Min(2, site.Items.Count(i => i.DefId == "research_station"));
        double points = WorkRate(person, discipline) * (1 + 0.1 * stations);
        project.Progress += points;
        project.Today += points;
        person.PointsThisWeek += points;
        person.Status = $"Recherche : {project.Def.Name}";
        if (project.Progress >= project.Def.Effort)
            CompleteResearch(project);
    }

    private void CompleteResearch(ResearchProject project)
    {
        ResearchDone.Add(project.DefId);
        if (project.Today > 0)
            Ledger.Accrue(Category.Research, project.Def.DailyCost);
        Research.Remove(project);
        Notify(Severity.Good, "recherche", $"Recherche terminée : {project.Def.Name}", project.Def.Unlocks, "research");
        Award($"research:{project.DefId}", 1, $"Recherche terminée : {project.Def.Name}.");
    }

    /// <summary>Chaque soir : le budget du jour de chaque recherche qui a avancé (rien si personne n'y a travaillé).</summary>
    private void PayResearch()
    {
        foreach (var project in Research.Where(r => r.Today > 0))
        {
            Ledger.Accrue(Category.Research, project.Def.DailyCost);
            project.Today = 0;
        }
    }
}
