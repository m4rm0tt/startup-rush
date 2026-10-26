using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Media;
using Core.requests;
using Core.simulation;
using Core.tasks;

namespace UI.ViewModels;

public record LogEntry(string Text, IBrush Brush);

/// <summary>
/// Lit l'état de la partie et le met en forme pour l'écran. Aucune règle de jeu ici :
/// les clics appellent directement les commandes de Company.
/// </summary>
public class PlayingViewModel : ViewModelBase
{
    private const int MaxLogEntries = 12;
    private const double FlashDuration = 2; // secondes d'affichage d'un gain

    private readonly GameSession _session;
    private string _balanceText = "";
    private IBrush _balanceBrush = Display.Normal;
    private string _operatingCostText = "";
    private string _elapsedText = "";
    private string _capacityText = "";
    private string _requestsHeader = "";
    private string _flashText = "";
    private double _flashUntil;

    public string CompanyName => _session.Company.Name;
    public string BalanceText { get => _balanceText; private set => SetField(ref _balanceText, value); }
    public IBrush BalanceBrush { get => _balanceBrush; private set => SetField(ref _balanceBrush, value); }
    public string OperatingCostText { get => _operatingCostText; private set => SetField(ref _operatingCostText, value); }
    public string ElapsedText { get => _elapsedText; private set => SetField(ref _elapsedText, value); }
    public string CapacityText { get => _capacityText; private set => SetField(ref _capacityText, value); }
    public string RequestsHeader { get => _requestsHeader; private set => SetField(ref _requestsHeader, value); }

    /// <summary>Gain récent affiché quelques secondes à côté du solde.</summary>
    public string FlashText { get => _flashText; private set => SetField(ref _flashText, value); }

    public ObservableCollection<RequestViewModel> Requests { get; } = new();
    public ObservableCollection<TaskViewModel> Tasks { get; } = new();
    public ObservableCollection<LogEntry> Log { get; } = new();

    public PlayingViewModel(GameSession session)
    {
        _session = session;

        // Le journal ne garde que les événements importants
        var company = session.Company;
        company.RequestExpired += r =>
            AddLog($"Offre expirée : {r.ClientName}", Display.Warning);
        company.TaskCompleted += OnTaskCompleted;
        company.TaskFailed += t =>
            AddLog($"Tâche échouée (retard) : {t.Request.ClientName}", Display.Danger);

        AddLog($"{company.Name} ouvre ses portes avec {Display.Money(company.WalletBalance)}", Display.Normal);
        Refresh();
    }

    public void Refresh()
    {
        var company = _session.Company;

        BalanceText = Display.Money(company.WalletBalance);
        BalanceBrush = company.WalletBalance < 200 ? Display.Danger : Display.Normal;
        OperatingCostText = $"Factures : {Display.Money(_session.CurrentBill.Total)}/mois";
        ElapsedText = Display.Clock(_session.ElapsedTime);
        CapacityText = $"Tâches actives ({company.ActiveTasks.Count}/{company.MaxActiveTasks})";
        RequestsHeader = $"Demandes en attente ({company.PendingRequests.Count})";

        if (_session.ElapsedTime > _flashUntil)
            FlashText = "";

        SyncRequests();
        SyncTasks();
    }

    private void OnTaskCompleted(WorkTask task)
    {
        string gain = $"+{Display.Money(task.EarnedReward)}";

        if (task.IsLate)
            AddLog($"Retard sanctionné : {task.Request.ClientName} {gain} au lieu de {Display.Money(task.Request.Value)}",
                Display.Warning);
        else
            AddLog($"Tâche terminée : {task.Request.ClientName} {gain}", Display.Good);

        FlashText = gain;
        _flashUntil = _session.ElapsedTime + FlashDuration;
    }

    private void AddLog(string text, IBrush brush)
    {
        Log.Insert(0, new LogEntry($"[{Display.Clock(_session.ElapsedTime)}] {text}", brush));

        while (Log.Count > MaxLogEntries)
            Log.RemoveAt(Log.Count - 1);
    }

    // Les listes sont synchronisées par identité plutôt que recréées :
    // recréer les boutons à chaque image empêcherait de cliquer dessus.
    private void SyncRequests()
    {
        var company = _session.Company;
        var pending = company.PendingRequests;

        foreach (var vm in Requests.Where(vm => pending.All(r => r.Id != vm.Id)).ToList())
            Requests.Remove(vm);

        foreach (var request in pending.Where(r => Requests.All(vm => vm.Id != r.Id)))
            Requests.Add(CreateRequestViewModel(request));

        foreach (var vm in Requests)
            vm.Refresh(company.HasFreeCapacity);
    }

    private RequestViewModel CreateRequestViewModel(ClientRequest request) =>
        new(request, _session.Company, _session.Config.WorkDurationFor(request.Difficulty), _session.Config.OfferDuration);

    private void SyncTasks()
    {
        var active = _session.Company.ActiveTasks;

        foreach (var vm in Tasks.Where(vm => !active.Contains(vm.Task)).ToList())
            Tasks.Remove(vm);

        foreach (var task in active.Where(t => Tasks.All(vm => vm.Task != t)))
            Tasks.Add(new TaskViewModel(task));

        foreach (var vm in Tasks)
            vm.Refresh();
    }
}
