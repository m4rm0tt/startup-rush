using Core.simulation;

namespace Core.states;

/// <summary>
/// Garde l'état courant du jeu et gère les transitions entre états.
/// </summary>
public class GameStateMachine
{
    private readonly Func<string, GameSession> _sessionFactory;

    public IGameState? CurrentState { get; private set; }

    public event Action<IGameState>? StateChanged;

    /// <param name="sessionFactory">
    /// Crée une nouvelle partie à partir du nom de l'entreprise.
    /// Les tests en passent une avec une graine et une config fixes.
    /// </param>
    public GameStateMachine(Func<string, GameSession>? sessionFactory = null)
    {
        _sessionFactory = sessionFactory ?? (name => new GameSession(companyName: name));
    }

    public GameSession CreateSession(string companyName) => _sessionFactory(companyName);

    /// <summary>Appelle Exit sur l'ancien état, puis Enter sur le nouveau.</summary>
    public void ChangeState(IGameState newState)
    {
        CurrentState?.Exit();
        CurrentState = newState;
        newState.Enter();
        StateChanged?.Invoke(newState);
    }

    public void Update(double deltaTime)
    {
        CurrentState?.Update(deltaTime);
    }
}
