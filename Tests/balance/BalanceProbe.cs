using Core.bots;
using Core.campaign;
using Core.finance;
using Core.work;

namespace Tests.balance;

/// <summary>Sonde rapide pour le réglage : une stratégie, une graine, un an. Lancée à la main (catégorie Explicit).</summary>
public class BalanceProbe
{
    [Test, Explicit]
    public void Probe([Values("freelance", "agence", "produit_tot", "produit_soigne", "croissance", "embauche_rapide", "marketing", "sous_qualite", "mixte")] string name)
    {
        var strategy = Strategies.All.First(s => s.Name == name);
        var campaign = Campaign.New("Probe", "Bot", "Bot SA", FounderProfile.Developer, 11);
        var pilot = new Autopilot(strategy);
        for (int month = 1; month <= 12 && campaign.Status == CampaignStatus.Running; month++)
        {
            pilot.Play(campaign, 30);
            string key = GameTime.ToDate(campaign.Minute).AddMonths(-1).ToString("yyyy-MM");
            var totals = campaign.Ledger.Months.GetValueOrDefault(key) ?? new();
            decimal income = totals.Where(t => t.Key is Category.Contracts or Category.Subscriptions or Category.Ads or Category.Commissions).Sum(t => t.Value);
            decimal spend = totals.Where(t => t.Value < 0).Sum(t => t.Value);
            int offers = campaign.Offers.Count();
            double idle = campaign.People.Count(x => x.Assignment.Kind == Core.team.AssignmentKind.None);
            TestContext.Out.Write($"[in {income,7:N0} out {spend,8:N0} offers {offers} idle {idle} pts {campaign.People.Sum(x => x.PointsThisWeek):0}] ");
            var p = pilot.ProductId is int id ? campaign.FindProduct(id) : null;
            TestContext.Out.WriteLine($"M{month,2} cash {campaign.Ledger.Cash,9:N0} rec {campaign.Ledger.OpenReceivables,7:N0} staff {campaign.Staff.Count()} " +
                                      $"contrats {campaign.Contracts.Count(c => c.Status == ContractStatus.Completed)} rep {campaign.MainCompany.Reputation:0} " +
                                      $"{campaign.Solvency.State} {(p == null ? "" : $"| {p.Name} v{p.LiveVersion} users {p.Users:0} pay {p.Paying:0} bugs {p.Bugs:0} rev30 {p.History.TakeLast(30).Sum(d => d.Revenue):N0}")}");
        }
        TestContext.Out.WriteLine(campaign.Status + " " + campaign.Solvency.LiquidationReport);
    }
}
