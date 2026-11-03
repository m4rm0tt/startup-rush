using Core.campaign;
using Core.team;
using static Tests.campaign.CampaignTestKit;

namespace Tests.space;

/// <summary>Chaque personne au travail à son poste est-elle sur une chaise que la scène peut dessiner ?</summary>
public class SeatedProbe
{
    [TestCase(7UL, 6)]
    [TestCase(11UL, 10)]
    public void EveryoneWorkingAtADeskHasAChairToSitOn(ulong seed, int hires)
    {
        var campaign = NewCampaign(seed);
        campaign.Ledger.Cash = 2_000_000;
        campaign.CheatUnlockAll();
        campaign.MoveTo("large");
        for (int i = 0; i < hires; i++)
            campaign.CheatHire();
        int missing = 0, checks = 0;
        for (int day = 0; day < 15; day++)
        {
            campaign.Advance(Core.campaign.GameTime.MinutesPerDay / 24 * 2);
            foreach (var p in campaign.People.Where(p => p.Activity == Activity.Working && p.Assignment.Kind != AssignmentKind.Production))
            {
                checks++;
                var post = campaign.Premises.SeatOf(p.Id);
                bool drawn = post != null && campaign.Premises.ChairOf(post) is { } chair && chair.AssignedPersonId == p.Id;
                if (!drawn)
                {
                    missing++;
                    TestContext.Out.WriteLine($"jour {day} {Core.campaign.GameTime.Hour(campaign.Minute)}h : {p.Name} au travail sans chaise dessinée (poste {(post == null ? "aucun" : post.Desk.DefId)}) à {p.Position}");
                }
            }
            campaign.AdvanceDays(1);
        }
        TestContext.Out.WriteLine($"{checks} vérifications, {missing} sans chaise");
        Assert.That(missing, Is.Zero);
    }
}
