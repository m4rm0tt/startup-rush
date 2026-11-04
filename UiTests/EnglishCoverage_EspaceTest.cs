using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Core.campaign;
using Core.space;
using Core.team;
using UI;
using UI.Game;
using UI.Hud;
using UI.Hud.Panels;

namespace UiTests;

/// <summary>
/// Couverture anglaise de la zone « espace » (aménagement, finitions, locaux, infrastructure, sites) : les catalogues du
/// moteur (meubles, finitions, locaux, modules, teintes), les messages des commandes, les panneaux Aménagement et Sites, la
/// fiche d'un objet et les infobulles. Chaque texte doit être traduit (ou ne plus avoir l'air français).
/// </summary>
public class EnglishCoverage_EspaceTest
{
    private string _previous = "fr";

    [SetUp]
    public void SetUp()
    {
        _previous = Loc.Language;
        Loc.Language = "en";
        Loc.RecordMisses = true;
        Loc.Misses.Clear();
    }

    [TearDown]
    public void TearDown()
    {
        Loc.Language = _previous;
        Loc.RecordMisses = false;
        Loc.Misses.Clear();
    }

    /// <summary>Vrai si le texte est traduit (ou n'a pas l'air français).</summary>
    private static bool Covered(string text) => !Loc.LooksFrench(Loc.Translate(text));

    private static void Check(List<string> missing, string? text, string where)
    {
        if (!string.IsNullOrWhiteSpace(text) && !Covered(text))
            missing.Add($"{where} : {text}");
    }

    private static void AssertNone(List<string> missing) =>
        Assert.That(missing.Distinct().ToList(), Is.Empty, string.Join("\n", missing.Distinct()));

    // ---------------------------------------------------------------- Catalogues du moteur

    [Test]
    public void EveryFurnitureNameAndEffectIsTranslated()
    {
        var missing = new List<string>();
        foreach (var def in FurnitureCatalog.All)
        {
            Check(missing, def.Name, $"nom de {def.Id}");
            Check(missing, def.Effect, $"effet de {def.Id}");
            // L'infobulle du catalogue : nom, effet et consigne de pose (assemblés par le panneau)
            foreach (string suffix in new[] { "Se pose sur un meuble.", "Se fixe sur un mur du fond.", "Au plafond.", "Emprise : 4 case(s)." })
                Check(missing, $"{def.Name} — {def.Effect} {suffix}", $"infobulle de {def.Id}");
            Check(missing, $"{def.Name} — à débloquer : Hardware.", $"infobulle verrouillée de {def.Id}");
        }
        AssertNone(missing);
        // Les nombres décimaux sont écrits à l'anglaise, et le français garde ses virgules
        Assert.That(Loc.Translate("Production ×0,85."), Is.EqualTo("Output ×0.85."));
        Assert.That(Loc.Translate("Au mur : +1 000 de débit réseau ; abonnement 120 CHF par mois."), Does.Contain("+1,000"));
    }

    [Test]
    public void EveryPlacedOrDeliveredAssetHasAnEnglishName()
    {
        var missing = new List<string>();
        foreach (var plan in new[] { "room_bedroom_start", "room_startup_small", "room_office_medium", "room_open_space", "room_server_compact",
                     "room_datacenter", "room_hw_workshop", "room_factory_hall", "room_advanced_lab", "room_hq_floor" })
            foreach (var o in Pack.Plan(plan).Objects)
                Check(missing, FurnitureCatalog.Get(o.Asset).Name, $"objet du plan {plan}");
        foreach (var module in Pack.Modules)
        {
            Check(missing, module.Name, $"module {module.Id}");
            foreach (var o in module.Objects)
            {
                Check(missing, FurnitureCatalog.Get(o.Asset).Name, $"objet du module {module.Id}");
                foreach (var c in o.Children)
                    Check(missing, FurnitureCatalog.Get(c.Asset).Name, $"accessoire du module {module.Id}");
            }
        }
        Check(missing, FurnitureCatalog.Get("moving_box").Name, "cartons");
        Check(missing, FurnitureCatalog.Get("floor_marking").Name, "marquage au sol");
        Check(missing, FurnitureCatalog.Get("an_unknown_asset").Effect, "décor");
        AssertNone(missing);
    }

