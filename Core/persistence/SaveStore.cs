using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Core.campaign;
using Core.finance;

namespace Core.persistence;

/// <summary>Ce qu'affiche la liste des sauvegardes, lisible sans charger toute la campagne.</summary>
public class SaveSummary
{
    public string CampaignId { get; set; } = "";
    public string CampaignName { get; set; } = "";
    public string Company { get; set; } = "";
    public long Minute { get; set; }
    public decimal Cash { get; set; }
    public string Situation { get; set; } = "";
    public int People { get; set; }
    public DateTime WrittenUtc { get; set; }
}

/// <summary>Le fichier de sauvegarde : un en-tête versionné, un résumé, puis la campagne complète.</summary>
public class SaveFile
{
    public int Format { get; set; }
    public string Game { get; set; } = "StartupRush-simulation";
    public SaveSummary Summary { get; set; } = new();
    public Campaign Campaign { get; set; } = new();
}

public enum SaveKind { Manual, Auto, Quick }

/// <summary>Une sauvegarde trouvée sur le disque.</summary>
public record SaveEntry(string Path, SaveKind Kind, string Label, SaveSummary? Summary, bool Corrupt, string? Error);

/// <summary>
/// Les campagnes sur le disque, dans %AppData%\StartupRush\campaigns\&lt;id&gt;\ :
/// sauvegardes manuelles nommées, 3 sauvegardes automatiques tournantes, et une sauvegarde de sortie.
/// Chaque écriture passe par un fichier temporaire puis un remplacement atomique, et garde la version précédente
/// en .bak. Un fichier illisible n'est jamais supprimé : il est signalé, et l'on peut charger une autre sauvegarde.
/// </summary>
public class SaveStore
{
    public const int CurrentFormat = 4;
    public const int AutosaveSlots = 3;

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = false,
        // Les propriétés calculées (sans setter) ne sont pas de l'état : on ne les écrit pas
        IgnoreReadOnlyProperties = true,
        Converters = { new JsonStringEnumConverter() },
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
    };

    public string Root { get; }

    public SaveStore(string root)
    {
        Root = root;
    }

    public static string DefaultRoot =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StartupRush", "campaigns");

    public string CampaignFolder(string campaignId) => Path.Combine(Root, campaignId);

    public static SaveSummary Summarize(Campaign campaign) => new()
    {
        CampaignId = campaign.Id,
        CampaignName = campaign.Name,
        Company = campaign.MainCompany.Name,
        Minute = campaign.Minute,
        Cash = campaign.Ledger.Cash,
        Situation = campaign.Status == CampaignStatus.Liquidated ? "Liquidée" : Solvency.Name(campaign.Solvency.State),
        People = campaign.People.Count,
        WrittenUtc = DateTime.UtcNow,
    };

    public static string Serialize(Campaign campaign) =>
        JsonSerializer.Serialize(new SaveFile { Format = CurrentFormat, Summary = Summarize(campaign), Campaign = campaign }, Options);

    /// <summary>La campagne seule, sans l'en-tête horodaté (pour comparer deux états).</summary>
    public static string SerializeState(Campaign campaign) => JsonSerializer.Serialize(campaign, Options);

    public static Campaign Deserialize(string json) => Deserialize(json, out _);

    /// <summary>Lit une sauvegarde ; une sauvegarde au format 1, 2 ou 3 est convertie (Migration, MigrationV2, MigrationV3) et migrated vaut true.</summary>
    public static Campaign Deserialize(string json, out bool migrated)
    {
        migrated = false;
        var root = JsonNode.Parse(json) as JsonObject ?? throw new InvalidDataException("Fichier vide.");
        if (root["Game"]?.GetValue<string>() != "StartupRush-simulation")
            throw new InvalidDataException("Ce n'est pas une sauvegarde de campagne.");
        int format = root["Format"]?.GetValue<int>() ?? 0;
        if (format > CurrentFormat)
            throw new InvalidDataException($"Sauvegarde d'une version plus récente du jeu (format {format}).");
        if (root["Campaign"] is not JsonObject campaignNode)
            throw new InvalidDataException("Campagne absente.");

        List<Migration.Pending> pending = [];
        List<string> notes = [];
        if (format == 1)
        {
            (pending, notes) = Migration.FromV1(campaignNode);
            migrated = true;
        }
        MigrationV2.Pending? v2 = null;
        if (format <= 2)
        {
            v2 = MigrationV2.FromV2(campaignNode);
            migrated = true;
        }
        string v3 = "";
        if (format == 3)
        {
            v3 = MigrationV3.FromV3(campaignNode);
            migrated = true;
        }
        var campaign = campaignNode.Deserialize<Campaign>(Options) ?? throw new InvalidDataException("Campagne illisible.");
        if (campaign.People.Count == 0 || campaign.Companies.Count == 0)
            throw new InvalidDataException("Campagne incomplète.");
        if (format == 1)
            Migration.Finish(campaign, pending, notes);
        if (v2 != null)
            campaign.MigrationNote = string.Join(" ", new[] { campaign.MigrationNote, MigrationV2.Finish(campaign, v2) }.Where(n => n != ""));
        if (v3 != "")
            campaign.MigrationNote = string.Join(" ", new[] { campaign.MigrationNote, v3 }.Where(n => n != ""));
        campaign.ImportWallItems();
        return campaign;
    }

    /// <summary>Écrit une sauvegarde de façon atomique. Renvoie le chemin écrit.</summary>
    public string Save(Campaign campaign, SaveKind kind, string? label = null)
    {
        string folder = CampaignFolder(campaign.Id);
        Directory.CreateDirectory(folder);
        string path = kind switch
        {
            SaveKind.Manual => Path.Combine(folder, $"manual-{Sanitize(label ?? "sauvegarde")}.json"),
            SaveKind.Quick => Path.Combine(folder, "exit.json"),
            _ => Path.Combine(folder, NextAutosaveName(folder)),
        };
        WriteAtomic(path, Serialize(campaign));
        return path;
    }

    private static string NextAutosaveName(string folder)
    {
        // La plus ancienne des 3 est remplacée
        var slots = Enumerable.Range(1, AutosaveSlots).Select(i => Path.Combine(folder, $"auto-{i}.json")).ToList();
        var free = slots.FirstOrDefault(s => !File.Exists(s));
        return Path.GetFileName(free ?? slots.OrderBy(File.GetLastWriteTimeUtc).First());
    }

    public static void WriteAtomic(string path, string content)
    {
        string temporary = path + ".tmp";
        File.WriteAllText(temporary, content);
        if (File.Exists(path))
            File.Replace(temporary, path, path + ".bak", ignoreMetadataErrors: true);
        else
            File.Move(temporary, path);
    }

    /// <summary>Charge une sauvegarde. Si elle vient d'une ancienne version, l'original est d'abord copié en .v&lt;format&gt;.json (jamais écrasé).</summary>
    public Campaign Load(string path)
    {
        string json = File.ReadAllText(path);
        var campaign = Deserialize(json, out bool migrated);
        if (migrated)
        {
            int format = JsonNode.Parse(json)?["Format"]?.GetValue<int>() ?? 1;
            string backup = Path.ChangeExtension(path, $".v{format}.json");
            if (!File.Exists(backup))
                File.Copy(path, backup);
        }
        return campaign;
    }

    /// <summary>Toutes les sauvegardes, les plus récentes d'abord, y compris celles qui sont abîmées (signalées).</summary>
    public List<SaveEntry> List(string? campaignId = null)
    {
        var result = new List<SaveEntry>();
        if (!Directory.Exists(Root))
            return result;
        var folders = campaignId != null ? [CampaignFolder(campaignId)] : Directory.GetDirectories(Root);
        foreach (string folder in folders.Where(Directory.Exists))
        foreach (string path in Directory.GetFiles(folder, "*.json"))
        {
            string name = Path.GetFileNameWithoutExtension(path);
            var kind = name.StartsWith("manual-") ? SaveKind.Manual : name == "exit" ? SaveKind.Quick : SaveKind.Auto;
            string label = kind switch
            {
                SaveKind.Manual => name["manual-".Length..],
                SaveKind.Quick => "Sauvegarde de sortie",
                _ => $"Automatique {name[^1]}"
            };
            try
            {
                result.Add(new SaveEntry(path, kind, label, ReadSummary(path), false, null));
            }
            catch (Exception e) when (e is JsonException or InvalidDataException or IOException)
            {
                result.Add(new SaveEntry(path, kind, label, null, true, e.Message));
            }
        }
        return result.OrderByDescending(e => e.Summary?.WrittenUtc ?? File.GetLastWriteTimeUtc(e.Path)).ToList();
    }

    private static SaveSummary ReadSummary(string path)
    {
        using var stream = File.OpenRead(path);
        using var document = JsonDocument.Parse(stream);
        var root = document.RootElement;
        if (!root.TryGetProperty("Summary", out var summary) || !root.TryGetProperty("Campaign", out _))
            throw new InvalidDataException("En-tête manquant.");
        return summary.Deserialize<SaveSummary>(Options) ?? throw new InvalidDataException("Résumé illisible.");
    }

    /// <summary>
    /// Charge la sauvegarde demandée ; si elle est abîmée, essaie sa copie .bak. Le fichier abîmé est renommé en
    /// .corrupt (gardé pour diagnostic), jamais supprimé.
    /// </summary>
    public (Campaign? Campaign, string Message) LoadWithRecovery(string path)
    {
        try
        {
            return (Load(path), "");
        }
        catch (Exception e) when (e is JsonException or InvalidDataException or IOException or NotSupportedException)
        {
            string backup = path + ".bak";
            string kept = path + ".corrupt";
            try
            {
                File.Copy(path, kept, overwrite: true);
            }
            catch (IOException)
            {
            }
            if (File.Exists(backup))
            {
                try
                {
                    var campaign = Load(backup);
                    return (campaign, $"Sauvegarde abîmée ({e.Message}). La version précédente a été chargée ; l'original est gardé en {Path.GetFileName(kept)}.");
                }
                catch (Exception inner) when (inner is JsonException or InvalidDataException or IOException)
                {
                }
            }
            return (null, $"Sauvegarde illisible ({e.Message}). Elle est gardée en {Path.GetFileName(kept)} ; charge une autre sauvegarde de la campagne.");
        }
    }

    /// <summary>La sauvegarde la plus récente toutes campagnes confondues (pour « Continuer »).</summary>
    public SaveEntry? Latest() => List().FirstOrDefault(e => !e.Corrupt);

    private static string Sanitize(string label)
    {
        var invalid = Path.GetInvalidFileNameChars();
        string clean = new string(label.Trim().Select(c => invalid.Contains(c) ? '_' : c).ToArray());
        return clean.Length == 0 ? "sauvegarde" : clean[..Math.Min(40, clean.Length)];
    }
}
