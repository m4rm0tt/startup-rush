using System.Diagnostics;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using UI.Hud.Panels;

namespace UiTests;

/// <summary>
/// Sonde de performance (lot 3, sur demande) : où passe le temps d'une image et d'une journée de simulation. Mesuré sur le VPS
/// (rendu Skia sur le processeur, sans carte graphique) le 3 novembre 2026 : simulation ~1 ms par image, 5 jours ~85 ms ;
/// l'interface seule ~12 ms ; une image complète ~500 à 700 ms.
///
/// Mesures du 4 novembre 2026 (même VPS, voir docs/equilibrage/ et la checklist) : une fenêtre Avalonia vide ou la vraie
/// MainWindow vide, dont un rectangle plein écran change à chaque image, coûtent 46 à 49 ms par image capturée ; Skia seul
/// remplit 1600 × 900 en 0,5 ms. Dans l'écran de jeu, dès que la scène (SceneCanvas) dessine quoi que ce soit, même un
/// rectangle de 50 × 50, l'image coûte 500 à 700 ms, et 8 à 20 ms quand elle ne dessine rien : le coût n'est donc ni la
/// simulation, ni la barre d'interface, ni le dessin de la pièce, et il n'est pas expliqué par le seul processeur de la
/// machine. Cause non trouvée sans profileur : à mesurer sur Windows avec carte graphique, puis avec un profileur.
/// </summary>
[Explicit]
public class PerfProbe
{
    [AvaloniaTest]
    public void WhereDoesAFrameGo()
    {
        var (window, game, campaign) = PanelsTest.Advanced();
        game.OpenRoot(new ProjectsPanel());
        Dispatcher.UIThread.RunJobs();
        window.CaptureRenderedFrame();
        double advance = 0, tick = 0, capture = 0;
        for (int i = 0; i < 20; i++)
        {
            var c = Stopwatch.StartNew();
            campaign.Advance(30);
            advance += c.Elapsed.TotalMilliseconds; c.Restart();
            game.Tick(0.016);
            Dispatcher.UIThread.RunJobs();
            tick += c.Elapsed.TotalMilliseconds; c.Restart();
            window.CaptureRenderedFrame();
            capture += c.Elapsed.TotalMilliseconds;
        }
        TestContext.Out.WriteLine($"simulation {advance / 20:0.0} ms · interface {tick / 20:0.0} ms · rendu {capture / 20:0.0} ms par image");
        game.Scene.IsVisible = false;
        Dispatcher.UIThread.RunJobs();
        var s = Stopwatch.StartNew();
        for (int i = 0; i < 10; i++) { game.Tick(0.016); Dispatcher.UIThread.RunJobs(); window.CaptureRenderedFrame(); }
        TestContext.Out.WriteLine($"interface sans la scène : {s.Elapsed.TotalMilliseconds / 10:0.0} ms par image");
        game.Scene.IsVisible = true;
        var days = Stopwatch.StartNew();
        campaign.AdvanceDays(5);
        TestContext.Out.WriteLine($"5 jours de simulation : {days.Elapsed.TotalMilliseconds:0} ms");
        window.Close();
    }

    /// <summary>Quel morceau de l'écran de jeu coûte l'image : on masque la scène, la barre du haut, la barre d'outils, le panneau.</summary>
    [AvaloniaTest]
    public void WhichPartOfTheGameScreenCostsTheFrame()
    {
        var (window, game, campaign) = PanelsTest.Advanced();
        window.Width = 1600;
        window.Height = 900;
        Dispatcher.UIThread.RunJobs();
        double Frame(string label)
        {
            window.CaptureRenderedFrame();
            var c = Stopwatch.StartNew();
            double jobs = 0, capture = 0;
            for (int i = 0; i < 8; i++)
            {
                var t = Stopwatch.StartNew();
                Dispatcher.UIThread.RunJobs();
                jobs += t.Elapsed.TotalMilliseconds; t.Restart();
                window.CaptureRenderedFrame();
                capture += t.Elapsed.TotalMilliseconds;
            }
            double ms = c.Elapsed.TotalMilliseconds / 8;
            TestContext.Out.WriteLine($"{label}: {ms:0.0} ms par image (RunJobs {jobs / 8:0.0}, capture {capture / 8:0.0})");
            return ms;
        }
        Frame("tout (scène seule, aucun panneau)");
        // Qui d'autre que la scène est dans l'arbre ? On masque chaque frère de la scène, un par un.
        var parent = (Avalonia.Controls.Panel)game.Scene.GetVisualParent()!;
        TestContext.Out.WriteLine($"Frères de la scène : {string.Join(", ", parent.Children.Select(c => c.GetType().Name))}");
        foreach (var sibling in parent.Children.Where(c => !ReferenceEquals(c, game.Scene)).ToList())
        {
            sibling.IsVisible = false;
            Frame($"scène + frères masqués jusqu'à {sibling.GetType().Name}");
        }
        var hudBars = window.GetVisualDescendants().OfType<UI.Pixel.PixelPanel>().ToList();
        foreach (var panel in hudBars) panel.IsVisible = false;
        Frame("scène + fenêtre, tous les PixelPanel masqués");
        foreach (var panel in hudBars) panel.IsVisible = true;
        game.OpenRoot(new ProjectsPanel());
        Dispatcher.UIThread.RunJobs();
        Frame("avec le panneau Projets");
        game.Scene.IsVisible = false;
        Frame("sans la scène");
        var hud = window.GetVisualDescendants().OfType<Avalonia.Controls.Control>().Where(c => c.GetType().Name == "ContentControl" || c.GetType().Name == "PixelPanel").ToList();
        foreach (var panel in window.GetVisualDescendants().OfType<UI.Pixel.PixelPanel>().ToList())
            panel.IsVisible = false;
        Frame("sans aucun PixelPanel (cadres du pack)");
        window.Close();
    }

