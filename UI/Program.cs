using Avalonia;
using System;

namespace UI;

class Program
{
    /// <summary>
    /// --capture &lt;dossier&gt; : joue un parcours scripté et enregistre des captures réelles de l'interface (voir Capture.cs).
    /// Les sauvegardes vont alors dans un dossier temporaire à part, jamais dans celles du joueur.
    /// </summary>
    public static string? CaptureFolder { get; private set; }

    /// <summary>Dossier des sauvegardes utilisé à la place de %AppData% (captures et tests).</summary>
    public static string? SaveRoot { get; private set; }

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--capture")
                CaptureFolder = args[i + 1];
            if (args[i] == "--saves")
                SaveRoot = args[i + 1];
        }
        if (CaptureFolder != null && SaveRoot == null)
            SaveRoot = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "StartupRushCapture", Guid.NewGuid().ToString("N"));

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
