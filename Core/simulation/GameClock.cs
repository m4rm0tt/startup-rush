namespace Core.simulation;

public class GameClock
{
    public double Time { get; private set; }
    
    public double Tick(double deltaTime)
    {
        if (deltaTime is > 0 and <= 0.1)
        {
            Time += deltaTime;
        }
        
        return deltaTime;
    }
}