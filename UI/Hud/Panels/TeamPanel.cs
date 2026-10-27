using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Core.campaign;
using Core.team;
using UI.Pixel;

namespace UI.Hud.Panels;

/// <summary>Équipe : qui travaille sur quoi, comment il va, et le recrutement. Salaires et formalités sont automatiques.</summary>
public class TeamPanel : GamePanel
{
    public override string Title => "Équipe";
    public override string Context => $"{Campaign.People.Count} personne(s) · {Campaign.Staff.Sum(p => p.Salary):N0} CHF de salaires par mois";

    public static string AssignmentText(Campaign campaign, Person person) =>
        person.Assignment.Kind == AssignmentKind.Project && campaign.FindProduct(person.Assignment.TargetId) is { } product
            ? product.Name
            : campaign.CurrentProject(person) is { } auto ? $"Auto ({auto.Name})" : "Auto";

    public static WrapPanel AssignmentChoice(Campaign campaign, Person person, System.Action<int> pick) =>
        Ui.Choice(campaign.Products.OrderBy(p => p.Priority).Select(p => (p.Id, p.Name)).Prepend((0, "Auto")),
            person.Assignment.Kind == AssignmentKind.Project ? person.Assignment.TargetId : 0, pick);

    public override Control Build()
    {
        var column = Ui.Column(8, Hint("team", "En « Auto », chacun travaille sur ton projet prioritaire. Tu peux aussi fixer quelqu'un sur un projet."));
        foreach (var person in Campaign.People)
            column.Children.Add(PersonCard(person));
        if (Campaign.Staff.Count() > 1)
            column.Children.Add(Ui.Button("Tout le monde en Auto", () =>
            {
                Campaign.AssignAllAuto();
                Refresh();
            }, "tab"));

        var candidates = Ui.Column(6);
        foreach (var candidate in Campaign.Candidates)
        {
            var p = candidate.Person;
            var row = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto") };
            if (Atlas.Cropped($"person{p.Look % 5}") is { } look)
                row.Children.Add(Ui.Image(look, look.SourceRect.Width * 1.5, look.SourceRect.Height * 1.5));
            var info = Ui.Column(0, Ui.Text($"{p.Name} · {p.Title}", 14, Palette.Snow, bold: true),
                Ui.Muted($"{p.Salary:N0} CHF/mois{(candidate.NoticeWorkdays > 0 ? $" · arrive dans {candidate.NoticeWorkdays} j" : "")}", 12));
            info.Margin = new Thickness(8, 0);
            Grid.SetColumn(info, 1);
            row.Children.Add(info);
            var hire = Ui.Button("Recruter", () => ConfirmHire(candidate), "green");
            Grid.SetColumn(hire, 2);
            row.Children.Add(hire);
            candidates.Children.Add(Ui.Card(row));
        }
        if (Campaign.Candidates.Count == 0)
            candidates.Children.Add(Ui.Muted("Personne cette semaine. Nouveaux profils chaque lundi.", 13));
        column.Children.Add(Ui.Section("Recrutement", candidates));
        return column;
    }

    private void ConfirmHire(Candidate candidate)
    {
        var p = candidate.Person;
        decimal after = Campaign.MonthlyFixedCosts + p.Salary - Campaign.RecurringRevenue30;
        bool free = Campaign.Premises.Desks.Any(d => d.AssignedPersonId == null && Core.space.Layout.Check(Campaign.Premises, d) == Core.space.WorkstationIssue.None);
        string detail = $"+{p.Salary:N0} CHF par mois. " +
                        (after > 0 ? $"Tu dépenseras alors {after:N0} CHF de plus que tes revenus chaque mois." : "Tes revenus couvrent encore tes dépenses.") +
                        (free ? "" : " Il faudra lui installer un poste.");
        Screen.Confirm($"Recruter {p.Name} ?", detail, "Recruter", () => Run(Campaign.Hire(p.Id)), danger: after > 0 && Campaign.Runway().Days < 90);
    }

