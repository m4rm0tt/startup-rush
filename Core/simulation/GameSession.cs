using Core.company;
using Core.requests;

namespace Core.simulation;

public class GameSession
{
    private readonly GameClock _gameClock;
    private readonly Company _company;
    private readonly RequestGenerator _requestGenerator;

    public Company Company => _company;
    public GameConfig Config { get; }

    public double ElapsedTime => _gameClock.Time;

    public GameSession(GameConfig? config = null, Random? random = null)
    {
        Config = config ?? new GameConfig();
        _gameClock = new GameClock(Config.MaxDeltaTime);
        _company = new Company(Config);
        _requestGenerator = new RequestGenerator(random ?? new Random(), Config);
    }

    public void Update(double deltaTime)
    {
        double delta = _gameClock.Tick(deltaTime);

        foreach (var request in _requestGenerator.Update(delta))
            _company.AddRequest(request);

        _company.Update(delta);
    }
}
