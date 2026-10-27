using System.Text.Json.Serialization;
using Core.products;
using Core.space;
using Core.team;
using Core.work;

namespace Core.campaign;

/// <summary>
/// Le temps qui passe, minute par minute : arrivées et départs, déplacements dans le local, pauses et travail.
/// Le travail ne vient que des personnes présentes à un poste complet, pendant leurs heures.
/// </summary>
public partial class Campaign
{
    private bool[,]? _blocked;

    /// <summary>Quelqu'un est-il au local en ce moment ? (L'interface accélère les nuits quand personne n'est là.)</summary>
    [JsonIgnore]
    public bool AnyoneInOffice => People.Any(p => p.Activity != Activity.Away && p.Activity != Activity.Training);

    /// <summary>Avance la campagne d'un nombre de minutes de jeu. Rien ne se passe après une liquidation.</summary>
    public void Advance(long minutes)
    {
        for (long i = 0; i < minutes && Status == CampaignStatus.Running; i++)
            Tick();
    }

    /// <summary>Avance jusqu'à une minute donnée (utilisé par les tests et les bots).</summary>
    public void AdvanceDays(int days) => Advance((long)days * GameTime.MinutesPerDay);

    private void Tick()
    {
        Minute++;
        if (GameTime.MinuteOfDay(Minute) == 0)
            DailyUpdate();
        if (Status != CampaignStatus.Running)
            return;
        UpdatePeople();
    }

    /// <summary>À appeler après tout changement d'aménagement : les chemins sont recalculés.</summary>
    public void InvalidateLayout() => _blocked = null;

    private bool[,] Blocked => _blocked ??= Premises.Occupancy();

    private int ArrivalMinute(Person person) => 8 * 60 + 25 + person.Id * 7 % 30;

    private int EndMinute(Person person) =>
        (person.Schedule == WorkSchedule.Extended ? Tuning.Work.ExtendedEndHour : Tuning.Work.DayEndHour) * 60 + person.Id * 3 % 10;

    private static bool IsWorkMinute(int minuteOfDay, Person person)
    {
        int hour = minuteOfDay / 60;
        int end = person.Schedule == WorkSchedule.Extended ? Tuning.Work.ExtendedEndHour : Tuning.Work.DayEndHour;
        return hour >= Tuning.Work.DayStartHour && hour < end && hour != Tuning.Work.LunchHour;
    }

    private void UpdatePeople()
    {
        int minuteOfDay = GameTime.MinuteOfDay(Minute);
        bool workday = GameTime.IsWorkday(Minute);

        // Qui travaille sur quoi en ce moment (pour la coordination des équipes)
        var crowd = new Dictionary<(AssignmentKind, int), (int Count, bool Lead)>();
        foreach (var p in People.Where(p => p.Activity == Activity.Working))
        {
            var key = (p.Assignment.Kind, p.Assignment.TargetId);
            var current = crowd.GetValueOrDefault(key);
            crowd[key] = (current.Count + 1, current.Lead || p.IsLead);
        }

        foreach (var person in People)
        {
            if (person.TrainingDiscipline != null)
            {
                if (Minute >= person.TrainingUntil)
                    FinishTraining(person);
                else
                {
                    person.Activity = Activity.Training;
                    continue;
                }
            }

            switch (person.Activity)
            {
                case Activity.Away or Activity.Training:
                    person.Activity = Activity.Away;
                    if (workday && Minute >= person.StartsAt && person.LastArrivalDay != Day
                        && minuteOfDay >= ArrivalMinute(person) && minuteOfDay < EndMinute(person) - 30)
                    {
                        person.LastArrivalDay = Day;
                        person.Position = Premises.Outside;
                        GoToPost(person);
                    }
                    break;

                case Activity.Walking:
                    StepWalk(person);
                    break;

                case Activity.Working or Activity.Idle:
                    if (minuteOfDay >= EndMinute(person) || !workday)
                    {
                        WalkTo(person, Premises.Outside, Activity.Away);
                        person.Status = "Rentre chez soi";
                        break;
                    }
                    if (minuteOfDay == Tuning.Work.LunchHour * 60 || (minuteOfDay == person.NextBreakAt % GameTime.MinutesPerDay && person.NextBreakAt / GameTime.MinutesPerDay == Day))
                    {
                        if (TakeBreak(person, minuteOfDay == Tuning.Work.LunchHour * 60 ? 60 : 15))
                            break;
                    }
                    if (person.Activity == Activity.Working && IsWorkMinute(minuteOfDay, person))
                    {
                        var info = crowd.GetValueOrDefault((person.Assignment.Kind, person.Assignment.TargetId));
                        DoWork(person, Math.Max(1, info.Count), info.Lead);
                    }
                    break;

                case Activity.Break:
                    if (Minute >= person.BreakUntil)
                        GoToPost(person);
                    break;
            }
        }
    }

