namespace Core.simulation;

public class GameSession
{
    private readonly GameClock _gameClock = new GameClock();
    
    public double ElapsedTime => _gameClock.Time;
    
    public void Update(double deltaTime)
    {
        _gameClock.Tick(deltaTime);
    }
}