    private Control PersonCard(Person person)
    {
        var look = Atlas.Cropped($"person{person.Look % 5}");
        string state = Campaign.OnVacation(person) ? "En congé" : person.TrainingDiscipline != null ? "En formation" : person.StartsAt > Campaign.Minute ? $"Arrive le {GameTime.FormatDate(person.StartsAt)}" : "";
        var header = Ui.Row(10,
            look != null ? Ui.Image(look, look.SourceRect.Width * 2, look.SourceRect.Height * 2) : null,
            Ui.Column(2,
                Ui.Row(6, Ui.Text(person.Name, 16, Palette.Snow, bold: true, wrap: false), state != "" ? Ui.Tag(state, Palette.Sky) : null,
                    person.LeavesAt != null ? Ui.Tag("Veut partir", Palette.Blood) : null),
                Ui.Muted(person.Title + (person.IsFounder ? "" : $" · {person.Salary:N0} CHF/mois"), 12),
                Ui.Row(6, Ui.Muted("Énergie", 11), Ui.Bar(person.Energy / 100, person.Energy < 35 ? Palette.Ember : Palette.Leaf, 6, 70),
                    Ui.Muted("Moral", 11), Ui.Bar(person.Morale / 100, person.Morale < 35 ? Palette.Blood : Palette.Sky, 6, 70))));
        var card = Ui.Column(6, header,
            AssignmentChoice(Campaign, person, id => Run(Campaign.Assign(person.Id, id))),
            Ui.Row(4,
                Ui.Button("Congés", () => Screen.Confirm($"Une semaine de congés pour {person.Name} ?", "Absent 5 jours ouvrés (salaire maintenu). Revient en pleine forme, avec un meilleur moral.",
                    "Valider", () => Run(Campaign.Vacation(person.Id)), danger: false), "tab", !Campaign.OnVacation(person) && person.TrainingDiscipline == null),
                Ui.Button("Fiche", () => Open(new PersonPanel(person.Id)), "tab")));
        return Ui.Card(card, person.LeavesAt != null ? Palette.Blood : null);
    }
}

/// <summary>La fiche d'une personne : compétences, formation, horaire, augmentation.</summary>
public class PersonPanel(int personId) : GamePanel
{
    private Person? Person => Campaign.FindPerson(personId);

    public override string Title => Person?.Name ?? "Personne";
    public override string Context => Person?.Title ?? "";

    public override Control Build()
    {
        var person = Person;
        if (person == null)
            return Ui.Muted("Cette personne a quitté l'entreprise.");

        var skills = Ui.Column(4);
        foreach (var (discipline, level) in person.Skills.OrderBy(s => s.Key))
            skills.Children.Add(Ui.Row(Ui.Text(Person.DisciplineName(discipline), 14, Palette.Fog), Ui.Bar(level / 100, Palette.Sky, 8, 130), Ui.Number($"{level:0}", 18),
                Ui.Button("Former", () => Screen.Confirm($"Former {person.Name} en {Person.DisciplineName(discipline).ToLowerInvariant()} ?",
                        $"{Campaign.TrainingPrice:N0} CHF et {Campaign.TrainingWorkdays} jours d'absence.", "Former", () => Run(Campaign.Train(person.Id, discipline)), danger: false),
                    "tab", person.TrainingDiscipline == null && !Campaign.OnVacation(person))));

        var column = Ui.Column(8,
            Ui.Section("Travaille sur", Ui.Text(TeamPanel.AssignmentText(Campaign, person), 15, Palette.Lemon),
                TeamPanel.AssignmentChoice(Campaign, person, id => Run(Campaign.Assign(person.Id, id)))),
            Ui.Section("Compétences", skills),
            Ui.Section("Horaire", Ui.Choice([(WorkSchedule.Normal, "9 h – 17 h"), (WorkSchedule.Extended, "9 h – 20 h")], person.Schedule, s => Run(Campaign.SetSchedule(person.Id, s))),
                Ui.Muted("Les soirées produisent un peu plus mais fatiguent et pèsent sur le moral.", 12)));

        if (!person.IsFounder)
        {
            var (allowed, extra, morale, reason) = Campaign.RaiseTerms(person);
            column.Children.Add(Ui.Section("Salaire",
                Ui.Text($"{person.Salary:N0} CHF par mois (référence pour ce niveau : {Campaign.MarketSalary(person):N0})", 14),
                Ui.Button(allowed ? $"Augmenter (+{extra:N0} CHF/mois, moral +{morale:0})" : "Augmenter",
                    () => Screen.Confirm($"Augmenter {person.Name} ?", $"+{extra:N0} CHF chaque mois. Moral +{morale:0}.", "Augmenter", () => Run(Campaign.Raise(person.Id)), danger: false),
                    "green", allowed, allowed ? null : reason),
                allowed ? null : Ui.Muted(reason, 12)));
            if (Campaign.IsUnlocked("leads"))
                column.Children.Add(Ui.Section("Rôle", Ui.Choice([(false, "Équipier"), (true, "Responsable")], person.IsLead, v => Run(Campaign.SetLead(person.Id, v)))));
            column.Children.Add(Ui.Details("Fin de contrat", Ui.Button("Licencier", () => Screen.Confirm($"Licencier {person.Name} ?",
                $"Indemnité : {person.Salary:N0} CHF, plus les jours dus. Le moral de l'équipe baisse.", "Licencier", () =>
                {
                    Run(Campaign.Fire(person.Id));
                    Screen.PopPanel();
                }), "red")));
        }
        return column;
    }
}
