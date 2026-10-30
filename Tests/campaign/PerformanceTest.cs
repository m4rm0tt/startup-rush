using System.Diagnostics;
using Core.bots;
using Core.campaign;

namespace Tests.campaign;

/// <summary>
/// Une campagne avancée représentative (stratégie « quantique » jouée 1 100 jours : laboratoire avancé et quantique en service, offres de calcul,
/// trois projets) : un jour de simulation doit rester très en dessous du temps réel le plus rapide du jeu (vitesse 4 : une
/// journée de bureau en 2,7 s), et une sauvegarde doit s'écrire et se relire vite.
/// </summary>
public class PerformanceTest
{
    [Test]
    public void ALateCampaignSimulatesADayAndSavesQuickly()
    {
        var campaign = Campaign.New("Perf", "Bot", "Bot SA", FounderProfile.Generalist, 23);
        new Autopilot(Strategies.All.First(s => s.Name == "quantique")).Play(campaign, 1_100);
        Assert.That(campaign.Status, Is.EqualTo(CampaignStatus.Running));

        var clock = Stopwatch.StartNew();
        campaign.AdvanceDays(60);
        double perDay = clock.Elapsed.TotalMilliseconds / 60;

        clock.Restart();
        string json = Core.persistence.SaveStore.Serialize(campaign);
        var back = Core.persistence.SaveStore.Deserialize(json);
        double save = clock.Elapsed.TotalMilliseconds;

        TestContext.Out.WriteLine($"Jour {campaign.Day} : {campaign.Sites.Count} sites, {campaign.People.Count} personnes, {campaign.Products.Count} projets, " +
                                  $"{campaign.Sites.Sum(s => s.Items.Count)} objets ; {perDay:0.0} ms par jour simulé ; " +
                                  $"sauvegarde de {json.Length / 1024} Ko écrite et relue en {save:0} ms");
        Assert.That(back.People, Has.Count.EqualTo(campaign.People.Count));
        Assert.That(perDay, Is.LessThan(100));
        Assert.That(save, Is.LessThan(2_000));
    }
}
