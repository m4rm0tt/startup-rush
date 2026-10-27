using System.Linq;
using Avalonia.Controls;
using Core.campaign;
using Core.space;
using Core.team;
using UI.Game;
using UI.Hud.Panels;
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
        var column = Ui.Column(6, Ui.Row(Atlas.Cropped(def.Id) is { } img ? Ui.Image(img, img.SourceRect.Width * 2, img.SourceRect.Height * 2) : null,
            Ui.Column(2, Ui.Title(def.Name, 18), Ui.Muted(def.Effect, 13))));

        if (def.Kind == FurnitureKind.Desk)
        {
            var issue = Layout.Check(campaign.Premises, item);
            var computer = campaign.Premises.ComputerOn(item);
            column.Children.Add(Ui.Text(Layout.Describe(issue), 15, issue == WorkstationIssue.None ? Palette.Leaf : Palette.Ember));
            if (computer != null)
                column.Children.Add(Ui.Muted($"Ordinateur : {computer.Def.Name}"));
            var owner = item.AssignedPersonId is int id ? campaign.FindPerson(id) : null;
            column.Children.Add(Ui.Text(owner != null ? $"Poste de {owner.Name}" : "Poste libre", 15, Palette.Snow));
            var choices = campaign.People.Select(p => ((int?)p.Id, p.Name)).Prepend(((int?)null, "Personne")).ToList();
            column.Children.Add(Ui.Choice(choices, item.AssignedPersonId, pid =>
            {
                var result = campaign.AssignDesk(item.Id, pid);
                screen.Toast(result.Message, result.Ok);
                screen.Session.Select(SelectionKind.Item, item.Id);
            }));
        }

        if (def.Movable)
        {
            decimal resale = Campaign.ResaleValue(def);
            column.Children.Add(Ui.Row(
                Ui.Button("Déplacer", () =>
                {
                    screen.OpenRoot(new BuildPanel());
                    screen.Session.SetTool(new BuildTool(def.Id, MovingItemId: item.Id));
                }),
                Ui.Button("Ranger", () =>
                {
                    var result = campaign.Store(item.Id);
                    screen.Toast(result.Ok ? $"{def.Name} rangé·e dans les cartons." : result.Message, result.Ok);
                    screen.Session.ClearSelection();
                }),
                Ui.Button(resale > 0 ? $"Revendre {resale:N0}" : "Jeter", () => screen.Confirm($"Revendre {def.Name} ?",
                    resale > 0 ? $"Tu récupères {resale:N0} CHF (la moitié du prix neuf). Ce qui est posé dessus part dans les cartons." : "Il ne vaut plus rien.",
                    "Revendre", () =>
                    {
                        var result = campaign.Sell(item.Id);
                        screen.Toast(result.Message, result.Ok);
                        screen.Session.ClearSelection();
                    }), "red")));
        }
        else
            column.Children.Add(Ui.Muted("Équipement fixe du local."));
        return column;
    }

    private static Control Person(GameScreen screen, Campaign campaign, Person person)
    {
        var look = Atlas.Cropped($"person{person.Look % 5}");
        var column = Ui.Column(6,
            Ui.Row(look != null ? Ui.Image(look, look.SourceRect.Width * 2, look.SourceRect.Height * 2) : null,
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
