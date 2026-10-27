using System.Text.Json.Serialization;
using Core.products;
using Core.space;
using Core.team;
using Core.work;

namespace Core.campaign;

/// <summary>
/// Le temps qui passe, minute par minute. Chacun s'installe à son poste en arrivant, travaille tout seul sur son projet
/// (ou sur le projet prioritaire en automatique) et enchaîne design, développement et tests sans réaffectation.
/// Sur un projet en ligne, une part du temps va automatiquement au support et à la maintenance.
/// Le fondateur, lui, ne développe que par le terminal (Campaign.Terminal.cs) ; le reste du temps il répond au support.
/// </summary>
public partial class Campaign
{
    private readonly Dictionary<int, bool[,]> _blocked = new();

    /// <summary>Quelqu'un est-il au local en ce moment ? (L'interface accélère les nuits quand personne n'est là.)</summary>
    [JsonIgnore]
    public bool AnyoneInOffice => People.Any(p => p.Activity is Activity.Working or Activity.Idle);

    public void Advance(long minutes)
    {
        for (long i = 0; i < minutes && Status == CampaignStatus.Running; i++)
            Tick();
    }

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

    /// <summary>À appeler après tout changement d'aménagement.</summary>
    public void InvalidateLayout()
    {
        _blocked.Clear();
        _comfort.Clear();
        _infra.Clear();
    }

    private bool[,] Blocked(Premises site) => _blocked.TryGetValue(site.Id, out var grid) && grid.GetLength(0) == site.Width && grid.GetLength(1) == site.Height
        ? grid : _blocked[site.Id] = site.Occupancy();

    private readonly Dictionary<int, (Premises Premises, int Items, int People, ComfortSummary Summary)> _comfort = new();

    /// <summary>Le confort du site affiché, pour les personnes qui y ont leur poste.</summary>
    public ComfortSummary SiteComfort() => SiteComfort(Premises);

    /// <summary>
    /// Le confort d'un site pour les personnes qui y travaillent (Layout.Comfort), gardé en mémoire tant que le site, ses
    /// objets et son effectif ne changent pas : la simulation le lit à chaque minute de travail.
    /// </summary>
    public ComfortSummary SiteComfort(Premises site)
    {
        int people = Math.Max(1, People.Count(p => SiteOf(p) == site));
        if (_comfort.TryGetValue(site.Id, out var c) && c.Premises == site && c.Items == site.Items.Count && c.People == people)
            return c.Summary;
        var summary = Layout.Comfort(site, people);
        _comfort[site.Id] = (site, site.Items.Count, people, summary);
        return summary;
    }

    private int ArrivalMinute(Person person) => 8 * 60 + 30 + person.Id * 7 % 30;

    private int EndMinute(Person person) =>
        (person.Schedule == WorkSchedule.Extended ? Tuning.Work.ExtendedEndHour : Tuning.Work.DayEndHour) * 60;

    private static bool IsWorkMinute(int minuteOfDay, Person person)
    {
        int hour = minuteOfDay / 60;
        int end = person.Schedule == WorkSchedule.Extended ? Tuning.Work.ExtendedEndHour : Tuning.Work.DayEndHour;
        return hour >= Tuning.Work.DayStartHour && hour < end && hour != Tuning.Work.LunchHour;
    }

    public bool OnVacation(Person person) => Minute < person.VacationUntil;

    private void UpdatePeople()
    {
        int minuteOfDay = GameTime.MinuteOfDay(Minute);
        bool workday = GameTime.IsWorkday(Minute);

        var crowd = new Dictionary<int, (int Count, bool Lead)>();
        foreach (var p in People.Where(p => p.Activity == Activity.Working && !p.IsFounder))
        {
            int key = CurrentProject(p)?.Id ?? 0;
            var current = crowd.GetValueOrDefault(key);
            crowd[key] = (current.Count + 1, current.Lead || p.IsLead && IsUnlocked("leads"));
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
            if (OnVacation(person))
            {
                person.Activity = Activity.Away;
                person.Status = $"En congé jusqu'au {GameTime.FormatDate(person.VacationUntil)}";
                continue;
            }

            bool present = workday && Minute >= person.StartsAt && minuteOfDay >= ArrivalMinute(person) && minuteOfDay < EndMinute(person);
            if (!present)
            {
                person.Activity = Activity.Away;
                continue;
            }
            if (person.Activity is Activity.Away or Activity.Training or Activity.Walking or Activity.Break)
                TakePlace(person);
            if (person.Activity == Activity.Working && IsWorkMinute(minuteOfDay, person))
            {
                var info = crowd.GetValueOrDefault(CurrentProject(person)?.Id ?? 0);
                DoWork(person, Math.Max(1, info.Count), info.Lead);
            }
        }
    }

