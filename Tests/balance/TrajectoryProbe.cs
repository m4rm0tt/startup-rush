using Core.bots;
using Core.campaign;

namespace Tests.balance;

/// <summary>Trajectoire d'une partie de bot, tous les 6 mois : sert à comprendre pourquoi une voie plafonne (lancée à la main).</summary>
public class TrajectoryProbe
{
    [Test, Explicit]
    public void Trajectory()
    {
        var names = (Environment.GetEnvironmentVariable("TRAJ_PATHS") ?? "ia,studio_xr,plusieurs_petits").Split(',');
        ulong seed = ulong.Parse(Environment.GetEnvironmentVariable("TRAJ_SEED") ?? "11");
        int months = int.Parse(Environment.GetEnvironmentVariable("TRAJ_MONTHS") ?? "72");
        var lines = new System.Text.StringBuilder();
        foreach (string name in names)
        {
            var campaign = Campaign.New("Trajectoire", "Bot", "Bot SA", FounderProfile.Generalist, seed);
            var pilot = new Autopilot(Strategies.All.First(s => s.Name == name));
            lines.AppendLine($"## {name} (graine {seed})");
            lines.AppendLine("| mois | argent | valorisation | revenus 30 j | bénéfice produits 30 j | produits en ligne | utilisateurs | équipe | sites |");
            lines.AppendLine("|---|---|---|---|---|---|---|---|---|");
            for (int day = 1; day <= months * 30 && campaign.Status == CampaignStatus.Running; day++)
            {
                pilot.Play(campaign, 1);
                if (day % 180 == 0)
                {
                    decimal profit = campaign.Products.Sum(p => p.History.TakeLast(30).Sum(d => d.Revenue - d.Costs));
                    lines.AppendLine($"| {day / 30} | {campaign.Ledger.Cash:N0} | {campaign.Valuation():N0} | {campaign.RecurringRevenue30:N0} | {profit:N0} | {campaign.Products.Count(p => p.IsLive)}/{campaign.Products.Count} | {campaign.Products.Sum(p => p.Users):N0} | {campaign.People.Count} | {campaign.Sites.Count} |");
                }
            }
            lines.AppendLine();
        }
        File.WriteAllText(Path.Combine(Environment.GetEnvironmentVariable("TRAJ_OUT") ?? Path.GetTempPath(), "trajectoire.md"), lines.ToString());
        TestContext.Out.WriteLine(lines.ToString());
    }
}
