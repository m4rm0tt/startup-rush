using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using UI.Hud;
using UI.Iso;
using UI.Pixel;

namespace UiTests;

/// <summary>
/// Les composants HUD du pack branchés pour la version complète, testés sur de vrais contrôles : l'action d'un clic, l'état
/// affiché (cadre du pack et son état), la valeur rendue au code appelant.
/// </summary>
public class HudKitTest
{
    private static Window Host(Control content)
    {
        var window = new Window { Width = 800, Height = 600, Content = content };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    private static IEnumerable<PixelPanel> Panels(Visual root) => root.GetVisualDescendants().OfType<PixelPanel>();

    private static void Click(Button button) => button.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));

    [AvaloniaTest]
    public void TheDropdownShowsTheChoiceOpensThePackListAndReturnsThePick()
    {
        string picked = "";
        var dropdown = (Button)HudKit.Dropdown([("a", "Atelier"), ("b", "Bureau")], "a", v => picked = v);
        var window = Host(dropdown);
        PixelPanel Frame() => Panels(dropdown).First(p => p.Skin.StartsWith("pack:hud_dropdown"));
        Assert.That(Frame().Skin, Is.EqualTo("pack:hud_dropdown:normal"));
        Assert.That(dropdown.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text), Does.Contain("Atelier"));
        var flyout = (Flyout)dropdown.Flyout!;
        flyout.ShowAt(dropdown);
        Dispatcher.UIThread.RunJobs();
        Assert.That(Frame().Skin, Is.EqualTo("pack:hud_dropdown:open"));
        var list = (PixelPanel)flyout.Content!;
        Assert.That(list.Skin, Is.EqualTo("pack:hud_dropdown_list"));
        var bureau = list.GetLogicalDescendants().OfType<Button>().First(b => b.GetLogicalDescendants().OfType<TextBlock>().Any(t => t.Text == "Bureau"));
        Click(bureau);
        Dispatcher.UIThread.RunJobs();
        Assert.That(picked, Is.EqualTo("b"));
        window.Close();
    }

    [AvaloniaTest]
    public void TheNumericFieldStepsWithinItsBoundsWithThePackArrows()
    {
        int value = 20;
        var field = HudKit.Numeric(value, 1, 25, 5, v => value = v);
        var window = Host(field);
        Assert.That(((PixelPanel)field).Skin, Is.EqualTo("pack:hud_field_numeric:normal"));
        var up = field.GetVisualDescendants().OfType<Button>().First(b => (string?)b.Tag == "hud_stepper_up");
        var down = field.GetVisualDescendants().OfType<Button>().First(b => (string?)b.Tag == "hud_stepper_down");
        Click(up);
        Assert.That(value, Is.EqualTo(25), "borné au maximum");
        Click(down);
        Assert.That(value, Is.EqualTo(15));
        var top = HudKit.Numeric(25, 1, 25, 5, _ => { });
        Host(top);
        Assert.That(top.GetVisualDescendants().OfType<Button>().First(b => (string?)b.Tag == "hud_stepper_up").IsEnabled, Is.False, "flèche désactivée au maximum");
        window.Close();
    }

    [AvaloniaTest]
    public void TheMultilineFieldAcceptsSeveralLinesAndReportsTheText()
    {
        string text = "";
        var field = HudKit.Multiline("", v => text = v);
        var window = Host(field);
        var box = field.GetVisualDescendants().OfType<TextBox>().Single();
        Assert.That(box.AcceptsReturn, Is.True);
        box.Text = "ligne 1\nligne 2";
        Dispatcher.UIThread.RunJobs();
        Assert.That(text, Is.EqualTo("ligne 1\nligne 2"));
        Assert.That(((PixelPanel)field).Skin, Does.StartWith("pack:hud_field_multiline"));
        window.Close();
    }

    [AvaloniaTest]
    public void TheMenuRunsAnEntryOnceSkipsDisabledOnesAndShowsSeparators()
    {
        int opened = 0, blocked = 0;
        var flyout = HudKit.Menu([new("Ouvrir", () => opened++), null, new("Interdit", () => blocked++, false)]);
        var anchor = new Button { Content = "…" };
        var window = Host(anchor);
        flyout.ShowAt(anchor);
        Dispatcher.UIThread.RunJobs();
        var panel = (PixelPanel)flyout.Content!;
        Assert.That(panel.Skin, Is.EqualTo("pack:hud_menu_panel"));
        var items = panel.GetLogicalDescendants().OfType<Button>().ToList();
        Assert.That(items.Select(i => i.Tag), Is.EqualTo(new[] { "Ouvrir", "Interdit" }));
        Assert.That(panel.GetLogicalDescendants().OfType<PixelPanel>().Count(p => p.Skin == "pack:hud_menu_separator"), Is.EqualTo(1));
        Assert.That(items[1].IsEnabled, Is.False);
        Click(items[0]);
        Assert.That(opened, Is.EqualTo(1));
        Assert.That(blocked, Is.EqualTo(0));
        Assert.That(panel.GetLogicalDescendants().OfType<PixelPanel>().First(p => p.Skin.StartsWith("pack:hud_menu_item")).Skin, Is.EqualTo("pack:hud_menu_item:normal"));
        window.Close();
    }

    [AvaloniaTest]
    public void ARightClickOpensTheContextMenuOfAControl()
    {
        var card = new Border { Width = 200, Height = 100, Background = Brushes.White };
        bool asked = false;
        HudKit.WithContextMenu(card, () =>
        {
            asked = true;
            return [new HudKit.MenuEntry("Ouvrir", () => { })];
        });
        var window = Host(card);
        window.MouseDown(card.TranslatePoint(new Point(50, 50), window)!.Value, MouseButton.Right);
        Dispatcher.UIThread.RunJobs();
        Assert.That(asked, Is.True);
        window.Close();
    }

    [AvaloniaTest]
    public void TheGaugeLightsOneSegmentPerTenth()
    {
        var empty = HudKit.Gauge(0);
        var half = HudKit.Gauge(0.5);
        var full = HudKit.Gauge(1.2);
        Assert.That(empty.Tag, Is.EqualTo("gauge:0"));
        Assert.That(half.Tag, Is.EqualTo("gauge:5"));
        Assert.That(full.Tag, Is.EqualTo("gauge:10"), "plafonnée à 10 segments");
        Assert.That(half.Source, Is.Not.SameAs(full.Source), "chaque niveau a sa propre image composée");
    }

    [AvaloniaTest]
    public void BannerChipSeparatorsSecondaryPanelCardStripedBarAndBarsUseTheirPackFrames()
    {
        var card = HudKit.Card(new TextBlock { Text = "Carte" });
        var column = new StackPanel
        {
            Children =
            {
                HudKit.Banner("Alerte"), HudKit.Chip("Jeu"), HudKit.SeparatorH(), new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Children = { new TextBlock { Text = "a" }, HudKit.SeparatorV(), new TextBlock { Text = "b" } } },
                HudKit.Secondary(new TextBlock { Text = "Détail" }), card, HudKit.Striped(0.4),
                HudKit.Bars([("Atelier", 1_200m), ("Siège", -400m)]),
            },
        };
        var window = Host(column);
        var skins = Panels(column).Select(p => p.Skin).ToList();
        Assert.That(skins, Does.Contain("pack:hud_banner").And.Contain("pack:hud_chip_dark").And.Contain("pack:hud_separator_h").And.Contain("pack:hud_separator_v")
            .And.Contain("pack:hud_panel_secondary").And.Contain("pack:hud_card:normal").And.Contain("pack:hud_progress_fill_striped"));
        Assert.That(column.GetVisualDescendants().OfType<Image>().Count(i => i.Source == PackSkins.Extra("hud_banner", "banner_end_left")), Is.EqualTo(1), "pointes du bandeau");
        // Les barres : hauteur proportionnelle à la valeur réelle, vers le haut ou vers le bas
        var bars = column.GetVisualDescendants().OfType<Image>().Where(i => (i.Tag as string)?.StartsWith("bar:") == true).ToList();
        Assert.That(bars.Count, Is.EqualTo(2));
        Assert.That(bars[0].Height, Is.EqualTo(3 * bars[1].Height).Within(0.01));
        Assert.That(bars[0].VerticalAlignment, Is.EqualTo(Avalonia.Layout.VerticalAlignment.Bottom));
        Assert.That(bars[1].VerticalAlignment, Is.EqualTo(Avalonia.Layout.VerticalAlignment.Top));
        // La carte réagit au survol
        window.MouseMove(card.TranslatePoint(new Point(5, 5), window)!.Value);
        Dispatcher.UIThread.RunJobs();
        Assert.That(card.Skin, Is.EqualTo("pack:hud_card:hover"));
        Assert.That(HudKit.Card(new TextBlock(), enabled: false).Skin, Is.EqualTo("pack:hud_card:disabled"));
        window.Close();
    }

    [AvaloniaTest]
    public void AWideTableScrollsHorizontallyWithThePackScrollbar()
    {
        var scroll = HudKit.Horizontal(new Border { Width = 2_000, Height = 40 });
        var window = Host(scroll);
        window.Width = 400;
        Dispatcher.UIThread.RunJobs();
        Assert.That(scroll.Extent.Width, Is.GreaterThan(scroll.Viewport.Width));
        var bar = scroll.GetVisualDescendants().OfType<ScrollBar>().First(b => b.Orientation == Avalonia.Layout.Orientation.Horizontal);
        Assert.That(bar.IsVisible, Is.True);
        Assert.That(bar.GetVisualDescendants().OfType<PixelPanel>().Select(p => p.Skin), Has.Some.StartsWith("pack:hud_scroll_track_h"));
        scroll.Offset = new Vector(500, 0);
        Assert.That(scroll.Offset.X, Is.EqualTo(500));
        window.Close();
    }

    [AvaloniaTest]
    public void ThePaletteIsThePapierThemeOfThePack()
    {
        var tokens = PackLibrary.Asset("hud_theme_papier").GetProperty("tokens");
        string Hex(IBrush brush) => ((ISolidColorBrush)brush).Color.ToString().ToLowerInvariant().Replace("#ff", "#");
        Assert.That(Hex(Palette.Ink), Is.EqualTo(tokens.GetProperty("ink").GetString()));
        Assert.That(Hex(Palette.Paper), Is.EqualTo(tokens.GetProperty("paper").GetString()));
        Assert.That(Hex(Palette.Fog), Is.EqualTo(tokens.GetProperty("text_muted").GetString()));
        Assert.That(Hex(Palette.Cloud), Is.EqualTo(tokens.GetProperty("ink_soft").GetString()));
        Assert.That(Hex(Palette.Leaf), Is.EqualTo(tokens.GetProperty("success_lo").GetString()));
        Assert.That(Hex(Palette.Ember), Is.EqualTo(tokens.GetProperty("coral_lo").GetString()));
        Assert.That(Hex(Palette.Gold), Is.EqualTo(tokens.GetProperty("sun_lo").GetString()));
        Assert.That(Hex(Palette.Lemon), Is.EqualTo(tokens.GetProperty("teal_lo").GetString()));
    }
}
