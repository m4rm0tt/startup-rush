using Core.company;

namespace Core.simulation;

public class GameSession
{
    private readonly GameClock _gameClock = new GameClock();
    private readonly Company _company;

    public Company Company => _company;

    public double ElapsedTime => _gameClock.Time;
    
    public GameSession()
    {
        _company = new Company();
    }
    
    public void Update(double deltaTime)
    {
        double delta = _gameClock.Tick(deltaTime);
        _company.Update(delta);
    }
}