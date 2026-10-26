using Core.company;
using Core.requests;
using Core.simulation;

namespace Tests.company;

[TestFixture]
public class CompanyRequestsTest
{
    private Company _company = null!;

    [SetUp]
    public void SetUp()
    {
        _company = new Company(new GameConfig { BaseOperatingCost = 0 });
    }

    private static ClientRequest MakeRequest(int id, double offerTimeLeft = 10) =>
        new(id, "Client", RequestType.Website, 100, 1, null, offerTimeLeft);

    [Test]
    public void AddedRequestIsPending()
    {
        _company.AddRequest(MakeRequest(1));
        Assert.That(_company.PendingRequests.Select(r => r.Id), Is.EqualTo(new[] { 1 }));
    }

    [Test]
    public void RejectRemovesRequest()
    {
        _company.AddRequest(MakeRequest(1));

        bool result = _company.RejectRequest(1);

        Assert.That(result, Is.True);
        Assert.That(_company.PendingRequests, Is.Empty);
    }

    [Test]
    public void AcceptRemovesRequestFromPending()
    {
        _company.AddRequest(MakeRequest(1));

        bool result = _company.AcceptRequest(1);

        Assert.That(result, Is.True);
        Assert.That(_company.PendingRequests, Is.Empty);
    }

    [Test]
    public void UnknownRequestIsRefusedCleanly()
    {
        _company.AddRequest(MakeRequest(1));

        Assert.That(_company.AcceptRequest(99), Is.False);
        Assert.That(_company.RejectRequest(99), Is.False);
        Assert.That(_company.PendingRequests, Has.Count.EqualTo(1));
    }

    [Test]
    public void ExpiredOfferDisappears()
    {
        _company.AddRequest(MakeRequest(1, offerTimeLeft: 5));
        _company.AddRequest(MakeRequest(2, offerTimeLeft: 20));
        var expired = new List<ClientRequest>();
        _company.RequestExpired += expired.Add;

        _company.UpdateRequests(6);

        Assert.That(_company.PendingRequests.Select(r => r.Id), Is.EqualTo(new[] { 2 }));
        Assert.That(expired.Select(r => r.Id), Is.EqualTo(new[] { 1 }));
    }

    [Test]
    public void SessionReceivesAndExpiresRequestsOverTime()
    {
        var config = new GameConfig { BaseOperatingCost = 0, OfferDuration = 10 };
        var session = new GameSession(config, new Random(3));
        int received = 0, expired = 0;
        session.Company.RequestReceived += _ => received++;
        session.Company.RequestExpired += _ => expired++;

        for (int i = 0; i < 600; i++) // 60 secondes
            session.Update(0.1);

        Assert.That(received, Is.GreaterThan(3));
        Assert.That(expired, Is.GreaterThan(0));
        Assert.That(session.Company.PendingRequests.Count, Is.EqualTo(received - expired));
    }
}
