using Core.finance;
using Core.team;

namespace Core.campaign;

/// <summary>Équipe : recruter, affecter, former, augmenter, licencier.</summary>
public partial class Campaign
{
    public const decimal TrainingCost = 900m;
    public const int TrainingWorkdays = 2;

    public CommandResult Hire(int candidateId)
    {
        var candidate = Candidates.FirstOrDefault(c => c.Person.Id == candidateId);
        if (candidate == null)
            return CommandResult.Fail("Ce candidat n'est plus disponible.");
        var person = candidate.Person;
        person.HiredAt = Minute;
        person.StartsAt = candidate.NoticeWorkdays == 0 ? Minute : GameTime.StartOfDay(GameTime.Day(GameTime.AddWorkdays(Minute, candidate.NoticeWorkdays)));
        person.Position = Premises.Outside;
        Candidates.Remove(candidate);
        People.Add(person);

        var freeDesk = Premises.Desks.FirstOrDefault(d => d.AssignedPersonId == null && space.Layout.Check(Premises, d) == space.WorkstationIssue.None);
        if (freeDesk != null)
            freeDesk.AssignedPersonId = person.Id;
        string start = candidate.NoticeWorkdays == 0 ? "dès aujourd'hui" : $"le {GameTime.FormatDate(person.StartsAt)}";
        string post = freeDesk != null ? "Un poste libre lui est attribué." : "Aucun poste complet libre : installe un bureau, une chaise et un ordinateur.";
        Notify(Severity.Good, "équipe", $"{person.Name} rejoint l'équipe", $"Commence {start}. Salaire {person.Salary:N0} CHF par mois. {post}", $"person:{person.Id}");
        return CommandResult.Success(post);
    }

    public CommandResult Fire(int personId)
    {
        var person = FindPerson(personId);
        if (person == null || person.IsFounder)
            return CommandResult.Fail("Impossible.");
        decimal severance = person.Salary * Tuning.Finance.SeverancePayMonths;
        AddBill($"Indemnité de départ de {person.Name}", Category.Severance, severance, Minute + 5L * GameTime.MinutesPerDay, person.Id);
        // Les jours travaillés depuis la dernière paie restent dus
        var date = GameTime.ToDate(Minute);
        var lastPayday = date.Day >= 25 ? new DateTime(date.Year, date.Month, 25) : new DateTime(date.Year, date.Month, 1).AddMonths(-1).AddDays(24);
        double share = Math.Clamp((Minute - Math.Max(GameTime.ToMinute(lastPayday), person.StartsAt)) / (30.0 * GameTime.MinutesPerDay), 0, 1);
        if (share > 0.02)
            AddBill($"Dernier salaire de {person.Name}", Category.Salaries, person.Salary * (decimal)share, Minute + 5L * GameTime.MinutesPerDay, person.Id);
        RemovePerson(person, $"{person.Name} a été licencié·e (indemnité {severance:N0} CHF).");
        foreach (var other in Staff)
            other.Morale = Math.Max(0, other.Morale - 6);
        return CommandResult.Success();
    }

    /// <summary>Affecte une personne à un projet, ou en automatique (projectId 0) : elle suit alors la priorité du portefeuille.</summary>
    public CommandResult Assign(int personId, int projectId)
    {
        var person = FindPerson(personId);
        if (person == null)
            return CommandResult.Fail("Personne inconnue.");
        if (projectId != 0 && FindProduct(projectId) == null)
            return CommandResult.Fail("Projet inconnu.");
        person.Assignment = projectId == 0 ? Assignment.None : new Assignment { Kind = AssignmentKind.Project, TargetId = projectId };
        person.CurrentWorkItemId = null;
        return CommandResult.Success();
    }

    /// <summary>Tout le monde en automatique.</summary>
    public void AssignAllAuto()
    {
        foreach (var person in People)
            Assign(person.Id, 0);
    }

    public CommandResult SetSchedule(int personId, WorkSchedule schedule)
    {
        var person = FindPerson(personId);
        if (person == null)
            return CommandResult.Fail("Personne inconnue.");
        person.Schedule = schedule;
        return CommandResult.Success();
    }

