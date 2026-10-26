using Core.company;

namespace Core.finance;

/// <summary>Le détail d'une facture mensuelle.</summary>
/// <param name="Salaries">Salaires et loyer des locaux de la boîte.</param>
public record BillBreakdown(double Housing, double Salaries, double Subscriptions)
{
    public double Total => Housing + Salaries + Subscriptions;
}

/// <summary>
/// Les factures arrivent à chaque fin de mois et sont toujours prélevées.
/// Si le solde est négatif après le prélèvement, c'est un impayé.
/// Trop d'impayés d'affilée, c'est la faillite.
/// </summary>
public class Bills
{
    private readonly double _monthDuration;
    private readonly int _maxMissedPayments;

    public double TimeUntilDue { get; private set; }
    public int MissedPayments { get; private set; }
    public int MaxMissedPayments => _maxMissedPayments;
    public bool IsBankrupt => MissedPayments >= _maxMissedPayments;

    /// <summary>Facture prélevée, et vrai si elle n'a pas pu être couverte.</summary>
    public event Action<BillBreakdown, bool>? BillPaid;

    public Bills(double monthDuration, int maxMissedPayments)
    {
        _monthDuration = monthDuration;
        _maxMissedPayments = maxMissedPayments;
        TimeUntilDue = monthDuration;
    }

    public void Update(double deltaTime, Wallet wallet, BillBreakdown currentBill)
    {
        TimeUntilDue -= deltaTime;
        if (TimeUntilDue > 0)
            return;

        TimeUntilDue += _monthDuration;
        wallet.Pay(currentBill.Total);

        bool unpaid = wallet.Balance < 0;
        MissedPayments = unpaid ? MissedPayments + 1 : 0;
        BillPaid?.Invoke(currentBill, unpaid);
    }
}
