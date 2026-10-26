using Core.requests;
using Core.simulation;

namespace Core.company;

public class Company
{
    private Wallet _wallet;
    private double _operatingCost;
    private readonly List<ClientRequest> _pendingRequests = new();

    public double WalletBalance => _wallet.Balance;

    /// <summary>Demandes en attente de décision du joueur.</summary>
    public IReadOnlyList<ClientRequest> PendingRequests => _pendingRequests;

    public event Action<double> BalanceChanged
    {
        add => _wallet.BalanceChanged += value;
        remove => _wallet.BalanceChanged -= value;
    }

    public event Action<ClientRequest>? RequestReceived;
    public event Action<ClientRequest>? RequestExpired;

    public Company(GameConfig config)
    {
        _wallet = new Wallet(config.StartingCapital);
        _operatingCost = config.BaseOperatingCost;
    }

    public void AddRequest(ClientRequest request)
    {
        _pendingRequests.Add(request);
        RequestReceived?.Invoke(request);
    }

    /// <summary>Accepte une demande en attente. Renvoie false si la demande est inconnue.</summary>
    public bool AcceptRequest(int requestId)
    {
        var request = FindPendingRequest(requestId);
        if (request == null)
            return false;

        _pendingRequests.Remove(request);
        return true;
    }

    /// <summary>Refuse une demande en attente. Renvoie false si la demande est inconnue.</summary>
    public bool RejectRequest(int requestId)
    {
        var request = FindPendingRequest(requestId);
        if (request == null)
            return false;

        _pendingRequests.Remove(request);
        return true;
    }

    public void Update(double deltaTime)
    {
        UpdateRequests(deltaTime);
        PayOperatingCosts(deltaTime);
    }

    /// <summary>Fait vieillir les offres et retire celles qui ont expiré.</summary>
    public void UpdateRequests(double deltaTime)
    {
        foreach (var request in _pendingRequests)
            request.Update(deltaTime);

        var expired = _pendingRequests.Where(r => r.IsExpired).ToList();
        foreach (var request in expired)
        {
            _pendingRequests.Remove(request);
            RequestExpired?.Invoke(request);
        }
    }

    public void PayOperatingCosts(double deltaTime)
    {
        var cost = _operatingCost * deltaTime;
        _wallet.Pay(cost);
    }

    private ClientRequest? FindPendingRequest(int requestId) =>
        _pendingRequests.FirstOrDefault(r => r.Id == requestId);
}
