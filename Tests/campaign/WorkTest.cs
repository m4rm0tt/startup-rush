using Core.campaign;
using Core.finance;
using Core.products;
using Core.space;
using Core.team;
using Core.work;
using static Tests.campaign.CampaignTestKit;

namespace Tests.campaign;

public class WorkTest
{
    private static double Progress(Campaign campaign, Product product) =>
        campaign.WorkFor(WorkOwner.Product, product.Id).Sum(w => w.Progress);

    [Test]
    public void TheFounderNoLongerDevelopsWithoutTheTerminal()
    {
        var campaign = NewCampaign();
        var product = NewProject(campaign);

        campaign.Advance(9 * 60);

        Assert.That(Progress(campaign, product), Is.EqualTo(0), "aucune production passive du fondateur");
        Assert.That(campaign.Founder.Status, Does.Contain("terminal"));
    }

    [Test]
    public void EachValidatedWordAdvancesTheRealTaskByABoundedAmount()
    {
        var campaign = NewCampaign();
        var product = NewProject(campaign);
        Assert.That(campaign.OpenTerminal(product.Id).Ok, Is.True);
        var session = campaign.Terminal!;

        var wrong = campaign.SubmitWord("pas-le-bon-mot", 1);
        Assert.That(wrong.Correct, Is.False);
        Assert.That(session.Errors, Is.EqualTo(1));
        Assert.That(Progress(campaign, product), Is.EqualTo(0), "une faute ne coûte ni ne rapporte rien");

        double max = Tuning.Terminal.PointsPerWord * 1.3 * Tuning.Terminal.FastFactor;
        for (int i = 0; i < 10; i++)
        {
            var result = campaign.SubmitWord(session.Word, session.Word.Length / 4.0);
            Assert.That(result.Correct, Is.True);
            Assert.That(result.Points, Is.GreaterThan(0).And.LessThanOrEqualTo(max));
        }
        Assert.That(Progress(campaign, product), Is.EqualTo(session.Points).Within(1e-9), "le travail du terminal est celui du projet");
        Assert.That(session.WordsTyped, Is.EqualTo(10));
    }

    [Test]
    public void TypingFasterHelpsButSlowTypingStaysViable()
    {
        double Yield(double charsPerSecond)
        {
            var campaign = NewCampaign();
            var product = NewProject(campaign);
            campaign.OpenTerminal(product.Id);
            string word = campaign.Terminal!.Word;
            return campaign.SubmitWord(word, word.Length / charsPerSecond).Points;
        }

        double slow = Yield(1), normal = Yield(4), fast = Yield(20);
        Assert.That(slow, Is.GreaterThanOrEqualTo(normal * 0.8));
        Assert.That(fast, Is.GreaterThan(normal));
        Assert.That(fast, Is.LessThanOrEqualTo(normal * 1.4), "le bonus de vitesse est plafonné");
    }

    [Test]
    public void TheFirstProjectTakesAFewHundredWords()
    {
        var campaign = NewCampaign();
        var product = NewProject(campaign);

        int words = 0;
        for (int day = 0; day < 60 && product.Queue.Count > 0; day++)
        {
            words += TypeWords(campaign, product, 120);
            campaign.AdvanceDays(1);
        }

        Assert.That(product.Queue, Is.Empty);
        Assert.That(words, Is.InRange(150, 450), "un premier lancement en 10 à 20 minutes de saisie, pas en heures");
        Assert.That(product.Components.Select(c => c.DefId), Is.EquivalentTo(new[] { "login", "dashboard", "subscribe" }));
        Assert.That(product.Components.All(c => c.Level == 1), Is.True, "chaque composant terminé une seule fois");
        Assert.That(campaign.CanLaunch(product), Is.True);
    }

    [Test]
    public void NothingIsCreditedOnceTheQueueIsEmpty()
    {
        var campaign = NewCampaign();
        var product = NewProject(campaign);
        FinishQueue(campaign, product);

        Assert.That(campaign.CanOpenTerminal(product.Id).Ok, Is.False);
        Assert.That(TypeWords(campaign, product, 20), Is.EqualTo(0));
        Assert.That(campaign.WorkFor(WorkOwner.Product, product.Id), Is.Empty);
    }

