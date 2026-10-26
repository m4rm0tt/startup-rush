namespace Core.simulation;

public class GameClock
{
    private readonly double _maxDeltaTime;

    public double Time { get; private set; }

    public GameClock(double maxDeltaTime = 0.25)
    {
        _maxDeltaTime = maxDeltaTime;
    }

    /// <summary>
    /// Fait avancer le temps et renvoie le delta réellement appliqué :
    /// 0 si le delta est négatif, plafonné s'il est trop grand.
    /// </summary>
    public double Tick(double deltaTime)
    {
        if (deltaTime <= 0)
            return 0;

        double appliedDelta = Math.Min(deltaTime, _maxDeltaTime);
        Time += appliedDelta;

        return appliedDelta;
    }
}
