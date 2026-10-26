namespace Core.states;

public class MenuState : IGameState
{
    private readonly GameStateMachine _machine;

    public MenuState(GameStateMachine machine)
    {
        _machine = machine;
    }

    public void Enter() { }

    public void Update(double deltaTime) { }

    public void Exit() { }

    public void StartNewGame(string companyName)
    {
        if (string.IsNullOrWhiteSpace(companyName))
            companyName = "Ma Startup";

        _machine.ChangeState(new PlayingState(_machine, _machine.CreateSession(companyName.Trim())));
    }
}
