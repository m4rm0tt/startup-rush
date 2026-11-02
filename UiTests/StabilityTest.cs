using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Core.campaign;
using Core.space;
using Core.team;
using UI.Game;
using UI.Hud;
using UI.Hud.Panels;
using UI.Pixel;

namespace UiTests;

/// <summary>
/// Régressions des retours de test (docs/BUGFIX_TRACKER.md) : les menus se mettent à jour sur place au lieu d'être
/// reconstruits (BUG-001 à 003, 005, 006), les fiches ne coupent plus leurs boutons (BUG-007), les vitesses ne bougent plus
/// (BUG-013), les cartons s'ouvrent au bon endroit (BUG-014), l'alerte de la porte disparaît (BUG-015).
/// </summary>
public class StabilityTest
{
    private static void Click(Button button) => button.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));

    private static void Pump()
    {
        Dispatcher.UIThread.RunJobs();
    }

    // ------------------------------------------------------------------ UiPatch, sur des contrôles simples

    [AvaloniaTest]
    public void APatchKeepsTheButtonAndRunsTheLatestAction()
    {
        int clicked = 0;
        Control Build(int n) => Ui.Column(Ui.Text($"Valeur {n}"), Ui.Button($"Ajouter {n}", () => clicked = n));
        var host = new ContentControl { Content = Build(1) };
        var window = new Window { Content = host };
        window.Show();
        Pump();
        var button = host.GetVisualDescendants().OfType<Button>().Single();

        UiPatch.Apply(host, Build(2));
        Pump();
        var after = host.GetVisualDescendants().OfType<Button>().Single();
        Assert.That(after, Is.SameAs(button), "le bouton sous la souris n'est pas recréé");
        Assert.That(host.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text), Does.Contain("Valeur 2"), "le texte suit l'état");
        Click(after);
        Assert.That(clicked, Is.EqualTo(2), "le bouton gardé exécute l'action de la dernière construction");
        window.Close();
    }

    [AvaloniaTest]
    public void APatchKeepsTheScrollPosition()
    {
        Control Build(string label)
        {
            var column = Ui.Column();
            for (int i = 0; i < 80; i++)
                column.Children.Add(Ui.Text($"{label} {i}"));
            return Ui.Scroll(column);
        }
        var host = new ContentControl { Content = Build("A") };
        var window = new Window { Width = 400, Height = 300, Content = host };
        window.Show();
        Pump();
        var scroll = (ScrollViewer)host.Content!;
        scroll.Offset = new Vector(0, 600);
        Pump();
        UiPatch.Apply(host, Build("B"));
        Pump();
        Assert.That(host.Content, Is.SameAs(scroll));
        Assert.That(scroll.Offset.Y, Is.EqualTo(600).Within(1), "la liste ne remonte pas en haut");
        window.Close();
    }

    [AvaloniaTest]
    public void APatchInsertsALineAtTheTopWithoutReplacingTheRest()
    {
        var host = new ContentControl { Content = Ui.Column(Ui.Button("Un", () => { }), Ui.Button("Deux", () => { })) };
        var window = new Window { Content = host };
        window.Show();
        Pump();
        var before = host.GetVisualDescendants().OfType<Button>().ToList();
        UiPatch.Apply(host, Ui.Column(Ui.Text("Nouveau"), Ui.Button("Un", () => { }), Ui.Button("Deux", () => { })));
        Pump();
        var after = host.GetVisualDescendants().OfType<Button>().ToList();
        Assert.That(after, Is.EqualTo(before), "les boutons existants sont gardés malgré la ligne ajoutée en haut");
        window.Close();
    }

    [AvaloniaTest]
    public void AProgressBarFollowsItsValueAfterAPatch()
    {
        var host = new ContentControl { Content = Ui.Bar(0.2, width: 200) };
        var window = new Window { Content = host };
        window.Show();
        Pump();
        UiPatch.Apply(host, Ui.Bar(0.7, width: 200));
        Pump();
        var fill = host.GetVisualDescendants().OfType<PixelPanel>().First(p => p.Skin.StartsWith("pack:hud_progress_fill"));
        Assert.That(fill.Bounds.Width, Is.EqualTo(0.7 * 198).Within(2), "le remplissage suit la nouvelle valeur");
        window.Close();
    }

    [AvaloniaTest]
    public void AStaleToggleTakesTheNewState()
    {
        bool? got = null;
        var host = new ContentControl { Content = Ui.CheckBox("Option", false, v => got = v) };
        var window = new Window { Content = host };
        window.Show();
        Pump();
        UiPatch.Apply(host, Ui.CheckBox("Option", true, v => got = v));
        Click((Button)host.Content!);
        Assert.That(got, Is.False, "une case cochée se décoche (l'action n'est plus celle de l'état d'avant)");
        window.Close();
    }

    [AvaloniaTest]
    public void ADoubleClickDoesNotGoThroughToAButtonThatJustAppeared()
    {
        int clicks = 0;
        var host = new ContentControl();
        var window = new Window { Width = 300, Height = 200, Content = host };
        window.Show();
        Pump();
        // Le premier clic fait apparaître un bouton sous la souris ; le second clic du double-clic arrive aussitôt
        host.Content = Ui.Button("Déplacer", () => clicks++);
        Pump();
        var button = (Button)host.Content;
        var center = button.TranslatePoint(new Point(button.Bounds.Width / 2, button.Bounds.Height / 2), window)!.Value;
        window.MouseDown(center, Avalonia.Input.MouseButton.Left);
        window.MouseUp(center, Avalonia.Input.MouseButton.Left);
        Pump();
        Assert.That(clicks, Is.EqualTo(0), "le second clic d'un double-clic n'actionne pas le bouton apparu entre-temps");
        Thread.Sleep(ClickGuard.ArmDelay + TimeSpan.FromMilliseconds(50));
        window.MouseDown(center, Avalonia.Input.MouseButton.Left);
        window.MouseUp(center, Avalonia.Input.MouseButton.Left);
        Pump();
        Assert.That(clicks, Is.EqualTo(1), "un clic voulu, ensuite, marche du premier coup");
        window.Close();
    }

    // ------------------------------------------------------------------ Le vrai écran de jeu

    [AvaloniaTest]
    public void TheBuildPanelKeepsItsControlsScrollAndCatalogAcrossRefreshes()
    {
        var (window, game, campaign) = PanelsTest.Advanced();
        game.OpenRoot(new BuildPanel());
        Pump();
        ScrollViewer Scroll() => window.GetVisualDescendants().OfType<ScrollViewer>().First(s => Equals(s.Tag, "panel-scroll"));
        var scroll = Scroll();
        var catalogButton = window.GetVisualDescendants().OfType<Button>().First(b => b.Classes.Contains("tab") && ToolTip.GetTip(b) is string tip && tip.Contains("Emprise"));
        scroll.Offset = new Vector(0, 400);
        Pump();
        double offset = scroll.Offset.Y;

        // Rafraîchissements automatiques, puis une action (choisir un meuble) : rien n'est recréé, la liste ne remonte pas
        for (int i = 0; i < 3; i++)
            game.RefreshPanel();
        Click(catalogButton);
        Pump();
        Assert.That(Scroll(), Is.SameAs(scroll), "la même liste défilante");
        Assert.That(Scroll().Offset.Y, Is.EqualTo(offset).Within(1), "la position de défilement est gardée après une action");
        Assert.That(TopLevel.GetTopLevel(catalogButton), Is.Not.Null, "le bouton cliqué est toujours affiché");
        Assert.That(catalogButton.Classes, Does.Contain("selected"), "et il montre l'objet choisi");
        Assert.That(game.Session.Tool, Is.Not.Null);
        // L'objet en cours de pose est affiché hors de la liste : elle ne saute pas
        Assert.That(window.GetVisualDescendants().OfType<Border>().First(b => Equals(b.Tag, "panel-sticky")).Child, Is.Not.Null);
        Click(catalogButton);
        Assert.That(game.Session.Tool, Is.Null, "un second clic repose l'outil (l'action suit l'état)");
        window.Close();
    }

    [AvaloniaTest]
    public void TheTopBarKeepsItsSpeedButtonsAndDoesNotMoveWithFastNights()
    {
        var (window, game, campaign) = PanelsTest.Advanced();
        Button Speed(string n) => window.GetVisualDescendants().OfType<Button>().First(b => ToolTip.GetTip(b) is string t && t.StartsWith($"Vitesse {n} "));
        var speed2 = Speed("2");
        var before = speed2.TranslatePoint(new Point(0, 0), window);
        game.Session.Settings.FastNights = true;
        for (int i = 0; i < 4; i++)
            game.Tick(0.3);
        // La nuit : personne au bureau, l'étiquette « Nuit accélérée » apparaît
        campaign.Advance(GameUtil.MinutesUntilHour(campaign, 23));
        game.Tick(0.3);
        Pump();
        Assert.That(Speed("2"), Is.SameAs(speed2), "le bouton de vitesse n'est pas recréé 4 fois par seconde");
        var after = Speed("2").TranslatePoint(new Point(0, 0), window);
        Assert.That(after!.Value.X, Is.EqualTo(before!.Value.X).Within(0.5), "les vitesses ne bougent pas quand l'étiquette apparaît");
        window.Close();
    }

    [AvaloniaTest]
    public void TheItemInspectorWrapsItsButtonsInsideItsFrame()
    {
        var (window, game, campaign) = PanelsTest.Advanced();
        var item = campaign.Premises.Floor.First(i => !i.Included && i.HostId == null && i.Def.Pack.Facings.Count > 1);
        game.Session.Select(SelectionKind.Item, item.Id);
        Pump();
        var frame = window.GetVisualDescendants().OfType<PixelPanel>().First(p => p.Tag is string t && t.StartsWith("Item:"));
        var frameBox = frame.Bounds;
        foreach (var button in frame.GetVisualDescendants().OfType<Button>())
        {
            var at = button.TranslatePoint(new Point(button.Bounds.Width, 0), frame)!.Value;
            Assert.That(at.X, Is.LessThanOrEqualTo(frameBox.Width - 8), $"le bouton {ToolTip.GetTip(button) ?? button.Content} reste dans la fiche");
        }
        window.Close();
    }

    [AvaloniaTest]
    public void OpenTheCartonsGoesToTheCartonsSection()
    {
        var (window, game, campaign) = PanelsTest.Advanced();
        var item = campaign.Premises.Floor.First(i => !i.Included && i.HostId == null);
        Assert.That(campaign.Store(item.Id).Ok, Is.True);
        game.OpenRoot(BuildPanel.AtCartons());
        Pump();
        var scroll = window.GetVisualDescendants().OfType<ScrollViewer>().First(s => Equals(s.Tag, "panel-scroll"));
        var section = window.GetVisualDescendants().OfType<Control>().First(c => Equals(c.Tag, BuildPanel.CartonsTag));
        var top = section.TranslatePoint(new Point(0, 0), scroll)!.Value.Y;
        Assert.That(top, Is.InRange(-10, 60), "la section des cartons est en haut de la zone visible");
        window.Close();
    }

    [AvaloniaTest]
    public void SomeoneWaitingAtTheDoorSitsAsSoonAsTheirPostIsBackEvenPaused()
    {
        var (window, game, campaign) = PanelsTest.Advanced();
        // En pleine journée de travail
        campaign.Advance(GameUtil.MinutesUntilHour(campaign, 10, workday: true));
        game.Tick(0.05);
        var person = campaign.Founder;
        campaign.SiteId = campaign.Sites.First(s => s.SeatOf(person.Id) != null).Id;
        Assert.That(person.Activity, Is.EqualTo(Activity.Working));
        var post = campaign.Premises.SeatOf(person.Id)!;
        var chair = campaign.Premises.ChairOf(post)!;
        Assert.That(campaign.Store(chair.Id).Ok, Is.True);
        game.Session.SetSpeed(1);
        game.Session.TogglePause();
        game.Tick(0.05);
        Assert.That(person.Activity, Is.EqualTo(Activity.Idle), "sans chaise, la personne attend à l'entrée");
        var stored = campaign.Storage.First(s => s.DefId == chair.DefId);
        var placed = chair.HostId is int desk ? campaign.AttachFromStorage(stored.Id, desk) : campaign.PlaceFromStorage(stored.Id, chair.X, chair.Y, chair.Facing);
        Assert.That(placed.Ok, Is.True, placed.Message);
        var again = campaign.Premises.ChairOf(campaign.Premises.Workstations.First(w => w.Desk.Id == post.Desk.Id))!;
        again.AssignedPersonId = person.Id;
        campaign.InvalidateLayout();
        game.Tick(0.05);
        Assert.That(game.Session.Running, Is.False, "le jeu est toujours en pause");
        Assert.That(person.Activity, Is.EqualTo(Activity.Working), "elle reprend son poste tout de suite : l'alerte de la porte disparaît");
        window.Close();
    }
}

internal static class GameUtil
{
    /// <summary>Minutes jusqu'à la prochaine heure donnée (un jour ouvré si demandé).</summary>
    public static long MinutesUntilHour(Campaign campaign, int hour, bool workday = false)
    {
        long m = campaign.Minute;
        long target = m + 1;
        while (GameTime.MinuteOfDay(target) != hour * 60 || workday && !GameTime.IsWorkday(target))
            target++;
        return target - m;
    }
}