    [Test]
    public void EveryFinishAndPremisesTypeIsTranslated()
    {
        var missing = new List<string>();
        foreach (var finish in Finishes.Catalog)
        {
            Check(missing, finish.Name, $"finition {finish.Id}");
            Check(missing, finish.Effect, $"effet de {finish.Id}");
            string unit = finish.Kind == FinishKind.Wall ? "20 CHF le segment de mur" : "20 CHF la case";
            Check(missing, $"{finish.Name} — {Loc.Translate(finish.Effect)} {Loc.Translate(unit)}. Clic : aperçu dans la scène.", $"infobulle de {finish.Id}");
            Check(missing, $"Aperçu : {finish.Name}, sarcelle", $"aperçu de {finish.Id}");
            Check(missing, $"Nouveau sol : {finish.Name}.", $"résultat de {finish.Id}");
        }
        foreach (var listing in RealEstate.Listings)
        {
            Check(missing, listing.Kind, $"type de local {listing.Id}");
            Check(missing, listing.Description, $"description de {listing.Id}");
            // (le nom de l'adresse est un nom propre : il reste tel quel)
            Check(missing, listing.NameAt("Flon"), $"nom de local {listing.Id}");
        }
        AssertNone(missing);
        Assert.That(Loc.Translate("Petit bureau, Flon"), Is.EqualTo("Small office, Flon"));
    }

    [Test]
    public void EveryShadeAndColorPartIsTranslated()
    {
        var missing = new List<string>();
        foreach (string channel in Campaign.PaintableChannels)
        {
            Check(missing, Campaign.ChannelName(channel), $"partie {channel}");
            foreach (string ramp in Pack.SwatchOptions(channel))
                Check(missing, $"{Campaign.ChannelName(channel)} : {UI.Screens.NewCampaignScreen.OutfitName(ramp)}", $"infobulle {channel}/{ramp}");
        }
        foreach (string channel in new[] { "floor", "wall" })
            foreach (string ramp in Pack.SwatchOptions(channel))
                Check(missing, $"Couleur : {UI.Screens.NewCampaignScreen.OutfitName(ramp)}", $"infobulle {channel}/{ramp}");
        AssertNone(missing);
        // Le français n'est jamais modifié
        Loc.Language = "fr";
        Assert.That(UI.Screens.NewCampaignScreen.OutfitName("teal"), Is.EqualTo("sarcelle"));
        Loc.Language = "en";
        Assert.That(UI.Screens.NewCampaignScreen.OutfitName("teal"), Is.EqualTo("teal"));
    }

    // ---------------------------------------------------------------- Messages des commandes

    private static Campaign Rich()
    {
        var campaign = Campaign.New("Espace", "Alex", "Studio", FounderProfile.Developer, 42);
        campaign.Ledger.Cash = 5_000_000;
        campaign.CheatUnlockAll();
        return campaign;
    }

    [Test]
    public void EveryPlacementRefusalAndWorkstationStateIsTranslated()
    {
        var campaign = Rich();
        Assert.That(campaign.RentSite("large").Ok, Is.True);
        var missing = new List<string>();
        foreach (var issue in Enum.GetValues<WorkstationIssue>())
            Check(missing, Layout.Describe(issue), $"état {issue}");
        var premises = campaign.Premises;
        foreach (var def in FurnitureCatalog.All.Where(d => d.Buyable))
            for (int x = -1; x <= 2; x++)
                for (int y = -1; y <= 2; y++)
                {
                    foreach (string facing in def.Pack.Facings.Keys)
                        Check(missing, Layout.CanPlace(premises, def, x, y, facing).Reason, $"pose de {def.Id}");
                    if (def.OnWall || def.Pack.Ceiling)
                        Check(missing, Layout.CanHang(premises, def, x, y, "se").Reason, $"accroche de {def.Id}");
                }
        // Sur un meuble ou dans une baie, quand il n'y a pas la place
        var desk = FurnitureCatalog.Get(Campaign.DefaultDesk);
        var (dx, dy, df) = campaign.FindSpot(desk)!.Value;
        Assert.That(campaign.BuyAndPlace(desk.Id, dx, dy, df).Ok, Is.True);
        var host = premises.Items.Last();
        foreach (var def in FurnitureCatalog.All.Where(d => d.Buyable && d.Attached))
        {
            Check(missing, Layout.CanAttach(premises, def, host, "no_such_socket").Reason, $"fixation de {def.Id}");
            Check(missing, Layout.CanAttach(premises, def, host, "slot:0").Reason, $"emplacement de {def.Id}");
        }
        AssertNone(missing);
    }

