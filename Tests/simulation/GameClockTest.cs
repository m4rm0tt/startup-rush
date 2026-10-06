using Core.simulation;

namespace Tests.simulation;

[TestFixture]
public class GameClockTest
{
    [Test]
    public void TickTest()
    {
        GameClock clock = new GameClock();
        
        clock.Tick(0.1);
        
        Assert.That(clock.Time, Is.EqualTo(0.1));
    }
    
    [Test]
    public void TickMultipleTimesTest()
    {
        double v = 0.1;
        
        GameClock clock = new GameClock();
    
        clock.Tick(v);
        clock.Tick(v);

        Assert.That(clock.Time, Is.EqualTo(2*v));
    }
    
    [Test]
    public void TickZeroTest()
    {
        GameClock clock = new GameClock();
    
        clock.Tick(0);

        Assert.That(clock.Time, Is.EqualTo(0));
    }
    
    [Test]
    public void TickNegativeTest()
    {
        GameClock clock = new GameClock();
    
        clock.Tick(-0.1);

        Assert.That(clock.Time, Is.EqualTo(0));
    }

    [Test]
    public void TickTooBigTest()
    {
        GameClock clock = new GameClock();
        
        clock.Tick(1000);

        Assert.That(clock.Time, Is.EqualTo(0));
    }
}