using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Core.campaign;
using Core.persistence;
using Core.products;
using UI;
using UI.Hud;
using UI.Hud.Panels;
using UI.Pixel;

namespace UiTests;

/// <summary>
/// Accessibilité vérifiable sans écran : contraste réel des textes affichés (WCAG AA), raccourcis clavier des menus, Échap
/// qui ne quitte jamais la partie, et saisie simplifiée du terminal habillé par le pack.
/// </summary>
public class AccessibilityTest
{
    private static double Contrast(Color a, Color b) => Palette.Contrast(a, b);

    /// <summary>
    /// Le fond sur lequel un texte est posé : le premier ancêtre qui a un fond opaque d'une seule couleur, ou un cadre du pack
    /// (son fond imposé, sinon le pixel de son image 9-slice sous le centre du texte).
    /// </summary>
    private static Color? BackgroundOf(Visual visual)
    {
        foreach (var ancestor in visual.GetVisualAncestors())
        {
            if (ancestor is PixelPanel frame && frame.Skin != "none")
            {
                if (frame.Background is ISolidColorBrush { Color.A: 255 } fill)
                    return fill.Color;
                var center = visual.TranslatePoint(new Point(visual.Bounds.Width / 2, visual.Bounds.Height / 2), frame);
                return center is { } point ? PixelPanel.ColorAt(frame.Skin, frame.Bounds.Size, point, frame.Scale) : null;
            }
            var brush = ancestor switch
            {
                Border border => border.Background,
                Panel panel => panel.Background,
                TemplatedControl control => control.Background,
                _ => null,
            };
            if (brush is ISolidColorBrush { Color.A: 255 } solid)
                return solid.Color;
            // Un dégradé ou une image : la couleur n'est pas connue ici, le texte n'est pas jugé
            if (brush != null && brush is not ISolidColorBrush)
                return null;
        }
        return null;
    }

    [AvaloniaTest]
    public void TheAccentTextTonesReadOnEveryPaper()
    {
        var papers = new[] { Palette.Paper, Palette.Night, Palette.Slate }.Select(b => ((ISolidColorBrush)b).Color).ToList();
        foreach (var accent in new[] { Palette.Gold, Palette.Ember, Palette.Leaf, Palette.Sky, Palette.Ice, Palette.Fog, Palette.Blood, Palette.Lemon, Palette.Cloud, Palette.Snow })
        {
            var tone = ((ISolidColorBrush)Palette.ForText(accent)).Color;
            foreach (var paper in papers)
                Assert.That(Contrast(tone, paper), Is.GreaterThanOrEqualTo(4.5), $"{tone} sur {paper}");
        }
        Assert.That(Palette.ForText(Palette.Gold), Is.Not.SameAs(Palette.Gold), "l'or des barres reste vif, celui des textes est foncé");
    }

    /// <summary>
    /// Chaque texte de chaque panneau d'une campagne avancée, mesuré contre son vrai fond : 4,5:1 au moins (3:1 pour un texte
    /// large, 18 px et plus, ou 14 px en gras). Les textes posés sur un cadre image du pack ne sont pas jugés ici.
    /// </summary>
    [AvaloniaTest]
    public void EveryTextOfEveryPanelHasEnoughContrastWithItsBackground()
    {
        var (window, game, campaign) = PanelsTest.Advanced();
        var panels = new List<GamePanel>
        {
            new ProjectsPanel(), new NewProjectPanel(), new TeamPanel(), new BuildPanel(), new TreePanel(), new FinancePanel(),
            new SitesPanel(), new CloudPanel(), new IndustryPanel(), new ResearchPanel(), new AiPanel(), new LabPanel(), new NoticesPanel(),
        };
        panels.AddRange(campaign.Products.Select(p => (GamePanel)new ProjectPanel(p.Id)));
        var failures = new List<string>();
        int judged = 0;
        foreach (var panel in panels)
        {
            game.OpenRoot(panel);
            Dispatcher.UIThread.RunJobs();
            foreach (var text in window.GetVisualDescendants().OfType<TextBlock>().Where(t => t.IsEffectivelyVisible && !string.IsNullOrWhiteSpace(t.Text)))
            {
                if (text.Foreground is not ISolidColorBrush { Color.A: 255 } fg || BackgroundOf(text) is not { } bg)
                    continue;
                judged++;
                bool large = text.FontSize >= 18 || text.FontSize >= 14 && text.FontWeight >= FontWeight.Bold;
                double ratio = Contrast(fg.Color, bg);
                if (ratio < (large ? 3 : 4.5))
                    failures.Add($"{panel.GetType().Name} « {text.Text} » {fg.Color} sur {bg} : {ratio:0.00}");
            }
        }
        window.Close();
        Assert.That(judged, Is.GreaterThan(200), "assez de textes mesurés pour que le test dise quelque chose");
        foreach (var failure in failures.Distinct())
            TestContext.Out.WriteLine(failure);
        Assert.That(failures.Distinct().ToList(), Is.Empty);
    }

