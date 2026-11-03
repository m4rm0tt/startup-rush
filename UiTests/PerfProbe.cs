using System.Diagnostics;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using UI.Hud.Panels;

namespace UiTests;

/// <summary>
/// Sonde de performance (lot 3, sur demande) : où passe le temps d'une image et d'une journée de simulation. Mesuré sur le VPS
/// (rendu Skia sur le processeur, sans carte graphique) le 3 novembre 2026 : simulation ~1 ms par image, 5 jours ~85 ms ;
/// l'interface seule ~12 ms ; une image complète ~500 ms, quel que soit le contenu de la scène (même vide) : c'est le
/// redessin de toute la fenêtre au processeur, pas un calcul du jeu. À mesurer sur un PC Windows avec carte graphique.
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
}