    [Test]
    public void CampaignCommandsAnswerInEnglish()
    {
        var campaign = Rich();
        var missing = new List<string>();
        void Say(CommandResult r, string where) => Check(missing, r.Message, where);

        // Sites : louer, déménager, ouvrir, fermer
        campaign.Notices.Clear();   // (le message de bienvenue n'est pas de cette zone)
        Say(campaign.ViewSite(999), "site inconnu");
        foreach (var listing in RealEstate.Listings)
        {
            var (_, why) = campaign.CanRent(listing, additional: false);
            Check(missing, why, $"louer {listing.Id}");
            var (_, whyMore) = campaign.CanRent(listing, additional: true);
            Check(missing, whyMore, $"ouvrir {listing.Id}");
        }
        Say(campaign.MoveTo("small"), "déménager");
        Say(campaign.MoveTo("small"), "déjà ici");
        Say(campaign.RentSite("server_room"), "ouvrir");
        Say(campaign.RentSite("server_room", "Crissier"), "adresse prise");
        Say(campaign.CloseSite(999), "fermer inconnu");
        foreach (var notice in campaign.Notices)
        {
            Check(missing, notice.Title, "titre de notification");
            Check(missing, notice.Detail, "détail de notification");
        }
        Say(campaign.CloseSite(campaign.Sites[1].Id), "fermer");
        Say(campaign.CloseSite(campaign.Sites[0].Id), "fermer le seul site");
        campaign.Ledger.Cash = 0;
        Say(campaign.MoveTo("floor"), "déménager sans argent");
        Say(campaign.RentSite("large"), "ouvrir sans argent");
        campaign.Ledger.Cash = 5_000_000;

        // Achats, pose, déplacement, rotation, revente, cartons
        Assert.That(campaign.MoveTo("large").Ok, Is.True);
        var premises = campaign.Premises;
        Say(campaign.BuyAndPlace("no_such_id", 1, 1), "achat inconnu");
        Say(campaign.BuyAndPlace("trophy_project", 1, 1), "objet qui ne s'achète pas");
        Say(campaign.BuyAndPlace("desk_simple", -3, -3), "hors du local");
        var desk = FurnitureCatalog.Get(Campaign.DefaultDesk);
        var (x, y, f) = campaign.FindSpot(desk)!.Value;
        Say(campaign.BuyAndPlace(desk.Id, x, y, f), "bureau posé");
        var placed = premises.Items.Last();
        Say(campaign.BuyAndPlace(desk.Id, x, y, f), "place prise");
        Say(campaign.BuyAndAttach("chair_budget", 99999), "hôte inconnu");
        Say(campaign.BuyAndAttach("chair_budget", placed.Id), "chaise");
        Say(campaign.BuyAndAttach("chair_budget", placed.Id), "deuxième chaise");
        Say(campaign.BuyAndAttach("laptop_standard", placed.Id), "portable");
        Say(campaign.BuyAndAttach("monitor_basic", placed.Id), "écran avec portable");
        Say(campaign.Move(placed.Id, -5, -5), "déplacer hors du local");
        Say(campaign.Move(99999, 1, 1), "déplacer inconnu");
        Say(campaign.Rotate(placed.Id), "tourner");
        Say(campaign.Rotate(99999), "tourner inconnu");
        Say(campaign.Rotate(premises.Items.First(i => i.HostId == placed.Id).Id), "tourner un accessoire");
        Say(campaign.PlaceFromStorage(99999, 1, 1), "carton inconnu");
        Say(campaign.AttachFromStorage(99999, placed.Id), "carton inconnu (fixation)");
        Say(campaign.AssignDesk(99999, null), "bureau inconnu");
        Say(campaign.AssignDesk(placed.Id, 99999), "personne inconnue");
        Say(campaign.AssignDesk(placed.Id, campaign.Founder.Id), "attribution");
        Say(campaign.AssignDesk(placed.Id, null), "libération");
        Say(campaign.Store(placed.Id), "ranger");
        Say(campaign.Store(99999), "ranger un objet inconnu");
        Say(campaign.SellStored(99999), "vendre un carton inconnu");
        Say(campaign.SellStored(campaign.Storage.First().Id), "vendre un carton");
        Say(campaign.Sell(99999), "vendre inconnu");
        var (bx, by, bf) = campaign.FindSpot(FurnitureCatalog.Get("sofa"))!.Value;
        Say(campaign.BuyAndPlace("sofa", bx, by, bf), "canapé");
        var sofa = premises.Items.Last();
        Say(campaign.AssignDesk(sofa.Id, null), "attribuer un canapé");
        Say(campaign.Sell(sofa.Id), "revendre");
        foreach (var item in premises.Items.ToList())
        {
            var (_, reason) = Campaign.CanRemove(item);
            Check(missing, reason, $"retirer {item.DefId}");
        }
        foreach (var item in premises.Items.Where(i => i.Included).Take(20).ToList())
        {
            Say(campaign.Sell(item.Id), $"vendre le livré {item.DefId}");
            Say(campaign.Store(item.Id), $"ranger le livré {item.DefId}");
        }

        // Couleurs
        campaign.BuyAndPlace("sofa", bx, by, bf);
        var painted = premises.Items.Last();
        Say(campaign.SetItemColor(99999, "fabric", "rose"), "couleur : objet inconnu");
        Say(campaign.SetItemColor(painted.Id, "no_part", "rose"), "couleur : partie qui ne change pas");
        Say(campaign.SetItemColor(painted.Id, "fabric", "no_such_shade"), "couleur : teinte inconnue");
        Say(campaign.SetItemColor(painted.Id, "fabric", "rose"), "couleur");
        Say(campaign.ResetItemColors(painted.Id), "couleurs d'origine");
        Say(campaign.ResetItemColors(99999), "couleurs d'origine : objet inconnu");

        // Finitions et modules
        Say(campaign.SetFinish("no_such_finish"), "finition inconnue");
        Say(campaign.SetFinish("floor_parquet", FinishTarget.Everywhere, "no_such_color"), "couleur inconnue");
        Say(campaign.SetFinish("pattern_brick", new FinishTarget("up"), null), "mur inconnu");
        Say(campaign.SetFinish("floor_parquet", FinishTarget.Rect(-9, -9, 1, 1)), "hors du local");
        foreach (var finish in Finishes.Catalog)
        {
            Say(campaign.SetFinish(finish.Id), $"finition {finish.Id}");
            Say(campaign.SetFinish(finish.Id), $"finition {finish.Id} déjà en place");
        }
        Say(campaign.SetFinish("floor_parquet", FinishTarget.Rect(1, 1, 1, 1)), "une case");
        Say(campaign.SetFinish("floor_carpet", FinishTarget.Rect(2, 2, 2, 2)), "une zone");
        Say(campaign.SetFinish("pattern_brick", new FinishTarget("left")), "mur de gauche");
        Say(campaign.SetFinish("pattern_wallpaper", new FinishTarget("right")), "mur de droite");
        Say(campaign.ResetFinish(FinishKind.Floor), "sols du plan");
        Say(campaign.ResetFinish(FinishKind.Wall), "murs du plan");
        Say(campaign.ResetFinish(FinishKind.Floor), "sols déjà ceux du plan");
        Say(campaign.ResetFinish(FinishKind.Wall), "murs déjà ceux du plan");
        Say(campaign.InstallModule("no_such_module"), "module inconnu");
        foreach (var module in Pack.Modules)
        {
            Check(missing, campaign.ModuleLock(module), $"module verrouillé {module.Id}");
            Say(campaign.InstallModule(module.Id), $"module {module.Id}");
        }
        campaign.Ledger.Cash = 0;
        Say(campaign.InstallModule(Pack.Modules.First().Id), "module sans argent");
        campaign.Ledger.Cash = 5_000_000;

        // Déchets
        Say(campaign.PickUpLitter(0, 0), "rien à ramasser");
        Say(campaign.CleanUp(), "le local est propre");
        premises.Litter.Add(new LitterSpot { X = 2, Y = 2, Kind = 0 });
        Say(campaign.PickUpLitter(2, 2), "un déchet");
        premises.Litter.Add(new LitterSpot { X = 2, Y = 2, Kind = 0 });
        Say(campaign.CleanUp(), "un seul déchet");
        premises.Litter.Add(new LitterSpot { X = 2, Y = 2, Kind = 0 });
        premises.Litter.Add(new LitterSpot { X = 3, Y = 3, Kind = 1 });
        Say(campaign.CleanUp(), "plusieurs déchets");

        // Projet hébergé en interne
        Say(campaign.SetInternalHosting(99999, true), "projet inconnu");
        AssertNone(missing);
    }

