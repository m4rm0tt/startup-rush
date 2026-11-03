using Core.campaign;
using Core.persistence;
using Core.products;
using static Tests.campaign.CampaignTestKit;

namespace Tests.products;

/// <summary>
/// Demandes des testeurs, lot 3 : préparer une vraie version puis la publier, choisir l'ordre de la file, choisir la façon de
/// développer (rapide, équilibrée, soignée), et une communauté qui récompense un produit qu'on fait vivre.
/// </summary>
public class ReleasesTest
{
    private static (Campaign Campaign, Product Product) Live()
    {
        var campaign = NewCampaign();
        var product = NewProject(campaign);
        FinishQueue(campaign, product);
        Assert.That(campaign.Launch(product.Id).Ok, Is.True);
        return (campaign, product);
    }

    [Test]
    public void FinishedWorkWaitsForTheNextVersionUntilItIsPublished()
    {
        var (campaign, product) = Live();
        Assert.That(product.Releases.Single().Version, Is.EqualTo(1), "le lancement est la version 1");
        Assert.That(campaign.CanPublish(product).Ok, Is.False);
        Assert.That(campaign.PublishRelease(product.Id, 0).Ok, Is.False, "rien à publier");

        Assert.That(campaign.QueueComponent(product.Id, "export").Ok, Is.True);
        Assert.That(campaign.QueueComponent(product.Id, "ui").Ok, Is.True);
        FinishQueue(campaign, product);
        double appeal = Design.Appeal(product);
        Assert.That(product.Staged, Has.Count.EqualTo(2));
        Assert.That(Campaign.AppealWithStaged(product), Is.GreaterThan(appeal), "la version préparée vaudra mieux");
        Assert.That(Design.Appeal(product), Is.EqualTo(appeal), "rien ne change pour les utilisateurs avant la publication");
        Assert.That(Design.CanQueue(product, "export", campaign.IsUnlocked).NextLevel, Is.EqualTo(2), "le niveau suivant compte ce qui est prêt");

        double bugs = product.Bugs, staged = product.Staged.Sum(s => s.Bugs);
        Assert.That(campaign.PublishRelease(product.Id, 1).Ok, Is.True);
        Assert.That(product.Version, Is.EqualTo(2));
        Assert.That(product.Level("export"), Is.EqualTo(1));
        Assert.That(product.Level("ui"), Is.EqualTo(1));
        Assert.That(product.Bugs, Is.EqualTo(bugs + staged * (1 - Campaign.LaunchBugReduction)).Within(0.11), "le mini-jeu retire la moitié des bugs de la version");
        var release = product.Releases.Last();
        Assert.That(release.Changes, Has.Count.EqualTo(2));
        Assert.That(release.AppealAfter, Is.GreaterThan(release.AppealBefore));
        Assert.That(product.Community, Is.GreaterThan(0));

        campaign.AdvanceDays(8);
        Assert.That(release.UsersAfter, Is.Not.Null, "le bilan d'une semaine est fait");
        Assert.That(campaign.Notices.Any(n => n.Title == $"Bilan de {product.Name} 2"), Is.True);

        var loaded = SaveStore.Deserialize(SaveStore.Serialize(campaign));
        var copy = loaded.FindProduct(product.Id)!;
        Assert.That(copy.Releases, Has.Count.EqualTo(2));
        Assert.That(copy.Community, Is.EqualTo(product.Community));
    }

    [Test]
    public void ABiggerReleaseMakesMoreNoise()
    {
        var (small, smallProduct) = Live();
        var (big, bigProduct) = Live();
        small.QueueComponent(smallProduct.Id, "export");
        FinishQueue(small, smallProduct);
        foreach (string id in new[] { "export", "ui", "notifications" })
            big.QueueComponent(bigProduct.Id, id);
        FinishQueue(big, bigProduct);
        smallProduct.Aware = bigProduct.Aware = 5_000;
        small.PublishRelease(smallProduct.Id, 0);
        big.PublishRelease(bigProduct.Id, 0);
        Assert.That(bigProduct.Aware, Is.LessThan(smallProduct.Aware), "plus de curieux à nouveau intéressés");
        Assert.That(bigProduct.Community, Is.GreaterThan(smallProduct.Community));
        Assert.That(big.Notices.Any(n => n.Detail.StartsWith("Version majeure")), Is.True);
    }

    [Test]
    public void TheCommunityFadesWhenAProductStopsEvolving()
    {
        var (campaign, product) = Live();
        product.Community = 0.5;
        product.LastUpdateAt = campaign.Minute;
        campaign.AdvanceDays(Campaign.CommunityGraceDays - 2);
        Assert.That(product.Community, Is.EqualTo(0.5), "pas d'érosion pendant le délai de grâce");
        campaign.AdvanceDays(20);
        Assert.That(product.Community, Is.LessThan(0.5));
    }

