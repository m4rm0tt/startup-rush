using Core.meta;
using Core.roguelike;
using Core.simulation;
using Core.states;

namespace Tests.states;

[TestFixture]
public class GameStateMachineTest
{
    private class RecordingState(string name, List<string> log) : IGameState
    {
        public void Enter() => log.Add($"{name}.Enter");
        public void Update(double deltaTime) => log.Add($"{name}.Update");
        public void Exit() => log.Add($"{name}.Exit");
    }

    private static readonly Character Dev = CharacterCatalog.All[0];
    private static readonly Character Heir = CharacterCatalog.All[^1];

    // Run courte : sans argent ni action, deux factures impayées et la faillite arrive en 10 secondes
    private static GameStateMachine NewMachine(Profile? profile = null, List<Profile>? saves = null) =>
        new(setup => new GameSession(new GameConfig { MonthDuration = 5, RoguelikeEnabled = false },
                new Random(setup.Seed), setup: setup with { Character = setup.Character with { StartingCash = 0 } }),
            profile, p => saves?.Add(p));

    private static void Run(GameStateMachine machine, double seconds)
    {
        for (int i = 0; i < seconds * 10; i++)
            machine.Update(0.1);
    }

    [Test]
    public void ChangeStateCallsExitThenEnter()
    {
        var log = new List<string>();
        var machine = new GameStateMachine();

        machine.ChangeState(new RecordingState("A", log));
        machine.ChangeState(new RecordingState("B", log));

        Assert.That(log, Is.EqualTo(new[] { "A.Enter", "A.Exit", "B.Enter" }));
    }

    [Test]
    public void UpdateIsForwardedToCurrentState()
    {
        var log = new List<string>();
        var machine = new GameStateMachine();
        machine.ChangeState(new RecordingState("A", log));

        machine.Update(0.1);

        Assert.That(log.Last(), Is.EqualTo("A.Update"));
    }

    [Test]
    public void StartRunGoesFromMenuToPlayingWithTheCharacter()
    {
        var machine = NewMachine();
        var menu = new MenuState(machine);
        machine.ChangeState(menu);

        bool started = menu.StartRun(Dev, seed: 42);

        var playing = machine.CurrentState as PlayingState;
        Assert.That(started, Is.True);
        Assert.That(playing, Is.Not.Null);
        Assert.That(playing!.Session.PlayerName, Is.EqualTo(Dev.Name));
        Assert.That(playing.Setup.Seed, Is.EqualTo(42));
    }

    [Test]
    public void LockedCharacterOrPressureCannotStart()
    {
        var machine = NewMachine();
        var menu = new MenuState(machine);
        machine.ChangeState(menu);

        Assert.That(menu.StartRun(Heir), Is.False);
        Assert.That(menu.StartRun(Dev, pressure: 1), Is.False);
        Assert.That(machine.CurrentState, Is.SameAs(menu));
    }

    [Test]
    public void PauseFreezesTheRun()
    {
        var machine = NewMachine();
        new MenuState(machine).StartRun(Dev);
        var playing = (PlayingState)machine.CurrentState!;

        playing.TogglePause();
        Run(machine, 3);

        Assert.That(playing.Session.ElapsedTime, Is.EqualTo(0));
    }

    [Test]
    public void BankruptcyGoesToGameOverAndRewardsTheProfile()
    {
        var saves = new List<Profile>();
        var machine = NewMachine(saves: saves);
        new MenuState(machine).StartRun(Dev);

        Run(machine, 15);

        var gameOver = machine.CurrentState as GameOverState;
        Assert.That(gameOver, Is.Not.Null);
        Assert.That(gameOver!.Result.IsVictory, Is.False);
        Assert.That(gameOver.Result.SurvivedTime, Is.EqualTo(10).Within(0.2));
        Assert.That(machine.Profile.Runs, Is.EqualTo(1));
        Assert.That(machine.Profile.Lingots, Is.EqualTo(gameOver.Reward.Lingots));
        Assert.That(saves, Has.Count.EqualTo(1));
    }

    [Test]
    public void AbandonEndsTheRun()
    {
        var machine = NewMachine();
        new MenuState(machine).StartRun(Dev);

        ((PlayingState)machine.CurrentState!).Abandon();

        var gameOver = machine.CurrentState as GameOverState;
        Assert.That(gameOver, Is.Not.Null);
        Assert.That(gameOver!.Result.CauseOfDeath, Is.EqualTo("Abandon"));
    }

    [Test]
    public void ReplayStartsAFreshRunWithSameCharacter()
    {
        var machine = NewMachine();
        new MenuState(machine).StartRun(Dev);
        Run(machine, 15);
        var gameOver = (GameOverState)machine.CurrentState!;

        gameOver.Replay();

        var playing = machine.CurrentState as PlayingState;
        Assert.That(playing, Is.Not.Null);
        Assert.That(playing!.Session.ElapsedTime, Is.EqualTo(0));
        Assert.That(playing.Session.IsOver, Is.False);
        Assert.That(playing.Setup.Character, Is.EqualTo(Dev));
    }

    [Test]
    public void BackToMenuFromGameOver()
    {
        var machine = NewMachine();
        new MenuState(machine).StartRun(Dev);
        Run(machine, 15);

        ((GameOverState)machine.CurrentState!).BackToMenu();

        Assert.That(machine.CurrentState, Is.InstanceOf<MenuState>());
    }

    [Test]
    public void LegacyPurchasesAreSaved()
    {
        var saves = new List<Profile>();
        var machine = NewMachine(new Profile { Lingots = 200 }, saves);
        var menu = new MenuState(machine);

        Assert.That(menu.UnlockCharacter(Heir), Is.True);
        Assert.That(menu.BuyLegacyUpgrade(Legacy.StartingCash), Is.True);

        Assert.That(machine.Profile.Lingots, Is.EqualTo(200 - Heir.UnlockCost - 10));
        Assert.That(saves, Has.Count.EqualTo(2));
        Assert.That(menu.StartRun(Heir), Is.True);
    }
}
