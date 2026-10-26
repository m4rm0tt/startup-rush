using Core.meta;
using Core.roguelike;
using Core.simulation;

namespace Core.states;

public class PlayingState : IGameState
{
    private readonly GameStateMachine _machine;

    public GameSession Session { get; }
    public RunSetup Setup { get; }

    /// <summary>Menu pause ouvert : le temps est figé.</summary>
    public bool IsPaused { get; private set; }

    public PlayingState(GameStateMachine machine, GameSession session, RunSetup setup)
    {
        _machine = machine;
        Session = session;
        Setup = setup;
    }

    public void Enter() { }

    /// <summary>Fait avancer la run et passe à l'écran de fin à la faillite ou à la victoire.</summary>
    public void Update(double deltaTime)
    {
        if (!IsPaused)
            Session.Update(deltaTime);

        if (Session.Result != null)
            Finish(Session.Result);
    }

    public void Exit() { }

    public void TogglePause() => IsPaused = !IsPaused;

    public void Abandon()
    {
        Session.Abandon();
        if (Session.Result != null)
            Finish(Session.Result);
    }

    private void Finish(GameResult result)
    {
        var reward = RunRewards.Compute(result, Setup.Pressure, _machine.Profile);
        RunRewards.Apply(_machine.Profile, result, reward);
        _machine.SaveProfile();
        _machine.ChangeState(new GameOverState(_machine, result, reward, Setup));
    }
}
