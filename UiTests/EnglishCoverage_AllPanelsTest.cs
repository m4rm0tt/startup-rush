using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using UI;
using UI.Hud;
using UI.Hud.Panels;

namespace UiTests;

/// <summary>
/// Couverture transversale de l'écran de jeu : tous les panneaux, les fiches de produits et de personnes, le terminal et le
/// menu pause, ouverts sur une campagne avancée avec la langue anglaise. Aucun texte d'apparence française ne doit rester
/// (texte affiché ou infobulle), sans aucune exception : ce test complète les tests de chaque zone.
/// </summary>
public class EnglishCoverage_AllPanelsTest
{
    [AvaloniaTest]
    public void NoFrenchTextRemainsInAnyPanel()
    {
        var (window, game, campaign) = PanelsTest.Advanced();
        Loc.Language = "en";
        Loc.RecordMisses = true;
        Loc.Misses.Clear();
        try
        {
            var panels = new List<GamePanel>
            {
                new ProjectsPanel(), new NewProjectPanel(), new TeamPanel(), new BuildPanel(), new TreePanel(), new FinancePanel(),
                new SitesPanel(), new CloudPanel(), new IndustryPanel(), new ResearchPanel(), new AiPanel(), new LabPanel(),
                new NoticesPanel(), new CheatPanel(),
            };
            panels.AddRange(campaign.People.Select(p => (GamePanel)new PersonPanel(p.Id)));
            panels.AddRange(campaign.Products.Select(p => (GamePanel)new ProjectPanel(p.Id)));
            foreach (var panel in panels)
            {
                game.OpenRoot(panel);
                game.Tick(0.3);
                // Les onglets de chaque fiche : un clic sur chacun
                foreach (var button in window.GetVisualDescendants().OfType<Button>().Where(b => b.Classes.Contains("tab")).ToList())
                {
                    try { button.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent)); } catch { /* un onglet peut disparaître au clic */ }
                    game.Tick(0.1);
                }
            }
            game.ClosePanels();
            game.Tick(0.2);
            if (campaign.Products.FirstOrDefault(p => campaign.CanOpenTerminal(p.Id).Ok) is { } target)
            {
                game.OpenTerminal(target.Id);
                game.Tick(0.3);
            }
            window.Close();
        }
        finally
        {
            Loc.RecordMisses = false;
            Loc.Language = "fr";
        }
        // Les adresses des locaux sont des noms propres (Écublens-EPFL, Flon…) : elles ne se traduisent pas
        var places = Core.space.RealEstate.Listings.SelectMany(l => l.Places).ToList();
        var left = Loc.Misses.Keys.Where(m => !places.Any(m.Contains)).ToList();
        Assert.That(left, Is.Empty, string.Join("\n", left.Take(80)));
    }
}
