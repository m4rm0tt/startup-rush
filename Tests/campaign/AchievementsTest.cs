using System.Text.Json;
using Core.campaign;

namespace Tests.campaign;

/// <summary>Succès Steam préparés : déduits de l'état de la partie, et d'accord avec docs/steam/achievements.json.</summary>
public class AchievementsTest
{
    private static string Root()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "StartupRush.sln")))
            dir = dir.Parent;
        return dir!.FullName;
    }

    [Test]
    public void EveryAchievementHasATitleAndDescriptionInFrenchAndEnglishAndNothingIsMissing()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root(), "docs", "steam", "achievements.json")));
        var listed = doc.RootElement.GetProperty("achievements").EnumerateArray().ToList();
        Assert.That(listed.Select(a => a.GetProperty("apiName").GetString()), Is.EquivalentTo(Campaign.AchievementDefs.Select(a => a.ApiName)));
        foreach (var a in listed)
            foreach (string lang in new[] { "fr", "en" })
            {
                var texts = a.GetProperty(lang).EnumerateArray().Select(t => t.GetString()).ToList();
                Assert.That(texts, Has.Count.EqualTo(2));
                Assert.That(texts, Has.All.Not.Empty);
            }
        Assert.That(Campaign.AchievementDefs.Select(a => a.ApiName), Is.Unique);
    }

    [Test]
    public void AchievementsAreDerivedFromTheStateAndAFreshGameHasNone()
    {
        var campaign = Campaign.New("Succès", "Alex", "Studio Test", FounderProfile.Generalist, 5);
        Assert.That(campaign.EarnedAchievements, Is.Empty);
        campaign.Milestones["first_launch"] = 1;
        campaign.FinalGoalReachedAt = 10;
        Assert.That(campaign.EarnedAchievements, Is.EquivalentTo(new[] { "ACH_FIRST_LAUNCH", "ACH_FINAL_GOAL" }));
    }

    [Test]
    public void ABotPlayingForTwoYearsEarnsTheEarlyAchievements()
    {
        var campaign = Campaign.New("Succès", "Bot", "Bot SA", FounderProfile.Generalist, 11);
        var pilot = new Core.bots.Autopilot(Core.bots.Strategies.All.First(s => s.Name == "plusieurs_petits"));
        for (int day = 0; day < 720 && campaign.Status == CampaignStatus.Running; day++)
            pilot.Play(campaign, 1);
        Assert.That(campaign.EarnedAchievements, Does.Contain("ACH_FIRST_LAUNCH").And.Contain("ACH_FIRST_HIRE"));
    }
}