    [Test]
    public void ThreePublishedUpdatesEarnADevelopmentPoint()
    {
        var (campaign, product) = Live();
        int points = campaign.DevPoints;
        foreach (string id in new[] { "export", "ui", "notifications" })
        {
            campaign.QueueComponent(product.Id, id);
            FinishQueue(campaign, product);
            Assert.That(campaign.PublishRelease(product.Id, 0).Ok, Is.True);
        }
        Assert.That(product.Version, Is.EqualTo(4));
        Assert.That(campaign.DevPoints, Is.GreaterThanOrEqualTo(points + 1));
        Assert.That(campaign.Awarded, Does.Contain($"releases3:{product.Id}"));
    }

    [Test]
    public void AutoPublishShipsEachChangeAtOnce()
    {
        var (campaign, product) = Live();
        campaign.AutoPublish = true;
        campaign.QueueComponent(product.Id, "export");
        FinishQueue(campaign, product);
        Assert.That(product.Staged, Is.Empty);
        Assert.That(product.Version, Is.EqualTo(2));
    }

    [Test]
    public void TheQueueCanBeReorderedButNotAgainstItsDependencies()
    {
        var campaign = NewCampaign();
        var product = NewProject(campaign);
        Assert.That(campaign.QueueComponent(product.Id, "ui").Ok, Is.True);
        var order = product.Queue.Select(q => q.DefId).ToList();
        Assert.That(order, Is.EqualTo(new[] { "login", "dashboard", "subscribe", "ui" }));

        var ui = product.Queue.Last();
        Assert.That(campaign.MoveQueued(product.Id, ui.Id, -3).Ok, Is.True);
        Assert.That(product.Queue.First().DefId, Is.EqualTo("ui"));
        // Le fondateur au terminal travaille maintenant d'abord sur l'interface
        Assert.That(campaign.OpenTerminal(product.Id).Ok, Is.True);
        Assert.That(campaign.Terminal!.Stage, Does.StartWith("Interface soignée"));
        campaign.CloseTerminal();

        // Le tableau de bord demande les comptes : il ne passe pas devant eux
        var dashboard = product.Queue.First(q => q.DefId == "dashboard");
        var refused = campaign.MoveQueued(product.Id, dashboard.Id, -2);
        Assert.That(refused.Ok, Is.False);
        Assert.That(refused.Message, Does.Contain("Comptes"));
        Assert.That(campaign.MoveQueued(product.Id, ui.Id, -1).Ok, Is.False, "déjà en tête");

        var loaded = SaveStore.Deserialize(SaveStore.Serialize(campaign));
        Assert.That(loaded.FindProduct(product.Id)!.Queue.First().DefId, Is.EqualTo("ui"), "l'ordre est sauvegardé");
    }

    [Test]
    public void AQuickOrPolishedApproachTradesTimeForQuality()
    {
        double Effort(Campaign c, Product p) => p.Queue.SelectMany(q => q.WorkItemIds).Select(c.FindWork).Sum(w => w!.Effort);
        var quick = NewCampaign();
        var polish = NewCampaign();
        var a = NewProject(quick, starters: []);
        var b = NewProject(polish, starters: []);
        Assert.That(quick.QueueComponent(a.Id, "login", Approaches.Quick).Ok, Is.True);
        Assert.That(polish.QueueComponent(b.Id, "login", Approaches.Polish).Ok, Is.True);
        Assert.That(quick.QueueComponent(a.Id, "ui", "nope").Ok, Is.False);
        Assert.That(Effort(quick, a), Is.LessThan(Effort(polish, b) * 0.6), "rapide : bien moins de travail");
        FinishQueue(quick, a);
        FinishQueue(polish, b);
        Assert.That(b.Component("login")!.Quality, Is.GreaterThan(a.Component("login")!.Quality + 0.15), "soigné : nettement mieux fait");
    }

    [Test]
    public void ABigProductCostsMoreToEvolve()
    {
        var campaign = NewCampaign();
        var product = NewProject(campaign);
        Assert.That(Campaign.ScopeFactor(product), Is.EqualTo(1));
        foreach (string id in new[] { "login", "dashboard", "subscribe", "ui", "export", "search", "stats" })
            product.Components.Add(new ProjectComponent { DefId = id, Level = 1 });
        Assert.That(Campaign.ScopeFactor(product), Is.EqualTo(1 + 4 * Tuning.Projects.IntegrationPerComponent).Within(1e-9));
    }
}
