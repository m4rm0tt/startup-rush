using System.Linq;
using Avalonia.Controls;
using Core.campaign;
using Core.space;
using Core.team;
using UI.Game;
using UI.Hud.Panels;
using UI.Iso;
using UI.Pixel;
using UI.Screens;

namespace UI.Hud;

/// <summary>La fiche de ce qui est sélectionné dans la scène, avec ses actions.</summary>
public static class SelectionInspector
{
    public static Control? Build(GameScreen screen)
    {
        var session = screen.Session;
        var campaign = session.Campaign;
        return session.SelectionKind switch
        {
            SelectionKind.Item when campaign.Premises.Find(session.SelectionId) is { } item => Item(screen, campaign, item),
            SelectionKind.Person when campaign.FindPerson(session.SelectionId) is { } person => Person(screen, campaign, person),
            _ => null
        };
    }

    private static Control Item(GameScreen screen, Campaign campaign, PlacedItem item)
    {
        var def = item.Def;
        var thumb = PackLibrary.Thumbnail(def.Id);
        var column = Ui.Column(6, Ui.Row(Ui.Image(thumb, thumb.PixelSize.Width, thumb.PixelSize.Height),
            Ui.Column(2, Ui.Title(def.Name, 18), Ui.Muted(def.Effect, 13))));

        // Station de production : son étape, son temps, ce qui la bloque
        if (Core.industry.IndustryCatalog.Station(def.Id) is { } station)
        {
            var status = campaign.Flow.TryGetValue(item.Id, out var s) ? s : new FlowStatus(false, "En attente");
            column.Children.Add(Ui.Muted($"Étape : {Core.industry.IndustryCatalog.StepName(station.Step)}, {station.Minutes:0} min par unité{(station.Manual ? ", un opérateur" : ", supervisée")}.", 13));
            column.Children.Add(Ui.Text(status.Text, 15, status.Working ? Palette.Leaf : Palette.Ember));
        }
        if (def.Kind == FurnitureKind.Desk)
            foreach (var post in campaign.Premises.Workstations.Where(p => p.Desk.Id == item.Id))
            {
                var issue = Layout.Check(campaign.Premises, post);
                column.Children.Add(Ui.Text(Layout.Describe(issue), 15, issue == WorkstationIssue.None ? Palette.Leaf : Palette.Ember));
                if (campaign.Premises.ComputerOf(post) is { } computer)
                    column.Children.Add(Ui.Muted($"Ordinateur : {computer.Def.Name}"));
                var chair = campaign.Premises.ChairOf(post);
                var owner = chair?.AssignedPersonId is int id ? campaign.FindPerson(id) : null;
                column.Children.Add(Ui.Text(owner != null ? $"Poste de {owner.Name}" : "Poste libre", 15, Palette.Snow));
                if (chair == null)
                    continue;
                var choices = campaign.People.Select(p => ((int?)p.Id, p.Name)).Prepend(((int?)null, "Personne")).ToList();
                column.Children.Add(Ui.Choice(choices, chair.AssignedPersonId, pid =>
                {
                    CommandResult result;
                    if (pid is int target && campaign.FindPerson(target) is { } person)
                    {
                        campaign.Seat(person, post);
                        result = CommandResult.Success(issue == WorkstationIssue.None ? "Poste attribué." : $"Poste attribué, mais incomplet : {Layout.Describe(issue)}.");
                    }
                    else
                    {
                        chair.AssignedPersonId = null;
                        result = CommandResult.Success("Poste libéré.");
                    }
                    screen.Toast(result.Message, result.Ok);
                    screen.Session.Select(SelectionKind.Item, item.Id);
                }));
            }

        if (!item.Included)
        {
            decimal resale = Campaign.ResaleValue(item.PaidPrice);
            column.Children.Add(Ui.Row(
                item.HostId == null ? Ui.Button("Déplacer", () =>
                {
                    screen.OpenRoot(new BuildPanel());
                    screen.Session.SetTool(new BuildTool(def.Id, MovingItemId: item.Id, Facing: item.Facing));
                }) : null,
                item.HostId == null && def.Pack.Facings.Count > 1 ? Ui.Button("Tourner", () =>
                {
                    var result = campaign.Rotate(item.Id);
                    if (!result.Ok)
                        screen.Toast(result.Message, false);
                }) : null,
                Ui.Button("Ranger", () =>
                {
                    var result = campaign.Store(item.Id);
                    screen.Toast(result.Ok ? $"{def.Name} rangé·e dans les cartons." : result.Message, result.Ok);
                    screen.Session.ClearSelection();
                }),
                Ui.Button(resale > 0 ? $"Revendre {resale:N0}" : "Jeter", () => screen.Confirm($"Revendre {def.Name} ?",
                    resale > 0 ? $"Tu récupères {resale:N0} CHF (la moitié du prix payé). Ce qui est posé dessus part dans les cartons." : "Il ne vaut rien à la revente : il était déjà là.",
                    "Revendre", () =>
                    {
                        var result = campaign.Sell(item.Id);
                        screen.Toast(result.Message, result.Ok);
                        screen.Session.ClearSelection();
                    }), "red")));
        }
        else
            column.Children.Add(Ui.Muted(item.HostId == null && def.Pack.Facings.Count > 1 ? "Livré avec le local : il reste ici, mais tu peux le déplacer." : "Livré avec le local : il reste ici."));
        if (item.Included && item.HostId == null)
            column.Children.Add(Ui.Row(
                Ui.Button("Déplacer", () =>
                {
                    screen.OpenRoot(new BuildPanel());
                    screen.Session.SetTool(new BuildTool(def.Id, MovingItemId: item.Id, Facing: item.Facing));
                }),
                def.Pack.Facings.Count > 1 ? Ui.Button("Tourner", () =>
                {
                    var result = campaign.Rotate(item.Id);
                    if (!result.Ok)
                        screen.Toast(result.Message, false);
                }) : null));
        return column;
    }

    private static Control Person(GameScreen screen, Campaign campaign, Person person)
    {
        var column = Ui.Column(6,
            Ui.Row(Ui.Image(Portraits.Of(person.Appearance), 64, 64),
                Ui.Column(2, Ui.Title(person.Name, 18), Ui.Muted(person.Title, 13))),
            Ui.Text(string.IsNullOrEmpty(person.Status) ? ActivityText(person) : person.Status, 14, Palette.Cloud),
            Ui.Row(Ui.Muted("Énergie", 13), Ui.Bar(person.Energy / 100, person.Energy < 35 ? Palette.Ember : Palette.Leaf, 8, 90),
                Ui.Muted("Moral", 13), Ui.Bar(person.Morale / 100, person.Morale < 35 ? Palette.Blood : Palette.Sky, 8, 90)),
            TeamPanel.AssignmentChoice(campaign, person, id =>
            {
                var result = campaign.Assign(person.Id, id);
                screen.Toast(result.Ok ? $"{person.Name} : {TeamPanel.AssignmentText(campaign, person)}" : result.Message, result.Ok);
                screen.Session.Select(SelectionKind.Person, person.Id);
            }),
            Ui.Button("Ouvrir la fiche", () =>
            {
                screen.OpenRoot(new TeamPanel());
                screen.PushPanel(new PersonPanel(person.Id));
            }));
        return column;
    }

    public static string ActivityText(Person person) => person.Activity switch
    {
        Activity.Walking => "Se déplace",
        Activity.Working => "Au travail",
        Activity.Break => "En pause",
        Activity.Idle => "Attend un poste",
        Activity.Training => "En formation",
        _ => "Absent·e"
    };
}
