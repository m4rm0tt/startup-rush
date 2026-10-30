using Core.campaign;
using Core.space;
using Core.team;
using UI.Iso;

namespace UiTests;

/// <summary>
/// Couverture du catalogue d'objets : chaque objet achetable se pose (au sol, au mur ou sur son support), apparaît dans
/// l'image du local, se sélectionne au clic, et chaque effet qu'il déclare change bien l'indicateur correspondant.
/// C'est la preuve d'affichage et de comportement exigée par le registre (docs/asset_usage.csv) pour les meubles et décors.
/// </summary>
public class CatalogCoverageTest
{
    private static readonly Dictionary<int, Seated> Nobody = new();

    /// <summary>Un grand local vide (open space) avec les supports utiles : bureau, baie, étagère, table de réunion.</summary>
    private static Campaign Bare()
    {
        var campaign = Campaign.New("Catalogue", "Alex", "Studio", FounderProfile.Developer, 42);
        campaign.Ledger.Cash = 10_000_000;
        campaign.CheatUnlockAll();
        Assert.That(campaign.RentSite("large").Ok, Is.True);
        foreach (var item in campaign.Premises.Items.ToList())
            campaign.Premises.Items.Remove(item);
        campaign.InvalidateLayout();
        return campaign;
    }

    /// <summary>
    /// Pose un objet : au sol ou au mur à la première place libre ; un objet accroché sur le premier support du catalogue qui
    /// l'accepte (bureau, baie, étagère, table…), posé pour l'occasion.
    /// </summary>
    private static PlacedItem? Place(Campaign campaign, FurnitureDef def)
    {
        if (!def.Attached && def.Kind != FurnitureKind.RackModule)
            return campaign.FindSpot(def) is var (x, y, f) && campaign.BuyAndPlace(def.Id, x, y, f).Ok ? campaign.Premises.Items.Last() : null;
        var hosts = new[] { Campaign.DefaultDesk, "rack_standard" }.Concat(FurnitureCatalog.Buyable.Where(d => !d.Attached && (d.Pack.Facing(d.Pack.DefaultFacing).Sockets.Count > 0 || d.Pack.Facing(d.Pack.DefaultFacing).Slots > 0)).Select(d => d.Id)).Distinct();
        foreach (string host in hosts)
        {
            var existing = campaign.Premises.Items.FirstOrDefault(i => i.DefId == host);
            if (existing == null)
            {
                var hostDef = FurnitureCatalog.Get(host);
                if (campaign.FindSpot(hostDef) is not var (hx, hy, hf) || !campaign.BuyAndPlace(host, hx, hy, hf).Ok)
                    continue;
                existing = campaign.Premises.Items.Last();
            }
            int before = campaign.Premises.Items.Count;
            if (campaign.BuyAndAttach(def.Id, existing.Id).Ok && campaign.Premises.Items.Count > before)
                return campaign.Premises.Items.Last();
        }
        return null;
    }

    /// <summary>Ce qu'un effet demande pour exister (« complète une cuisine », « avec des chaises », « un coin pause »).</summary>
    private static void Prerequisites(Campaign campaign, FurnitureDef def, PlacedItem placed)
    {
        if (def.Id == "kitchen_sink")
            Place(campaign, FurnitureCatalog.Get("fridge"));
        if (def.Id == "table_coffee")
            Place(campaign, FurnitureCatalog.Get("sofa"));
        // Des chaises à chaque place d'une table de réunion
        while (Layout.FreeSocket(campaign.Premises, FurnitureCatalog.Get(Campaign.DefaultChair), placed) != null
               && campaign.BuyAndAttach(Campaign.DefaultChair, placed.Id).Ok)
        {
        }
        // Une baie ne sert qu'avec son réseau
        if (def.Kind == FurnitureKind.RackModule && placed.HostId is { } rack)
            // (le tiroir de disques demande un stockage 2U dans la même baie)
            foreach (string module in def.Id == "module_storage_jbod_4u" ? ["module_patch_panel_1u", "module_network_switch_1u", "module_storage_2u"] : new[] { "module_patch_panel_1u", "module_network_switch_1u" })
                if (module != def.Id)
                    campaign.BuyAndAttach(module, rack);
    }

    private static int Differences(Img a, Img b)
    {
        if (a.W != b.W || a.H != b.H)
            return int.MaxValue;
        int count = 0;
        for (int i = 0; i < a.P.Length; i += 4)
            if (a.P[i] != b.P[i] || a.P[i + 1] != b.P[i + 1] || a.P[i + 2] != b.P[i + 2] || a.P[i + 3] != b.P[i + 3])
                count++;
        return count;
    }

    public static IEnumerable<string> BuyableIds() => FurnitureCatalog.Buyable.Select(d => d.Id);

