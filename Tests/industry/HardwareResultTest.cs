using Core.bots;
using Core.campaign;
using Core.industry;

namespace Tests.industry;

/// <summary>Le résultat d'un produit matériel (et donc la valorisation) compte les matières des unités vendues.</summary>
public class HardwareResultTest
{
    [Test]
    public void EveryHardwareSaleDayCountsTheMaterialsOfTheUnitsSold()
    {
        var campaign = Campaign.New("Matériel", "Bot", "Bot SA", FounderProfile.Generalist, 11);
        var pilot = new Autopilot(Strategies.All.First(s => s.Name == "materiel_pc"));
        int checkedDays = 0;
        for (int day = 0; day < 450 && campaign.Status == CampaignStatus.Running; day++)
        {
            pilot.Play(campaign, 1);
            // Le jour qui vient de se terminer : ses coûts contiennent les matières des unités vendues
            foreach (var product in campaign.Products.Where(p => p.IsHardware && p.IsLive && p.History.Count > 0))
            {
                var last = product.History[^1];
                if (last.Sold <= 0)
                    continue;
                decimal materials = Math.Round((decimal)last.Sold * IndustryCatalog.UnitCost(product), 2);
                Assert.That(materials, Is.GreaterThan(0));
                Assert.That(last.Costs, Is.GreaterThanOrEqualTo(materials - 0.01m), $"jour {campaign.Day} : {product.Name}");
                checkedDays++;
            }
        }
        Assert.That(checkedDays, Is.GreaterThan(30), "des unités se sont vendues pendant la partie");
    }
}