    /// <summary>S'installe à son poste s'il est complet, sinon attend près de l'entrée (personnages assis, pas de déplacements).</summary>
    private void TakePlace(Person person)
    {
        var (site, post) = PostOf(person);
        if (post != null && Layout.Check(site, post) == WorkstationIssue.None)
        {
            person.Position = site.SeatCell(post);
            person.Activity = Activity.Working;
            return;
        }
        person.Position = NearbyFreeCell(site, site.Entrance, person.Id) ?? site.Entrance;
        person.Activity = Activity.Idle;
        person.Status = post == null ? "Pas de poste de travail" : Layout.Describe(Layout.Check(site, post));
    }

    private Cell? NearbyFreeCell(Premises site, Cell around, int salt)
    {
        var reach = Pathfinder.Reachable(site, Blocked(site));
        var candidates = new List<Cell>();
        for (int dy = -3; dy <= 3; dy++)
        for (int dx = -3; dx <= 3; dx++)
        {
            var cell = around.Offset(dx, dy);
            if (site.InBounds(cell) && reach[cell.X, cell.Y] && cell != site.Entrance)
                candidates.Add(cell);
        }
        if (candidates.Count == 0)
            return null;
        candidates.Sort((a, b) => (Math.Abs(a.X - around.X) + Math.Abs(a.Y - around.Y)).CompareTo(Math.Abs(b.X - around.X) + Math.Abs(b.Y - around.Y)));
        return candidates[salt % Math.Min(candidates.Count, 6)];
    }

    /// <summary>Le projet de la personne : celui où elle est affectée, ou le projet prioritaire en automatique.</summary>
    public Product? CurrentProject(Person person) =>
        person.Assignment.Kind == AssignmentKind.Project ? FindProduct(person.Assignment.TargetId) : AutoTarget();

    /// <summary>Production du poste de la personne (0 sans ordinateur), écrans en plus et concentration du local compris.</summary>
    public double StationOutput(Person person)
    {
        var (site, post) = PostOf(person);
        if (post == null || site.ComputerOf(post)?.Def is not { } computer)
            return 0;
        return computer.Output * (1 + Layout.ScreenBonus(site, post)) * (1 + SiteComfort(site).Focus);
    }

    public double WorkRate(Person person, Discipline discipline, int teamSize = 1, bool lead = false)
    {
        double skill = 0.4 + person.Skill(discipline) * 0.012;
        double energy = person.Energy >= 40 ? 1 : 0.6 + person.Energy / 100;
        double morale = 0.85 + person.Morale / 100 * 0.3;
        double overtime = GameTime.Hour(Minute) >= Tuning.Work.DayEndHour ? 0.9 : 1;
        int free = (lead ? Tuning.Work.CoordinationFreeTeamWithLead : Tuning.Work.CoordinationFreeTeam) + (int)Math.Floor(SiteComfort(SiteOf(person)).Coordination)
                   + (IsUnlocked("bigteam") ? 2 : 0);
        double coordination = teamSize <= free ? 1 : 1 / (1 + Tuning.Work.CoordinationLossPerExtraPerson * (teamSize - free));
        double tools = IsUnlocked("ai_tools") ? 1.1 : 1;
        return Tuning.Work.PointsPerHourAtLevel50 * skill * StationOutput(person) * energy * morale * overtime * coordination * tools / 60;
    }

    public double WorkQuality(Person person, Discipline discipline)
    {
        var (site, post) = PostOf(person);
        var computer = post == null ? null : site.ComputerOf(post)?.Def;
        double quality = 0.3 + person.Skill(discipline) * 0.006 + (computer?.Quality ?? 0) + (person.Morale - 50) / 1000
                         + (IsUnlocked("quality_lab") ? 0.05 : 0);
        if (person.Energy < 40)
            quality -= 0.1;
        if (GameTime.Hour(Minute) >= Tuning.Work.DayEndHour)
            quality -= 0.05;
        return Math.Clamp(quality, 0.1, 1);
    }

    private void DoWork(Person person, int teamSize, bool lead)
    {
        var (site, post) = PostOf(person);
        var chair = post == null ? null : site.ChairOf(post)?.Def;
        double drain = GameTime.Hour(Minute) >= Tuning.Work.DayEndHour ? Tuning.Work.EnergyPerOvertimeHour : Tuning.Work.EnergyPerHour;
        person.Energy = Math.Max(0, person.Energy - drain * (chair?.EnergyDrain ?? 1) / 60);

        if (person.IsFounder)
        {
            FounderTime(person);
            return;
        }

        var product = CurrentProject(person);
        if (product == null)
        {
            person.CurrentWorkItemId = null;
            person.Status = Products.Count == 0 ? "Attend un premier projet" : "Rien à faire : mets une amélioration en file";
            return;
        }

        // Sur un projet en ligne, une part fixe du temps va à l'entretien (support et maintenance), sans action du joueur
        bool upkeepTurn = product.IsLive && (Minute + person.Id) % 20 < UpkeepShare(product) * 20;
        var item = upkeepTurn ? null : PickWork(person, product);
        if (item == null)
        {
            if (product.IsLive)
                DoUpkeep(person, product, teamSize, lead);
            return;
        }

        double rate = WorkRate(person, item.Discipline, teamSize, lead);
        double points = Math.Min(rate, item.Remaining);
        item.Progress += points;
        item.QualityPoints += points * WorkQuality(person, item.Discipline);
        person.PointsThisWeek += points;
        person.Status = $"{product.Name} : {item.Title}";
        if (item.Done)
        {
            person.CurrentWorkItemId = null;
            OnWorkItemDone(product, item);
        }
    }