    [Test]
    public void OnlyOneSessionAtATimeAndNotAtNight()
    {
        var campaign = NewCampaign();
        var product = NewProject(campaign);

        Assert.That(campaign.OpenTerminal(product.Id).Ok, Is.True);
        Assert.That(campaign.OpenTerminal(product.Id).Ok, Is.False, "pas de double session");
        campaign.CloseTerminal();
        Assert.That(campaign.Terminal, Is.Null);

        campaign.Advance(16 * 60); // minuit
        Assert.That(campaign.CanOpenTerminal(product.Id).Ok, Is.False, "pas de travail la nuit");
        campaign.Advance(8 * 60);
        campaign.Vacation(campaign.Founder.Id);
        Assert.That(campaign.CanOpenTerminal(product.Id).Ok, Is.False, "pas de travail en congé");
    }

    [Test]
    public void TheFounderAnswersSupportOnLiveProjectsWithoutDeveloping()
    {
        var campaign = NewCampaign();
        var product = NewProject(campaign);
        FinishQueue(campaign, product);
        campaign.Launch(product.Id);
        product.Users = 400;
        product.SupportBacklog = 60;
        campaign.QueueComponent(product.Id, "ui");

        campaign.Advance(4 * 60);

        Assert.That(product.SupportDone, Is.GreaterThan(0), "le support répond tout seul");
        Assert.That(Progress(campaign, product), Is.EqualTo(0), "mais rien n'avance sans terminal");
        Assert.That(campaign.Founder.Status, Does.Contain("répond"));
    }

    [Test]
    public void DesignThenCodeThenTestsFollowWithoutReassignment()
    {
        var campaign = NewCampaign();
        var developer = HireDeveloper(campaign);
        var product = NewProject(campaign);

        for (int i = 0; i < 120 && product.Queue.Count > 0; i++)
            campaign.AdvanceDays(1);

        Assert.That(product.Queue, Is.Empty, "l'employé seul vide la file");
        Assert.That(product.Components.All(c => c.Level == 1 && c.Quality > 0.2), Is.True);
        Assert.That(developer.Assignment.Kind, Is.EqualTo(AssignmentKind.None), "aucune affectation manuelle");
    }

    [Test]
    public void WithoutAComputerNothingGetsDone()
    {
        var campaign = NewCampaign();
        var developer = HireDeveloper(campaign);
        var post = campaign.Premises.SeatOf(developer.Id)!;
        campaign.Store(campaign.Premises.ComputerOf(post)!.Id);
        var product = NewProject(campaign);

        campaign.Advance(6 * 60);

        Assert.That(Progress(campaign, product), Is.EqualTo(0));
        Assert.That(developer.Status, Does.Contain("ordinateur"));
    }

    [Test]
    public void APersonWorksOnOneProjectAtATime()
    {
        var campaign = NewCampaign();
        var developer = HireDeveloper(campaign);
        var first = NewProject(campaign);
        var second = NewProject(campaign, "casual");
        campaign.Assign(developer.Id, second.Id);

        campaign.Advance(6 * 60);

        Assert.That(Progress(campaign, first), Is.EqualTo(0));
        Assert.That(Progress(campaign, second), Is.GreaterThan(1).And.LessThan(8));
    }

    [Test]
    public void AutomaticModeFollowsThePortfolioPriority()
    {
        var campaign = NewCampaign();
        HireDeveloper(campaign);
        var first = NewProject(campaign);
        var second = NewProject(campaign, "casual");
        campaign.MoveProject(second.Id, -1);

        campaign.Advance(6 * 60);

        Assert.That(campaign.AutoTarget(), Is.SameAs(second));
        Assert.That(Progress(campaign, first), Is.EqualTo(0));
        Assert.That(Progress(campaign, second), Is.GreaterThan(1));
    }

