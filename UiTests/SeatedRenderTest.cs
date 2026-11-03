using Avalonia.Headless.NUnit;
using Core.campaign;
using Core.team;
using UI.Iso;

namespace UiTests;

/// <summary>Chaque personne assise à un poste est dessinée (une tête par personne assise), dans un grand local plein.</summary>
public class SeatedRenderTest
{
    [AvaloniaTest]
    public void EverySeatedPersonIsDrawn()
    {
        var campaign = Campaign.New("Test", "Alex", "Studio Léman", FounderProfile.Developer, 11);
        campaign.Ledger.Cash = 2_000_000;
        campaign.CheatUnlockAll();
        campaign.MoveTo("large");
        for (int i = 0; i < 10; i++)
            campaign.CheatHire();
        int checks = 0;
        for (int day = 0; day < 10; day++)
        {
            campaign.AdvanceDays(1);
            campaign.Advance(10 * 60);
            var seated = new Dictionary<int, Seated>();
            foreach (var person in campaign.People.Where(p => p.Activity is Activity.Working or Activity.Home))
                if (campaign.Premises.SeatOf(person.Id) is { } post && campaign.Premises.ChairOf(post) is { } chair)
                    seated[chair.Id] = new Seated(person.Id, person.Appearance);
            var image = RoomRenderer.Render(campaign.Premises, seated);
            var missing = seated.Values.Where(s => !image.Heads.ContainsKey(s.PersonId)).Select(s => campaign.People.FirstOrDefault(p => p.Id == s.PersonId)?.Name).ToList();
            checks += seated.Count;
            Assert.That(missing, Is.Empty, $"jour {day} : assis mais non dessinés");
        }
        TestContext.Out.WriteLine($"{checks} personnes assises vérifiées");
        Assert.That(checks, Is.GreaterThan(20));
    }
}
