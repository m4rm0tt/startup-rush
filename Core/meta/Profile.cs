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

    public GameSettings Settings { get; set; } = new();
}

/// <summary>Les réglages du joueur, gardés avec le profil. Un ancien profil sans réglages prend ces valeurs.</summary>
public class GameSettings
{
    /// <summary>Volume de la musique, de 0 à 1.</summary>
    public double MusicVolume { get; set; } = 0.6;

    /// <summary>Volume des bruitages, de 0 à 1.</summary>
    public double SoundVolume { get; set; } = 0.8;

    /// <summary>Secousses et flashs adoucis.</summary>
    public bool ReduceEffects { get; set; }
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
            // Écriture dans un fichier à côté puis remplacement : une coupure en pleine écriture
            // ne laisse jamais un profil à moitié écrit
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            string temporary = _path + ".tmp";
            File.WriteAllText(temporary, JsonSerializer.Serialize(profile, Options));
            File.Move(temporary, _path, overwrite: true);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // Une sauvegarde ratée ne doit jamais faire planter le jeu
        }
    }
}
