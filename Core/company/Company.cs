using Core.requests;
using Core.simulation;
using Core.tasks;

namespace Core.company;

public class Company
{
    private readonly GameConfig _config;
    private Wallet _wallet;
    private readonly List<ClientRequest> _pendingRequests = new();
    private readonly List<WorkTask> _activeTasks = new();

    public double WalletBalance => _wallet.Balance;
    public bool IsBankrupt => _wallet.Balance < 0;

    /// <summary>Charges de fonctionnement actuelles, par seconde.</summary>
    public double OperatingCost { get; private set; }

    public double TotalEarned { get; private set; }
    public int CompletedTaskCount { get; private set; }
    public int FailedTaskCount { get; private set; }

    /// <summary>Demandes en attente de décision du joueur.</summary>
    public IReadOnlyList<ClientRequest> PendingRequests => _pendingRequests;

    /// <summary>Tâches en cours de réalisation.</summary>
    public IReadOnlyList<WorkTask> ActiveTasks => _activeTasks;

    public int MaxActiveTasks => _config.MaxActiveTasks;
    public bool HasFreeCapacity => _activeTasks.Count < _config.MaxActiveTasks;

    public event Action<double> BalanceChanged
    {
        add => _wallet.BalanceChanged += value;
        remove => _wallet.BalanceChanged -= value;
    }

    public event Action<ClientRequest>? RequestReceived;
    public event Action<ClientRequest>? RequestExpired;
    public event Action<WorkTask>? TaskCompleted;
    public event Action<WorkTask>? TaskFailed;

    public Company(GameConfig config)
    {
        _config = config;
        _wallet = new Wallet(config.StartingCapital);
        OperatingCost = config.BaseOperatingCost;
    }

    public void AddRequest(ClientRequest request)
    {
        _pendingRequests.Add(request);
        RequestReceived?.Invoke(request);
    }

    /// <summary>
    /// Accepte une demande en attente et lance la tâche correspondante.
    /// Renvoie false si la demande est inconnue ou si la capacité est atteinte.
    /// </summary>
    public bool AcceptRequest(int requestId)
    {
        var request = FindPendingRequest(requestId);
        if (request == null || !HasFreeCapacity)
            return false;

        _pendingRequests.Remove(request);
        _activeTasks.Add(new WorkTask(request, _config));
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
        UpdateTasks(deltaTime);
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

    /// <summary>Fait avancer les tâches, encaisse celles qui sont terminées et retire les échouées.</summary>
    public void UpdateTasks(double deltaTime)
    {
        foreach (var task in _activeTasks)
            task.Update(deltaTime);

        var finished = _activeTasks.Where(t => t.Status != WorkTaskStatus.InProgress).ToList();
        foreach (var task in finished)
        {
            _activeTasks.Remove(task);

            if (task.Status == WorkTaskStatus.Completed)
            {
                _wallet.Earn(task.EarnedReward);
                TotalEarned += task.EarnedReward;
                CompletedTaskCount++;
                TaskCompleted?.Invoke(task);
            }
            else
            {
                FailedTaskCount++;
                TaskFailed?.Invoke(task);
            }
        }
    }

    /// <summary>Retire les charges du delta, puis les fait augmenter avec le temps.</summary>
    public void PayOperatingCosts(double deltaTime)
    {
        var cost = OperatingCost * deltaTime;
        _wallet.Pay(cost);
        OperatingCost += _config.OperatingCostGrowthPerMinute * deltaTime / 60;
    }

    private ClientRequest? FindPendingRequest(int requestId) =>
        _pendingRequests.FirstOrDefault(r => r.Id == requestId);
}
