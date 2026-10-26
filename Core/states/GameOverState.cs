using Core.meta;
using Core.roguelike;
using Core.simulation;

namespace Core.states;

/// <summary>Résumé de la run et lingots gagnés.</summary>
public class GameOverState : IGameState
{
    private readonly GameStateMachine _machine;

    public GameResult Result { get; }
    public RunReward Reward { get; }
    public RunSetup Setup { get; }
    public Profile Profile => _machine.Profile;

    public GameOverState(GameStateMachine machine, GameResult result, RunReward reward, RunSetup setup)
    {
        _machine = machine;
        Result = result;
        Reward = reward;
        Setup = setup;
    }

    public void Enter() { }

    public void Update(double deltaTime) { }

    public void Exit() { }

    /// <summary>Relance une run avec le même personnage et la même pression, sur une nouvelle graine.</summary>
    public void Replay()
    {
        new MenuState(_machine).StartRun(Setup.Character, Setup.Pressure);
    }

    public void BackToMenu()
    {
        _machine.ChangeState(new MenuState(_machine));
    }
}
