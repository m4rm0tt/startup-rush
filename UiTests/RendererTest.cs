using Core.campaign;
using Core.industry;
using Core.space;
using Core.team;
using UI.Iso;

namespace UiTests;

/// <summary>
/// Le compositeur de la pièce (UI/Iso/RoomRenderer.cs), testé sur l'image produite : opérateurs assis à leur station,
/// tenues recolorées par masque, objets hauts estompés. Rendu logiciel, sans fenêtre.
/// </summary>
public class RendererTest
{
    private static Campaign Workshop()
    {
        var campaign = Campaign.New("Rendu", "Alex", "Studio", FounderProfile.Developer, 42);
        campaign.Ledger.Cash = 1_000_000;
        campaign.Unlocked.UnionWith(["multisite", "hardware", "industry"]);
        campaign.ResearchDone.Add("res_boards");
        Assert.That(campaign.RentSite("hw_workshop").Ok, Is.True);
        Assert.That(campaign.InstallLayout("assembly").Ok, Is.True);
        return campaign;
    }

    private static int Differences(Img a, Img b)
    {
        int count = 0;
        for (int i = 0; i < a.P.Length; i += 4)
            if (a.P[i] != b.P[i] || a.P[i + 1] != b.P[i + 1] || a.P[i + 2] != b.P[i + 2] || a.P[i + 3] != b.P[i + 3])
                count++;
        return count;
    }

    private static readonly Dictionary<int, Seated> Nobody = new();

    [Test]
    public void AnOperatorSitsOnAChairAtTheWorkSpotOfTheStation()
    {
        var campaign = Workshop();
        var station = campaign.Premises.Items.First(i => IndustryCatalog.Station(i.DefId) is { Manual: true });
        var founder = campaign.Founder;
        var without = RoomRenderer.Render(campaign.Premises, Nobody);
        var with = RoomRenderer.Render(campaign.Premises, Nobody, new Dictionary<int, Seated> { [station.Id] = new(founder.Id, founder.Appearance) });

        Assert.That(without.Heads, Is.Empty);
        Assert.That(with.Heads.Keys, Is.EquivalentTo(new[] { founder.Id }), "la tête de l'opérateur porte son icône d'activité");
        Assert.That(with.Hits, Has.Some.Matches<HitRegion>(h => h.Person && h.Id == founder.Id), "on peut cliquer sur l'opérateur");
        Assert.That(Differences(with.Image, without.Image), Is.GreaterThan(300), "une chaise et un personnage en plus");

        // La tête est au-dessus de la case de travail du pack (devant la station, côté accès)
        var (hx, hy) = with.Heads[founder.Id];
        var (sx, sy) = (with.OriginX + (station.X - station.Y) * 32, with.OriginY + (station.X + station.Y) * 16);
        Assert.That(Math.Abs(hx - sx), Is.LessThan(120));
        Assert.That(Math.Abs(hy - sy), Is.LessThan(120));
    }

    [Test]
    public void AnOutfitRecolorsTheClothesWithoutTouchingTheRest()
    {
        var campaign = Campaign.New("Rendu", "Alex", "Studio", FounderProfile.Developer, 42);
        var founder = campaign.Founder;
        var post = campaign.Premises.SeatOf(founder.Id)!;
        var chair = campaign.Premises.ChairOf(post)!;
        var plain = RoomRenderer.Render(campaign.Premises, new Dictionary<int, Seated> { [chair.Id] = new(founder.Id, founder.Appearance) });
        Assert.That(campaign.SetOutfit(founder.Id, "coral", "navy").Ok, Is.True);
        var dressed = RoomRenderer.Render(campaign.Premises, new Dictionary<int, Seated> { [chair.Id] = new(founder.Id, founder.Appearance) });
        int changed = Differences(plain.Image, dressed.Image);
        Assert.That(changed, Is.GreaterThan(30), "la tenue change de couleur");
        Assert.That(changed, Is.LessThan(40 * 72), "seul le personnage change");
        Assert.That(campaign.SetOutfit(founder.Id, "violet", null).Ok, Is.False, "seules les teintes du pack");
        Assert.That(campaign.SetOutfit(founder.Id, null, null).Ok, Is.True);
        Assert.That(Differences(plain.Image, RoomRenderer.Render(campaign.Premises, new Dictionary<int, Seated> { [chair.Id] = new(founder.Id, founder.Appearance) }).Image),
            Is.EqualTo(0), "la tenue d'origine revient à l'identique");
    }

    [Test]
    public void PackedObjectsWaitInMovingBoxesAtTheEntranceAndCanBeClicked()
    {
        var campaign = Campaign.New("Rendu", "Alex", "Studio", FounderProfile.Developer, 42);
        var empty = RoomRenderer.Render(campaign.Premises, Nobody);
        var packed = RoomRenderer.Render(campaign.Premises, Nobody, cartons: 9);
        Assert.That(empty.Hits.Any(h => h.Id == RoomRenderer.CartonsId), Is.False);
        Assert.That(packed.Hits.Count(h => h.Id == RoomRenderer.CartonsId), Is.EqualTo(3), "un carton pour 4 objets, 3 au plus");
        Assert.That(Differences(empty.Image, packed.Image), Is.GreaterThan(200));
    }

