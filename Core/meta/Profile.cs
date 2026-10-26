using System.Text.Json;

namespace Core.meta;

/// <summary>Ce qui reste d'une run à l'autre : lingots, records, déblocages.</summary>
public class Profile
{
    public int Lingots { get; set; }
    public int Runs { get; set; }
    public int Wins { get; set; }
    public int BestStage { get; set; }
    public double BestNetWorth { get; set; }
    public double FastestWin { get; set; }

    /// <summary>Plus haut niveau de pression jouable (chaque victoire débloque le suivant).</summary>
    public int MaxPressureUnlocked { get; set; }

    public Dictionary<string, int> LegacyLevels { get; set; } = new();
    public HashSet<string> UnlockedCharacters { get; set; } = ["dev"];
}

/// <summary>Lit et écrit le profil en JSON. Un fichier absent ou abîmé donne un profil neuf.</summary>
public class ProfileStore
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    private readonly string _path;

    public ProfileStore(string path)
    {
        _path = path;
    }

    /// <summary>Dossier de sauvegarde standard de l'utilisateur.</summary>
    public static string DefaultPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StartupRush", "profile.json");

    public Profile Load()
    {
        try
        {
            if (!File.Exists(_path))
                return new Profile();

            return JsonSerializer.Deserialize<Profile>(File.ReadAllText(_path)) ?? new Profile();
        }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException)
        {
            return new Profile();
        }
    }

    public void Save(Profile profile)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllText(_path, JsonSerializer.Serialize(profile, Options));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // Une sauvegarde ratée ne doit jamais faire planter le jeu
        }
    }
}
