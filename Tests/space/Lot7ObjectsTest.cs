using Core.campaign;
using Core.products;
using Core.space;
using Core.team;
using static Tests.campaign.CampaignTestKit;

namespace Tests.space;

/// <summary>
/// Lot 7 : l'informatique du pack qui n'avait pas encore d'usage. Chaque test vérifie une conséquence dans le jeu (production
/// ou qualité d'un poste, coordination, défauts, studio) et les plafonds, pas seulement la présence au catalogue.
/// </summary>
public class Lot7ObjectsTest
{
    private static int _id = 9000;

    private static PlacedItem Put(Premises room, string defId, int? hostId = null, string? socket = null)
    {
        var item = new PlacedItem { Id = _id++, DefId = defId, X = 1, Y = 1, HostId = hostId, Socket = socket };
        room.Items.Add(item);
        return item;
    }

    private static (Campaign Campaign, Workstation Post) Founder()
    {
        var campaign = NewCampaign();
        campaign.Ledger.Cash = 50_000;
        return (campaign, campaign.Premises.SeatOf(campaign.Founder.Id)!);
    }

    [Test]
    public void PeripheralsHelpTheirOwnPostUpToACapAndTheDockOnlyWithALaptop()
    {
        var (campaign, post) = Founder();
        var tower = campaign.Premises.ComputerOf(post)!;
        Assert.That(tower.Def.Kind, Is.EqualTo(FurnitureKind.Tower), "le fondateur commence sur une tour");
        double before = campaign.StationOutput(campaign.Founder);
        double quality = campaign.WorkQuality(campaign.Founder, Discipline.Design);

        // Le bureau n'a que deux emplacements d'objets (sockets du pack) : on choisit ses périphériques
        Assert.That(campaign.BuyAndAttach("headset_stand", post.Desk.Id).Ok, Is.True);
        Assert.That(campaign.StationOutput(campaign.Founder), Is.EqualTo(before * 1.02).Within(1e-9));
        Assert.That(campaign.BuyAndAttach("graphics_tablet", post.Desk.Id).Ok, Is.True);
        Assert.That(campaign.WorkQuality(campaign.Founder, Discipline.Design), Is.EqualTo(quality + 0.02).Within(1e-9));
        Assert.That(campaign.BuyAndAttach("docking_station", post.Desk.Id).Ok, Is.False, "plus de place sur le bureau");

        var tablet = campaign.Premises.Items.Last(i => i.DefId == "graphics_tablet");
        Assert.That(campaign.Store(tablet.Id).Ok, Is.True);
        Assert.That(campaign.BuyAndAttach("docking_station", post.Desk.Id).Ok, Is.True);
        Assert.That(campaign.StationOutput(campaign.Founder), Is.EqualTo(before * 1.02).Within(1e-9), "la station d'accueil ne sert pas une tour");

        // Avec un portable, la station d'accueil compte, jusqu'au plafond du poste
        double perOutput = before / tower.Def.Output;
        Assert.That(campaign.Store(tower.Id).Ok, Is.True);
        // Un portable remplace l'écran et le clavier
        foreach (var part in campaign.Premises.MonitorsOf(post).Append(campaign.Premises.KeyboardOf(post)!).ToList())
            campaign.Store(part.Id);
        var attached = campaign.BuyAndAttach("laptop_gaming", post.Desk.Id, "laptop");
        Assert.That(attached.Ok, Is.True, attached.Message);
        Assert.That(campaign.StationOutput(campaign.Founder), Is.EqualTo(perOutput * 1.05 * (1 + Layout.MaxPeripheralFocus)).Within(1e-9),
            "portable de jeu ×1,05, casque et station d'accueil plafonnés à +3 %");
    }

    [Test]
    public void AMiniPcOnTheDeskIsThePostComputerAndTheBetterMachinesPayOff()
    {
        var (campaign, post) = Founder();
        var tower = campaign.Premises.ComputerOf(post)!;
        double perOutput = campaign.StationOutput(campaign.Founder) / tower.Def.Output;

        Assert.That(campaign.Store(tower.Id).Ok, Is.True);
        Assert.That(campaign.StationOutput(campaign.Founder), Is.EqualTo(0), "sans ordinateur, pas de production");
        Assert.That(campaign.BuyAndAttach("mini_pc", post.Desk.Id).Ok, Is.True);
        Assert.That(campaign.Premises.ComputerOf(post)?.DefId, Is.EqualTo("mini_pc"), "posé sur le bureau, il sert d'ordinateur");
        Assert.That(campaign.StationOutput(campaign.Founder), Is.EqualTo(perOutput * 0.95).Within(1e-9));
        Assert.That(FurnitureCatalog.Get("mini_pc").Power, Is.LessThan(FurnitureCatalog.Get("tower_office").Power + 0.1));
        Assert.That(FurnitureCatalog.Get("workstation_creative").Quality, Is.GreaterThan(FurnitureCatalog.Get("workstation_dev").Quality));
    }

