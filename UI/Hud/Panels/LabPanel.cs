using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Core.campaign;
using Core.space;
using UI.Pixel;

namespace UI.Hud.Panels;

/// <summary>
/// Studio audiovisuel, zone XR, laboratoire avancé et installations quantiques : ce que chaque installation apporte
/// aujourd'hui, ce qui lui manque, et les actions (mise en route quantique, location d'un laboratoire avancé).
/// </summary>
public class LabPanel : GamePanel
{
    public override string Title => "Studio, XR et quantique";
    public override string Context => $"studio {Campaign.BestStudio().Score:P0} · zone XR {Campaign.BestXr().Capacity:P0}";
    public override bool Large => true;

    public override Control Build()
    {
        var column = Ui.Column(10, Hint("lab", "Ces installations se posent depuis Aménagement. Chacune a un effet plafonné : aucune ne rend tout meilleur."));
        bool any = false;
        if (Campaign.IsUnlocked("studio"))
        {
            column.Children.Add(Studio());
            any = true;
        }
        if (Campaign.IsUnlocked("xr"))
        {
            column.Children.Add(Xr());
            any = true;
        }
        if (Campaign.IsUnlocked("advanced_lab"))
        {
            column.Children.Add(Quantum());
            any = true;
        }
        if (!any)
            column.Children.Add(Ui.Muted("Rien de débloqué pour l'instant : Développement propose le studio audiovisuel, la réalité étendue et les recherches avancées."));
        return column;
    }

    private static Control Meter(string label, double fraction, string value) =>
        Ui.Column(2, Ui.Row(8, Ui.Muted(label, 13), Ui.Text(value, 13, Palette.Snow, wrap: false)), Ui.Bar(fraction, Palette.Gold, 8));

    private Control Studio()
    {
        var site = Campaign.Sites.OrderByDescending(s => Campaign.StudioOf(s).Score).First();
        var studio = Campaign.StudioOf(site);
        var body = Ui.Column(6,
            Ui.Text($"Meilleur studio : {site.Name}", 15, Palette.Snow, bold: true),
            Meter("Vidéo (caméra, fond vert, éclairage)", studio.Video, $"{studio.Video:P0}"),
            Meter("Audio (micro, enceintes, acoustique)", studio.Audio, $"{studio.Audio:P0}"),
            Ui.Pair("Effet", $"contenu +{studio.Bonus:P0} (graphismes, sons, niveaux, immersion) · campagnes marketing +{studio.Bonus / 2:P0}"),
            studio.Staffed ? null : Ui.Text("Personne ne travaille sur ce site : le studio ne rend qu'à moitié.", 13, Palette.Ember),
            studio.Missing.Count > 0 ? Ui.Muted($"Il manque : {string.Join(", ", studio.Missing)}.", 13) : Ui.Text("Studio complet.", 13, Palette.Leaf),
            Ui.Muted("Plafonné à +30 % sur le contenu. Les studios de plusieurs sites ne s'additionnent pas.", 12));
        return Ui.Section("Studio audiovisuel", Ui.Card(body));
    }

    private Control Xr()
    {
        var site = Campaign.Sites.OrderByDescending(s => Campaign.XrOf(s).Capacity).First();
        var zone = Campaign.XrOf(site);
        var body = Ui.Column(6,
            Ui.Text($"Meilleure zone : {site.Name}", 15, Palette.Snow, bold: true),
            Meter("Capacité de test", zone.Capacity, $"{zone.Capacity:P0}"),
            Ui.Pair("Effet", $"−{zone.DefectCut:P0} de défauts sur chaque amélioration d'une application XR, −{zone.DefectCut * 0.67:P0} sur la série d'un kit XR"),
            zone.Missing.Count > 0 ? Ui.Muted($"Il manque : {string.Join(", ", zone.Missing)}. Un casque seul sert au prototype (40 %).", 13) : Ui.Text("Zone complète : casque, balises, tapis et quelqu'un sur place.", 13, Palette.Leaf));
        return Ui.Section("Zone de test XR", Ui.Card(body));
    }

    private Control Quantum()
    {
        var column = Ui.Column(8);
        var sites = Campaign.Sites.Where(s => Campaign.SiteInfra(s).QuantumUnits > 0 || s.Items.Any(i => i.Def.Kind == FurnitureKind.Quantum)).ToList();
        foreach (var site in sites)
        {
            var infra = Campaign.SiteInfra(site);
            var state = Campaign.QuantumSites.FirstOrDefault(q => q.SiteId == site.Id);
            int online = Campaign.QuantumOnline(site);
            var body = Ui.Column(5,
                Ui.Text(site.Name, 15, Palette.Snow, bold: true),
                Ui.Pair("Installations stabilisées", $"{online} en service sur {infra.QuantumUnits} prises en charge"),
                Ui.Pair("Calcul quantique fourni", $"{infra.Quantum * (infra.QuantumUnits == 0 ? 0 : online / (double)infra.QuantumUnits):0.#} unité(s)"),
                Ui.Pair("Énergie et froid", $"{infra.PowerDraw:0.#} / {infra.PowerSupply:0.#} kW · {infra.Heat:0.#} / {infra.Cooling:0.#} kW de chaleur", infra.Factor < 0.99 ? Palette.Ember : null));
            foreach (string issue in infra.Issues)
                body.Children.Add(Ui.Text(issue, 13, Palette.Ember));
            if (state is { WarmupLeft: > 0 })
            {
                body.Children.Add(Ui.Bar(1 - state.WarmupLeft / (double)Campaign.QuantumWarmupDays, Palette.Gold, 8));
                body.Children.Add(Ui.Muted($"Mise en route : encore {state.WarmupLeft} jour(s){(state.Stabilized ? " (signaux stabilisés)" : "")}.", 13));
                if (!state.Stabilized)
                    body.Children.Add(Ui.Button("Stabiliser les signaux (mini-jeu)", () => ChallengeView.ShowQuantum(Screen, site.Id), "gold"));
            }
            column.Children.Add(Ui.Card(body));
        }
        if (sites.Count == 0)
            column.Children.Add(Ui.Muted("Aucune installation quantique. Loue un laboratoire avancé et achète un cryostat avec sa baie de contrôle et son support cryogénique."));
        bool lab = Campaign.Sites.Any(s => s.ListingId == "advanced_lab");
        column.Children.Add(Ui.Row(8,
            lab ? null : Ui.Button("Louer un laboratoire avancé", () => Open(new SitesPanel()), "gold"),
            Ui.Button("Recherche", () => Open(new ResearchPanel()), "tab"),
            Campaign.IsUnlocked("quantum_offers") ? Ui.Button("Service quantique", () => Open(new CloudPanel()), "tab") : null));
        column.Children.Add(Ui.Muted("Le cryostat ouvert sert à la recherche et à la calibration ; le cryostat fermé, une fois la calibration cryogénique terminée, fournit " +
                                     $"{Infrastructure.QuantumPerUnit:0} unités de calcul quantique après {Campaign.QuantumWarmupDays} jours de mise en route. Le quantique n'accélère pas tout : seulement la recherche de calcul intensif (+25 %) et le service vendu.", 12));
        return Ui.Section("Laboratoire avancé et quantique", column);
    }
}
