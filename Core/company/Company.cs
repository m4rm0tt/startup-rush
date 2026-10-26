using Core.progression;
using Core.requests;
using Core.simulation;
using Core.tasks;

namespace Core.company;

/// <summary>Résultat d'un clic pour charbonner : du travail sur la tâche en cours, ou un petit boulot payé.</summary>
public record HustleResult(double Work, double Money);

/// <summary>
/// L'activité du joueur : son argent, ses demandes, ses tâches et sa réputation.
/// D'abord un freelance, puis une vraie boîte une fois fondée.
/// </summary>
public class Company
{
    private readonly GameConfig _config;
    private readonly Wallet _wallet;
    private readonly List<ClientRequest> _pendingRequests = new();
    private readonly List<WorkTask> _activeTasks = new();

    public string Name { get; private set; }
    public bool IsFounded { get; private set; }
    public Wallet Wallet => _wallet;
    public double WalletBalance => _wallet.Balance;
    public PlayerStats Stats { get; }
    public Reputation Reputation { get; }

    public double TotalEarned { get; private set; }
    public int CompletedTaskCount { get; private set; }
    public int FailedTaskCount { get; private set; }

    /// <summary>Demandes en attente de décision du joueur.</summary>
    public IReadOnlyList<ClientRequest> PendingRequests => _pendingRequests;

    /// <summary>Tâches en cours de réalisation.</summary>
    public IReadOnlyList<WorkTask> ActiveTasks => _activeTasks;

    public int MaxActiveTasks => Stats.TaskCapacity;
    public bool HasFreeCapacity => _activeTasks.Count < Stats.TaskCapacity;

    public event Action<double> BalanceChanged
    {
        add => _wallet.BalanceChanged += value;
        remove => _wallet.BalanceChanged -= value;
    }

    public event Action<ClientRequest>? RequestReceived;
    public event Action<ClientRequest>? RequestExpired;
    public event Action<WorkTask>? TaskCompleted;
    public event Action<WorkTask>? TaskFailed;

    public Company(GameConfig config, string name = "Freelance", PlayerStats? stats = null)
    {
        _config = config;
        Name = name;
        Stats = stats ?? new PlayerStats(config);
        Reputation = new Reputation(config.ReputationLevels);
        _wallet = new Wallet(config.StartingCapital);
    }

    /// <summary>Transforme le freelance en vraie entreprise. Renvoie false si déjà fondée ou trop cher.</summary>
    public bool Found(string name)
    {
        if (IsFounded || !_wallet.TrySpend(_config.CompanyFoundingCost))
            return false;

        IsFounded = true;
        Name = string.IsNullOrWhiteSpace(name) ? "Ma Startup" : name.Trim();
        return true;
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

    /// <summary>Vrai si la demande, acceptée maintenant, peut être livrée avant son échéance.</summary>
    public bool CanFinishOnTime(ClientRequest request) =>
        !request.DeadlineTimeLeft.HasValue ||
        _config.WorkDurationFor(request.Difficulty) / Stats.WorkSpeed <= request.DeadlineTimeLeft.Value;

    /// <summary>
    /// Ce que fait un commercial : accepte les demandes faisables à temps,
    /// les plus rentables d'abord, tant qu'il reste de la place.
    /// </summary>
    public void AutoAcceptRequests()
    {
        if (!Stats.AutoAccept)
            return;

        var candidates = _pendingRequests
            .Where(CanFinishOnTime)
            .OrderByDescending(r => r.Value / r.Difficulty)
            .ToList();

        foreach (var request in candidates)
        {
            if (!HasFreeCapacity)
                break;
            AcceptRequest(request.Id);
        }
    }

    /// <summary>
    /// Le joueur charbonne : le clic avance la première tâche en cours,
    /// ou rapporte un petit boulot s'il n'y a aucune tâche.
    /// </summary>
    public HustleResult Hustle()
    {
        var task = _activeTasks.FirstOrDefault(t => t.Status == WorkTaskStatus.InProgress);
        if (task != null)
        {
            double work = _config.ClickWork * Stats.ClickPower;
            task.AddWork(work);
            return new HustleResult(work, 0);
        }

        double money = _config.ClickGigMoney * Stats.ClickPower;
        _wallet.Earn(money);
        TotalEarned += money;
        return new HustleResult(0, money);
    }

    public void Update(double deltaTime)
    {
        UpdateRequests(deltaTime);
        UpdateTasks(deltaTime);
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

    /// <summary>
    /// Fait avancer les tâches à la vitesse actuelle, encaisse celles qui sont terminées
    /// et met à jour la réputation : livrer à temps la fait monter, échouer la fait baisser.
    /// </summary>
    public void UpdateTasks(double deltaTime)
    {
        foreach (var task in _activeTasks)
            task.Update(deltaTime, Stats.WorkSpeed);

        var finished = _activeTasks.Where(t => t.Status != WorkTaskStatus.InProgress).ToList();
        foreach (var task in finished)
        {
            _activeTasks.Remove(task);

            if (task.Status == WorkTaskStatus.Completed)
            {
                _wallet.Earn(task.EarnedReward);
                TotalEarned += task.EarnedReward;
                CompletedTaskCount++;
                if (!task.IsLate)
                    Reputation.Add(task.Request.Difficulty);
                TaskCompleted?.Invoke(task);
            }
            else
            {
                FailedTaskCount++;
                Reputation.Add(-task.Request.Difficulty * _config.FailedReputationPenalty);
                TaskFailed?.Invoke(task);
            }
        }
    }

    private ClientRequest? FindPendingRequest(int requestId) =>
        _pendingRequests.FirstOrDefault(r => r.Id == requestId);
}
