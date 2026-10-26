using Core.requests;
using Core.simulation;

namespace Tests.requests;

[TestFixture]
public class RequestGeneratorTest
{
    private static List<ClientRequest> Generate(int seed, int count)
    {
        var generator = new RequestGenerator(new Random(seed), new GameConfig());
        return Enumerable.Range(0, count).Select(_ => generator.CreateRequest()).ToList();
    }

    [Test]
    public void SameSeedGivesSameRequests()
    {
        var first = Generate(42, 20);
        var second = Generate(42, 20);

        for (int i = 0; i < first.Count; i++)
        {
            Assert.That(second[i].Id, Is.EqualTo(first[i].Id));
            Assert.That(second[i].ClientName, Is.EqualTo(first[i].ClientName));
            Assert.That(second[i].Type, Is.EqualTo(first[i].Type));
            Assert.That(second[i].Value, Is.EqualTo(first[i].Value));
            Assert.That(second[i].Difficulty, Is.EqualTo(first[i].Difficulty));
            Assert.That(second[i].DeadlineTimeLeft, Is.EqualTo(first[i].DeadlineTimeLeft));
        }
    }

    [Test]
    public void IdsAreUnique()
    {
        var requests = Generate(1, 50);
        Assert.That(requests.Select(r => r.Id), Is.Unique);
    }

    [Test]
    public void GeneratedValuesStayWithinConfig()
    {
        var config = new GameConfig();
        var generator = new RequestGenerator(new Random(7), config);

        for (int i = 0; i < 200; i++)
        {
            var request = generator.CreateRequest();
            double maxValue = request.Difficulty * config.ValuePerDifficulty * (1 + config.ValueVariation);

            Assert.That(request.Difficulty, Is.InRange(config.MinDifficulty, config.MaxDifficulty));
            Assert.That(request.Value, Is.InRange(0, maxValue + 5));
            Assert.That(request.OfferTimeLeft, Is.EqualTo(config.OfferDuration));
            if (request.HasDeadline)
                Assert.That(request.DeadlineTimeLeft, Is.GreaterThan(config.WorkDurationFor(request.Difficulty)));
        }
    }

    [Test]
    public void FirstRequestAppearsAfterFirstDelay()
    {
        var config = new GameConfig { FirstRequestDelay = 2 };
        var generator = new RequestGenerator(new Random(0), config);

        Assert.That(generator.Update(1.9), Is.Empty);
        Assert.That(generator.Update(0.2), Has.Count.EqualTo(1));
    }

    [Test]
    public void RequestsKeepComingAtConfiguredInterval()
    {
        var config = new GameConfig { FirstRequestDelay = 0, MinRequestInterval = 5, MaxRequestInterval = 5 };
        var generator = new RequestGenerator(new Random(0), config);

        int total = 0;
        for (int i = 0; i < 300; i++) // 30 secondes
            total += generator.Update(0.1).Count;

        // t = 0, 5, 10, 15, 20, 25 (30 peut tomber juste avant ou après à cause des arrondis)
        Assert.That(total, Is.InRange(6, 7));
    }
}
