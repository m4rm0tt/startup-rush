using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Core.campaign;
using Core.space;
using Core.team;
using Core.work;
using UI.Game;
using UI.Pixel;

namespace UI.Hud.Panels;

/// <summary>Équipe : qui fait quoi, les goulots, le recrutement.</summary>
public class TeamPanel : GamePanel
{
    public override string Title => "Équipe";
    public override string Context => $"{Campaign.People.Count} personne(s) · masse salariale {Campaign.Staff.Sum(p => p.Salary):N0} CHF/mois";

    public static string AssignmentText(Campaign campaign, Person person) => person.Assignment.Kind switch
    {
        AssignmentKind.Contract => $"Contrat : {campaign.FindContract(person.Assignment.TargetId)?.Client ?? "?"}",
        AssignmentKind.Product => $"Développe : {campaign.FindProduct(person.Assignment.TargetId)?.Name ?? "?"}",
        AssignmentKind.Maintenance => $"Maintenance : {campaign.FindProduct(person.Assignment.TargetId)?.Name ?? "?"}",
        AssignmentKind.Support => $"Support : {campaign.FindProduct(person.Assignment.TargetId)?.Name ?? "?"}",
        _ => "Sans affectation"
    };

    /// <summary>Toutes les cibles possibles d'une affectation.</summary>
    public static List<((AssignmentKind, int) Value, string Label)> Targets(Campaign campaign)
    {
        var list = new List<((AssignmentKind, int), string)> { ((AssignmentKind.None, 0), "Rien") };
        foreach (var c in campaign.ActiveContracts)
            list.Add(((AssignmentKind.Contract, c.Id), $"Contrat {c.Client}"));
        foreach (var p in campaign.Products)
        {
            list.Add(((AssignmentKind.Product, p.Id), $"Dév. {p.Name}"));
            if (p.IsLive)
            {
                list.Add(((AssignmentKind.Maintenance, p.Id), $"Maint. {p.Name}"));
                list.Add(((AssignmentKind.Support, p.Id), $"Support {p.Name}"));
            }
        }
        return list;
    }

    public override Control Build()
    {
        var column = Ui.Column(8);
        var people = Ui.Column(8);
        var targets = Targets(Campaign);
        foreach (var person in Campaign.People)
        {
            var desk = Campaign.Premises.DeskOf(person.Id);
            var issue = desk == null ? (WorkstationIssue?)null : Layout.Check(Campaign.Premises, desk);
            var look = Atlas.Cropped($"person{person.Look % 5}");
            string skills = string.Join(" · ", person.Skills.OrderByDescending(s => s.Value).Select(s => $"{Person.DisciplineName(s.Key)} {s.Value:0}"));
            var header = Ui.Row(look != null ? Ui.Image(look, look.SourceRect.Width * 2, look.SourceRect.Height * 2) : null,
                Ui.Column(1,
                    Ui.Row(Ui.Text(person.Name, 16, Palette.Snow, bold: true), person.IsLead ? Ui.Tag("Responsable", Palette.Gold) : null,
                        person.LeavesAt != null ? Ui.Tag("Démissionne", Palette.Blood) : null,
                        person.StartsAt > Campaign.Minute ? Ui.Tag($"Arrive le {GameTime.FormatDate(person.StartsAt)}", Palette.Sky) : null),
                    Ui.Muted($"{person.Title}{(person.IsFounder ? "" : $" · {person.Salary:N0} CHF/mois")}", 13),
                    Ui.Muted(skills, 12)));
            var card = Ui.Column(4,
                header,
                Ui.Text(string.IsNullOrEmpty(person.Status) ? UI.Hud.SelectionInspector.ActivityText(person) : person.Status, 13, Palette.Cloud),
                desk == null ? Ui.Text("Aucun poste attribué : clique un bureau dans Aménagement.", 13, Palette.Ember)
                    : issue != WorkstationIssue.None ? Ui.Text($"Poste incomplet : {Layout.Describe(issue!.Value)}", 13, Palette.Ember) : null,
                Ui.Row(Ui.Muted("Énergie", 12), Ui.Bar(person.Energy / 100, person.Energy < 35 ? Palette.Ember : Palette.Leaf, 6, 80),
                    Ui.Muted("Moral", 12), Ui.Bar(person.Morale / 100, person.Morale < 35 ? Palette.Blood : Palette.Sky, 6, 80),
                    Ui.Muted($"{person.PointsThisWeek:0} pts cette semaine", 12)),
                Ui.Choice(targets, (person.Assignment.Kind, person.Assignment.TargetId), t => Run(Campaign.Assign(person.Id, t.Item1, t.Item2))),
                Ui.Button("Fiche détaillée", () => Open(new PersonPanel(person.Id))));
            people.Children.Add(Ui.Card(card, person.LeavesAt != null ? Palette.Blood : null));
        }
        column.Children.Add(Ui.Section("L'équipe", people, Ui.Muted("Une personne n'a qu'une affectation. Au-delà de 3 personnes sur une même cible, " +
                                                                    "la coordination coûte (un responsable et des tableaux blancs repoussent cette limite).")));

        // Goulots par cible
        var bottlenecks = Ui.Column(2);
        foreach (var c in Campaign.ActiveContracts)
            bottlenecks.Children.Add(Ui.Muted($"{c.Client} : {Campaign.Bottleneck(WorkOwner.Contract, c.Id)} en attente", 13));
        foreach (var p in Campaign.Products.Where(p => Campaign.WorkFor(WorkOwner.Product, p.Id).Any(w => !w.Done)))
            bottlenecks.Children.Add(Ui.Muted($"{p.Name} : {Campaign.Bottleneck(WorkOwner.Product, p.Id)} en attente", 13));
        if (bottlenecks.Children.Count > 0)
            column.Children.Add(Ui.Section("Travail prêt, par discipline la plus chargée", bottlenecks));

        var candidates = Ui.Column(8);
        foreach (var candidate in Campaign.Candidates)
        {
            var p = candidate.Person;
            string skills = string.Join(" · ", p.Skills.OrderByDescending(s => s.Value).Take(3).Select(s => $"{Person.DisciplineName(s.Key)} {s.Value:0}"));
            candidates.Children.Add(Ui.Card(Ui.Column(3,
                Ui.Row(Ui.Text(p.Name, 15, Palette.Snow, bold: true), Ui.Tag(p.Title, Palette.Steel, Palette.Snow)),
                Ui.Muted(skills, 13),
                Ui.Pair("Salaire demandé", $"{p.Salary:N0} CHF/mois", Palette.Lemon),
                Ui.Muted(candidate.NoticeWorkdays == 0 ? "Disponible tout de suite." : $"Préavis : {candidate.NoticeWorkdays} jours ouvrés. Disponible jusqu'au {GameTime.FormatDate(candidate.ExpiresAt)}.", 12),
                Ui.Button("Embaucher", () => Screen.Confirm($"Embaucher {p.Name} ?",
                    $"Salaire de {p.Salary:N0} CHF par mois, payé le 25. Il lui faudra un poste complet libre pour travailler.",
                    "Embaucher", () => Run(Campaign.Hire(p.Id)), danger: false), "green"))));
        }
        if (Campaign.Candidates.Count == 0)
            candidates.Children.Add(Ui.Muted("Personne de disponible cette semaine. De nouveaux profils arrivent chaque lundi."));
        column.Children.Add(Ui.Section("Recrutement", candidates));
        return column;
    }
}

