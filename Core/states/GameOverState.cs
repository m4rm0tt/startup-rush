using Core.simulation;

namespace Core.states;

public class GameOverState : IGameState
{
    private readonly GameStateMachine _machine;

    public GameResult Result { get; }
    public string PlayerName { get; }

    public GameOverState(GameStateMachine machine, GameResult result, string playerName)
    {
        _machine = machine;
        Result = result;
        PlayerName = playerName;
    }

    public void Enter() { }

    public void Update(double deltaTime) { }

    public void Exit() { }

    /// <summary>Relance une partie neuve avec le même joueur.</summary>
    public void Replay()
    {
        _machine.ChangeState(new PlayingState(_machine, _machine.CreateSession(PlayerName)));
    }

    public void BackToMenu()
    {
        _machine.ChangeState(new MenuState(_machine));
    }
}
