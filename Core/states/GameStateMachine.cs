using Core.meta;
using Core.roguelike;
using Core.simulation;

namespace Core.states;

/// <summary>
/// Garde l'état courant du jeu et gère les transitions entre états.
/// Elle porte aussi le profil du joueur, qui survit d'une run à l'autre.
/// </summary>
public class GameStateMachine
{
    private readonly Func<RunSetup, GameSession> _sessionFactory;
    private readonly Action<Profile>? _saveProfile;

    public IGameState? CurrentState { get; private set; }
    public Profile Profile { get; }

    public event Action<IGameState>? StateChanged;

    /// <param name="sessionFactory">
    /// Crée une run à partir de son paramétrage.
    /// Les tests en passent une avec une config fixe.
    /// </param>
    /// <param name="saveProfile">Appelé après chaque changement du profil (fin de run, achat dans l'Héritage).</param>
    public GameStateMachine(Func<RunSetup, GameSession>? sessionFactory = null, Profile? profile = null,
        Action<Profile>? saveProfile = null)
    {
        _sessionFactory = sessionFactory ?? (setup => new GameSession(setup: setup));
        Profile = profile ?? new Profile();
        _saveProfile = saveProfile;
    }

    public GameSession CreateSession(RunSetup setup) => _sessionFactory(setup);

    public void SaveProfile() => _saveProfile?.Invoke(Profile);

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
