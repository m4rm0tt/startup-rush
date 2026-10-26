using Core.requests;

namespace Tests.requests;

[TestFixture]
public class ClientRequestTest
{
    [Test]
    public void UpdateDecreasesOfferAndDeadlineTimes()
    {
        var request = new ClientRequest(1, "Client", RequestType.Logo, 100, 1, deadlineTimeLeft: 20, offerTimeLeft: 10);

        request.Update(4);

        Assert.That(request.OfferTimeLeft, Is.EqualTo(6));
        Assert.That(request.DeadlineTimeLeft, Is.EqualTo(16));
        Assert.That(request.IsExpired, Is.False);
    }

    [Test]
    public void RequestWithoutDeadlineKeepsNoDeadline()
    {
        var request = new ClientRequest(1, "Client", RequestType.Logo, 100, 1, deadlineTimeLeft: null, offerTimeLeft: 10);

        request.Update(4);

        Assert.That(request.HasDeadline, Is.False);
    }

    [Test]
    public void RequestIsExpiredWhenOfferTimeIsOver()
    {
        var request = new ClientRequest(1, "Client", RequestType.Logo, 100, 1, null, offerTimeLeft: 10);

        request.Update(10);

        Assert.That(request.IsExpired, Is.True);
    }
}