    public CommandResult SetLead(int personId, bool lead)
    {
        var person = FindPerson(personId);
        if (person == null)
            return CommandResult.Fail("Personne inconnue.");
        if (lead && !IsUnlocked("leads"))
            return CommandResult.Fail("À débloquer dans Développement : Responsables.");
        if (lead && person.Skill(person.BestDiscipline) < 55)
            return CommandResult.Fail("Il faut au moins un niveau confirmé (55) pour encadrer une équipe.");
        person.IsLead = lead;
        return CommandResult.Success();
    }

    public decimal TrainingPrice => IsUnlocked("academy") ? TrainingCost / 2 : TrainingCost;

    public CommandResult Train(int personId, Discipline discipline)
    {
        var person = FindPerson(personId);
        if (person == null)
            return CommandResult.Fail("Personne inconnue.");
        if (person.TrainingDiscipline != null || OnVacation(person))
            return CommandResult.Fail("Pas disponible en ce moment.");
        var paid = Spend(TrainingPrice, Category.Training, $"Formation {Person.DisciplineName(discipline)} : {person.Name}");
        if (!paid.Ok)
            return paid;
        person.TrainingDiscipline = discipline;
        person.TrainingUntil = GameTime.StartOfDay(GameTime.Day(GameTime.AddWorkdays(Minute, TrainingWorkdays))) + 8 * 60;
        return CommandResult.Success($"En formation jusqu'au {GameTime.FormatDate(person.TrainingUntil)}.");
    }

    public const int VacationWorkdays = 5;

    /// <summary>Congés : une semaine sans travailler (le salaire continue), l'énergie revient au maximum et le moral remonte.</summary>
    public CommandResult Vacation(int personId)
    {
        var person = FindPerson(personId);
        if (person == null)
            return CommandResult.Fail("Personne inconnue.");
        if (OnVacation(person) || person.TrainingDiscipline != null)
            return CommandResult.Fail("Pas disponible en ce moment.");
        person.VacationUntil = GameTime.StartOfDay(GameTime.Day(GameTime.AddWorkdays(Minute, VacationWorkdays))) + 8 * 60;
        person.Morale = Math.Min(100, person.Morale + 6);
        return CommandResult.Success($"En congé jusqu'au {GameTime.FormatDate(person.VacationUntil)}.");
    }

    public const decimal RaiseShare = 0.08m;
    public const int MaxRaisesPerYear = 2;

    /// <summary>
    /// Ce que coûterait une augmentation et ce qu'elle apporterait : +8 % de salaire ; le moral gagne 15 points,
    /// la moitié si une autre augmentation a eu lieu dans l'année. Deux par an au plus.
    /// </summary>
    public (bool Allowed, decimal ExtraPerMonth, double Morale, string Reason) RaiseTerms(Person person)
    {
        long yearAgo = Minute - 365L * GameTime.MinutesPerDay;
        int recent = person.RaiseDates.Count(d => d > yearAgo);
        decimal extra = Math.Round(person.Salary * RaiseShare / 50m) * 50m;
        double morale = 15 * Math.Pow(0.5, recent);
        if (person.IsFounder)
            return (false, 0, 0, "Ta rémunération est fixe.");
        if (recent >= MaxRaisesPerYear)
            return (false, extra, 0, "Déjà deux augmentations cette année.");
        return (true, extra, morale, "");
    }

    public CommandResult Raise(int personId)
    {
        var person = FindPerson(personId);
        if (person == null)
            return CommandResult.Fail("Personne inconnue.");
        var (allowed, extra, morale, reason) = RaiseTerms(person);
        if (!allowed)
            return CommandResult.Fail(reason);
        person.Salary += extra;
        person.Morale = Math.Min(100, person.Morale + morale);
        person.RaiseDates.Add(Minute);
        return CommandResult.Success($"Nouveau salaire : {person.Salary:N0} CHF par mois.");
    }
}
