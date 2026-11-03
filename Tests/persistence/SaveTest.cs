using Core.campaign;
using Core.persistence;
using Core.products;
using Core.team;
using Core.work;
using static Tests.campaign.CampaignTestKit;

namespace Tests.persistence;

public class SaveTest
{
    private string _root = "";

    [SetUp]
    public void CreateFolder()
    {
        _root = Path.Combine(Path.GetTempPath(), "StartupRushTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    [TearDown]
    public void DeleteFolder()
    {
        try
        {
            Directory.Delete(_root, true);
        }
        catch (IOException)
        {
        }
    }

    /// <summary>Une campagne bien remplie : local aménagé, employé, un projet en ligne avec une amélioration en cours, un jeu en développement.</summary>
    private static Campaign BusyCampaign()
    {
        var campaign = NewCampaign(7);
        campaign.Ledger.Cash = 40_000;
        var hired = HireDeveloper(campaign);
        var app = NewProject(campaign);
        app.Color = 9;
        FinishQueue(campaign, app);
        campaign.Launch(app.Id);
        campaign.QueueComponent(app.Id, "export");
        var game = NewProject(campaign, "casual");
        campaign.Assign(hired.Id, game.Id);
        campaign.SetUpkeep(app.Id, Upkeep.High);
        campaign.Vacation(campaign.Founder.Id);
        campaign.AdvanceDays(3);
        campaign.Advance(5 * 60 + 17);
        return campaign;
    }

    [Test]
    public void ReloadingRestoresTheExactStateAndTheSameFuture()
    {
        var campaign = BusyCampaign();
        string state = SaveStore.SerializeState(campaign);

        var copy = SaveStore.Deserialize(SaveStore.Serialize(campaign));

        Assert.That(SaveStore.SerializeState(copy), Is.EqualTo(state), "rien n'est perdu ni ajouté");

        // Même graine, même état : les deux futurs sont identiques
        campaign.AdvanceDays(20);
        copy.AdvanceDays(20);
        Assert.That(SaveStore.SerializeState(copy), Is.EqualTo(SaveStore.SerializeState(campaign)));
    }

    [Test]
    public void ReloadingDoesNotDuplicatePeopleNorRepayInvoices()
    {
        var campaign = BusyCampaign();
        campaign.AdvanceDays(30);
        int people = campaign.People.Count;
        int entries = campaign.Ledger.Entries.Count;
        decimal cash = campaign.Ledger.Cash;

        var copy = SaveStore.Deserialize(SaveStore.Serialize(campaign));

        Assert.That(copy.People, Has.Count.EqualTo(people));
        Assert.That(copy.People.Select(p => p.Id).Distinct().Count(), Is.EqualTo(people));
        Assert.That(copy.Ledger.Entries, Has.Count.EqualTo(entries));
        Assert.That(copy.Ledger.Cash, Is.EqualTo(cash));
        Assert.That(copy.NextId, Is.EqualTo(campaign.NextId), "les identifiants restent stables");
    }

    [Test]
    public void ComponentsQueuesAndSettingsSurviveAReload()
    {
        var campaign = BusyCampaign();
        var app = campaign.Products[0];

        var copy = SaveStore.Deserialize(SaveStore.Serialize(campaign));
        var reloaded = copy.Products[0];

        Assert.That(reloaded.Color, Is.EqualTo(9));
        Assert.That(reloaded.Upkeep, Is.EqualTo(Upkeep.High));
        Assert.That(reloaded.Components.Select(c => (c.DefId, c.Level)), Is.EqualTo(app.Components.Select(c => (c.DefId, c.Level))));
        Assert.That(reloaded.Queue.Select(q => q.DefId), Is.EqualTo(app.Queue.Select(q => q.DefId)));
        Assert.That(copy.People.Single(p => p.IsFounder).VacationUntil, Is.EqualTo(campaign.Founder.VacationUntil));
        Assert.That(copy.Unlocked, Is.EquivalentTo(campaign.Unlocked));
    }

    [Test]
    public void ProjectNotesAreBoundedAndSurviveAReload()
    {
        var campaign = BusyCampaign();
        var app = campaign.Products[0];
        Assert.That(campaign.SetNotes(app.Id, "Prix à revoir\nExport en mai").Ok, Is.True);
        Assert.That(campaign.SetNotes(999, "x").Ok, Is.False);

        var copy = SaveStore.Deserialize(SaveStore.Serialize(campaign));
        Assert.That(copy.Products[0].Notes, Is.EqualTo("Prix à revoir\nExport en mai"));

        campaign.SetNotes(app.Id, new string('a', 5_000));
        Assert.That(app.Notes, Has.Length.EqualTo(1_000), "les notes sont bornées");
    }

    [Test]
    public void AnOldSaveWithContractsIsMigratedWithoutLosingMoney()
    {
        string path = Path.Combine(_root, "old", "manual-Partie.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.Copy(Path.Combine(balance.BalanceReportTest.RootFolder(), "Tests", "fixtures", "campaign_v1.json"), path);
        var old = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(path))!["Campaign"]!;
        decimal cash = old["Ledger"]!["Cash"]!.GetValue<decimal>();
        int invoices = old["Ledger"]!["Invoices"]!.AsArray().Count;

        var campaign = new SaveStore(_root).Load(path);

        Assert.That(campaign.Ledger.Cash, Is.EqualTo(cash), "aucune somme ajoutée ou retirée");
        Assert.That(campaign.Ledger.Invoices, Has.Count.EqualTo(invoices), "les factures restent dues");
        Assert.That(campaign.Work.All(w => w.Owner == WorkOwner.Product), Is.True, "plus de travail de contrat");
        Assert.That(campaign.People.All(p => p.Assignment.Kind is AssignmentKind.None or AssignmentKind.Project), Is.True);
        var product = campaign.Products.Single();
        Assert.That(product.IsLive, Is.True);
        Assert.That(product.Components.Select(c => c.DefId), Is.SupersetOf(new[] { "login", "dashboard", "subscribe", "ui" }));
        Assert.That(product.Queue.Select(q => q.DefId), Is.EqualTo(new[] { "export" }), "la version en cours repart en file");
        Assert.That(product.Color, Is.EqualTo(9));
        Assert.That(campaign.MigrationNote, Does.Contain("prestation"));
        Assert.That(File.Exists(Path.ChangeExtension(path, ".v1.json")), Is.True, "l'original est gardé");

        // Et la campagne continue sans erreur ni pénalité de contrat
        campaign.AdvanceDays(30);
        Assert.That(campaign.Status, Is.EqualTo(CampaignStatus.Running));
        Assert.That(SaveStore.Deserialize(SaveStore.Serialize(campaign)).Products.Single().Components.Count, Is.EqualTo(product.Components.Count));
    }

    /// <summary>
    /// Une vraie sauvegarde de chaque format intermédiaire (4 : sites et cloud ; 5 : matériel et stocks ; 6 : studio, XR et IA ;
    /// 7 : finitions), écrite par le jeu du dernier commit de ce format (partie jouée par un bot, graine 37) : elle se charge
    /// sans rien perdre, se relit à l'identique au format actuel, et la partie continue.
    /// </summary>
    [Test]
    public void ASaveFromEachEarlierFormatLoadsWithoutLosingAnything([Values(4, 5, 6, 7)] int format)
    {
        string path = Path.Combine(_root, $"v{format}", "auto-1.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.Copy(Path.Combine(balance.BalanceReportTest.RootFolder(), "Tests", "fixtures", $"campaign_v{format}.json"), path);
        var file = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(path))!;
        Assert.That(file["Format"]!.GetValue<int>(), Is.EqualTo(format));
        var old = file["Campaign"]!;
        decimal cash = old["Ledger"]!["Cash"]!.GetValue<decimal>();
        var people = old["People"]!.AsArray().Select(p => p!["Name"]!.GetValue<string>()).ToList();
        var products = old["Products"]!.AsArray().Select(p => (p!["Id"]!.GetValue<int>(), p["Users"]!.GetValue<double>())).ToList();
        int sites = old["Sites"]!.AsArray().Count;
        int stock = old["Stock"]?.AsObject().Sum(site => site.Value!.AsObject().Sum(item => item.Value!.GetValue<int>())) ?? 0;
        int research = old["ResearchDone"]?.AsArray().Count ?? 0;

        var campaign = new SaveStore(_root).Load(path);

        Assert.That(campaign.Ledger.Cash, Is.EqualTo(cash), "aucune somme ajoutée ou retirée");
        Assert.That(campaign.People.Select(p => p.Name), Is.EqualTo(people));
        Assert.That(campaign.Products.Select(p => (p.Id, p.Users)), Is.EqualTo(products));
        Assert.That(campaign.Sites, Has.Count.EqualTo(sites));
        Assert.That(campaign.Stock.Values.Sum(s => s.Values.Sum()), Is.EqualTo(stock), "aucun stock perdu ni dupliqué");
        Assert.That(campaign.ResearchDone, Has.Count.EqualTo(research));
        Assert.That(campaign.People.All(p => p.Skills.ContainsKey(Discipline.Operations) && p.Skills.ContainsKey(Discipline.Research)), Is.True,
            "les métiers d'opérateur et de chercheur (format 8) ont une compétence pour chacun");
        Assert.That(File.Exists(Path.ChangeExtension(path, $".v{format}.json")), Is.True, "l'original est gardé");

        string saved = SaveStore.Serialize(campaign);
        Assert.That(System.Text.Json.Nodes.JsonNode.Parse(saved)!["Format"]!.GetValue<int>(), Is.EqualTo(SaveStore.CurrentFormat));
        Assert.That(SaveStore.SerializeState(SaveStore.Deserialize(saved)), Is.EqualTo(SaveStore.SerializeState(campaign)));

        campaign.AdvanceDays(30);
        Assert.That(campaign.Status, Is.EqualTo(CampaignStatus.Running));
        Assert.That(campaign.Stock.Values.SelectMany(s => s.Values).All(q => q >= 0), Is.True);
        string month = GameTime.ToDate(campaign.Minute).AddMonths(-1).ToString("yyyy-MM");
        Assert.That(campaign.SiteResults(month).Sum(r => r.Result), Is.EqualTo(Campaign.OperatingResult(campaign.Ledger.Months[month])),
            "le résultat par site se réconcilie avec le total du mois");
    }

    [Test]
    public void ATycoonSaveMovesToThePackPremisesWithoutLosingAnything()
    {
        string path = Path.Combine(_root, "v2", "auto-1.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.Copy(Path.Combine(balance.BalanceReportTest.RootFolder(), "Tests", "fixtures", "campaign_v2.json"), path);
        var old = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(path))!["Campaign"]!;
        decimal cash = old["Ledger"]!["Cash"]!.GetValue<decimal>();
        int bills = old["Ledger"]!["Bills"]!.AsArray().Count;
        int people = old["People"]!.AsArray().Count;
        int products = old["Products"]!.AsArray().Count;
        string listing = old["Premises"]!["ListingId"]!.GetValue<string>();

        var campaign = new SaveStore(_root).Load(path);

        Assert.That(campaign.Ledger.Cash, Is.EqualTo(cash), "rien n'est acheté pendant la migration");
        Assert.That(campaign.Ledger.Bills, Has.Count.EqualTo(bills));
        Assert.That(campaign.People, Has.Count.EqualTo(people));
        Assert.That(campaign.Products, Has.Count.EqualTo(products));
        Assert.That(campaign.Premises.ListingId, Is.EqualTo(listing));
        Assert.That(campaign.Premises.PlanId, Is.EqualTo(Core.space.RealEstate.Get(listing).PlanId));
        Assert.That(campaign.Founder.Appearance.Base, Is.EqualTo("player_m"));
        var post = campaign.Premises.SeatOf(campaign.Founder.Id);
        Assert.That(post, Is.Not.Null);
        Assert.That(Core.space.Layout.Check(campaign.Premises, post!), Is.EqualTo(Core.space.WorkstationIssue.None), "le poste se refait avec les anciens meubles");
        Assert.That(campaign.Premises.Items.Concat<object>(campaign.Storage).Count(), Is.GreaterThan(0));
        Assert.That(campaign.MigrationNote, Does.Contain("terminal"));
        Assert.That(File.Exists(Path.ChangeExtension(path, ".v2.json")), Is.True, "l'original est gardé");

        // Le résultat est une sauvegarde au format 3 qui se relit à l'identique
        string saved = SaveStore.Serialize(campaign);
        Assert.That(System.Text.Json.Nodes.JsonNode.Parse(saved)!["Format"]!.GetValue<int>(), Is.EqualTo(SaveStore.CurrentFormat));
        Assert.That(SaveStore.SerializeState(SaveStore.Deserialize(saved)), Is.EqualTo(SaveStore.SerializeState(campaign)));
        campaign.AdvanceDays(10);
        Assert.That(campaign.Status, Is.EqualTo(CampaignStatus.Running));
    }

    [Test]
    public void CampaignsAreStoredSeparatelyAndAutosavesRotate()
    {
        var store = new SaveStore(_root);
        var first = NewCampaign(1);
        var second = NewCampaign(2);

        store.Save(first, SaveKind.Manual, "Début");
        store.Save(second, SaveKind.Manual, "Début");
        for (int i = 0; i < 5; i++)
        {
            first.AdvanceDays(1);
            store.Save(first, SaveKind.Auto);
        }

        Assert.That(store.List(first.Id).Count(e => e.Kind == SaveKind.Auto), Is.EqualTo(SaveStore.AutosaveSlots));
        Assert.That(store.List(second.Id), Has.Count.EqualTo(1));
        var reloadedSecond = store.Load(store.List(second.Id)[0].Path);
        Assert.That(reloadedSecond.Seed, Is.EqualTo(2UL));
        Assert.That(store.List().Select(e => e.Summary!.CampaignId).Distinct().Count(), Is.EqualTo(2));
    }

    [Test]
    public void ACorruptSaveIsKeptAndThePreviousVersionIsOffered()
    {
        var store = new SaveStore(_root);
        var campaign = NewCampaign(3);
        string path = store.Save(campaign, SaveKind.Manual, "Partie");
        campaign.AdvanceDays(2);
        store.Save(campaign, SaveKind.Manual, "Partie"); // la version précédente passe en .bak
        File.WriteAllText(path, "{ \"Format\": 1, \"Campa"); // fichier tronqué

        var entry = store.List(campaign.Id).Single(e => e.Path == path);
        Assert.That(entry.Corrupt, Is.True);

        var (loaded, message) = store.LoadWithRecovery(path);

        Assert.That(loaded, Is.Not.Null);
        Assert.That(message, Does.Contain("version précédente"));
        Assert.That(File.Exists(path + ".corrupt"), Is.True, "le fichier abîmé n'est pas supprimé");
    }

    [Test]
    public void WhenTheSaveAndItsBackupAreUnreadableTheLatestAutosaveIsLoaded()
    {
        var store = new SaveStore(_root);
        var campaign = NewCampaign(4);
        campaign.AdvanceDays(3);
        store.Save(campaign, SaveKind.Auto);
        long autosaveMinute = campaign.Minute;
        string path = store.Save(campaign, SaveKind.Quick);
        File.WriteAllText(path, "{ abîmé");
        File.WriteAllText(path + ".bak", "aussi abîmé");
        File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddMinutes(1));

        var (loaded, message) = store.LoadWithRecovery(path);

        Assert.That(loaded, Is.Not.Null, message);
        Assert.That(loaded!.Minute, Is.EqualTo(autosaveMinute));
        Assert.That(message, Does.Contain("auto-1"));
        Assert.That(File.Exists(path + ".corrupt"), Is.True);
    }

    [Test]
    public void AFailedWriteLeavesThePreviousSaveIntactAndNoTemporaryFile()
    {
        var store = new SaveStore(_root);
        var campaign = NewCampaign(5);
        string path = store.Save(campaign, SaveKind.Manual, "Partie");
        string before = File.ReadAllText(path);
        // Le temporaire est occupé par un dossier : l'écriture échoue avant de toucher au fichier
        Directory.CreateDirectory(path + ".tmp");
        Assert.That(() => SaveStore.WriteAtomic(path, "nouveau contenu"), Throws.InstanceOf<Exception>());
        Assert.That(File.ReadAllText(path), Is.EqualTo(before));
        Directory.Delete(path + ".tmp");

        SaveStore.WriteAtomic(path, before);
        Assert.That(File.Exists(path + ".tmp"), Is.False, "pas de temporaire après une écriture réussie");
    }

    [Test]
    public void DeletingACampaignMovesItOutOfTheListWithoutErasingIt()
    {
        var store = new SaveStore(Path.Combine(_root, "campaigns"));
        var kept = NewCampaign(6);
        var removed = NewCampaign(7);
        store.Save(kept, SaveKind.Manual, "A");
        store.Save(removed, SaveKind.Manual, "B");
        store.Save(removed, SaveKind.Auto);

        string? target = store.DeleteCampaign(removed.Id);

        Assert.That(target, Is.Not.Null);
        Assert.That(store.List().Select(e => e.Summary!.CampaignId), Is.All.EqualTo(kept.Id));
        Assert.That(Directory.GetFiles(target!, "*.json"), Has.Length.EqualTo(2), "les fichiers sont gardés dans « deleted »");
        Assert.That(store.DeleteCampaign("inconnue"), Is.Null);
    }

    [Test]
    public void UnreadableSettingsFallBackToDefaults()
    {
        string path = Path.Combine(_root, "settings.json");
        File.WriteAllText(path, "{ pas du json");
        var settings = Settings.Load(path, Path.Combine(_root, "none.json"));
        Assert.That(settings.MusicVolume, Is.EqualTo(new Settings().MusicVolume));
        Directory.CreateDirectory(Path.Combine(_root, "dossier.json"));
        Assert.That(() => Settings.Load(Path.Combine(_root, "dossier.json"), Path.Combine(_root, "none.json")), Throws.Nothing);
    }

    [Test]
    public void ContinueNeverPicksTheEmergencySave()
    {
        var store = new SaveStore(_root);
        var campaign = NewCampaign(9);
        store.Save(campaign, SaveKind.Auto);
        string folder = store.CampaignFolder(campaign.Id);
        File.WriteAllText(Path.Combine(folder, "crash.json"), SaveStore.Serialize(campaign));
        File.SetLastWriteTimeUtc(Path.Combine(folder, "crash.json"), DateTime.UtcNow.AddMinutes(5));
        Assert.That(store.List(campaign.Id).Any(e => SaveStore.IsEmergency(e.Path)), Is.True, "proposée dans la liste");
        Assert.That(SaveStore.IsEmergency(store.Latest()!.Path), Is.False, "jamais reprise par « Continuer »");
    }

    [Test]
    public void MigrationCopiesAreNotListedAsSaves()
    {
        var store = new SaveStore(_root);
        var campaign = NewCampaign(8);
        string path = store.Save(campaign, SaveKind.Auto);
        File.Copy(path, Path.ChangeExtension(path, ".v7.json"));
        Assert.That(store.List(campaign.Id), Has.Count.EqualTo(1));
    }

    [Test]
    public void TheLegacyRoguelikeProfileIsReadButNeverModified()
    {
        string legacy = Path.Combine(_root, "profile.json");
        string content = """{"Lingots":42,"Runs":7,"Settings":{"MusicVolume":0.2,"SoundVolume":0.4,"ReduceEffects":true}}""";
        File.WriteAllText(legacy, content);

        var settings = Settings.Load(Path.Combine(_root, "settings.json"), legacy);
        settings.Save(Path.Combine(_root, "settings.json"));

        Assert.That(settings.MusicVolume, Is.EqualTo(0.2));
        Assert.That(settings.ReduceEffects, Is.True);
        Assert.That(settings.MigratedFromLegacy, Is.True);
        Assert.That(File.ReadAllText(legacy), Is.EqualTo(content));
    }

    [Test]
    public void AnOldRoguelikeFileIsNotMistakenForACampaign()
    {
        Assert.Throws<InvalidDataException>(() => SaveStore.Deserialize("""{"Lingots":42,"Runs":7}"""));
    }
}
