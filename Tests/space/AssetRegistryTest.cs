using System.Text.Json;

namespace Tests.space;

/// <summary>
/// Contrôle statique du registre d'usage (docs/asset_usage.csv) face au manifeste du pack original.
/// Il vérifie les IDs et les références déclarées, pas qu'un objet apporte réellement son effet en jeu :
/// cette preuve-là vient des scénarios de test nommés dans chaque ligne validée.
/// </summary>
public class AssetRegistryTest
{
    private const string Validated = "validé";

    private static string Root
    {
        get
        {
            var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "StartupRush.sln")))
                dir = dir.Parent;
            return dir!.FullName;
        }
    }

    private static string PackRoot => Path.Combine(Root, "assets", "StartupRushOriginalAssets");

    private static HashSet<string> ManifestIds()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(PackRoot, "manifest.json")));
        var root = document.RootElement;
        var ids = root.GetProperty("assets").EnumerateArray().Select(a => a.GetProperty("id").GetString()!).ToHashSet();
        foreach (string key in new[] { "rooms", "room_modules" })
        {
            var entries = root.GetProperty(key);
            var items = entries.ValueKind == JsonValueKind.Array ? entries.EnumerateArray() : entries.EnumerateObject().Select(p => p.Value);
            foreach (var entry in items)
                ids.Add(entry.GetProperty("id").GetString()!);
        }
        return ids;
    }

    /// <summary>Le registre est un CSV simple écrit par tools/build_asset_registry.py (guillemets doublés si besoin).</summary>
    private static List<Dictionary<string, string>> Registry()
    {
        var lines = File.ReadAllLines(Path.Combine(Root, "docs", "asset_usage.csv"));
        var header = ParseLine(lines[0]);
        return lines.Skip(1).Where(l => l.Length > 0)
            .Select(l => header.Zip(ParseLine(l)).ToDictionary(p => p.First, p => p.Second))
            .ToList();
    }

    private static List<string> ParseLine(string line)
    {
        var cells = new List<string>();
        var current = new System.Text.StringBuilder();
        bool quoted = false;
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (quoted && c == '"' && i + 1 < line.Length && line[i + 1] == '"')
            {
                current.Append('"');
                i++;
            }
            else if (c == '"')
                quoted = !quoted;
            else if (c == ',' && !quoted)
            {
                cells.Add(current.ToString());
                current.Clear();
            }
            else
                current.Append(c);
        }
        cells.Add(current.ToString());
        return cells;
    }

    [Test]
    public void TheRegistryCoversExactlyThePackIds()
    {
        var manifest = ManifestIds();
        var registry = Registry().Select(r => r["id"]).ToList();

        Assert.That(registry, Is.Unique, "ID en double dans le registre");
        Assert.That(manifest.Except(registry), Is.Empty, "IDs du pack absents du registre (relancer tools/build_asset_registry.py)");
        Assert.That(registry.Except(manifest), Is.Empty, "IDs du registre absents du pack");
    }

    [Test]
    public void EveryEntryHasAPlannedRole()
    {
        foreach (var row in Registry())
            Assert.That(row["role_prevu"], Is.Not.Empty, $"{row["id"]} : rôle prévu manquant");
    }

    [Test]
    public void AValidatedEntryNamesARealConsumerAndATest()
    {
        foreach (var row in Registry().Where(r => r["etat"] == Validated))
        {
            Assert.That(row["consommateur"], Is.Not.Empty, $"{row["id"]} : consommateur manquant");
            Assert.That(row["scenario_test"], Is.Not.Empty, $"{row["id"]} : scénario de test manquant");
            Assert.That(row["fichier_definition"], Is.Not.Empty, $"{row["id"]} : fichier de définition manquant");
            foreach (string file in row["fichier_definition"].Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                Assert.That(File.Exists(Path.Combine(Root, file)), Is.True, $"{row["id"]} : {file} n'existe pas");
        }
    }
}