    [TestCaseSource(nameof(BuyableIds))]
    public void EveryBuyableObjectIsPlacedDrawnAndClickable(string id)
    {
        var def = FurnitureCatalog.Get(id);
        var campaign = Bare();
        var placed = Place(campaign, def);
        Assert.That(placed, Is.Not.Null, $"{def.Name} : aucune place ni aucun support");
        var with = RoomRenderer.Render(campaign.Premises, Nobody);
        Assert.That(campaign.Store(placed!.Id).Ok, Is.True);
        var without = RoomRenderer.Render(campaign.Premises, Nobody);
        Assert.That(Differences(with.Image, without.Image), Is.GreaterThan(20), $"{def.Name} est dessiné");
        // Un objet au sol ou au mur se sélectionne lui-même ; un objet accroché se sélectionne par son support
        int clickable = def.Attached || def.Kind == FurnitureKind.RackModule ? placed.HostId ?? placed.Id : placed.Id;
        Assert.That(with.Hits.Any(h => !h.Person && h.Id == clickable), Is.True, $"{def.Name} se sélectionne au clic");
    }

    [TestCaseSource(nameof(BuyableIds))]
    public void EveryDeclaredEffectMovesItsIndicator(string id)
    {
        var def = FurnitureCatalog.Get(id);
        var campaign = Bare();
        var placed = Place(campaign, def);
        Assert.That(placed, Is.Not.Null);
        Prerequisites(campaign, def, placed!);
        // Avec et sans l'objet (rangé dans les cartons), le reste du local identique
        campaign.InvalidateLayout();
        var comfort1 = Layout.Comfort(campaign.Premises, 4);
        var infra1 = Infrastructure.Of(campaign.Premises);
        var post = campaign.Premises.Workstations.FirstOrDefault(w => w.Desk.Id == placed!.Id || w.Desk.Id == placed!.HostId);
        double postMorale1 = post == null ? 0 : Layout.PostMorale(campaign.Premises, post);
        Assert.That(campaign.Store(placed!.Id).Ok, Is.True);
        campaign.InvalidateLayout();
        var comfort0 = Layout.Comfort(campaign.Premises, 4);
        var infra0 = Infrastructure.Of(campaign.Premises);
        string name = def.Name;
        double postMorale0 = post == null || def.Kind == FurnitureKind.Desk ? 0 : Layout.PostMorale(campaign.Premises, post);

        if (def.Kind is FurnitureKind.Desk or FurnitureKind.Personal && def.Morale > 0)
            Assert.That(postMorale1, Is.GreaterThan(postMorale0), $"{name} : moral de la personne du poste");
        if (def.Kind == FurnitureKind.Storage && def.Covers > 0)
            Assert.That(comfort1.StorageCoverage, Is.GreaterThan(comfort0.StorageCoverage), $"{name} : rangement");
        if (def.Kind == FurnitureKind.Partition && def.Covers > 0)
            Assert.That(comfort1.Focus, Is.GreaterThan(comfort0.Focus), $"{name} : concentration des personnes couvertes");
        // Services du site : même nom dans la définition de l'objet et dans le bilan d'infrastructure
        foreach (string service in new[] { "Network", "PowerSupply", "Cooling", "Backup", "Organization", "Security" })
            if ((double)typeof(FurnitureDef).GetProperty(service)!.GetValue(def)! > 0)
                Assert.That((double)typeof(InfraSummary).GetProperty(service)!.GetValue(infra1)!,
                    Is.GreaterThan((double)typeof(InfraSummary).GetProperty(service)!.GetValue(infra0)!), $"{name} : {service}");

        if (def.Kind is FurnitureKind.Plant or FurnitureKind.Ambiance && def.Morale > 0)
            Assert.That(comfort1.Ambiance, Is.GreaterThan(comfort0.Ambiance), $"{name} : ambiance");
        if (def.Kind == FurnitureKind.Lounge && def.Morale > 0)
            Assert.That(comfort1.Lounge, Is.GreaterThan(comfort0.Lounge), $"{name} : détente");
        if (def.BreakRecovery > 0)
            Assert.That(comfort1.Break, Is.GreaterThan(comfort0.Break), $"{name} : pause");
        if (def.Coordination > 0)
            Assert.That(comfort1.Coordination, Is.GreaterThan(comfort0.Coordination), $"{name} : coordination");
        // Les périphériques agissent sur leur poste, pas sur le local (Lot7ObjectsTest le vérifie)
        if (def.Focus > 0 && def.Kind != FurnitureKind.Peripheral)
            Assert.That(comfort1.Focus, Is.GreaterThan(comfort0.Focus), $"{name} : concentration");
        if (def.TrainingBonus > 0)
            Assert.That(comfort1.Training, Is.GreaterThan(comfort0.Training), $"{name} : formation");
        if (def.Recruits > 0)
            Assert.That(comfort1.Recruits, Is.GreaterThan(comfort0.Recruits), $"{name} : candidats");
        if (def.Power > 0)
            Assert.That(infra1.PowerDraw, Is.GreaterThan(infra0.PowerDraw), $"{name} : consommation électrique");
        if (def.Hosting + def.Compute + def.Gpu + def.Storage > 0)
            Assert.That(infra1.Hosting + infra1.Compute + infra1.Gpu + infra1.Storage, Is.GreaterThan(infra0.Hosting + infra0.Compute + infra0.Gpu + infra0.Storage), $"{name} : capacité");
    }
}
