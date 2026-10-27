using Core.campaign;
using Core.progress;
using Core.team;
using static Tests.campaign.CampaignTestKit;

namespace Tests.campaign;

public class ProgressAndTeamTest
{
    [Test]
    public void TheStartOffersAFirstChoiceAndBasicToolsAreFree()
    {
        var campaign = NewCampaign();

        Assert.That(campaign.DevPoints, Is.EqualTo(DevTree.StartingPoints));
        Assert.That(campaign.TypeAvailable("saas"), Is.True);
        Assert.That(campaign.TypeAvailable("game"), Is.True);
        Assert.That(campaign.TypeAvailable("social"), Is.False);
        Assert.That(DevTree.Nodes.Where(n => n.Cost <= DevTree.StartingPoints && n.Requires == null), Is.Not.Empty);
    }

    [Test]
    public void UnlockingNeedsPointsAndPrerequisites()
    {
        var campaign = NewCampaign();
        campaign.DevPoints = 10;

        Assert.That(campaign.Unlock("multiplayer").Ok, Is.False, "il faut d'abord les composants avancés");
        Assert.That(campaign.Unlock("advanced").Ok, Is.True);
        Assert.That(campaign.Unlock("advanced").Ok, Is.False, "déjà acquis");
        Assert.That(campaign.Unlock("multiplayer").Ok, Is.True);
        Assert.That(campaign.DevPoints, Is.EqualTo(10 - 1 - 2));
    }

    [Test]
    public void AnUnlockedTypeBecomesPlayable()
    {
        var campaign = NewCampaign();
        Assert.That(campaign.CreateProject("Agora", "students", Core.products.RevenueModel.Ads).Result.Ok, Is.False);

        campaign.DevPoints = 2;
        campaign.Unlock("community");

        Assert.That(campaign.CreateProject("Agora", "students", Core.products.RevenueModel.Ads).Result.Ok, Is.True);
    }

    [Test]
    public void EmptyProjectsEarnNoPointsAndEachAchievementCountsOnce()
    {
        var campaign = NewCampaign();
        int start = campaign.DevPoints;
        for (int i = 0; i < 5; i++)
        {
            var empty = NewProject(campaign, "freelances", ["login", "support"]);
            FinishQueue(campaign, empty);
            campaign.Launch(empty.Id);
        }
        campaign.AdvanceDays(3);
        Assert.That(campaign.DevPoints, Is.EqualTo(start), "lancer des coquilles vides ne rapporte rien");

        var real = campaign.Products[0];
        real.Users = 600;
        campaign.AdvanceDays(2);
        int afterAdoption = campaign.DevPoints;
        campaign.AdvanceDays(5);

        Assert.That(afterAdoption, Is.EqualTo(start + 2 + 1), "adoption (50) puis 500 utilisateurs");
        Assert.That(campaign.DevPoints, Is.EqualTo(afterAdoption), "pas de points en double");
    }

    [Test]
    public void VacationRestoresEnergyAndTakesThePersonAway()
    {
        var campaign = NewCampaign();
        NewProject(campaign);
        campaign.Founder.Energy = 30;
        double morale = campaign.Founder.Morale;

        campaign.Vacation(campaign.Founder.Id);
        campaign.Advance(4 * 60);

        Assert.That(campaign.Founder.Activity, Is.EqualTo(Activity.Away));
        Assert.That(campaign.Founder.Morale, Is.GreaterThan(morale));
        campaign.AdvanceDays(1);
        Assert.That(campaign.Founder.Energy, Is.EqualTo(100));
    }

    [Test]
    public void RaisesShowTheirCostAndAreLimited()
    {
        var campaign = NewCampaign();
        var candidate = campaign.Candidates.First();
        campaign.Hire(candidate.Person.Id);
        var person = candidate.Person;
        person.Morale = 40;

        var first = campaign.RaiseTerms(person);
        Assert.That(first.Allowed, Is.True);
        Assert.That(first.ExtraPerMonth, Is.GreaterThan(0));
        decimal salary = person.Salary;
        campaign.Raise(person.Id);
        Assert.That(person.Salary, Is.EqualTo(salary + first.ExtraPerMonth));

        var second = campaign.RaiseTerms(person);
        Assert.That(second.Morale, Is.EqualTo(first.Morale / 2), "la deuxième augmentation fait moins d'effet");
        campaign.Raise(person.Id);
        Assert.That(campaign.RaiseTerms(person).Allowed, Is.False, "deux par an au plus");
    }

    [Test]
    public void TrainingIsCheaperWithTheAcademy()
    {
        var campaign = NewCampaign();
        decimal normal = campaign.TrainingPrice;
        campaign.Unlocked.Add("academy");

        Assert.That(campaign.TrainingPrice, Is.EqualTo(normal / 2));
    }

    [Test]
    public void RepeatedProblemsAreGroupedAndResolvedOnceFixed()
    {
        var campaign = NewCampaign();
        campaign.Notify(Core.campaign.Severity.Warning, "projet", "Panne", key: "outage:1");
        campaign.Notify(Core.campaign.Severity.Warning, "projet", "Panne", key: "outage:1");

        var notice = campaign.Notices.Single(n => n.Key == "outage:1");
        Assert.That(notice.Count, Is.EqualTo(2));

        campaign.Resolve("outage:1");
        Assert.That(notice.Resolved, Is.True);
        campaign.Notify(Core.campaign.Severity.Warning, "projet", "Panne", key: "outage:1");
        Assert.That(campaign.Notices.Count(n => n.Key == "outage:1"), Is.EqualTo(2), "un nouveau problème après résolution");
    }
}

public class AppearanceTest
{
    [Test]
    public void RecruitsAndFoundersUseOnlyPackLayersAndKeepTheirLook()
    {
        var campaign = Campaign.New("Test", "Alex", "Studio", FounderProfile.Developer, 5,
            new Core.team.Appearance { Base = "player_f", Hair = "hair_bun", HairRamp = "hair_auburn", Skin = "skin_deep" });
        var random = new SimRandom(11);
        var bases = new HashSet<string>();
        var hairs = new HashSet<string>();
        for (int i = 0; i < 400; i++)
        {
            var look = Core.team.Recruitment.Generate(random, 0, i, 0).Person.Appearance;
            Assert.That(Core.space.Pack.Asset(look.Base).Kind, Is.EqualTo("character_base"), look.Base);
            Assert.That(Core.space.Pack.Asset(look.Hair).Kind, Is.EqualTo("character_hair"), look.Hair);
            Assert.That(Core.team.Appearance.HairRamps, Does.Contain(look.HairRamp));
            Assert.That(Core.team.Appearance.Skins, Does.Contain(look.Skin));
            bases.Add(look.Base);
            hairs.Add(look.Hair);
        }
        Assert.That(bases, Has.Count.EqualTo(10), "les dix bases d'employés apparaissent");
        Assert.That(hairs, Has.Count.EqualTo(6), "les six coiffures apparaissent");

        var reloaded = Core.persistence.SaveStore.Deserialize(Core.persistence.SaveStore.Serialize(campaign));
        Assert.That(reloaded.Founder.Appearance.Base, Is.EqualTo("player_f"));
        Assert.That(reloaded.Founder.Appearance.Hair, Is.EqualTo("hair_bun"));
        Assert.That(campaign.WorkRate(campaign.Founder, Core.team.Discipline.Code),
            Is.EqualTo(Campaign.New("Test", "Alex", "Studio", FounderProfile.Developer, 5).WorkRate(campaign.Founder, Core.team.Discipline.Code)),
            "l'apparence ne change rien à la simulation");
    }
}
