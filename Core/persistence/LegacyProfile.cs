using System.Text.Json;
using System.Text.Json.Nodes;

namespace Core.persistence;

/// <summary>
/// L'ancien profil du roguelike (%AppData%\StartupRush\profile.json) : lingots, Héritage, records.
/// Son contenu n'a pas d'équivalent dans la simulation et n'est donc pas converti. Seuls les réglages
/// (volumes, effets) sont repris une fois. Le fichier n'est jamais modifié ni supprimé.
/// </summary>
public static class LegacyProfile
{
    public static string DefaultPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StartupRush", "profile.json");

    public static bool Exists(string path) => File.Exists(path);

    /// <summary>Lit les réglages de l'ancien profil, ou null s'il n'y en a pas (ou s'il est illisible).</summary>
    public static Settings? ReadSettings(string path)
    {
        try
        {
            if (!File.Exists(path))
                return null;
            var node = JsonNode.Parse(File.ReadAllText(path))?["Settings"];
            if (node == null)
                return null;
            return new Settings
            {
                MusicVolume = node["MusicVolume"]?.GetValue<double>() ?? 0.6,
                SoundVolume = node["SoundVolume"]?.GetValue<double>() ?? 0.8,
                ReduceEffects = node["ReduceEffects"]?.GetValue<bool>() ?? false,
            };
        }
        catch (Exception e) when (e is IOException or JsonException or InvalidOperationException or FormatException)
        {
            return null;
        }
    }
}

/// <summary>Réglages du jeu, communs à toutes les campagnes (settings.json).</summary>
public class Settings
{
    public double MusicVolume { get; set; } = 0.6;
    public double SoundVolume { get; set; } = 0.8;

    /// <summary>Accessibilité : moins d'effets (pas de clignotement du curseur, effets de scène réduits).</summary>
    public bool ReduceEffects { get; set; }

    /// <summary>Forme du curseur du terminal : « block », « bar » ou « underline » (sprites term_cursor_* du pack).</summary>
    public string TerminalCursor { get; set; } = "block";

    /// <summary>Échelle de l'interface (0 = automatique selon la taille de la fenêtre).</summary>
    public double UiScale { get; set; }

    /// <summary>Le temps s'arrête quand un grand menu (produits, finances) est ouvert.</summary>
    public bool PauseInLargeMenus { get; set; } = true;

    /// <summary>Les nuits et week-ends passent plus vite quand personne n'est au bureau.</summary>
    public bool FastNights { get; set; } = true;

    /// <summary>Accessibilité : l'entreprise se met en pause pendant la saisie au terminal (le rendement par mot ne change pas).</summary>
    public bool PauseDuringTerminal { get; set; }

    /// <summary>
    /// Accessibilité : saisie simplifiée. Le mot s'écrit tout seul au terminal, Entrée le valide ; le rendement est celui de la
    /// frappe lente. Désactivée par défaut : le terminal reste un jeu de saisie.
    /// </summary>
    public bool SimplifiedTyping { get; set; }

    public bool MigratedFromLegacy { get; set; }

    /// <summary>Petites aides déjà lues (une phrase à la première ouverture d'un outil).</summary>
    public HashSet<string> SeenHints { get; set; } = new();

    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public static string DefaultPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StartupRush", "settings.json");

    /// <summary>Charge les réglages ; au premier lancement, reprend les volumes de l'ancien profil s'il existe.</summary>
    public static Settings Load(string path, string legacyPath)
    {
        try
        {
            if (File.Exists(path))
                return JsonSerializer.Deserialize<Settings>(File.ReadAllText(path)) ?? new Settings();
        }
        catch (Exception e) when (e is IOException or JsonException)
        {
        }
        var legacy = LegacyProfile.ReadSettings(legacyPath);
        if (legacy == null)
            return new Settings();
        legacy.MigratedFromLegacy = true;
        return legacy;
    }

    public void Save(string path)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            SaveStore.WriteAtomic(path, JsonSerializer.Serialize(this, Options));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // Des réglages non écrits ne doivent pas faire planter le jeu
        }
    }
}
