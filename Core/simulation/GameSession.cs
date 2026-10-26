using Core.company;

namespace Core.simulation;

public class GameSession
{
    private readonly GameClock _gameClock;
    private readonly Company _company;

    public Company Company => _company;
    public GameConfig Config { get; }

    public double ElapsedTime => _gameClock.Time;

    public GameSession(GameConfig? config = null)
    {
        Config = config ?? new GameConfig();
        _gameClock = new GameClock(Config.MaxDeltaTime);
        _company = new Company(Config);
    }

    public void Update(double deltaTime)
    {
        double delta = _gameClock.Tick(deltaTime);
        _company.Update(delta);
    }
}
