using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Core.meta;
using Core.states;
using UI.Audio;

namespace UI;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Le profil (lingots, déblocages, records) est sauvegardé dans le dossier de l'utilisateur
            var store = new ProfileStore(ProfileStore.DefaultPath);
            var machine = new GameStateMachine(profile: store.Load(), saveProfile: store.Save);

            Sfx.Init();
            desktop.Exit += (_, _) => Sfx.Shutdown();
            desktop.MainWindow = new MainWindow(machine);
        }

        base.OnFrameworkInitializationCompleted();
    }
}
