using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Core.persistence;

namespace UI;

/// <summary>
/// Journaux d'erreur exploitables : toute exception non rattrapée est écrite dans %AppData%\StartupRush\logs\ (pile, version,
/// système, partie en cours), et la partie en cours est sauvegardée à part (« crash.json » dans son dossier) sans jamais
/// remplacer les sauvegardes existantes, qui restent la dernière version valide.
/// </summary>
public static class CrashLog
{
    public static string Folder { get; set; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StartupRush", "logs");

    /// <summary>La fenêtre du jeu, pour retrouver la partie en cours au moment du plantage.</summary>
    public static MainWindow? Window { get; set; }

    private static bool _installed;

    public static void Install()
    {
        if (_installed)
            return;
        _installed = true;
        AppDomain.CurrentDomain.UnhandledException += (_, e) => Report(e.ExceptionObject as Exception, "AppDomain", fatal: e.IsTerminating);
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Report(e.Exception, "Task", fatal: false);
            e.SetObserved();
        };
    }

    /// <summary>Écrit le journal et la sauvegarde d'urgence ; renvoie le chemin du journal (null si rien n'a pu être écrit).</summary>
    private static Exception? _last;

    public static string? Report(Exception? exception, string source, bool fatal)
    {
        // La même exception remonte parfois par deux chemins (fil d'interface puis AppDomain) : un seul journal
        if (exception != null && ReferenceEquals(exception, _last))
            return null;
        _last = exception;
        try
        {
            Directory.CreateDirectory(Folder);
            string path = Path.Combine(Folder, $"crash-{DateTime.Now:yyyyMMdd-HHmmss-fff}.txt");
            var text = new StringBuilder();
            text.AppendLine($"StartupRush — erreur {(fatal ? "fatale" : "non fatale")} ({source})");
            text.AppendLine($"Date : {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            text.AppendLine($"Version : {typeof(CrashLog).Assembly.GetName().Version}");
            text.AppendLine($"Système : {Environment.OSVersion} · .NET {Environment.Version} · {(Environment.Is64BitProcess ? "64" : "32")} bits");
            text.AppendLine($"Langue du système : {System.Globalization.CultureInfo.InstalledUICulture.Name}");
            string? emergency = EmergencySave(text);
            text.AppendLine();
            text.AppendLine(exception?.ToString() ?? "(exception inconnue)");
            File.WriteAllText(path, text.ToString());
            Trace.WriteLine($"Journal d'erreur : {path}" + (emergency != null ? $" ; sauvegarde d'urgence : {emergency}" : ""));
            return path;
        }
        catch
        {
            return null;
        }
    }

    private static string? EmergencySave(StringBuilder text)
    {
        try
        {
            if (Window?.Current is not Screens.GameScreen game)
            {
                text.AppendLine("Partie : aucune en cours");
                return null;
            }
            var campaign = game.Session.Campaign;
            text.AppendLine($"Partie : {campaign.Name} ({campaign.Id}), {Core.campaign.GameTime.Format(campaign.Minute)}, graine {campaign.Seed}");
            string folder = Window.Store.CampaignFolder(campaign.Id);
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, SaveStore.EmergencyName + ".json");
            SaveStore.WriteAtomic(path, SaveStore.Serialize(campaign));
            text.AppendLine($"Sauvegarde d'urgence : {path} (les autres sauvegardes ne sont pas touchées)");
            return path;
        }
        catch (Exception e)
        {
            text.AppendLine($"Sauvegarde d'urgence impossible : {e.Message}");
            return null;
        }
    }

    /// <summary>Ouvre le dossier des journaux dans l'explorateur (le crée au besoin).</summary>
    public static void OpenFolder()
    {
        try
        {
            Directory.CreateDirectory(Folder);
            string opener = OperatingSystem.IsWindows() ? "explorer.exe" : OperatingSystem.IsMacOS() ? "open" : "xdg-open";
            Process.Start(new ProcessStartInfo(opener, $"\"{Folder}\"") { UseShellExecute = false });
        }
        catch (Exception e) when (e is System.ComponentModel.Win32Exception or InvalidOperationException or IOException)
        {
        }
    }
}
