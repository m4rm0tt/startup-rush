using Core.meta;
using Core.progression;
using Core.roguelike;
using Core.simulation;

namespace Tests.roguelike;

/// <summary>Atouts, crises, événements, personnages et méta-progression.</summary>
[TestFixture]
public class RoguelikeTest
{
    private static void Run(GameSession session, double seconds)
    {
        for (int i = 0; i < seconds * 10 && !session.IsPaused && !session.IsOver; i++)
            session.Update(0.1);
    }

    private static Perk TestPerk(string id, Rarity rarity, Action<Modifiers>? apply = null, int shields = 0) =>
        new(id, id, "", rarity, "star", apply ?? (_ => { }), shields);

    // Sans événements aléatoires, pour contrôler exactement ce qui arrive
    private static GameConfig QuietConfig(double startingCapital = 100_000) => new()
    {
        StartingCapital = startingCapital,
        MonthDuration = 10,
        Events = []
    };

    // --- Pioche et choix d'atouts ---

    [Test]
    public void DeckNeverDrawsCursesOrDuplicates()
    {
        var deck = new PerkDeck(PerkCatalog.All, new Random(1), legendaryUnlocked: true);

        var options = deck.Draw(5, Rarity.Common);

        Assert.That(options.Select(p => p.Id), Is.Unique);
        Assert.That(options.Any(p => p.Rarity == Rarity.Curse), Is.False);
    }

    [Test]
    public void LockedLegendariesAreNotInTheDeck()
    {
        var deck = new PerkDeck(PerkCatalog.All, new Random(1), legendaryUnlocked: false);

        var everything = deck.Draw(100, Rarity.Common);

        Assert.That(everything.Any(p => p.RequiresUnlock), Is.False);
    }

    [Test]
    public void MinRarityIsRespected()
    {
        var deck = new PerkDeck(PerkCatalog.All, new Random(3), legendaryUnlocked: true);

        var options = deck.Draw(3, Rarity.Rare);

        Assert.That(options.All(p => p.Rarity >= Rarity.Rare), Is.True);
    }

    [Test]
    public void QuarterlyReviewOffersADraftAndPausesTheRun()
    {
        // Assez peu d'argent pour ne pas changer de statut avant le bilan
        var session = new GameSession(QuietConfig(startingCapital: 2_000), new Random(0));

        Run(session, 31);

        Assert.That(session.PendingChoice, Is.InstanceOf<Draft>());
        Assert.That(session.PendingChoice!.Title, Is.EqualTo("Bilan du trimestre"));
        Assert.That(session.IsPaused, Is.True);
        double time = session.ElapsedTime;
        session.Update(0.1);
        Assert.That(session.ElapsedTime, Is.EqualTo(time));
    }

    [Test]
    public void ChoosingAPerkAppliesItAndResumes()
    {
        var perk = TestPerk("fast", Rarity.Common, m => m.WorkSpeed *= 2);
        var session = new GameSession(QuietConfig() with { Perks = [perk] }, new Random(0));
        double before = session.Stats.WorkSpeed;
        session.QueueDraft("Test", Rarity.Common);

        Assert.That(session.ChoosePerk(0), Is.True);

        Assert.That(session.IsPaused, Is.False);
        Assert.That(session.Perks, Has.Member(perk));
        Assert.That(session.Stats.WorkSpeed, Is.EqualTo(before * 2));
    }

    [Test]
    public void RerollCostsMoneyAndGrows()
    {
        var session = new GameSession(QuietConfig(), new Random(0));
        session.QueueDraft("Test", Rarity.Common);
        var draft = (Draft)session.PendingChoice!;
        double cash = session.Company.WalletBalance;
        double firstCost = draft.RerollCost;

        Assert.That(session.RerollDraft(), Is.True);

        Assert.That(session.Company.WalletBalance, Is.EqualTo(cash - firstCost).Within(1e-6));
        Assert.That(draft.RerollCost, Is.GreaterThan(firstCost));
    }