    [Test]
    public void TheUltrawideScreenHelpsWithinTheScreenCap()
    {
        var (campaign, post) = Founder();
        double before = campaign.StationOutput(campaign.Founder);
        campaign.Store(campaign.Premises.ChildAt(post.Desk, "monitor_main")!.Id);
        Assert.That(campaign.BuyAndAttach("monitor_ultrawide", post.Desk.Id, "monitor_main").Ok, Is.True);
        Assert.That(campaign.StationOutput(campaign.Founder), Is.EqualTo(before * 1.05).Within(1e-9));
        Assert.That(campaign.BuyAndAttach("monitor_arm_second", post.Desk.Id, "monitor_second").Ok, Is.True);
        Assert.That(campaign.StationOutput(campaign.Founder), Is.EqualTo(before * (1 + Layout.MaxScreenBonus)).Within(1e-9));
    }

    [Test]
    public void CollaborationEquipmentAddsCoordinationWithinTheCaps()
    {
        var room = new Premises { Name = "Test", Width = 8, Height = 8 };
        var screen = Put(room, "presentation_screen");
        Assert.That(Layout.Comfort(room, 1).Coordination, Is.EqualTo(FurnitureCatalog.Get("presentation_screen").Coordination).Within(1e-9));
        double baseline = Layout.Comfort(room, 1).Coordination;
        Put(room, "videoconf_bar", screen.Id, "videoconf_bar");
        Assert.That(Layout.Comfort(room, 1).Coordination, Is.EqualTo(baseline + 0.5).Within(1e-9), "une salle de visio");
        Put(room, "webcam");
        Put(room, "speakerphone");
        Put(room, "printer_mfp");
        Put(room, "webcam");
        Put(room, "webcam");
        double small = 0.25 * 5;
        Assert.That(Layout.Comfort(room, 1).Coordination, Is.EqualTo(Math.Min(Layout.MaxCoordination, baseline + 0.5 + Math.Min(Layout.MaxSmallCoordination, small))).Within(1e-9),
            "les petits supports comptent pour 1 au plus");
    }

    [Test]
    public void TestBenchesRemoveDefectsOfTheirFamilyOnly()
    {
        double Bugs(string segment, string? bench)
        {
            var campaign = NewCampaign(7);
            campaign.Ledger.Cash = 100_000;
            campaign.CheatUnlockAll();
            if (bench != null)
                Put(campaign.Premises, bench);
            var (result, product) = campaign.CreateProject("Banc", segment, segment == "casual" ? RevenueModel.Purchase : RevenueModel.Subscription);
            Assert.That(result.Ok, Is.True, result.Message);
            FinishQueue(campaign, product!);
            return product!.Bugs;
        }
        double game = Bugs("casual", null), gameKit = Bugs("casual", "console_test_kit");
        Assert.That(game, Is.GreaterThan(0));
        Assert.That(gameKit, Is.EqualTo(game * (1 - Campaign.TestBenchCut)).Within(Math.Max(0.3, game * 0.12)), "kit console : jeux");
        double app = Bugs("freelances", null), appRack = Bugs("freelances", "device_test_rack");
        Assert.That(appRack, Is.EqualTo(app * (1 - Campaign.TestBenchCut)).Within(Math.Max(0.3, app * 0.12)), "banc mobile : applications");
        Assert.That(Bugs("freelances", "console_test_kit"), Is.EqualTo(app).Within(Math.Max(0.3, app * 0.12)), "le kit console n'aide pas une application");
    }

    [Test]
    public void ACaptureBoxCountsAsTheStudioCamera()
    {
        var campaign = NewCampaign();
        Put(campaign.Premises, "greenscreen_backdrop");
        double without = campaign.StudioOf(campaign.Premises).Video;
        Put(campaign.Premises, "capture_device");
        Assert.That(campaign.StudioOf(campaign.Premises).Video, Is.EqualTo(without + 0.35).Within(1e-9));
        Assert.That(campaign.StudioOf(campaign.Premises).Missing, Does.Not.Contain("caméra"));
    }
}
