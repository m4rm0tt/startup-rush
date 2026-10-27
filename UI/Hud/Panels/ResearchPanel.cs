using System.Linq;
using Avalonia.Controls;
using Core.campaign;
using Core.team;
using UI.Pixel;

namespace UI.Hud.Panels;

/// <summary>
/// La recherche : une file de projets choisis, avancés par les personnes affectées à la recherche, sur un site qui a les
/// équipements demandés. Durée et budget annoncés ; suspendre ne perd rien.
/// </summary>
public class ResearchPanel : GamePanel
{
    public override string Title => "Recherche";
    public override string Context => $"{Campaign.ResearchDone.Count} terminée(s)";
    public override bool Large => true;

    public override Control Build()
    {
        var researchers = Campaign.Staff.Where(p => p.Assignment.Kind == AssignmentKind.Research).ToList();
        var column = Ui.Column(10, Hint("research", "Affecte des personnes à la recherche (fiche d'une personne) et équipe leur site : établis, bancs, instruments."),
            Ui.Muted(researchers.Count == 0 ? "Personne n'est affecté à la recherche : ouvre la fiche d'une personne, « Travaille sur », Recherche."
                : $"Chercheurs : {string.Join(", ", researchers.Select(p => $"{p.Name} ({Campaign.SiteOf(p).Name})"))}.", 14));

        var running = Ui.Column(6);
        foreach (var project in Campaign.Research)
        {
            var def = project.Def;
            var sites = Campaign.Sites.Where(s => Campaign.MissingEquipment(s, def).Count == 0).Select(s => s.Name).ToList();
            double rate = researchers.Where(p => sites.Contains(Campaign.SiteOf(p).Name)).Sum(_ => 8.0);
            string eta = rate > 0 ? $"~{System.Math.Ceiling((def.Effort - project.Progress) / rate)} j" : "personne d'équipé dessus";
            running.Children.Add(Ui.Card(Ui.Column(4,
                Ui.Row(Ui.Text(def.Name, 16, Palette.Snow, bold: true), Ui.Tag(def.Field, Palette.Sky), project.Paused ? Ui.Tag("SUSPENDUE", Palette.Lemon) : null),
                Ui.Muted(def.Unlocks, 13),
                Ui.Bar(project.Progress / def.Effort, Palette.Gold, 8),
                Ui.Muted($"{project.Progress:0}/{def.Effort:0} points · {eta} · {def.DailyCost:N0} CHF par jour de travail", 13),
                Ui.Muted(sites.Count > 0 ? $"Équipé : {string.Join(", ", sites)}" : $"Il manque : {string.Join(", ", Campaign.MissingEquipment(Campaign.Premises, def))}", 13),
                Ui.Button(project.Paused ? "Reprendre" : "Suspendre", () => Run(Campaign.PauseResearch(def.Id, !project.Paused))))));
        }
        if (Campaign.Research.Count == 0)
            running.Children.Add(Ui.Muted("Aucune recherche en cours."));
        column.Children.Add(Ui.Section("En cours", running));

        var catalog = new WrapPanel();
        foreach (var def in Campaign.ResearchCatalog.Where(d => !Campaign.ResearchDone.Contains(d.Id) && Campaign.Research.All(r => r.DefId != d.Id)))
        {
            var (ok, why) = Campaign.CanResearch(def);
            var body = Ui.Column(4, Ui.Text(def.Name, 15, Palette.Snow, bold: true), Ui.Tag(def.Field, Palette.Sky), Ui.Muted(def.Unlocks, 13),
                Ui.Muted($"{def.Effort:0} points · {def.DailyCost:N0} CHF par jour", 12),
                Ui.Muted("Équipement : " + string.Join(" + ", def.Equipment.Select(g => string.Join(" ou ", g.Select(id => Core.space.FurnitureCatalog.Get(id).Name)))), 12),
                ok ? Ui.Button("Lancer", () => Run(Campaign.StartResearch(def.Id)), "gold") : Ui.Muted(why, 12));
            body.Width = 300;
            catalog.Children.Add(new PixelPanel { Skin = "inset", Padding = new Avalonia.Thickness(10, 8), Margin = new Avalonia.Thickness(0, 0, 8, 8), Child = body });
        }
        column.Children.Add(Ui.Section("Recherches possibles", catalog));
        if (Campaign.ResearchDone.Count > 0)
            column.Children.Add(Ui.Section("Terminées", Ui.Muted(string.Join(" · ", Campaign.ResearchDone.Select(id => Campaign.ResearchDefOf(id).Name)), 13)));
        return column;
    }
}