    private bool TakeBreak(Person person, int minutes)
    {
        var spots = Layout.BreakSpots(Premises).ToList();
        if (spots.Count == 0)
            return false;
        var spot = spots[(person.Id + (int)Day) % spots.Count];
        var front = Premises.FrontCell(spot.Def, spot.X, spot.Y);
        var target = NearbyFreeCell(front, person.Id) ?? front;
        double recovery = Layout.BreakRecovery(Premises, People.Count(p => p.Activity != Activity.Away));
        if (!WalkTo(person, target, Activity.Break))
            return false;
        person.BreakUntil = Minute + person.Path.Count + minutes;
        person.Energy = Math.Min(100, person.Energy + recovery * (minutes >= 60 ? 1.5 : 1));
        person.Status = minutes >= 60 ? "Pause de midi" : "Pause café";
        return true;
    }

    /// <summary>Va à son poste s'il est complet, sinon reste debout quelque part en attendant.</summary>
    private void GoToPost(Person person)
    {
        var desk = Premises.DeskOf(person.Id);
        if (desk != null && Layout.Check(Premises, desk) == WorkstationIssue.None)
        {
            if (WalkTo(person, Premises.SeatCell(desk), Activity.Working))
            {
                person.NextBreakAt = Day * GameTime.MinutesPerDay + (GameTime.MinuteOfDay(Minute) < 12 * 60 ? 10 * 60 + 30 : 15 * 60) + person.Id % 20;
                return;
            }
        }
        var spot = NearbyFreeCell(Premises.Entrance.Offset(0, -2), person.Id) ?? Premises.Entrance;
        WalkTo(person, spot, Activity.Idle);
        person.Status = desk == null ? "Sans poste de travail attribué" : Layout.Describe(Layout.Check(Premises, desk));
    }

    private Cell? NearbyFreeCell(Cell around, int salt)
    {
        var reach = Pathfinder.Reachable(Premises, Blocked);
        var candidates = new List<Cell>();
        for (int dy = -3; dy <= 3; dy++)
        for (int dx = -3; dx <= 3; dx++)
        {
            var cell = around.Offset(dx, dy);
            if (Premises.InBounds(cell) && reach[cell.X, cell.Y] && cell != Premises.Entrance)
                candidates.Add(cell);
        }
        if (candidates.Count == 0)
            return null;
        candidates.Sort((a, b) => (Math.Abs(a.X - around.X) + Math.Abs(a.Y - around.Y)).CompareTo(Math.Abs(b.X - around.X) + Math.Abs(b.Y - around.Y)));
        return candidates[salt % Math.Min(candidates.Count, 6)];
    }

    private bool WalkTo(Person person, Cell target, Activity goal)
    {
        var path = Pathfinder.FindPath(Premises, Blocked, person.Position, target);
        if (path == null)
        {
            // Plus de chemin (meuble déplacé entre-temps) : on se replace à l'entrée
            person.Position = Premises.InBounds(person.Position) ? Premises.Entrance : person.Position;
            path = Pathfinder.FindPath(Premises, Blocked, person.Position, target);
            if (path == null)
                return false;
        }
        person.Path = path;
        person.PathIndex = 0;
        person.WalkGoal = goal;
        person.Activity = path.Count <= 1 ? goal : Activity.Walking;
        return true;
    }

    private void StepWalk(Person person)
    {
        person.PathIndex++;
        if (person.PathIndex < person.Path.Count)
            person.Position = person.Path[person.PathIndex];
        if (person.PathIndex >= person.Path.Count - 1)
        {
            person.Activity = person.WalkGoal;
            person.Path.Clear();
            person.PathIndex = 0;
            if (person.Activity == Activity.Working)
                person.Status = "";
        }
    }

