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
        AddWorkstation(campaign, 11, 5);
        var candidate = campaign.Candidates.First();
        candidate.NoticeWorkdays = 0;
        campaign.Hire(candidate.Person.Id);
        var app = NewProject(campaign);
        app.Color = 9;
        FinishQueue(campaign, app);
        campaign.Launch(app.Id);
        campaign.QueueComponent(app.Id, "export");
        var game = NewProject(campaign, "casual");
        campaign.Assign(candidate.Person.Id, game.Id);
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