    [Test]
    public void AnOversizedTeamLosesEfficiencyWithoutCoordination()
    {
        var campaign = NewCampaign();
        var developer = HireDeveloper(campaign);
        double alone = campaign.WorkRate(developer, Discipline.Code, teamSize: 1);
        double crowd = campaign.WorkRate(developer, Discipline.Code, teamSize: 8);
        double led = campaign.WorkRate(developer, Discipline.Code, teamSize: 8, lead: true);

        Assert.That(crowd, Is.LessThan(alone * 0.65));
        Assert.That(led, Is.GreaterThan(crowd));
    }

    [Test]
    public void BetterComputersHelpWithDiminishingReturns()
    {
        var campaign = NewCampaign();
        var tower = campaign.Premises.ComputerOf(campaign.Premises.SeatOf(campaign.Founder.Id)!)!;
        Assert.That(tower.DefId, Is.EqualTo("tower_used"), "la vieille tour de la chambre");
        double used = campaign.WorkRate(campaign.Founder, Discipline.Code);
        tower.DefId = "tower_office";
        double office = campaign.WorkRate(campaign.Founder, Discipline.Code);
        tower.DefId = "workstation_dev";
        double pro = campaign.WorkRate(campaign.Founder, Discipline.Code);

        Assert.That(office, Is.GreaterThan(used));
        Assert.That(pro - office, Is.LessThan(office - used));
    }

    [Test]
    public void ExtraScreensHelpAPostUpToACap()
    {
        var campaign = NewCampaign();
        campaign.Ledger.Cash = 50_000;
        var post = campaign.Premises.SeatOf(campaign.Founder.Id)!;
        double before = campaign.StationOutput(campaign.Founder);

        Assert.That(campaign.BuyAndAttach("monitor_arm_second", post.Desk.Id, "monitor_second").Ok, Is.True);
        double second = campaign.StationOutput(campaign.Founder);
        campaign.Store(campaign.Premises.ChildAt(post.Desk, "monitor_main")!.Id);
        campaign.BuyAndAttach("monitor_large", post.Desk.Id, "monitor_main");
        double both = campaign.StationOutput(campaign.Founder);

        Assert.That(second, Is.EqualTo(before * 1.03).Within(1e-9));
        Assert.That(both, Is.EqualTo(before * (1 + Layout.MaxScreenBonus)).Within(1e-9), "plafond par poste");
    }

    [Test]
    public void ANewHireWithoutAWorkstationDoesNotProduce()
    {
        var campaign = NewCampaign();
        var candidate = campaign.Candidates.First();
        candidate.NoticeWorkdays = 0;
        campaign.Hire(candidate.Person.Id);
        var product = NewProject(campaign);

        campaign.Advance(6 * 60);

        Assert.That(Progress(campaign, product), Is.EqualTo(0));
        Assert.That(candidate.Person.Activity, Is.EqualTo(Activity.Idle));
        Assert.That(candidate.Person.Status, Does.Contain("poste"));
    }

    [Test]
    public void SeatedPeopleAreAtTheirDesk()
    {
        var campaign = NewCampaign();
        NewProject(campaign);
        campaign.Advance(90);

        var seat = campaign.Premises.SeatCell(campaign.Premises.SeatOf(campaign.Founder.Id)!);
        Assert.That(campaign.Founder.Activity, Is.EqualTo(Activity.Working));
        Assert.That(campaign.Founder.Position, Is.EqualTo(seat));
    }

    [Test]
    public void SalariesAndRentAreBilledOncePerMonth()
    {
        var campaign = NewCampaign();
        campaign.Ledger.Cash = 100_000;
        campaign.MoveTo("small");
        campaign.AdvanceDays(70);

        var rents = campaign.Ledger.Bills.Where(b => b.Category == Category.Rent).ToList();
        var draws = campaign.Ledger.Bills.Where(b => b.Category == Category.FounderDraw).ToList();
        Assert.That(rents.Select(b => GameTime.MonthKey(b.IssuedAt)).Distinct().Count(), Is.EqualTo(rents.Count));
        Assert.That(draws.Select(b => GameTime.MonthKey(b.IssuedAt)).Distinct().Count(), Is.EqualTo(draws.Count));
        Assert.That(rents, Has.Count.EqualTo(2));
    }
}
