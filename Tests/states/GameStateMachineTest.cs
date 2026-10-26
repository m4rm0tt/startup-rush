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

    // Partie courte : sans action, la faillite arrive en 10 secondes
    private static GameStateMachine NewMachine() =>
        new(name => new GameSession(
            new GameConfig { StartingCapital = 100, BaseOperatingCost = 10, OperatingCostGrowthPerMinute = 0 },
            new Random(0),
            name));

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
    public void StateChangedIsRaised()
    {
        var machine = new GameStateMachine();
        IGameState? received = null;
        machine.StateChanged += s => received = s;
        var menu = new MenuState(machine);

        machine.ChangeState(menu);

        Assert.That(received, Is.SameAs(menu));
    }

    [Test]
    public void NewGameGoesFromMenuToPlaying()
    {
        var machine = NewMachine();
        var menu = new MenuState(machine);
        machine.ChangeState(menu);

        menu.StartNewGame("Acme");

        var playing = machine.CurrentState as PlayingState;
        Assert.That(playing, Is.Not.Null);
        Assert.That(playing!.Session.Company.Name, Is.EqualTo("Acme"));
    }

    [Test]
    public void EmptyCompanyNameGetsDefaultName()
    {
        var machine = NewMachine();
        var menu = new MenuState(machine);
        machine.ChangeState(menu);

        menu.StartNewGame("   ");

        Assert.That(((PlayingState)machine.CurrentState!).Session.Company.Name, Is.EqualTo("Ma Startup"));
    }

    [Test]
    public void PlayingStateAdvancesTheSession()
    {
        var machine = NewMachine();
        new MenuState(machine).StartNewGame("Acme");
        var playing = (PlayingState)machine.CurrentState!;

        Run(machine, 3);

        Assert.That(playing.Session.ElapsedTime, Is.EqualTo(3).Within(1e-9));
    }

    [Test]
    public void BankruptcyGoesFromPlayingToGameOver()
    {
        var machine = NewMachine();
        new MenuState(machine).StartNewGame("Acme");

        Run(machine, 15);

        var gameOver = machine.CurrentState as GameOverState;
        Assert.That(gameOver, Is.Not.Null);
        Assert.That(gameOver!.Result.SurvivedTime, Is.EqualTo(10).Within(0.2));
        Assert.That(gameOver.CompanyName, Is.EqualTo("Acme"));
    }

    [Test]
    public void ReplayStartsFromScratch()
    {
        var machine = NewMachine();
        new MenuState(machine).StartNewGame("Acme");
        Run(machine, 15);
        var gameOver = (GameOverState)machine.CurrentState!;

        gameOver.Replay();

        var playing = machine.CurrentState as PlayingState;
        Assert.That(playing, Is.Not.Null);
        Assert.That(playing!.Session.ElapsedTime, Is.EqualTo(0));
        Assert.That(playing.Session.Company.WalletBalance, Is.EqualTo(100));
        Assert.That(playing.Session.IsOver, Is.False);
        Assert.That(playing.Session.Company.Name, Is.EqualTo("Acme"));
    }

    [Test]
    public void BackToMenuFromGameOver()
    {
        var machine = NewMachine();
        new MenuState(machine).StartNewGame("Acme");
        Run(machine, 15);

        ((GameOverState)machine.CurrentState!).BackToMenu();

        Assert.That(machine.CurrentState, Is.InstanceOf<MenuState>());
    }
}
