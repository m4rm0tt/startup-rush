using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Core.campaign;
using Core.persistence;
using UI;

namespace UiTests;

/// <summary>Lot 6 : les écrans traduits, en anglais, pour vérifier à l'œil les débordements de texte (sur demande).</summary>
[Explicit, Category("Shots")]
public class EnglishShots
{
    [AvaloniaTest]
    public void MenusInEnglish()
    {
        string root = Path.Combine(Path.GetTempPath(), "startuprush_uitests", Guid.NewGuid().ToString("N"), "campaigns");
        var store = new SaveStore(root);
        var a = Campaign.New("Game A", "Alex", "Lakeside Studio", FounderProfile.Generalist, 1);
        store.Save(a, SaveKind.Manual, "Start");
        store.Save(a, SaveKind.Auto);
        var window = new MainWindow(store, new Settings(), Path.Combine(root, "settings.json")) { Width = 1600, Height = 900 };
        window.Show();
        window.SetLanguage("en");
        try
        {
            window.ShowTitle();
            Shots.Save(window, "en_title");
            window.ShowNewCampaign();
            Shots.Save(window, "en_new");
            window.ShowLoad();
            Shots.Save(window, "en_load");
            window.ShowSettings(() => window.ShowTitle());
            Shots.Save(window, "en_settings");
            var game = window.StartGame(a, null);
            Dispatcher.UIThread.RunJobs();
            game.Tick(0.3);
            Shots.Save(window, "en_game");
            game.HandleKey(Avalonia.Input.Key.Escape, Avalonia.Input.KeyModifiers.None);
            game.Tick(0.3);
            Shots.Save(window, "en_pause");
        }
        finally
        {
            window.SetLanguage("fr");
            window.Close();
        }
    }
}