    /// <summary>Débit brut de Skia sur cette machine : remplir une surface 1600 × 900 au processeur, sans Avalonia.</summary>
    [Test]
    public void RawSkiaFillSpeed()
    {
        using var surface = SkiaSharp.SKSurface.Create(new SkiaSharp.SKImageInfo(1600, 900, SkiaSharp.SKColorType.Bgra8888, SkiaSharp.SKAlphaType.Premul));
        using var paint = new SkiaSharp.SKPaint { Color = new SkiaSharp.SKColor(0x2b, 0x21, 0x28), IsAntialias = false };
        surface.Canvas.DrawRect(0, 0, 1600, 900, paint);
        var c = Stopwatch.StartNew();
        for (int i = 0; i < 50; i++)
            surface.Canvas.DrawRect(0, 0, 1600, 900, paint);
        TestContext.Out.WriteLine($"SKIA fill 1600x900 : {c.Elapsed.TotalMilliseconds / 50:0.00} ms");
        using var bitmap = new SkiaSharp.SKBitmap(656, 500);
        bitmap.Erase(SkiaSharp.SKColors.Red);
        using var image = SkiaSharp.SKImage.FromBitmap(bitmap);
        c.Restart();
        for (int i = 0; i < 50; i++)
            surface.Canvas.DrawImage(image, new SkiaSharp.SKRect(0, 0, 1312, 1000), new SkiaSharp.SKSamplingOptions(SkiaSharp.SKFilterMode.Nearest));
        TestContext.Out.WriteLine($"SKIA image 656x500 x2 : {c.Elapsed.TotalMilliseconds / 50:0.00} ms");
    }

    /// <summary>
    /// Une fenêtre Avalonia sans rien du jeu, dont un rectangle change de couleur à chaque image : si une image complète coûte
    /// autant qu'une image de la scène, le coût est celui du rendu sans carte graphique de cette machine, pas du code du jeu.
    /// </summary>
    [AvaloniaTest]
    public void AnEmptyAvaloniaWindowThatChangesEveryFrame()
    {
        // Le rectangle qui change couvre toute la fenêtre (comme la scène) : c'est la surface à redessiner qui compte
        var box = new Avalonia.Controls.Border { Background = Avalonia.Media.Brushes.Black };
        var window = new Avalonia.Controls.Window { Width = 1600, Height = 900, Content = new Avalonia.Controls.Grid { Background = Avalonia.Media.Brushes.DimGray, Children = { box } } };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.CaptureRenderedFrame();
        var c = Stopwatch.StartNew();
        for (int i = 0; i < 10; i++)
        {
            box.Background = i % 2 == 0 ? Avalonia.Media.Brushes.Red : Avalonia.Media.Brushes.Blue;
            Dispatcher.UIThread.RunJobs();
            window.CaptureRenderedFrame();
        }
        TestContext.Out.WriteLine($"AVALONIA fenêtre vide 1600x900, un rectangle plein écran change : {c.Elapsed.TotalMilliseconds / 10:0.0} ms par image");
        window.Close();
    }

    /// <summary>La même fenêtre vide, mais dans la vraie fenêtre du jeu (MainWindow) : isole ce que la fenêtre elle-même coûte.</summary>
    [AvaloniaTest]
    public void AnEmptyGameWindowThatChangesEveryFrame()
    {
        string root = Path.Combine(Path.GetTempPath(), "startuprush_uitests", Guid.NewGuid().ToString("N"));
        var window = new UI.MainWindow(new Core.persistence.SaveStore(root), new Core.persistence.Settings(), Path.Combine(root, "settings.json")) { Width = 1600, Height = 900 };
        window.Show();
        var box = new Avalonia.Controls.Border { Background = Avalonia.Media.Brushes.Black, Width = 60, Height = 60, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top };
        window.Content = new Avalonia.Controls.Grid { Background = Avalonia.Media.Brushes.DimGray, Children = { box } };
        Dispatcher.UIThread.RunJobs();
        window.CaptureRenderedFrame();
        var c = Stopwatch.StartNew();
        for (int i = 0; i < 10; i++)
        {
            box.Background = i % 2 == 0 ? Avalonia.Media.Brushes.Red : Avalonia.Media.Brushes.Blue;
            Dispatcher.UIThread.RunJobs();
            window.CaptureRenderedFrame();
        }
        TestContext.Out.WriteLine($"MAINWINDOW vide 1600x900, un rectangle change : {c.Elapsed.TotalMilliseconds / 10:0.0} ms par image");
        window.Close();
    }
}
