using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Core.persistence;
using UI.Audio;

namespace UI;

public partial class App : Application
{
    // Police pixel nette : tailles sur la grille de 12 px, avant la création du premier contrôle
    static App() => UI.Pixel.PixelText.Install();

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Réglages communs à toutes les campagnes ; au premier lancement, les volumes de l'ancien profil sont repris
            var settings = Settings.Load(Settings.DefaultPath, LegacyProfile.DefaultPath);
            if (settings.MigratedFromLegacy)
                settings.Save(Settings.DefaultPath);

            Sfx.Init();
            Sfx.MusicVolume = settings.MusicVolume;
            Sfx.SoundVolume = settings.SoundVolume;
            desktop.Exit += (_, _) => Sfx.Shutdown();

            var store = new SaveStore(Program.SaveRoot ?? SaveStore.DefaultRoot);
            var window = new MainWindow(store, settings, Program.SaveRoot != null ? System.IO.Path.Combine(Program.SaveRoot, "settings.json") : Settings.DefaultPath);
            desktop.MainWindow = window;
            if (Program.CaptureFolder != null)
                Capture.Run(window, Program.CaptureFolder);
        }

        base.OnFrameworkInitializationCompleted();
    }
}
