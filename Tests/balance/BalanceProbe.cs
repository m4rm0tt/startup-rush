using Core.bots;
using Core.campaign;
using Core.finance;

namespace Tests.balance;

/// <summary>Sonde de réglage : une stratégie, une graine, 18 mois, une ligne par mois. Lancée à la main.</summary>
public class BalanceProbe
{
    [Test, Explicit]
    public void Probe([Values("un_projet", "plusieurs_petits", "ambitieux", "jeu_simple", "marketing_excessif", "recrutement_precoce", "trop_de_projets", "exces_puis_correction", "delegation", "hebergement_maison")] string name)
    {
        var strategy = Strategies.All.First(s => s.Name == name);
        var campaign = Campaign.New("Probe", "Bot", "Bot SA", FounderProfile.Generalist, 11);
        var pilot = new Autopilot(strategy);
        for (int month = 1; month <= 18 && campaign.Status == CampaignStatus.Running; month++)
        {
            pilot.Play(campaign, 30);
            string key = GameTime.ToDate(campaign.Minute).AddMonths(-1).ToString("yyyy-MM");
            decimal result = campaign.Ledger.Months.TryGetValue(key, out var t) ? Campaign.OperatingResult(t) : 0;
            string projects = string.Join(" | ", campaign.Products.Select(p => $"{p.Name}{(p.IsLive ? $" v{p.Version}" : $" {p.Queue.Count}q")} u{p.Users:0} pay{p.Paying:0.0} rev30 {Campaign.Result30(p):N0} bugs{p.Bugs:0} {string.Join(",", p.Components.Select(c => c.DefId + c.Level))}"));
            string costs = t == null ? "" : string.Join(" ", t.Where(x => x.Value != 0 && x.Key is Category.Rent or Category.Energy or Category.Hosting or Category.Cloud or Category.Equipment).Select(x => $"{x.Key}:{x.Value:N0}"));
            TestContext.Out.WriteLine($"M{month,2} cash {campaign.Ledger.Cash,8:N0} res {result,7:N0} [{costs}] host {campaign.Infra().Hosting:0}/{campaign.HostingLeft:0} cloud {string.Join(",", campaign.CloudOffers.Select(o => $"{o.Kind}:{o.Served:0}/{o.Demand:0}/{o.Reserved:0}@{o.Price}"))} staff {campaign.Staff.Count()} pts {campaign.DevPoints} [{string.Join(",", campaign.Unlocked)}] {campaign.Solvency.State} {projects}");
        }
        TestContext.Out.WriteLine($"{campaign.Status} actions {pilot.Actions} {campaign.Solvency.LiquidationReport}");
    }
}
