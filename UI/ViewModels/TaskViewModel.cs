using Avalonia.Media;
using Core.tasks;

namespace UI.ViewModels;

/// <summary>Une tâche en cours affichée avec sa barre de progression.</summary>
public class TaskViewModel : ViewModelBase
{
    private readonly WorkTask _task;
    private double _progressPercent;
    private string _progressText = "";
    private string _deadlineText = "";
    private IBrush _deadlineBrush = Display.Muted;

    public WorkTask Task => _task;
    public string ClientName => _task.Request.ClientName;
    public string TypeText => Display.TypeName(_task.Request.Type);
    public string ValueText => Display.Money(_task.Request.Value);

    public double ProgressPercent { get => _progressPercent; private set => SetField(ref _progressPercent, value); }
    public string ProgressText { get => _progressText; private set => SetField(ref _progressText, value); }
    public string DeadlineText { get => _deadlineText; private set => SetField(ref _deadlineText, value); }
    public IBrush DeadlineBrush { get => _deadlineBrush; private set => SetField(ref _deadlineBrush, value); }

    public TaskViewModel(WorkTask task)
    {
        _task = task;
    }

    public void Refresh(double workSpeed)
    {
        double remaining = _task.TimeRemainingAt(workSpeed);
        ProgressPercent = _task.Progress * 100;
        ProgressText = $"{_task.Progress:P0} — reste {Display.Seconds(remaining)}";

        if (_task.DeadlineTimeLeft is not { } deadline)
        {
            DeadlineText = "Sans échéance";
            DeadlineBrush = Display.Good;
        }
        else if (_task.IsLate)
        {
            DeadlineText = "En retard ! Gain réduit";
            DeadlineBrush = Display.Danger;
        }
        else if (_task.WillBeLate(workSpeed))
        {
            DeadlineText = $"Échéance {Display.Seconds(deadline)} — clique pour accélérer !";
            DeadlineBrush = Display.Danger;
        }
        else
        {
            DeadlineText = $"Échéance dans {Display.Seconds(deadline)}";
            DeadlineBrush = deadline - remaining < 5 ? Display.Warning : Display.Good;
        }
    }
}
