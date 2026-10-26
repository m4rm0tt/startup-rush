using Core.meta;
using Core.roguelike;

namespace Core.states;

/// <summary>Écran titre, choix du personnage et Héritage.</summary>
public class MenuState : IGameState
{
    private readonly GameStateMachine _machine;

    public Profile Profile => _machine.Profile;

    public MenuState(GameStateMachine machine)
    {
        _machine = machine;
    }

    public void Enter() { }

    public void Update(double deltaTime) { }

    public void Exit() { }

    /// <summary>Lance une run. Refusé si le personnage ou la pression ne sont pas débloqués.</summary>
    public bool StartRun(Character character, int pressure = 0, int? seed = null)
    {
        if (!Legacy.IsUnlocked(Profile, character) || pressure < 0 || pressure > Profile.MaxPressureUnlocked)
            return false;

        var setup = new RunSetup(character, pressure, seed ?? Random.Shared.Next(1, 999_999), Legacy.BonusesFor(Profile));
        _machine.ChangeState(new PlayingState(_machine, _machine.CreateSession(setup), setup));
        return true;
    }

    public bool BuyLegacyUpgrade(string id)
    {
        if (!Legacy.TryBuy(Profile, id))
            return false;

        _machine.SaveProfile();
        return true;
    }

    public bool UnlockCharacter(Character character)
    {
        if (!Legacy.TryUnlock(Profile, character))
            return false;

        _machine.SaveProfile();
        return true;
    }
}