    [Test]
    public void InfrastructureIssuesAreTranslatedInEveryCase()
    {
        var campaign = Rich();
        Assert.That(campaign.RentSite("server_room").Ok, Is.True);
        var missing = new List<string>();
        var premises = campaign.Premises;
        foreach (var item in premises.Items.Where(i => i.Def.Kind is FurnitureKind.Rack or FurnitureKind.Utility or FurnitureKind.Cabling).ToList())
            campaign.Store(item.Id);
        // Des baies sans switch, des disques sans contrôleur, un serveur sans électricité ni froid ni réseau, un cryostat incomplet
        foreach (string rackId in new[] { "rack_standard", "rack_compact" })
        {
            if (campaign.FindSpot(FurnitureCatalog.Get(rackId)) is not var (rx, ry, rf) || !campaign.BuyAndPlace(rackId, rx, ry, rf).Ok)
                continue;
            var rack = premises.Items.Last();
            foreach (string module in new[] { "module_server_1u", "module_gpu_4u", "module_storage_jbod_4u", "module_compute_2u" })
                campaign.BuyAndAttach(module, rack.Id);
        }
        foreach (string id in new[] { "server_tower_classic", "quantum_cryostat_closed", "quantum_cryostat_closed", "quantum_control_rack" })
            if (campaign.FindSpot(FurnitureCatalog.Get(id)) is var (x, y, f) && campaign.BuyAndPlace(id, x, y, f).Ok) { }
        campaign.Premises.Items.RemoveAll(i => i.DefId == "power_distribution_cabinet" || i.DefId == "crac_unit");
        campaign.InvalidateLayout();
        var summary = campaign.SiteInfra(premises);
        Assert.That(summary.Issues, Is.Not.Empty);
        foreach (var issue in summary.Issues)
            Check(missing, issue, "problème d'infrastructure");
        foreach (var (item, list) in campaign.ItemIssues(premises))
            foreach (var issue in list)
                Check(missing, issue.Text, $"problème de {item}");
        AssertNone(missing);
        Assert.That(Loc.Translate("Puissance insuffisante : 12,5 kW demandés pour 10 kW disponibles (armoire électrique)."),
            Is.EqualTo("Not enough power: 12,5 kW requested for 10 kW available (electrical cabinet)."));
    }

