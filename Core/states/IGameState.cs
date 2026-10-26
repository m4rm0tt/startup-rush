namespace Core.states;

/// <summary>Un écran logique du jeu (menu, partie, fin de partie).</summary>
public interface IGameState
{
    void Enter();
    void Update(double deltaTime);
    void Exit();
}
