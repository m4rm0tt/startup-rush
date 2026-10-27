using System.Text.Json;
using Core.space;

namespace Tests.space;

/// <summary>Le manifeste de l'atlas (produit par tools/build_assets.py) doit couvrir le catalogue avec les mêmes emprises.</summary>
public class AssetManifestTest
{
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

    [Test]
    public void EveryFurnitureHasASpriteWithTheSameFootprint()
    {
        string path = Path.Combine(Root, "UI", "Assets", "Sprites", "furniture.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var manifest = document.RootElement;

        foreach (var def in FurnitureCatalog.All)
        {
            Assert.That(manifest.TryGetProperty(def.Id, out var entry), Is.True, $"sprite manquant : {def.Id}");
            var footprint = entry.GetProperty("footprint");
            Assert.That((footprint[0].GetInt32(), footprint[1].GetInt32()), Is.EqualTo((def.Width, def.Height)), $"emprise de {def.Id}");
            Assert.That(entry.GetProperty("w").GetInt32(), Is.GreaterThan(0));
        }
        for (int i = 0; i < 5; i++)
            Assert.That(manifest.TryGetProperty($"person{i}", out _), Is.True);
        Assert.That(File.Exists(Path.Combine(Root, "UI", "Assets", "Sprites", "furniture.png")), Is.True);
    }

    [Test]
    public void TheStartingApartmentIsAValidLayout()
    {
        var campaign = Core.campaign.Campaign.New("Test", "Alex", "Studio", Core.campaign.FounderProfile.Developer, 1);
        var premises = campaign.Premises;

        Assert.That(Layout.CompleteWorkstations(premises).Count(), Is.EqualTo(1));
        var occupied = new HashSet<Cell>();
        foreach (var item in premises.Items)
            foreach (var cell in premises.Footprint(item))
            {
                Assert.That(premises.InBounds(cell), Is.True, item.DefId);
                Assert.That(occupied.Add(cell), Is.True, $"chevauchement en {cell}");
            }
    }
}
