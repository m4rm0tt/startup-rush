using System.Diagnostics;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using UI.Audio;
using UI.Hud.Panels;
using UI.Iso;

namespace UiTests;

/// <summary>
/// Performance du rendu sur une campagne avancée (scène, HUD, panneau ouvert ; composition d'un grand local), et sons :
/// chaque bruitage et chaque musique se décode depuis le build et part quand on le demande. L'écoute réelle reste à faire à
/// la main (VALIDATION.md).
/// </summary>
public class PerformanceAndAudioTest
{
    [AvaloniaTest]
    public void AnAdvancedCampaignRendersAFrameQuickly()
    {
        var (window, game, campaign) = PanelsTest.Advanced();
        game.OpenRoot(new ProjectsPanel());
        Dispatcher.UIThread.RunJobs();
        window.CaptureRenderedFrame();
        const int frames = 20;
        var clock = Stopwatch.StartNew();
        for (int i = 0; i < frames; i++)
        {
            campaign.Advance(30);
            Dispatcher.UIThread.RunJobs();
            Assert.That(window.CaptureRenderedFrame(), Is.Not.Null);
        }
        double perFrame = clock.Elapsed.TotalMilliseconds / frames;
        TestContext.Out.WriteLine($"Fenêtre complète (rendu Skia sur le processeur) : {perFrame:0.0} ms par image");
        window.Close();
        // Le jeu redessine à la demande (scène recomposée quand le local change) : une image complète doit rester sous 250 ms
        Assert.That(perFrame, Is.LessThan(250));
    }

    [AvaloniaTest]
    public void TheLargestPremisesComposeInAFractionOfASecond()
    {
        var campaign = Core.campaign.Campaign.New("Perf", "Alex", "Studio", Core.campaign.FounderProfile.Generalist, 5);
        campaign.Ledger.Cash = 5_000_000;
        campaign.CheatUnlockAll();
        foreach (string listing in new[] { "hq", "datacenter", "factory" })
            Assert.That(campaign.RentSite(listing).Ok, Is.True, listing);
        foreach (var site in campaign.Sites)
        {
            RoomRenderer.Render(site, new Dictionary<int, Seated>());
            var clock = Stopwatch.StartNew();
            for (int i = 0; i < 5; i++)
                RoomRenderer.Render(site, new Dictionary<int, Seated>());
            double ms = clock.Elapsed.TotalMilliseconds / 5;
            TestContext.Out.WriteLine($"{site.ListingId} ({site.Width} × {site.Height}, {site.Items.Count} objets) : {ms:0} ms par composition");
            Assert.That(ms, Is.LessThan(1_000), site.ListingId);
        }
    }

    [AvaloniaTest]
    public void EverySoundAndTrackLoadsFromTheBuildAndPlaysOnDemand()
    {
        Sfx.Init();
        try
        {
            Assert.That(Sfx.LoadedSounds, Is.EqualTo(Enum.GetValues<Sound>().Length), "un fichier par bruitage");
            Assert.That(Sfx.LoadedTracks, Is.EqualTo(4), "menu, deux thèmes de travail, crise");
            lock (Sfx.Played)
                Sfx.Played.Clear();
            Sfx.Play(Sound.Coin);
            Sfx.Play(Sound.Launch);
            lock (Sfx.Played)
            {
                // Sans carte son (machine de test), le jeu reste silencieux : on ne juge alors que le chargement
                if (Sfx.Played.Count == 0)
                    Assert.Inconclusive("Aucune sortie audio sur cette machine : chargement vérifié, déclenchement non vérifiable.");
                Assert.That(Sfx.Played, Is.EqualTo(new[] { Sound.Coin, Sound.Launch }));
            }
        }
        finally
        {
            Sfx.Shutdown();
        }
    }
}