    [AvaloniaTest]
    public void EveryMenuShortcutOpensItsPanelAndEscapeClosesItWithoutLeavingTheGame()
    {
        var (window, game, campaign) = PanelsTest.Advanced();
        var shortcuts = new (Key Key, Type Panel)[]
        {
            (Key.P, typeof(ProjectsPanel)), (Key.T, typeof(TeamPanel)), (Key.B, typeof(BuildPanel)), (Key.D, typeof(TreePanel)),
            (Key.F, typeof(FinancePanel)), (Key.N, typeof(NoticesPanel)),
        };
        foreach (var (key, panel) in shortcuts)
        {
            Assert.That(game.HandleKey(key, KeyModifiers.None), Is.True, key.ToString());
            Dispatcher.UIThread.RunJobs();
            Assert.That(game.TopPanel?.GetType(), Is.EqualTo(panel), $"{key} ouvre {panel.Name}");
            Assert.That(game.HandleKey(Key.Escape, KeyModifiers.None), Is.True);
            Dispatcher.UIThread.RunJobs();
            Assert.That(game.TopPanel, Is.Null, $"Échap ferme {panel.Name}");
        }
        // Rien d'ouvert : Échap ouvre la pause, jamais la sortie
        Assert.That(game.HandleKey(Key.Escape, KeyModifiers.None), Is.True);
        Dispatcher.UIThread.RunJobs();
        Assert.That(game.PauseMenuOpen, Is.True);
        Assert.That(campaign.Status, Is.EqualTo(CampaignStatus.Running));
        Assert.That(window.IsVisible, Is.True);
        window.Close();
    }

    [AvaloniaTest]
    public void SimplifiedTypingWritesTheWordAndEnterValidatesOnlyAWholeWord()
    {
        string root = Path.Combine(Path.GetTempPath(), "startuprush_uitests", Guid.NewGuid().ToString("N"));
        var window = new MainWindow(new SaveStore(root), new Settings { SimplifiedTyping = true }, Path.Combine(root, "settings.json"));
        var campaign = Campaign.New("Saisie", "Alex", "Studio Test", FounderProfile.Generalist, 3);
        campaign.CreateProject("Appli", "freelances", RevenueModel.Subscription);
        window.Show();
        var game = window.StartGame(campaign, null);
        Dispatcher.UIThread.RunJobs();

        Assert.That(game.HandleKey(Key.Enter, KeyModifiers.None), Is.True, "Entrée ouvre le terminal");
        Dispatcher.UIThread.RunJobs();
        var terminal = game.OpenTerminalView;
        Assert.That(terminal, Is.Not.Null);
        var texts = window.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text ?? "").ToList();
        Assert.That(texts, Has.Some.Contains("Saisie simplifiée"), "le mode est annoncé dans le terminal");

        string word = campaign.Terminal!.Word;
        var input = window.GetVisualDescendants().OfType<TextBox>().First(t => t.IsEffectivelyVisible);
        // Un mot à moitié écrit ne se valide pas
        var clock = System.Diagnostics.Stopwatch.StartNew();
        while (string.IsNullOrEmpty(input.Text) && clock.Elapsed.TotalSeconds < 5)
        {
            terminal!.Tick();
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(20);
        }
        if (input.Text!.Length < word.Length)
        {
            terminal!.Submit();
            Assert.That(campaign.Terminal!.Word, Is.EqualTo(word), "un mot incomplet n'est pas validé");
        }
        // Le mot s'écrit tout seul au rythme réglé, puis Entrée le valide
        while (input.Text != word && clock.Elapsed.TotalSeconds < 15)
        {
            terminal!.Tick();
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(20);
        }
        Assert.That(input.Text, Is.EqualTo(word), "le mot s'est écrit entièrement");
        double before = campaign.Terminal!.Points;
        // Entrée arrive au champ du terminal (c'est lui qui a le focus pendant la saisie)
        input.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter, Source = input });
        Dispatcher.UIThread.RunJobs();
        Assert.That(campaign.Terminal!.Points, Is.GreaterThan(before), "le mot validé fait avancer le travail");
        window.Close();
    }
}