    [Test]
    public void SkippingADraftPaysAReward()
    {
        var session = new GameSession(QuietConfig(), new Random(0));
        session.QueueDraft("Test", Rarity.Common);
        var draft = (Draft)session.PendingChoice!;
        double cash = session.Company.WalletBalance;

        Assert.That(session.SkipDraft(), Is.True);

        Assert.That(session.Company.WalletBalance, Is.EqualTo(cash + draft.SkipReward));
        Assert.That(session.IsPaused, Is.False);
    }

    [Test]
    public void ReachingAStageOffersARareDraft()
    {
        var session = new GameSession(QuietConfig(), new Random(0));

        session.Update(0.1);

        var draft = session.PendingChoice as Draft;
        Assert.That(draft, Is.Not.Null);
        Assert.That(draft!.Title, Does.Contain("Freelance"));
        Assert.That(draft.Options.All(p => p.Rarity >= Rarity.Rare), Is.True);
    }

    // --- Atouts particuliers ---

    [Test]
    public void ShieldForgivesAnUnpaidBill()
    {
        var session = new GameSession(QuietConfig(startingCapital: 0), new Random(0));
        session.AddPerk(TestPerk("shield", Rarity.Epic, shields: 1));

        Run(session, 10.5);

        Assert.That(session.Bills.MissedPayments, Is.EqualTo(0));
        Assert.That(session.Bills.Shields, Is.EqualTo(0));
    }

    [Test]
    public void InterestIsPaidAtMonthEnd()
    {
        var session = new GameSession(QuietConfig(startingCapital: 1_000), new Random(0));
        session.AddPerk(TestPerk("savings", Rarity.Rare, m => m.MonthlyInterest += 0.1));
        double bill = session.CurrentBill.Total;

        Run(session, 10.5);

        Assert.That(session.Company.WalletBalance, Is.EqualTo(1_000 - bill + (1_000 - bill) * 0.1).Within(1));
    }

    [Test]
    public void BillModifierChangesTheBill()
    {
        var session = new GameSession(QuietConfig(), new Random(0));
        double before = session.CurrentBill.Total;

        session.AddPerk(TestPerk("cheap", Rarity.Rare, m => m.Bills *= 0.5));

        Assert.That(session.CurrentBill.Total, Is.EqualTo(before * 0.5).Within(1e-9));
    }

    // --- Crises ---

    [Test]
    public void CrisisIsAnnouncedThenHitsAndEnds()
    {
        var crisis = new CrisisInfo("test", "Test", "", m => m.Bills *= 3);
        // Sous le seuil de l'impôt sur la fortune, pour que la facture ne bouge qu'avec la crise
        var config = QuietConfig(startingCapital: 20_000) with { Crises = [crisis], FirstCrisisMonth = 2, CrisisEvery = 3, MonthsPerQuarter = 100 };
        var session = new GameSession(config, new Random(0));
        session.SkipDraftIfAny();

        Assert.That(session.NextCrisis, Is.EqualTo(crisis));
        Assert.That(session.NextCrisisMonth, Is.EqualTo(2));
        double normalBill = session.CurrentBill.Total;

        RunSkippingDrafts(session, 10.5);
        Assert.That(session.ActiveCrisis, Is.EqualTo(crisis));
        Assert.That(session.CurrentBill.Total, Is.EqualTo(normalBill * 3).Within(1e-6));

        RunSkippingDrafts(session, 10);
        Assert.That(session.ActiveCrisis, Is.Null);
        Assert.That(session.NextCrisisMonth, Is.EqualTo(5));
    }

    [Test]
    public void CrashCrisisDropsMarketPrices()
    {
        var crash = CrisisCatalog.All.First(c => c.Id == "crash");
        var session = new GameSession(QuietConfig(), new Random(0));
        double price = session.Market.Assets[1].Price;

        crash.OnStart!(session);

        Assert.That(session.Market.Assets[1].Price, Is.EqualTo(price * 0.7).Within(1e-9));
    }

    // --- Événements ---