    [Test]
    public void StationsGetFloorMarkingsAndModulesGetThresholds()
    {
        var campaign = Workshop();
        var site = campaign.Premises;
        var station = site.Items.First(i => IndustryCatalog.Station(i.DefId) is { Manual: true });
        var with = RoomRenderer.Render(site, Nobody);
        // Les hachures sont sur la case d'accès, devant la station : la station rangée, elles disparaissent avec elle
        Assert.That(campaign.Store(station.Id).Ok, Is.True);
        var without = RoomRenderer.Render(site, Nobody);
        var o = PackLibrary.Orient(PackLibrary.Asset(station.DefId), station.Facing);
        var spot = o.GetProperty("access").EnumerateArray().First().GetProperty("tile");
        int u = station.X + spot[0].GetInt32(), v = station.Y + spot[1].GetInt32();
        int cx = with.OriginX + (u - v) * 32, cy = with.OriginY + (u + v) * 16 + 16;
        int changed = 0;
        for (int y = cy - 4; y <= cy + 4; y++)
            for (int x = cx - 12; x <= cx + 12; x++)
            {
                int i = (y * with.Image.W + x) * 4;
                if (with.Image.P[i] != without.Image.P[i] || with.Image.P[i + 1] != without.Image.P[i + 1])
                    changed++;
            }
        Assert.That(changed, Is.GreaterThan(10), "marquage au sol sur la case de l'opérateur");

        // Un sol de module (carrelage) au milieu d'un autre : un seuil sur la bordure
        var plain = RoomRenderer.Render(site, Nobody);
        site.FloorPatches.Add(new FloorPatch { Material = "floor_tile_ceramic", U = 2, V = 2, Width = 2, Depth = 2 });
        var patched = RoomRenderer.Render(site, Nobody);
        Assert.That(Differences(plain.Image, patched.Image), Is.GreaterThan(500), "le carrelage et ses seuils (en partie sous les meubles)");
    }

    [Test]
    public void AHoveredTallObjectFadesOnlyAboveItsUsefulHeight()
    {
        var campaign = Workshop();
        var rack = campaign.Premises.Items.First(i => RoomRenderer.Occludes(i));
        var plain = RoomRenderer.Render(campaign.Premises, Nobody);
        var faded = RoomRenderer.Render(campaign.Premises, Nobody, null, rack.Id);
        int changed = Differences(plain.Image, faded.Image);
        Assert.That(changed, Is.GreaterThan(100), "le haut de l'objet s'estompe");

        // Rien ne change sous la hauteur utile : le bas de l'objet et le sol restent pleins
        var o = PackLibrary.Orient(PackLibrary.Asset(rack.DefId), rack.Facing);
        int fadeAbove = o.GetProperty("occlusion").GetProperty("fade_above_px").GetInt32();
        int floorY = plain.OriginY + (rack.X + rack.Y) * 16;
        for (int y = floorY - fadeAbove + 40; y < plain.Image.H; y++)
            for (int x = 0; x < plain.Image.W; x++)
            {
                int i = (y * plain.Image.W + x) * 4;
                Assert.That(faded.Image.P[i + 3], Is.EqualTo(plain.Image.P[i + 3]), $"pixel ({x}, {y}) sous la hauteur utile");
            }
    }

    [Test]
    public void ATallObjectInFrontOfASeatedPersonFadesByItself()
    {
        var campaign = Workshop();
        var station = campaign.Premises.Items.First(i => IndustryCatalog.Station(i.DefId) is { Manual: true });
        var founder = campaign.Founder;
        var operators = new Dictionary<int, Seated> { [station.Id] = new(founder.Id, founder.Appearance) };
        // Une armoire posée juste devant l'opérateur (côté caméra) : elle le cache, elle s'estompe
        var def = FurnitureCatalog.Get("locker");
        var o = PackLibrary.Orient(PackLibrary.Asset(station.DefId), station.Facing);
        var spot = o.GetProperty("access").EnumerateArray().First(a => a.GetProperty("kind").GetString() is "work" or "operate").GetProperty("tile");
        int u = station.X + spot[0].GetInt32(), v = station.Y + spot[1].GetInt32();
        var placed = new[] { (u + 1, v), (u, v + 1), (u + 1, v + 1) }.FirstOrDefault(c => campaign.BuyAndPlace(def.Id, c.Item1, c.Item2, "se").Ok);
        Assume.That(placed, Is.Not.EqualTo(default((int, int))), "pas de place devant l'opérateur dans ce plan");
        var locker = campaign.Premises.Items.Last();
        Assert.That(RoomRenderer.Occludes(locker), Is.True);

        // Avec l'opérateur derrière, l'armoire est estompée d'elle-même : même image que si on la survolait
        var hidden = RoomRenderer.Render(campaign.Premises, Nobody, operators);
        var hiddenHovered = RoomRenderer.Render(campaign.Premises, Nobody, operators, locker.Id);
        Assert.That(Differences(hidden.Image, hiddenHovered.Image), Is.EqualTo(0), "l'armoire qui cache quelqu'un est déjà estompée");
        // Sans personne derrière, elle reste pleine (le survol, lui, l'estompe)
        var alone = RoomRenderer.Render(campaign.Premises, Nobody);
        var aloneHovered = RoomRenderer.Render(campaign.Premises, Nobody, null, locker.Id);
        Assert.That(Differences(alone.Image, aloneHovered.Image), Is.GreaterThan(100));
    }
}
