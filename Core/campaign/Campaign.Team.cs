using Core.finance;
using Core.team;
using Core.work;

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

    public CommandResult Assign(int personId, AssignmentKind kind, int targetId)
    {
        var person = FindPerson(personId);
        if (person == null)
            return CommandResult.Fail("Personne inconnue.");
        if (kind is AssignmentKind.Contract && FindContract(targetId) is not { Status: ContractStatus.Active })
            return CommandResult.Fail("Ce contrat n'est pas en cours.");
        if (kind is AssignmentKind.Product or AssignmentKind.Maintenance or AssignmentKind.Support && FindProduct(targetId) == null)
            return CommandResult.Fail("Produit inconnu.");
        // Une seule affectation : changer de cible remplace l'ancienne
        person.Assignment = new Assignment { Kind = kind, TargetId = targetId };
        person.CurrentWorkItemId = null;
        return CommandResult.Success();
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
        if (lead && person.Skill(person.BestDiscipline) < 55)
            return CommandResult.Fail("Il faut au moins un niveau confirmé (55) pour encadrer une équipe.");
        person.IsLead = lead;
        return CommandResult.Success();
    }

    public CommandResult Train(int personId, Discipline discipline)
    {
        var person = FindPerson(personId);
        if (person == null)
            return CommandResult.Fail("Personne inconnue.");
        if (person.TrainingDiscipline != null)
            return CommandResult.Fail("Déjà en formation.");
        var paid = Spend(TrainingCost, Category.Training, $"Formation {Person.DisciplineName(discipline)} : {person.Name}");
        if (!paid.Ok)
            return paid;
        person.TrainingDiscipline = discipline;
        person.TrainingUntil = GameTime.StartOfDay(GameTime.Day(GameTime.AddWorkdays(Minute, TrainingWorkdays))) + 8 * 60;
        return CommandResult.Success($"En formation jusqu'au {GameTime.FormatDate(person.TrainingUntil)}.");
    }

    public CommandResult Raise(int personId, decimal percent)
    {
        var person = FindPerson(personId);
        if (person == null || person.IsFounder)
            return CommandResult.Fail("Impossible.");
        person.Salary = Math.Round(person.Salary * (1 + percent) / 50m) * 50m;
        person.Morale = Math.Min(100, person.Morale + (double)percent * 60);
        return CommandResult.Success($"Nouveau salaire : {person.Salary:N0} CHF.");
    }
}