    [Test]
    public void EventPausesUntilResolvedAndConfirmed()
    {
        var info = new GameEventInfo("test", "Test", "", "star",
        [
            new EventOption("Gagner", "", (s, _) => { s.Company.Wallet.Earn(500); return "Gagné"; }),
            new EventOption("Rien", "", (_, _) => "Rien"),
        ]);
        // Peu d'argent : pas de changement de statut (et donc pas de choix d'atout) pendant le test
        var config = QuietConfig(startingCapital: 2_000) with { Events = [info], FirstEventDelay = 5 };
        var session = new GameSession(config, new Random(0));
        double cash = session.Company.WalletBalance;

        RunSkippingDrafts(session, 6, stopOnEvent: true);
        var gameEvent = session.PendingChoice as GameEvent;
        Assert.That(gameEvent, Is.Not.Null);

        Assert.That(session.ConfirmEvent(), Is.False, "il faut d'abord choisir");
        Assert.That(session.ChooseEventOption(0), Is.True);
        Assert.That(gameEvent!.Outcome, Is.EqualTo("Gagné"));
        Assert.That(session.IsPaused, Is.True, "le joueur lit le résultat");
        Assert.That(session.ConfirmEvent(), Is.True);
        Assert.That(session.IsPaused, Is.False);
        Assert.That(session.Company.WalletBalance, Is.EqualTo(cash + 500).Within(1));
    }

    [Test]
    public void UnavailableOptionCannotBeChosen()
    {
        var info = new GameEventInfo("test", "Test", "", "star",
        [
            new EventOption("Impossible", "", (_, _) => "", _ => false),
            new EventOption("Rien", "", (_, _) => "Rien"),
        ]);
        var config = QuietConfig() with { Events = [info], FirstEventDelay = 5 };
        var session = new GameSession(config, new Random(0));
        session.SkipDraftIfAny();
        RunSkippingDrafts(session, 6, stopOnEvent: true);

        Assert.That(session.ChooseEventOption(0), Is.False);
        Assert.That(session.ChooseEventOption(1), Is.True);
    }

    [Test]
    public void EveryCatalogEventCanBeResolvedWithItsLastOption()
    {
        var session = new GameSession(QuietConfig(), new Random(0));
        foreach (var info in EventCatalog.All)
        {
            var last = info.Options[^1];
            Assert.That(last.IsAvailable?.Invoke(session) ?? true, Is.True, info.Id);
            Assert.That(last.Resolve(session, new Random(0)), Is.Not.Empty, info.Id);
        }
    }

    [Test]
    public void TimedEffectExpiresAfterItsMonths()
    {
        var session = new GameSession(QuietConfig() with { MonthsPerQuarter = 100 }, new Random(0));
        session.SkipDraftIfAny();
        double before = session.Stats.WorkSpeed;
        session.AddTimedEffect(new TimedEffect("Boost", "", true, 1, m => m.WorkSpeed *= 2));
        Assert.That(session.Stats.WorkSpeed, Is.EqualTo(before * 2));

        RunSkippingDrafts(session, 10.5);

        Assert.That(session.TimedEffects, Is.Empty);
        Assert.That(session.Stats.WorkSpeed, Is.EqualTo(before));
    }

    // --- Personnages, pression, méta ---

    [Test]
    public void CharacterSetsStartingCashUpgradesAndTrait()
    {
        var heir = CharacterCatalog.All.First(c => c.Id == "heir");
        var setup = new RunSetup(heir, 0, 7, new LegacyBonuses(ExtraStartingCash: 400));
        var session = new GameSession(setup: setup);

        Assert.That(session.PlayerName, Is.EqualTo(heir.Name));
        Assert.That(session.Company.WalletBalance, Is.EqualTo(15_400));
        Assert.That(session.Upgrades[Core.upgrades.UpgradeTrackId.Housing].Level, Is.EqualTo(1));
        Assert.That(session.Modifiers.Bills, Is.EqualTo(1.5));
    }

    [Test]
    public void TopPressureStartsWithACurse()
    {
        var setup = new RunSetup(CharacterCatalog.All[0], 3, 7, new LegacyBonuses());
        var session = new GameSession(setup: setup);

        Assert.That(session.Perks.Count(p => p.Rarity == Rarity.Curse), Is.EqualTo(1));
    }

    [Test]
    public void SameSeedGivesSameRun()
    {
        var setup = new RunSetup(CharacterCatalog.All[0], 0, 1234, new LegacyBonuses());
        var a = new GameSession(QuietConfig(), setup: setup);
        var b = new GameSession(QuietConfig(), setup: setup);

        RunSkippingDrafts(a, 8);
        RunSkippingDrafts(b, 8);

        Assert.That(a.Company.PendingRequests.Select(r => r.Value), Is.EqualTo(b.Company.PendingRequests.Select(r => r.Value)));
    }

