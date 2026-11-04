using Core.bots;
using Core.campaign;
using UI;

namespace UiTests;

/// <summary>
/// Couverture transversale : tout ce que le moteur écrit dans le journal pendant de vraies parties jouées par les bots
/// (huit parcours, dont deux qui finissent en crise ou en faillite) doit être traduit en anglais à l'affichage.
/// Les noms de produits, de personnes et d'entreprises générés ne sont pas des textes à traduire.
/// </summary>
public class EnglishCoverage_NoticesTest
{
    private static readonly string[] Paths =
    [
        "un_projet", "plusieurs_petits", "materiel_pc", "ia", "quantique", "studio_xr", "marketing_excessif", "recrutement_precoce",
    ];

    [Test]
    public void EveryJournalEntryOfRealBotGamesIsTranslated()
    {
        string previous = Loc.Language;
        var misses = new SortedDictionary<string, string>();
        int seen = 0;
        try
        {
            foreach (string path in Paths)
            {
                var campaign = Campaign.New("Couverture", "Bot", "Bot SA", FounderProfile.Generalist, 23);
                var pilot = new Autopilot(Strategies.All.First(s => s.Name == path));
                int lastId = 0;
                var texts = new HashSet<string>();
                for (int day = 0; day < 540 && campaign.Status == CampaignStatus.Running; day++)
                {
                    pilot.Play(campaign, 1);
                    foreach (var notice in campaign.Notices.Where(n => n.Id > lastId).ToList())
                    {
                        lastId = Math.Max(lastId, notice.Id);
                        texts.Add(notice.Title);
                        if (notice.Detail != "")
                            texts.Add(notice.Detail);
                    }
                }
                texts.Add(campaign.Solvency.LiquidationReport ?? "");
                Loc.Language = "en";
                foreach (string text in texts.Where(t => t != ""))
                {
                    seen++;
                    string english = Loc.Translate(text);
                    if (english == text && Loc.LooksFrench(text))
                        misses[text] = path;
                }
                Loc.Language = "fr";
            }
        }
        finally
        {
            Loc.Language = previous;
        }
        TestContext.Out.WriteLine($"{seen} textes du journal relus");
        Assert.That(seen, Is.GreaterThan(200), "les parties produisent bien des notifications");
        Assert.That(misses, Is.Empty, string.Join("\n", misses.Select(m => $"[{m.Value}] {m.Key}").Take(60)));
    }

    [Test]
    public void MilestonesAndTheirDescriptionsAreTranslated()
    {
        string previous = Loc.Language;
        try
        {
            Loc.Language = "en";
            foreach (var (_, name, description) in Campaign.MilestoneDefs)
            {
                Assert.That(Loc.Translate(name), Is.Not.EqualTo(name), name);
                Assert.That(Loc.Translate(description), Is.Not.EqualTo(description), description);
            }
        }
        finally
        {
            Loc.Language = previous;
        }
    }
}
