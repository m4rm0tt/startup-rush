using System.Windows.Input;
using Avalonia.Media;
using Core.company;
using Core.requests;

namespace UI.ViewModels;

/// <summary>Une demande en attente affichée dans la colonne des demandes.</summary>
public class RequestViewModel : ViewModelBase
{
    private readonly ClientRequest _request;
    private readonly double _workDuration;
    private readonly double _offerDuration;
    private string _deadlineText = "";
    private IBrush _deadlineBrush = Display.Muted;
    private string _offerText = "";
    private double _offerPercent;
    private IBrush _offerBrush = Display.Muted;
    private bool _canAccept;

    public int Id => _request.Id;
    public string ClientName => _request.ClientName;
    public string TypeText => Display.TypeName(_request.Type);
    public string ValueText => Display.Money(_request.Value);
    public string DifficultyText => Display.Stars(_request.Difficulty);
    public string WorkText => $"Travail : {Display.Seconds(_workDuration)}";

    public string DeadlineText { get => _deadlineText; private set => SetField(ref _deadlineText, value); }
    public IBrush DeadlineBrush { get => _deadlineBrush; private set => SetField(ref _deadlineBrush, value); }
    public string OfferText { get => _offerText; private set => SetField(ref _offerText, value); }
    public double OfferPercent { get => _offerPercent; private set => SetField(ref _offerPercent, value); }
    public IBrush OfferBrush { get => _offerBrush; private set => SetField(ref _offerBrush, value); }
    public bool CanAccept { get => _canAccept; private set => SetField(ref _canAccept, value); }

    public ICommand AcceptCommand { get; }
    public ICommand RejectCommand { get; }

    public RequestViewModel(ClientRequest request, Company company, double workDuration, double offerDuration)
    {
        _request = request;
        _workDuration = workDuration;
        _offerDuration = offerDuration;
        AcceptCommand = new RelayCommand(() => company.AcceptRequest(request.Id));
        RejectCommand = new RelayCommand(() => company.RejectRequest(request.Id));
    }

    public void Refresh(bool hasFreeCapacity)
    {
        CanAccept = hasFreeCapacity;

        OfferText = $"Expire dans {Display.Seconds(_request.OfferTimeLeft)}";
        OfferPercent = 100 * _request.OfferTimeLeft / _offerDuration;
        OfferBrush = _request.OfferTimeLeft < 5 ? Display.Danger : Display.Muted;

        if (_request.DeadlineTimeLeft is not { } deadline)
        {
            DeadlineText = "Sans échéance";
            DeadlineBrush = Display.Muted;
        }
        else if (deadline < _workDuration)
        {
            DeadlineText = $"Échéance : {Display.Seconds(deadline)} — déjà trop tard";
            DeadlineBrush = Display.Danger;
        }
        else
        {
            DeadlineText = $"Échéance : {Display.Seconds(deadline)}";
            DeadlineBrush = deadline - _workDuration < 5 ? Display.Warning : Display.Normal;
        }
    }
}