/// <summary>La fiche d'une personne : compétences, horaire, formation, salaire, poste.</summary>
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
            skills.Children.Add(Ui.Row(Ui.Text(Person.DisciplineName(discipline), 14, Palette.Fog), Ui.Bar(level / 100, Palette.Sky, 8, 160), Ui.Number($"{level:0}", 18),
                Ui.Button($"Former ({Campaign.TrainingCost:N0} CHF)", () => Run(Campaign.Train(person.Id, discipline)), enabled: person.TrainingDiscipline == null,
                    tip: $"{Campaign.TrainingWorkdays} jours ouvrés hors du bureau. Gain plus faible à haut niveau.")));

        var column = Ui.Column(8,
            Ui.Section("Compétences", skills,
                person.TrainingDiscipline != null ? Ui.Text($"En formation jusqu'au {GameTime.FormatDate(person.TrainingUntil)}", 14, Palette.Lemon) : null),
            Ui.Section("Affectation",
                Ui.Text(TeamPanel.AssignmentText(Campaign, person), 15, Palette.Lemon),
                Ui.Choice(TeamPanel.Targets(Campaign), (person.Assignment.Kind, person.Assignment.TargetId), t => Run(Campaign.Assign(person.Id, t.Item1, t.Item2)))),
            Ui.Section("Horaire",
                Ui.Choice([(WorkSchedule.Normal, "9 h – 17 h"), (WorkSchedule.Extended, "9 h – 20 h")], person.Schedule, s => Run(Campaign.SetSchedule(person.Id, s))),
                Ui.Muted("L'horaire étendu donne 3 heures de plus, moins efficaces, fatigue nettement plus et pèse sur le moral.")),
            Ui.Section("État",
                Ui.Pair("Énergie", $"{person.Energy:0}/100", person.Energy < 35 ? Palette.Ember : null),
                Ui.Pair("Moral", $"{person.Morale:0}/100", person.Morale < 35 ? Palette.Blood : null),
                person.UnpaidDays > 0 ? Ui.Pair("Salaire impayé depuis", $"{person.UnpaidDays} jours", Palette.Blood) : null,
                person.LeavesAt is long leaves ? Ui.Text($"A annoncé son départ pour le {GameTime.FormatDate(leaves)}.", 14, Palette.Blood) : null));

        column.Children.Add(Ui.Section("Encadrement",
            Ui.Choice([(false, "Équipier"), (true, "Responsable d'équipe")], person.IsLead, v => Run(Campaign.SetLead(person.Id, v))),
            Ui.Muted("Un responsable permet à son équipe de travailler à 6 sans perte de coordination (au lieu de 3).")));

        if (!person.IsFounder)
        {
            column.Children.Add(Ui.Section("Contrat de travail",
                Ui.Pair("Salaire", $"{person.Salary:N0} CHF par mois"),
                Ui.Row(Ui.Button("Augmenter de 5 %", () => Run(Campaign.Raise(person.Id, 0.05m))), Ui.Button("Augmenter de 10 %", () => Run(Campaign.Raise(person.Id, 0.10m)))),
                Ui.Button("Licencier", () => Screen.Confirm($"Licencier {person.Name} ?",
                    $"Indemnité d'un mois de salaire ({person.Salary:N0} CHF) plus les jours travaillés depuis la dernière paie. Le moral de l'équipe baisse.",
                    "Licencier", () =>
                    {
                        Run(Campaign.Fire(person.Id));
                        Screen.PopPanel();
                    }), "red")));
        }

        var desk = Campaign.Premises.DeskOf(person.Id);
        column.Children.Add(Ui.Section("Poste",
            Ui.Text(desk == null ? "Aucun bureau attribué." : Layout.Describe(Layout.Check(Campaign.Premises, desk)), 14),
            desk != null ? Ui.Button("Voir le bureau", () => Session.Select(SelectionKind.Item, desk.Id)) : null));
        return column;
    }
}