    /// <summary>Points de travail par minute de cette personne, à ce poste, dans son état actuel.</summary>
    public double WorkRate(Person person, Discipline discipline, int teamSize = 1, bool lead = false)
    {
        var desk = Premises.DeskOf(person.Id);
        var computer = desk == null ? null : Premises.ComputerOn(desk)?.Def;
        double skill = 0.4 + person.Skill(discipline) * 0.012;
        double energy = person.Energy >= 40 ? 1 : 0.6 + person.Energy / 100;
        double morale = 0.85 + person.Morale / 100 * 0.3;
        double overtime = GameTime.Hour(Minute) >= Tuning.Work.DayEndHour ? 0.9 : 1;
        int free = (lead ? Tuning.Work.CoordinationFreeTeamWithLead : Tuning.Work.CoordinationFreeTeam) + Layout.CoordinationBonus(Premises);
        double coordination = teamSize <= free ? 1 : 1 / (1 + Tuning.Work.CoordinationLossPerExtraPerson * (teamSize - free));
        return Tuning.Work.PointsPerHourAtLevel50 * skill * (computer?.Output ?? 0) * energy * morale * overtime * coordination / 60;
    }

    public double WorkQuality(Person person, Discipline discipline)
    {
        var desk = Premises.DeskOf(person.Id);
        var computer = desk == null ? null : Premises.ComputerOn(desk)?.Def;
        double quality = 0.3 + person.Skill(discipline) * 0.006 + (computer?.Quality ?? 0) + (person.Morale - 50) / 1000;
        if (person.Energy < 40)
            quality -= 0.1;
        if (GameTime.Hour(Minute) >= Tuning.Work.DayEndHour)
            quality -= 0.05;
        return Math.Clamp(quality, 0.1, 1);
    }

    private void DoWork(Person person, int teamSize, bool lead)
    {
        var desk = Premises.DeskOf(person.Id)!;
        var chair = Premises.ChairAt(Premises.SeatCell(desk))?.Def;
        double overtime = GameTime.Hour(Minute) >= Tuning.Work.DayEndHour ? Tuning.Work.EnergyPerOvertimeHour : Tuning.Work.EnergyPerHour;
        person.Energy = Math.Max(0, person.Energy - overtime * (chair?.EnergyDrain ?? 1) / 60);

        var assignment = person.Assignment;
        switch (assignment.Kind)
        {
            case AssignmentKind.Contract or AssignmentKind.Product:
            {
                var owner = assignment.Kind == AssignmentKind.Contract ? WorkOwner.Contract : WorkOwner.Product;
                var item = PickWork(person, owner, assignment.TargetId);
                if (item == null)
                    return;
                double rate = WorkRate(person, item.Discipline, teamSize, lead);
                double points = Math.Min(rate, item.Remaining);
                item.Progress += points;
                item.QualityPoints += points * WorkQuality(person, item.Discipline);
                person.PointsThisWeek += points;
                person.Status = $"{Person.DisciplineName(item.Discipline)} : {item.Title}";
                if (item.Done)
                {
                    person.CurrentWorkItemId = null;
                    OnWorkItemDone(item);
                }
                break;
            }
            case AssignmentKind.Maintenance:
            {
                var product = FindProduct(assignment.TargetId);
                if (product is not { IsLive: true })
                {
                    person.Status = "Rien à maintenir (produit non publié)";
                    return;
                }
                var discipline = person.Skill(Discipline.Code) >= person.Skill(Discipline.Test) ? Discipline.Code : Discipline.Test;
                double points = WorkRate(person, discipline, teamSize, lead);
                product.MaintenanceDone += points * WorkQuality(person, discipline) / 0.6;
                person.PointsThisWeek += points;
                person.Status = $"Maintenance de {product.Name}";
                break;
            }
            case AssignmentKind.Support:
            {
                var product = FindProduct(assignment.TargetId);
                if (product is not { IsLive: true })
                {
                    person.Status = "Aucun utilisateur à aider (produit non publié)";
                    return;
                }
                double points = WorkRate(person, Discipline.Support, teamSize, lead);
                SupportToday[product.Id] = SupportToday.GetValueOrDefault(product.Id) + points;
                person.PointsThisWeek += points;
                person.Status = $"Support de {product.Name}";
                break;
            }
            default:
                person.Status = "Sans affectation : choisis un projet dans Équipe";
                break;
        }
    }

