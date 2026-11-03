using Core.campaign;
using static Tests.campaign.CampaignTestKit;

namespace Tests.campaign;

/// <summary>Les mini-jeux de terminal : variés d'une version à l'autre, reproductibles, notés selon leur règle, de plus en plus fournis.</summary>
public class MiniGamesTest
{
    private static (Campaign Campaign, Core.products.Product Product) Live()
    {
        var campaign = NewCampaign();
        var product = NewProject(campaign);
        FinishQueue(campaign, product);
        campaign.Launch(product.Id);
        return (campaign, product);
    }

    [Test]
    public void SuccessiveVersionsGetDifferentMiniGames()
    {
        var (campaign, product) = Live();
        var kinds = new List<string>();
        for (int i = 0; i < MiniGames.SoftwareKinds.Length; i++)
        {
            kinds.Add(campaign.ReleaseChallenge(product.Id).Kind);
            product.Version++;
        }
        Assert.That(kinds.Distinct().Count(), Is.EqualTo(MiniGames.SoftwareKinds.Length), "chaque genre revient à son tour");
        for (int i = 1; i < kinds.Count; i++)
            Assert.That(kinds[i], Is.Not.EqualTo(kinds[i - 1]), "jamais deux fois le même de suite");
    }

    [Test]
    public void AMiniGameIsReproducible()
    {
        var (a, pa) = Live();
        var (b, pb) = Live();
        var x = a.ReleaseChallenge(pa.Id);
        var y = b.ReleaseChallenge(pb.Id);
        Assert.That(x.Kind, Is.EqualTo(y.Kind));
        Assert.That(x.Items, Is.EqualTo(y.Items));
        Assert.That(x.Answer, Is.EqualTo(y.Answer));
    }

    [Test]
    public void TheLogGameRewardsOnlyRealErrors()
    {
        var challenge = MiniGames.Logs(new SimRandom(3), 2);
        var errors = challenge.Items.Select((t, i) => (t, i)).Where(x => x.t.StartsWith("[ERREUR]")).Select(x => x.i).ToList();
        Assert.That(errors, Is.EquivalentTo(challenge.Answer));
        Assert.That(challenge.Score(errors), Is.EqualTo(1));
        var trap = challenge.Items.Select((t, i) => (t, i)).Where(x => x.t.Contains("erreur") && !x.t.StartsWith("[ERREUR]")).Select(x => x.i).ToList();
        Assert.That(trap, Is.Not.Empty, "avec l'expérience, des lignes piège parlent d'erreur sans en être");
        Assert.That(challenge.Score(errors.Concat(trap).ToList()), Is.LessThan(1), "cocher un piège coûte");
    }

    [Test]
    public void FirewallAndMergeFollowTheirRule()
    {
        var firewall = MiniGames.Firewall(new SimRandom(5), 1);
        Assert.That(firewall.Matching, Is.True);
        Assert.That(firewall.Score(firewall.Answer.ToList()), Is.EqualTo(1));
        Assert.That(firewall.Score(firewall.Answer.Select(a => 1 - a).ToList()), Is.EqualTo(0));
        // « Bloque ce qui vient de 10.0.x.x ou qui vise /admin. » : la règle affichée suffit pour répondre
        var rule = System.Text.RegularExpressions.Regex.Match(firewall.Rule, @"de (\d+\.\d+)\.x\.x ou qui vise (/\w+)");
        Assert.That(rule.Success, Is.True, firewall.Rule);
        for (int i = 0; i < firewall.Items.Count; i++)
        {
            bool blocked = firewall.Items[i].StartsWith(rule.Groups[1].Value + ".") || firewall.Items[i].EndsWith(rule.Groups[2].Value);
            Assert.That(firewall.Answer[i] == 1, Is.EqualTo(blocked), firewall.Items[i]);
        }

        var merge = MiniGames.Merge(new SimRandom(8), 0);
        Assert.That(merge.Options, Is.EqualTo(new[] { "Garder A", "Garder B" }));
        Assert.That(merge.Score(merge.Answer.ToList()), Is.EqualTo(1));
    }

    [Test]
    public void TheCommandMustBeTypedExactly()
    {
        var (campaign, product) = Live();
        product.Name = "Écho Planète";
        var command = MiniGames.Command(new SimRandom(1), product, 2, 1);
        Assert.That(command.Items[0], Is.EqualTo("publier echoplanete v2"), "sans accent ni espace dans le nom");
        Assert.That(command.Score("publier echoplanete v2"), Is.EqualTo(1));
        Assert.That(command.Score("  publier echoplanete v2  "), Is.EqualTo(1));
        Assert.That(command.Score("publier echoplanet v2"), Is.GreaterThan(0.8).And.LessThan(1));
        Assert.That(command.Score("rien"), Is.EqualTo(0));
    }

    [Test]
    public void DifficultyGrowsWithExperience()
    {
        Assert.That(MiniGames.Logs(new SimRandom(1), 3).Items.Count, Is.GreaterThan(MiniGames.Logs(new SimRandom(1), 0).Items.Count));
        Assert.That(MiniGames.Firewall(new SimRandom(1), 3).Items.Count, Is.GreaterThan(MiniGames.Firewall(new SimRandom(1), 0).Items.Count));
        Assert.That(MiniGames.Difficulty(0), Is.EqualTo(0));
        Assert.That(MiniGames.Difficulty(30), Is.EqualTo(3));
    }
}
