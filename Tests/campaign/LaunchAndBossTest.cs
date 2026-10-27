using Core.campaign;
using Core.team;
using static Tests.campaign.CampaignTestKit;

namespace Tests.campaign;

/// <summary>Lot 3 : mini-jeux de lancement, délégation et outils du boss.</summary>
public class LaunchAndBossTest
{
    private static Core.products.Product Ready(Campaign campaign, string segment = "freelances")
    {
        var product = NewProject(campaign, segment);
        FinishQueue(campaign, product);
        return product;
    }

    [Test]
    public void TheDeployChallengeIsStableAndScoresTheOrder()
    {
        var campaign = NewCampaign();
        var product = Ready(campaign);
        var challenge = campaign.Challenge(product.Id);

        Assert.That(challenge.Ordering, Is.True);
        Assert.That(campaign.Challenge(product.Id).Items, Is.EqualTo(challenge.Items), "même graine, même mini-jeu");
        Assert.That(challenge.Score(challenge.Answer), Is.EqualTo(1));
        Assert.That(challenge.Score(Enumerable.Range(0, challenge.Items.Count).ToList()), Is.LessThan(1), "l'ordre présenté est mélangé");
    }

    [Test]
    public void TheGameReviewPenalisesFalseAlarms()
    {
        var campaign = NewCampaign();
        var game = NewProject(campaign, "casual");
        var challenge = campaign.Challenge(game.Id);

        Assert.That(challenge.Ordering, Is.False);
        Assert.That(challenge.Answer, Has.Count.EqualTo(3));
        foreach (int i in challenge.Answer)
        {
            var parts = challenge.Items[i].Split(' ');
            Assert.That(parts[^3].TrimEnd(','), Is.Not.EqualTo(parts[^1]), "une anomalie ne donne pas le résultat attendu");
        }
        Assert.That(challenge.Score(challenge.Answer), Is.EqualTo(1));
        Assert.That(challenge.Score(Enumerable.Range(0, 8).ToList()), Is.EqualTo(0), "tout cocher ne rapporte rien");
    }

    [Test]
    public void TheLaunchBonusIsCappedAndPlayingIsOptional()
    {
        var a = NewCampaign();
        var b = NewCampaign();
        var pa = Ready(a);
        var pb = Ready(b);
        pa.Bugs = pb.Bugs = 10;

        Assert.That(a.LaunchWith(pa.Id, 1).Ok, Is.True);
        Assert.That(b.LaunchWith(pb.Id, 0).Ok, Is.True, "on peut lancer sans jouer");
        Assert.That(pa.Bugs, Is.EqualTo(10 * (1 - Campaign.LaunchBugReduction)).Within(0.1));
        Assert.That(pb.Bugs, Is.EqualTo(10));
        Assert.That(a.LaunchWith(pa.Id, 1).Ok, Is.False, "pas deux fois");
    }

    [Test]
    public void DelegationNeedsTheNodeAndAnEmployeeThenLaunchesAlone()
    {
        var campaign = NewCampaign();
        var product = Ready(campaign);
        Assert.That(campaign.CanDelegateLaunch(product).Ok, Is.False);
        campaign.Unlocked.Add("delegation");
        Assert.That(campaign.CanDelegateLaunch(product).Reason, Does.Contain("employé"));
        HireDeveloper(campaign);
        campaign.DelegateLaunches = true;

        campaign.AdvanceDays(1);

        Assert.That(product.IsLive, Is.True, "le projet prêt sort tout seul");
    }

    [Test]
    public void BossToolsChangeRealTerminalCapacities()
    {
        double FirstWord(Campaign c, Core.products.Product p)
        {
            c.OpenTerminal(p.Id);
            double points = c.SubmitWord(c.Terminal!.Word, c.Terminal.Word.Length / 4.0).Points;
            c.CloseTerminal();
            return points;
        }
        var plain = NewCampaign();
        var fast = NewCampaign();
        fast.Unlocked.UnionWith(["boss_speed", "boss_comfort", "boss_macros"]);
        double basePoints = FirstWord(plain, NewProject(plain));
        var fastProject = NewProject(fast);
        double energy = fast.Founder.Energy;
        double fastPoints = FirstWord(fast, fastProject);

        Assert.That(fastPoints, Is.EqualTo(basePoints * 1.15).Within(1e-9));
        Assert.That(energy - fast.Founder.Energy, Is.EqualTo(Tuning.Terminal.EnergyPerWord * 0.6).Within(1e-9));
        fast.OpenTerminal(fastProject.Id);
        Assert.That(fast.Terminal!.Length, Is.EqualTo(Tuning.Terminal.WordsPerSessionWithMacros));
    }
}
