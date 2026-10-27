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

    /// <summary>Une campagne bien remplie : local aménagé, contrat en cours, produit publié avec un brouillon, employé.</summary>
    private static Campaign BusyCampaign()
    {
        var campaign = NewCampaign(7);
        campaign.Ledger.Cash = 40_000;
        AddWorkstation(campaign, 11, 5);
        var candidate = campaign.Candidates.First();
        candidate.NoticeWorkdays = 0;
        campaign.Hire(candidate.Person.Id);
        var offer = AddOffer(campaign, design: 10, code: 30, test: 10);
        campaign.AcceptContract(offer.Id, DeadlineOption.Safe, true);
        campaign.Assign(candidate.Person.Id, AssignmentKind.Contract, offer.Id);

        var (_, product) = campaign.CreateProduct(campaign.MainCompany.Id, "Factura", "freelances", RevenueModel.Subscription, 19);
        var home = product!.Draft.Pages[0];
        home.Theme = "sombre";
        home.Accent = 9;
        foreach (string id in new[] { "login", "dashboard", "subscribe" })
            home.Modules.Add(ProductDesign.NewModule(product, id));
        campaign.Assign(campaign.Founder.Id, AssignmentKind.Product, product.Id);
        campaign.StartBuild(product.Id);
        campaign.AdvanceDays(3);
        campaign.Advance(5 * 60 + 17); // en pleine journée : des gens marchent, travaillent
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
    public void PagesDraftsAndVersionsSurviveAReload()
    {
        var campaign = BusyCampaign();
        var product = campaign.Products.Single();

        var copy = SaveStore.Deserialize(SaveStore.Serialize(campaign));
        var reloaded = copy.Products.Single();

        Assert.That(reloaded.Draft.Pages[0].Theme, Is.EqualTo("sombre"));
        Assert.That(reloaded.Draft.Pages[0].Accent, Is.EqualTo(9));
        Assert.That(reloaded.Draft.AllModules.Select(m => m.DefId), Is.EquivalentTo(product.Draft.AllModules.Select(m => m.DefId)));
        Assert.That(reloaded.Build!.WorkItemIds, Is.EqualTo(product.Build!.WorkItemIds));
        Assert.That(copy.WorkFor(WorkOwner.Product, reloaded.Id).Sum(w => w.Progress), Is.EqualTo(campaign.WorkFor(WorkOwner.Product, product.Id).Sum(w => w.Progress)));
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