    [Test]
    public void ProfileIsSavedAndLoaded()
    {
        string path = Path.Combine(Path.GetTempPath(), $"startuprush-test-{Guid.NewGuid()}.json");
        try
        {
            var store = new ProfileStore(path);
            var profile = new Profile { Lingots = 42, Wins = 1 };
            profile.UnlockedCharacters.Add("heir");
            profile.LegacyLevels[Legacy.Mentor] = 1;

            store.Save(profile);
            var loaded = store.Load();

            Assert.That(loaded.Lingots, Is.EqualTo(42));
            Assert.That(loaded.UnlockedCharacters, Does.Contain("heir"));
            Assert.That(Legacy.BonusesFor(loaded).ExtraDraftChoices, Is.EqualTo(1));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void MissingOrBrokenProfileGivesAFreshOne()
    {
        string path = Path.Combine(Path.GetTempPath(), $"startuprush-test-{Guid.NewGuid()}.json");
        Assert.That(new ProfileStore(path).Load().Lingots, Is.EqualTo(0));

        File.WriteAllText(path, "{ pas du json");
        try
        {
            Assert.That(new ProfileStore(path).Load().Runs, Is.EqualTo(0));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void SettingsAreSavedAndOldProfilesGetDefaults()
    {
        string path = Path.Combine(Path.GetTempPath(), $"startuprush-test-{Guid.NewGuid()}.json");
        try
        {
            // Un profil écrit avant l'ajout des réglages
            File.WriteAllText(path, """{ "Lingots": 12, "Runs": 3 }""");
            var store = new ProfileStore(path);
            var old = store.Load();
            Assert.That(old.Lingots, Is.EqualTo(12));
            Assert.That(old.Settings.MusicVolume, Is.EqualTo(new GameSettings().MusicVolume));

            old.Settings.MusicVolume = 0.2;
            old.Settings.ReduceEffects = true;
            store.Save(old);
            var loaded = store.Load();

            Assert.That(loaded.Settings.MusicVolume, Is.EqualTo(0.2));
            Assert.That(loaded.Settings.ReduceEffects, Is.True);
            Assert.That(File.Exists(path + ".tmp"), Is.False);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void LegacyUpgradeCostsLingotsAndHasMaxLevel()
    {
        var profile = new Profile { Lingots = 1_000 };

        Assert.That(Legacy.TryBuy(profile, Legacy.Mentor), Is.True);
        Assert.That(Legacy.TryBuy(profile, Legacy.Mentor), Is.False);
        Assert.That(profile.Lingots, Is.EqualTo(960));
    }

    [Test]
    public void VictoryUnlocksNextPressureAndPaysMore()
    {
        var profile = new Profile();
        var win = new GameResult(true, 3000, 1e9, 1e9, "Roi du monde", 0, 0, 0)
            { StageIndex = (int)CareerStage.KingOfTheWorld, Months = 50 };
        var loss = win with { IsVictory = false, StageIndex = 2, Months = 10 };

        var winReward = RunRewards.Compute(win, 0, profile);
        var lossReward = RunRewards.Compute(loss, 0, profile);
        RunRewards.Apply(profile, win, winReward);

        Assert.That(winReward.Lingots, Is.GreaterThan(lossReward.Lingots));
        Assert.That(winReward.PressureUnlocked, Is.True);
        Assert.That(profile.MaxPressureUnlocked, Is.EqualTo(1));
        Assert.That(profile.Wins, Is.EqualTo(1));
    }

    private static void RunSkippingDrafts(GameSession session, double seconds, bool stopOnEvent = false)
    {
        for (int i = 0; i < seconds * 10 && !session.IsOver; i++)
        {
            session.SkipDraftIfAny();
            if (stopOnEvent && session.PendingChoice is GameEvent)
                return;
            session.Update(0.1);
        }
    }
}

internal static class SessionTestExtensions
{
    public static void SkipDraftIfAny(this GameSession session)
    {
        while (session.PendingChoice is Draft)
            session.SkipDraft();
    }
}
