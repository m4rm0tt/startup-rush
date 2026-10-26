using Core.simulation;

namespace Core.states;

public class GameOverState : IGameState
{
    private readonly GameStateMachine _machine;

    public GameResult Result { get; }
    public string CompanyName { get; }

    public GameOverState(GameStateMachine machine, GameResult result, string companyName)
    {
        _machine = machine;
        Result = result;
        CompanyName = companyName;
    }

    public void Enter() { }

    public void Update(double deltaTime) { }

    public void Exit() { }

    /// <summary>Relance une partie neuve avec le même nom d'entreprise.</summary>
    public void Replay()
    {
        _machine.ChangeState(new PlayingState(_machine, _machine.CreateSession(CompanyName)));
    }

    public void BackToMenu()
    {
        _machine.ChangeState(new MenuState(_machine));
    }
}
