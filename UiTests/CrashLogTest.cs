using Avalonia.Headless.NUnit;
using Core.persistence;
using UI;

namespace UiTests;

/// <summary>Lot 3 : un plantage laisse un journal lisible et une sauvegarde d'urgence, sans toucher aux autres sauvegardes.</summary>
public class CrashLogTest
{
    [AvaloniaTest]
    public void ACrashWritesALogAndAnEmergencySaveWithoutTouchingOtherSaves()
    {
        var (window, game, campaign) = Shots.Fresh(tutorial: false);
        string logs = Path.Combine(Path.GetTempPath(), "startuprush_logs", Guid.NewGuid().ToString("N"));
        string previousFolder = CrashLog.Folder;
        var previousWindow = CrashLog.Window;
        try
        {
            CrashLog.Folder = logs;
            CrashLog.Window = window;
            game.Session.Save(SaveKind.Auto);
            string auto = game.Session.LastSavePath!;
            string before = File.ReadAllText(auto);
            campaign.AdvanceDays(2);

            string? log = CrashLog.Report(new InvalidOperationException("test de plantage"), "Test", fatal: true);

            Assert.That(log, Is.Not.Null);
            string text = File.ReadAllText(log!);
            Assert.That(text, Does.Contain("test de plantage").And.Contain("Partie : Shots").And.Contain("Sauvegarde d'urgence"));
            string crash = Path.Combine(window.Store.CampaignFolder(campaign.Id), "crash.json");
            Assert.That(window.Store.Load(crash).Minute, Is.EqualTo(campaign.Minute));
            Assert.That(File.ReadAllText(auto), Is.EqualTo(before), "la dernière sauvegarde valide n'est pas remplacée");
            Assert.That(window.Store.List(campaign.Id).Any(e => e.Label.StartsWith("Sauvegarde d'urgence")), Is.True);
        }
        finally
        {
            CrashLog.Folder = previousFolder;
            CrashLog.Window = previousWindow;
            window.Close();
        }
    }

    [AvaloniaTest]
    public void FullscreenAndLargeTextAreRemembered()
    {
        var (window, _, _) = Shots.Fresh(tutorial: false);
        window.SetFullscreen(true);
        Assert.That(window.WindowState, Is.EqualTo(Avalonia.Controls.WindowState.FullScreen));
        window.SetFullscreen(false);
        window.SetLargeText(true);
        try
        {
            var loaded = Settings.Load(window.SettingsPath, Path.Combine(Path.GetTempPath(), "none.json"));
            Assert.That(loaded.LargeText, Is.True);
            Assert.That(loaded.Fullscreen, Is.False);
            Assert.That(new Avalonia.Controls.TextBlock { FontSize = 13 }.FontSize, Is.EqualTo(24));
        }
        finally
        {
            window.SetLargeText(false);
            window.Close();
        }
        Assert.That(new Avalonia.Controls.TextBlock { FontSize = 13 }.FontSize, Is.EqualTo(18));
    }
}