    // ---------------------------------------------------------------- Panneaux, fiches, infobulles

    private static IEnumerable<string> Tips(Window window) =>
        window.GetVisualDescendants().OfType<Control>().Select(ToolTip.GetTip).OfType<string>();

    private static IEnumerable<string> Texts(Window window) =>
        window.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text ?? "").Where(t => t != "");

    /// <summary>Les textes d'apparence française affichés (ou en infobulle) qui ne sont pas déjà dans <paramref name="known"/>.</summary>
    private static List<string> Leftovers(Window window, HashSet<string> known)
    {
        var all = Texts(window).Concat(Tips(window)).Concat(Loc.Misses.Keys).Where(t => Loc.LooksFrench(Loc.Translate(t)));
        // Les adresses sont des noms propres (« Écublens-EPFL »)
        return all.Where(t => !known.Contains(t) && !RealEstate.Listings.Any(l => l.Places.Any(p => t.Contains(p)))).Distinct().ToList();
    }

    [AvaloniaTest]
    public void TheFurnishAndSitesPanelsAreEntirelyInEnglish()
    {
        Loc.Language = "fr";
        var (window, game, campaign) = PanelsTest.Advanced();
        Loc.Language = "en";
        // Du monde, des postes, des déchets, un canapé dans les cartons : toutes les cartes des panneaux
        campaign.SeatEveryone(buy: true);
        campaign.Premises.Litter.Add(new LitterSpot { X = 2, Y = 3, Kind = 0 });
        if (campaign.FindSpot(FurnitureCatalog.Get("sofa")) is { } s)
            Assert.That(campaign.BuyAndPlace("sofa", s.X, s.Y, s.Facing).Ok, Is.True);
        campaign.Store(campaign.Premises.Items.Last().Id);
        game.ClosePanels();
        game.Tick(0.2);
        Dispatcher.UIThread.RunJobs();
        // Ce qui est affiché sans rien ouvrir (barres, textes d'autres zones) n'est pas de cette zone
        var known = new HashSet<string>(Leftovers(window, new HashSet<string>()));
        foreach (string status in campaign.People.Select(p => p.Status))
            known.Add(status);

        var left = new List<string>();
        void Look(string step)
        {
            game.Tick(0.2);
            Dispatcher.UIThread.RunJobs();
            // (ce que dit une personne de son travail est écrit par une autre zone)
            var statuses = campaign.People.Select(p => p.Status).ToHashSet();
            left.AddRange(Leftovers(window, known).Where(t => !statuses.Contains(t)).Select(t => $"{step} : {t}"));
        }
        void Open(GamePanel panel, string step)
        {
            game.OpenRoot(panel);
            Look(step);
        }
        Open(new BuildPanel(), "Aménagement");
        // (preuve que le panneau est bien affiché, et en anglais)
        Assert.That(Texts(window), Does.Contain("Furnish").And.Contain("CATALOG").And.Contain("CLEANLINESS").And.Contain("Trash bins"));
        Assert.That(Tips(window).Select(Loc.Translate), Has.Some.Contain("Footprint"));
        Open(BuildPanel.AtCartons(), "Cartons");
        Assert.That(Texts(window), Has.Some.Contain("BOXES"));
        Open(new SitesPanel(), "Sites");
        Assert.That(Texts(window), Does.Contain("YOUR SITES").And.Contain("PREMISES TO RENT"));

        // Un objet en main, puis chaque aperçu de finition (sol, murs, couleurs)
        game.OpenRoot(new BuildPanel());
        foreach (var tool in new[] { "desk_simple", "module_server_1u", "monitor_basic", "poster_project", "lamp_ceiling" })
        {
            game.Session.SetTool(new BuildTool(tool));
            game.RefreshPanel();
            Look($"Pose de {tool}");
        }
        game.Session.SetTool(null);
        foreach (var finish in new[] { new FinishTool("floor_parquet", "all", "walnut"), new FinishTool("floor_carpet", "zone"), new FinishTool("floor_tile_ceramic", "tile"),
                     new FinishTool("pattern_brick", "both", "sage"), new FinishTool("pattern_acoustic", "pick") })
        {
            game.Session.SetFinish(finish);
            game.RefreshPanel();
            Look($"Finition {finish.FinishId}");
        }
        game.Session.SetFinish(null);

        // La fiche de chaque sorte d'objet, puis celle d'une personne
        game.ClosePanels();
        foreach (var item in campaign.Premises.Items.Where(i => i.HostId == null).GroupBy(i => i.DefId).Select(g => g.First()).Take(40).ToList())
        {
            game.Session.Select(SelectionKind.Item, item.Id);
            Look($"Fiche de {item.DefId}");
        }
        game.Session.ClearSelection();
        game.Session.Select(SelectionKind.Person, campaign.Founder.Id);
        Look("Fiche d'une personne");
        window.Close();

        var mine = left.Distinct().ToList();
        Assert.That(mine, Is.Empty, string.Join("\n", mine));
    }
}
