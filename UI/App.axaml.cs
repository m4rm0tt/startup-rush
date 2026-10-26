using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Core.simulation;

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
            GameSession session = new GameSession();
            desktop.MainWindow = new MainWindow(session);
        }

        base.OnFrameworkInitializationCompleted();
    }
}