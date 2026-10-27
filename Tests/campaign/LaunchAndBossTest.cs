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

    [Test]
    public void ReadyProjectsMakeASingleGroupedProposal()
    {
        var campaign = NewCampaign();
        var first = Ready(campaign);
        var second = Ready(campaign, "casual");

        var proposals = campaign.Notices.Where(n => n.Key == Campaign.ReadyKey && !n.Resolved).ToList();
        Assert.That(proposals, Has.Count.EqualTo(1), "une seule sollicitation pour deux projets prêts");
        Assert.That(proposals[0].Title, Does.Contain("2 projets"));
        Assert.That(proposals[0].Severity, Is.EqualTo(Severity.Good), "proposée par une bulle");

        Assert.That(campaign.Launch(first.Id).Ok, Is.True);
        Assert.That(proposals[0].Resolved, Is.False);
        Assert.That(proposals[0].Title, Does.Contain(second.Name));
        Assert.That(campaign.Launch(second.Id).Ok, Is.True);
        Assert.That(proposals[0].Resolved, Is.True, "plus rien à lancer");
    }

    [Test]
    public void TheBigActionPreferenceAndDelegationKeepTheGameQuiet()
    {
        var quiet = NewCampaign();
        quiet.ProposeBigActions = false;
        Ready(quiet);
        Assert.That(quiet.Notices.Single(n => n.Key == Campaign.ReadyKey).Severity, Is.EqualTo(Severity.Info), "dans le journal seulement");

        var delegated = NewCampaign();
        delegated.Unlocked.Add("delegation");
        delegated.DelegateLaunches = true;
        HireDeveloper(delegated);
        var product = NewProject(delegated);
        FinishQueue(delegated, product);
        Assert.That(delegated.Notices.Any(n => n.Key == Campaign.ReadyKey && !n.Resolved && !product.IsLive), Is.False,
            "un employé s'en charge : rien à proposer");
        delegated.AdvanceDays(1);
        Assert.That(product.IsLive, Is.True);
    }

    [Test]
    public void RemoteSupervisionOpensTheTerminalAwayFromTheDesk()
    {
        var campaign = NewCampaign();
        var product = NewProject(campaign);
        campaign.Premises.Items.RemoveAll(i => i.DefId == "tower_used");
        campaign.InvalidateLayout();
        Assert.That(campaign.CanOpenTerminal(product.Id).Ok, Is.False, "sans ordinateur");

        campaign.Unlocked.Add("boss_remote");
        Assert.That(campaign.CanOpenTerminal(product.Id).Ok, Is.True);
        campaign.OpenTerminal(product.Id);
        var word = campaign.SubmitWord(campaign.Terminal!.Word, campaign.Terminal.Word.Length / 4.0);
        campaign.CloseTerminal();

        var reference = NewCampaign();
        var other = NewProject(reference);
        reference.OpenTerminal(other.Id);
        double withTower = reference.SubmitWord(reference.Terminal!.Word, reference.Terminal.Word.Length / 4.0).Points;
        Assert.That(word.Points, Is.EqualTo(withTower).Within(1e-9), "à distance : le rendement d'un vieux portable (la vieille tour aussi : ×0,85)");
        Assert.That(word.Points, Is.GreaterThan(0));
    }

    [Test]
    public void AssistedTypingYieldsTheSlowRateWhateverTheSpeed()
    {
        double Word(bool assisted, double seconds)
        {
            var c = NewCampaign();
            var p = NewProject(c);
            c.OpenTerminal(p.Id);
            return c.SubmitWord(c.Terminal!.Word, seconds, assisted).Points;
        }
        double slow = Word(false, 30);
        Assert.That(Word(true, 0.1), Is.EqualTo(slow).Within(1e-9), "valider vite ne rapporte pas plus");
        Assert.That(Word(false, 0.1), Is.GreaterThan(slow), "une vraie frappe rapide rapporte davantage");
    }

    [Test]
    public void AdvancedAutomationQueuesSmallUpdatesWithAPaidTeam()
    {
        Campaign Live(bool unlocked, bool staff)
        {
            var c = NewCampaign();
            var p = Ready(c);
            Assert.That(c.Launch(p.Id).Ok, Is.True);
            if (unlocked)
                c.Unlocked.UnionWith(["delegation", "procedures", "automation"]);
            if (staff)
                HireDeveloper(c);
            // Jusqu'au lundi suivant (au plus 7 jours), sans rien faire
            for (int d = 0; d < 7 && p.Queue.Count == 0; d++)
                c.AdvanceDays(1);
            return c;
        }
        Assert.That(Live(true, true).Products.Single().Queue, Is.Not.Empty, "l'équipe a préparé une mise à jour");
        Assert.That(Live(false, true).Products.Single().Queue, Is.Empty, "sans le nœud");
        Assert.That(Live(true, false).Products.Single().Queue, Is.Empty, "le fondateur seul ne travaille pas en secret");
    }
}