    /// <summary>
    /// Le fondateur à son poste hors terminal : il répond aux utilisateurs du projet en ligne qui en a le plus besoin.
    /// Il ne développe pas, ne corrige pas de bug et ne termine aucun composant tout seul.
    /// </summary>
    private void FounderTime(Person founder)
    {
        founder.CurrentWorkItemId = null;
        if (Terminal != null)
        {
            founder.Status = "Au terminal";
            return;
        }
        var product = Products.Where(p => p.IsLive && p.SupportBacklog > 1).OrderByDescending(p => p.SupportBacklog).FirstOrDefault();
        if (product == null)
        {
            founder.Status = Products.Any(p => p.Queue.Count > 0) ? "Disponible : ouvre le terminal pour travailler" : "Disponible";
            return;
        }
        double points = WorkRate(founder, Discipline.Support);
        product.SupportDone += points;
        founder.PointsThisWeek += points;
        founder.Status = $"{product.Name} : répond aux utilisateurs";
    }

    /// <summary>Entretien : répondre aux utilisateurs et corriger les bugs, en alternance quand les deux sont nécessaires.</summary>
    private void DoUpkeep(Person person, Product product, int teamSize, bool lead)
    {
        // Moitié support, moitié maintenance quand les deux sont nécessaires ; tout à l'un si l'autre n'a besoin de rien
        bool needsMaintenance = product.Bugs >= 1 || product.MaintenanceDebt > 1;
        bool needsSupport = product.SupportBacklog > 1;
        bool support = needsSupport && (!needsMaintenance || (Minute / 30 + person.Id) % 2 == 0);
        var discipline = support ? Discipline.Support : person.Skill(Discipline.Code) >= person.Skill(Discipline.Test) ? Discipline.Code : Discipline.Test;
        double points = WorkRate(person, discipline, teamSize, lead);
        if (support)
            product.SupportDone += points;
        else
            product.MaintenanceDone += points * WorkQuality(person, discipline) / 0.6;
        person.PointsThisWeek += points;
        person.CurrentWorkItemId = null;
        person.Status = support ? $"{product.Name} : répond aux utilisateurs" : $"{product.Name} : maintenance";
    }

    /// <summary>La prochaine tâche prête du projet, dans l'ordre de la file, de préférence dans la spécialité de la personne.</summary>
    internal WorkItem? PickWork(Person person, Product product)
    {
        if (person.CurrentWorkItemId is int current && FindWork(current) is { Done: false } kept && kept.OwnerId == product.Id && IsReady(kept))
            return kept;

        var order = product.Queue.Select((q, i) => (q.Id, i)).ToDictionary(x => x.Id, x => x.i);
        var ready = WorkFor(WorkOwner.Product, product.Id).Where(w => !w.Done && IsReady(w)).ToList();
        if (ready.Count == 0)
        {
            person.CurrentWorkItemId = null;
            return null;
        }
        double best = ready.Max(w => person.Skill(w.Discipline));
        var choice = ready
            .OrderBy(w => person.Skill(w.Discipline) >= best * 0.7 ? 0 : 1)
            .ThenBy(w => order.GetValueOrDefault(w.ModuleId ?? 0, int.MaxValue))
            .ThenBy(w => w.Id)
            .First();
        person.CurrentWorkItemId = choice.Id;
        return choice;
    }

    public bool IsReady(WorkItem item) => item.DependsOn.All(id => FindWork(id) is not { Done: false });

    internal void OnWorkItemDone(Product product, WorkItem item)
    {
        var queued = product.Queue.FirstOrDefault(q => q.Id == item.ModuleId);
        if (queued != null && queued.WorkItemIds.All(id => FindWork(id) is not { Done: false }))
            CompleteQueued(product, queued);
    }

    private void FinishTraining(Person person)
    {
        var discipline = person.TrainingDiscipline!.Value;
        double level = person.Skill(discipline);
        double gain = 7 * (1 + SiteComfort(SiteOf(person)).Training) * (IsUnlocked("academy") ? 1.3 : 1) * Math.Max(0.15, 1 - level / 110);
        person.Skills[discipline] = Math.Round(Math.Min(95, level + gain), 1);
        person.TrainingDiscipline = null;
        person.Activity = Activity.Away;
        Notify(Severity.Good, "équipe", $"{person.Name} a fini sa formation", $"{Person.DisciplineName(discipline)} : {level:0} → {person.Skills[discipline]:0}.", $"person:{person.Id}");
    }
}