    /// <summary>Choisit la prochaine tâche : prête (dépendances finies), dans une discipline maîtrisée, la meilleure d'abord.</summary>
    private WorkItem? PickWork(Person person, WorkOwner owner, int ownerId)
    {
        if (person.CurrentWorkItemId is int current && FindWork(current) is { Done: false } kept
            && kept.Owner == owner && kept.OwnerId == ownerId && IsReady(kept))
            return kept;

        var open = WorkFor(owner, ownerId).Where(w => !w.Done).ToList();
        if (open.Count == 0)
        {
            person.CurrentWorkItemId = null;
            person.Status = owner == WorkOwner.Product ? "Rien à développer : lance une version dans Produits" : "Projet terminé";
            return null;
        }

        var choice = open
            .Where(w => IsReady(w) && person.Skill(w.Discipline) >= Tuning.Work.MinimumSkill)
            .OrderByDescending(w => person.Skill(w.Discipline))
            .ThenBy(w => w.Id)
            .FirstOrDefault();
        person.CurrentWorkItemId = choice?.Id;
        if (choice == null)
        {
            var waiting = open.FirstOrDefault(w => !IsReady(w));
            person.Status = waiting != null && open.All(w => !IsReady(w) || person.Skill(w.Discipline) < Tuning.Work.MinimumSkill)
                ? $"Bloqué : attend {Bottleneck(owner, ownerId)}"
                : "Aucune tâche dans ses compétences";
        }
        return choice;
    }

    public bool IsReady(WorkItem item) => item.DependsOn.All(id => FindWork(id) is not { Done: false });

    /// <summary>Le goulot d'une cible : la discipline dont les tâches prêtes attendent le plus.</summary>
    public string Bottleneck(WorkOwner owner, int ownerId)
    {
        var ready = WorkFor(owner, ownerId).Where(w => !w.Done && IsReady(w)).ToList();
        if (ready.Count == 0)
            return "rien";
        var top = ready.GroupBy(w => w.Discipline).OrderByDescending(g => g.Sum(w => w.Remaining)).First();
        return $"{Person.DisciplineName(top.Key).ToLowerInvariant()} ({top.Sum(w => w.Remaining):0} pts)";
    }

    private void OnWorkItemDone(WorkItem item)
    {
        if (item.Owner == WorkOwner.Contract)
        {
            var contract = FindContract(item.OwnerId);
            if (contract is { Status: ContractStatus.Active } && WorkFor(WorkOwner.Contract, contract.Id).All(w => w.Done))
                DeliverContract(contract);
            return;
        }

        var product = FindProduct(item.OwnerId);
        if (product == null)
            return;
        switch (item.Kind)
        {
            case "fix":
                if (WorkFor(WorkOwner.Product, product.Id).Where(w => w.Kind == "fix" && w.Title == item.Title).All(w => w.Done))
                {
                    var batch = WorkFor(WorkOwner.Product, product.Id).Where(w => w.Kind == "fix" && w.Title == item.Title).ToList();
                    double quality = batch.Sum(w => w.QualityPoints) / Math.Max(1e-6, batch.Sum(w => w.Progress));
                    double fixedBugs = Math.Min(product.Bugs, batch.Where(w => w.Discipline == Discipline.Code).Sum(w => w.Effort) / 3 * Math.Min(1, quality / 0.6));
                    product.Bugs = Math.Max(0, product.Bugs - fixedBugs);
                    Work.RemoveAll(w => batch.Contains(w));
                    Notify(Severity.Good, "produit", $"{product.Name} : {fixedBugs:0} bugs corrigés", $"Il en reste {product.Bugs:0}.", $"product:{product.Id}");
                }
                break;
            case "optimize":
                product.Optimizations = Math.Min(3, product.Optimizations + 1);
                Work.Remove(item);
                Notify(Severity.Good, "produit", $"{product.Name} : performances optimisées", "La charge serveur baisse de 15 %.", $"product:{product.Id}");
                break;
            default:
                if (product.Build != null && product.Build.WorkItemIds.All(id => FindWork(id) is not { Done: false }))
                    Notify(Severity.Good, "produit", $"{product.Name} v{product.Build.Version} prête", "Tout est développé et testé : tu peux la publier.", $"product:{product.Id}");
                break;
        }
    }

    private void FinishTraining(Person person)
    {
        var discipline = person.TrainingDiscipline!.Value;
        double level = person.Skill(discipline);
        double gain = 7 * (1 + Layout.TrainingBonus(Premises)) * Math.Max(0.15, 1 - level / 110);
        person.Skills[discipline] = Math.Round(Math.Min(95, level + gain), 1);
        person.TrainingDiscipline = null;
        person.Activity = Activity.Away;
        Notify(Severity.Good, "équipe", $"{person.Name} a fini sa formation", $"{Person.DisciplineName(discipline)} : {level:0} → {person.Skills[discipline]:0}.", $"person:{person.Id}");
    }
}
