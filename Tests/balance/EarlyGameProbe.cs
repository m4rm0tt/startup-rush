using Core.campaign;
using Core.products;
using static Tests.campaign.CampaignTestKit;

namespace Tests.balance;

/// <summary>
/// Le début de partie (demande des testeurs : « injouable de lenteur ») et la vitesse d'une grande équipe, mesurés. Garde-fous :
/// le premier projet sort en moins de 220 mots, une grande équipe ne va pas proportionnellement plus vite qu'une petite.
/// </summary>
public class EarlyGameProbe
{
    [TestCase("freelances")]
    [TestCase("casual")]
    public void TheFirstProjectShipsAfterAFewMinutesOfTyping(string segment)
    {
        var campaign = NewCampaign(profile: FounderProfile.Generalist);
        var product = NewProject(campaign, segment);
        Assert.That(product.Prototype, Is.True);
        int words = 0;
        for (int day = 0; day < 30 && !campaign.ReadyToLaunch.Any(); day++)
        {
            words += TypeWords(campaign, product, 400);
            campaign.AdvanceDays(1);
        }
        TestContext.Out.WriteLine($"{segment} : {words} mots pour sortir le premier projet");
        Assert.That(campaign.ReadyToLaunch, Has.Member(product));
        Assert.That(words, Is.LessThan(220), "le premier projet sort vite (prototype et série)");
        Assert.That(words, Is.GreaterThan(80), "mais il reste un vrai travail");

        // Le second projet n'est plus un prototype
        var (_, second) = campaign.CreateProject("Deux", segment, product.Model);
        Assert.That(second!.Prototype, Is.False);
    }

    /// <summary>Jours ouvrés pour finir le même travail avec une équipe de n développeurs (embauchés depuis un mois).</summary>
    private static double DaysFor(int team)
    {
        var campaign = NewCampaign(seed: 7);
        campaign.Ledger.Cash = 1_000_000;
        campaign.CheatUnlockAll();
        campaign.MoveTo("large");
        for (int i = 0; i < team; i++)
            Assert.That(campaign.CheatHire().Ok, Is.True);
        campaign.AdvanceDays(30);
        // Un projet ordinaire (pas le prototype du tout début de partie) : la file est recréée sans la réduction
        var product = NewProject(campaign, "freelances", []);
        product.Prototype = false;
        foreach (string id in new[] { "login", "dashboard", "subscribe", "ui", "export", "search", "stats", "messaging" })
            Assert.That(campaign.QueueComponent(product.Id, id).Ok, Is.True, id);
        long start = campaign.Minute;
        for (int d = 0; d < 400 && product.Queue.Count > 0; d++)
            campaign.AdvanceDays(1);
        return (campaign.Minute - start) / (double)GameTime.MinutesPerDay;
    }

    [Test]
    public void ABigTeamIsFasterButNotProportionally()
    {
        double one = DaysFor(1), three = DaysFor(3), eight = DaysFor(8);
        TestContext.Out.WriteLine($"1 dév : {one:0} j · 3 dév : {three:0} j · 8 dév : {eight:0} j");
        Assert.That(three, Is.LessThan(one), "embaucher aide");
        Assert.That(eight, Is.LessThan(three));
        Assert.That(eight, Is.GreaterThan(one / 5), "huit personnes vont moins de cinq fois plus vite, même bien organisées");
    }
}
