using Core.space;
using UI.Iso;

namespace UiTests;

/// <summary>
/// Notre compositeur (UI/Iso/RoomRenderer.cs) face au compositeur de référence du pack
/// (assets/StartupRushOriginalAssets/tools/csharp/StartupRushReference/Program.cs). Le pack livre ses rendus des 10 plans
/// (previews/rooms/*.png), identiques au pixel au compositeur C# selon son QA_REPORT.md : ils servent d'oracle, sans exécuter
/// de code du pack. Les différences attendues sont les personnages du plan (le jeu ne les reprend pas) et ce que le jeu ajoute
/// (marquage au sol devant les machines, seuils de porte).
/// </summary>
public class ReferenceCompositorTest
{
    private static readonly Dictionary<int, Seated> Nobody = new();

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

    public static IEnumerable<string> Plans() => Directory.GetFiles(Path.Combine(Root, "assets", "StartupRushOriginalAssets", "rooms"), "room_*.json")
        .Select(Path.GetFileNameWithoutExtension).Where(id => Pack.FindPlan(id!) != null)!;

    /// <summary>Le plan complet, comme le compositeur de référence le pose : tous ses objets, leurs enfants, le décor mural.</summary>
    private static Premises FullPlan(RoomPlan plan)
    {
        int id = 0;
        var premises = new Premises { Id = 1, PlanId = plan.Id, Width = plan.Width, Height = plan.Depth, EntranceX = plan.Entrance.X, EntranceY = plan.Entrance.Y, EntranceWall = plan.EntranceWall };
        foreach (var o in plan.Objects)
        {
            var item = new PlacedItem { Id = ++id + 100, DefId = o.Asset, X = o.U, Y = o.V, Facing = o.Facing, Swatches = new Dictionary<string, string>(o.Swatches) };
            premises.Items.Add(item);
            foreach (var child in o.Children)
                premises.Items.Add(new PlacedItem { Id = ++id + 100, DefId = child.Asset, HostId = item.Id, Socket = child.Socket });
        }
        premises.Items.AddRange(RealEstate.WallItems(plan, () => ++id + 100));
        return premises;
    }

    [TestCaseSource(nameof(Plans))]
    public void ThePlanIsComposedLikeThePackReference(string planId)
    {
        var plan = Pack.Plan(planId);
        var ours = RoomRenderer.Render(FullPlan(plan), Nobody);
        var reference = Png.Load(File.ReadAllBytes(Path.Combine(Root, "assets", "StartupRushOriginalAssets", "previews", "rooms", planId + ".png")), planId);
        // Origine de la case (0, 0) dans la référence : marge 40, puis D × 32 à gauche et 112 + 10 en haut (Program.cs, Main)
        int rx = 40 + plan.Depth * 32, ry = 40 + 112 + 10;
        int compared = 0, same = 0;
        for (int y = 0; y < reference.H; y++)
            for (int x = 0; x < reference.W; x++)
            {
                int ri = (y * reference.W + x) * 4;
                // Fond de la référence (46, 40, 50) : hors de la pièce
                if (reference.P[ri] == 46 && reference.P[ri + 1] == 40 && reference.P[ri + 2] == 50)
                    continue;
                int ox = x - rx + ours.OriginX, oy = y - ry + ours.OriginY;
                compared++;
                if (ox < 0 || oy < 0 || ox >= ours.Image.W || oy >= ours.Image.H)
                    continue;
                int oi = (oy * ours.Image.W + ox) * 4;
                if (Math.Abs(ours.Image.P[oi] - reference.P[ri]) <= 2 && Math.Abs(ours.Image.P[oi + 1] - reference.P[ri + 1]) <= 2
                    && Math.Abs(ours.Image.P[oi + 2] - reference.P[ri + 2]) <= 2)
                    same++;
            }
        if (Environment.GetEnvironmentVariable("SR_DIFF_DIR") is { Length: > 0 } dir)
        {
            // Image de contrôle : la référence, les pixels différents en magenta
            var diff = new Img(reference.W, reference.H);
            for (int y = 0; y < reference.H; y++)
                for (int x = 0; x < reference.W; x++)
                {
                    int ri = (y * reference.W + x) * 4, ox = x - rx + ours.OriginX, oy = y - ry + ours.OriginY;
                    bool inside = ox >= 0 && oy >= 0 && ox < ours.Image.W && oy < ours.Image.H;
                    int oi = inside ? (oy * ours.Image.W + ox) * 4 : 0;
                    bool differs = !inside || Math.Abs(ours.Image.P[oi] - reference.P[ri]) > 2 || Math.Abs(ours.Image.P[oi + 1] - reference.P[ri + 1]) > 2 || Math.Abs(ours.Image.P[oi + 2] - reference.P[ri + 2]) > 2;
                    bool background = reference.P[ri] == 46 && reference.P[ri + 1] == 40 && reference.P[ri + 2] == 50;
                    diff.P[ri] = differs && !background ? (byte)255 : reference.P[ri];
                    diff.P[ri + 1] = differs && !background ? (byte)0 : reference.P[ri + 1];
                    diff.P[ri + 2] = differs && !background ? (byte)255 : reference.P[ri + 2];
                    diff.P[ri + 3] = 255;
                }
            // RGBA brut (largeur × hauteur dans le nom), sans dépendre de la plateforme graphique
            File.WriteAllBytes(Path.Combine(dir, $"{planId}_diff_{reference.W}x{reference.H}.rgba"), diff.P);
        }
        double share = same / (double)compared;
        TestContext.Out.WriteLine($"{planId} : {share:P2} des {compared} pixels de la pièce identiques à la référence");
        // Sans personnage dans le plan, seuls le marquage au sol et les seuils du jeu diffèrent ; avec, les personnages en plus
        bool people = plan.Objects.Any(o => o.Children.Any(c => c.HasCharacter));
        Assert.That(share, Is.GreaterThan(people ? 0.90 : 0.98), planId);
    }
}
