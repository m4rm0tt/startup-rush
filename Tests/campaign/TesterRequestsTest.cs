using Core.campaign;
using Core.persistence;
using Core.space;
using Core.team;
using static Tests.campaign.CampaignTestKit;

namespace Tests.campaign;

/// <summary>
/// Demandes des testeurs, lot 1 : nom d'entreprise obligatoire, tutoriel validé par les vraies actions, fondateur qui dort dans
/// son lit, série au terminal, réglages de pause.
/// </summary>
public class TesterRequestsTest
{
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("\t")]
    [TestCase("A")]
    public void ABlankOrTooShortCompanyNameIsRefused(string name)
    {
        Assert.That(Campaign.CompanyNameError(name), Is.Not.Null);
        Assert.Throws<ArgumentException>(() => Campaign.New("Test", "Alex", name, FounderProfile.Developer, 1));
    }

    [Test]
    public void ACompanyNameIsTrimmedAndSurvivesASave()
    {
        Assert.That(Campaign.CompanyNameError(new string('x', Campaign.CompanyNameMax + 1)), Is.Not.Null);
        var campaign = Campaign.New("Test", "Alex", "  Studio Léman  ", FounderProfile.Developer, 1);
        Assert.That(campaign.MainCompany.Name, Is.EqualTo("Studio Léman"));
        var loaded = SaveStore.Deserialize(SaveStore.Serialize(campaign));
        Assert.That(loaded.MainCompany.Name, Is.EqualTo("Studio Léman"));
        Assert.That(SaveStore.Summarize(loaded).Company, Is.EqualTo("Studio Léman"));
    }

    [Test]
    public void TheTutorialAdvancesOnRealActionsAndRewardsTheEnd()
    {
        var campaign = NewCampaign();
        campaign.TutorialOn = true;
        Assert.That(campaign.CurrentTutorialStep!.Id, Is.EqualTo("view"));
        Assert.That(campaign.CheckTutorial(), Is.False, "rien n'est validé sans action");

        campaign.TutorialEvent("camera");
        Assert.That(campaign.CurrentTutorialStep!.Id, Is.EqualTo("founder"));
        campaign.TutorialEvent("select:founder");
        Assert.That(campaign.CurrentTutorialStep!.Id, Is.EqualTo("project"));

        var product = NewProject(campaign);
        campaign.CheckTutorial();
        Assert.That(campaign.CurrentTutorialStep!.Id, Is.EqualTo("terminal"));
        TypeWords(campaign, product, 8);
        campaign.CheckTutorial();
        Assert.That(campaign.CurrentTutorialStep!.Id, Is.EqualTo("streak"));

        FinishQueue(campaign, product);
        campaign.CheckTutorial();
        Assert.That(campaign.CurrentTutorialStep!.Id, Is.EqualTo("launch"));
        Assert.That(campaign.Launch(product.Id).Ok, Is.True);
        campaign.CheckTutorial();
        Assert.That(campaign.CurrentTutorialStep!.Id, Is.EqualTo("time"));

        // Une étape faite en avance est validée d'office en y arrivant
        campaign.TutorialEvent("open:finances");
        campaign.TutorialEvent("speed");
        Assert.That(campaign.CurrentTutorialStep!.Id, Is.EqualTo("reviews"));
        campaign.TutorialEvent("tab:reviews");
        Assert.That(campaign.CurrentTutorialStep!.Id, Is.EqualTo("update"));

        var saved = SaveStore.Deserialize(SaveStore.Serialize(campaign));
        Assert.That(saved.CurrentTutorialStep!.Id, Is.EqualTo("update"), "l'étape est sauvegardée");

        Assert.That(campaign.QueueComponent(product.Id, "ui").Ok, Is.True);
        campaign.CheckTutorial();
        Assert.That(campaign.CurrentTutorialStep!.Id, Is.EqualTo("build"), "les finances étaient déjà vues");
        int points = campaign.DevPoints;
        campaign.TutorialEvent("open:build");
        Assert.That(campaign.CurrentTutorialStep, Is.Null);
        Assert.That(campaign.TutorialOn, Is.False);
        Assert.That(campaign.DevPoints, Is.EqualTo(points + Campaign.TutorialReward));
    }

    [Test]
    public void SkippingTheTutorialStopsItWithoutReward()
    {
        var campaign = NewCampaign();
        campaign.TutorialOn = true;
        int points = campaign.DevPoints;
        campaign.SkipTutorial();
        Assert.That(campaign.CurrentTutorialStep, Is.Null);
        campaign.TutorialEvent("camera");
        Assert.That(campaign.DevPoints, Is.EqualTo(points));
        Assert.That(campaign.TutorialEvents, Is.Empty, "un tutoriel arrêté n'écoute plus rien");
    }

    [Test]
    public void TheFounderSleepsInTheBedAtNightAndStaysHomeInTheEvening()
    {
        var campaign = NewCampaign();
        var founder = campaign.Founder;
        var bed = campaign.BedOf(founder);
        Assert.That(bed, Is.Not.Null, "la chambre de départ a un lit");

        campaign.Advance(2 * 60); // lundi 10 h
        Assert.That(founder.Activity, Is.EqualTo(Activity.Working));

        campaign.Advance(9 * 60); // 19 h : chez lui, à son bureau
        Assert.That(founder.Activity, Is.EqualTo(Activity.Home));
        Assert.That(campaign.AnyoneInOffice, Is.False, "les nuits passent vite même s'il est chez lui");

        campaign.Advance(5 * 60); // minuit : dans son lit
        Assert.That(founder.Activity, Is.EqualTo(Activity.Sleeping));
        Assert.That(founder.Position, Is.EqualTo(new Cell(bed!.X, bed.Y)));

        campaign.Advance(7 * 60 + 30); // 7 h 30 : levé
        Assert.That(founder.Activity, Is.EqualTo(Activity.Home));
        campaign.Advance(2 * 60); // 9 h 30 : au travail
        Assert.That(founder.Activity, Is.EqualTo(Activity.Working));
    }

    [Test]
    public void WithoutABedTheFounderGoesHomeLikeEveryoneElse()
    {
        var campaign = NewCampaign();
        var bed = campaign.BedOf(campaign.Founder)!;
        campaign.Premises.Items.Remove(bed);
        campaign.InvalidateLayout();
        campaign.Advance(16 * 60); // minuit
        Assert.That(campaign.Founder.Activity, Is.EqualTo(Activity.Away));
    }

    [Test]
    public void AStreakOfCorrectWordsYieldsMoreAndAMistakeResetsIt()
    {
        var campaign = NewCampaign();
        NewProject(campaign);
        // Un projet ordinaire (le prototype a de petites tâches, qui plafonneraient ce que rapporte un mot)
        var product = NewProject(campaign);
        Assert.That(product.Prototype, Is.False);
        Assert.That(campaign.OpenTerminal(product.Id).Ok, Is.True);
        var session = campaign.Terminal!;
        double first = campaign.SubmitWord(session.Word, session.Word.Length / 4.0).Points;
        double second = 0;
        for (int i = 0; i < 9; i++)
            second = campaign.SubmitWord(session.Word, session.Word.Length / 4.0).Points;
        Assert.That(session.Streak, Is.EqualTo(10));
        Assert.That(second, Is.GreaterThan(first * 1.3), "dix mots d'affilée rapportent nettement plus");
        Assert.That(campaign.SubmitWord("faux", 1).Correct, Is.False);
        Assert.That(session.Streak, Is.EqualTo(0));
        Assert.That(session.BestStreak, Is.EqualTo(10));
        Assert.That(campaign.WordsTypedTotal, Is.EqualTo(10));
    }

    [Test]
    public void OldSettingsStopPausingInLargeMenusOnce()
    {
        string folder = Path.Combine(Path.GetTempPath(), "startuprush_settings_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, "settings.json");
        File.WriteAllText(path, "{\"PauseInLargeMenus\":true,\"MusicVolume\":0.3}");
        var settings = Settings.Load(path, Path.Combine(folder, "none.json"));
        Assert.That(settings.PauseInLargeMenus, Is.False, "plus de pause automatique dans les menus par défaut");
        Assert.That(settings.MusicVolume, Is.EqualTo(0.3));
        // Un joueur qui la remet la garde
        settings.PauseInLargeMenus = true;
        settings.Save(path);
        Assert.That(Settings.Load(path, Path.Combine(folder, "none.json")).PauseInLargeMenus, Is.True);
        Assert.That(new Settings().PauseInLargeMenus, Is.False);
    }
}
