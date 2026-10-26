using Core.simulation;

namespace Core.states;

public class PlayingState : IGameState
{
    private readonly GameStateMachine _machine;

    public GameSession Session { get; }

    public PlayingState(GameStateMachine machine, GameSession session)
    {
        _machine = machine;
        Session = session;
    }

    public void Enter() { }

    /// <summary>Fait avancer la partie et passe à la fin de partie en cas de faillite ou à la victoire.</summary>
    public void Update(double deltaTime)
    {
        Session.Update(deltaTime);

        if (Session.Result != null)
            _machine.ChangeState(new GameOverState(_machine, Session.Result, Session.PlayerName));
    }

    public void Exit() { }
}
