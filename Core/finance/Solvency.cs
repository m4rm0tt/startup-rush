namespace Core.finance;

/// <summary>
/// La situation financière, du plus sain au plus grave. Seule la liquidation termine la campagne,
/// et seulement au bout d'une procédure annoncée : impayés, puis mise en demeure de 30 jours, puis liquidation.
/// </summary>
public enum SolvencyState
{
    /// <summary>Tout est payé, plus d'un mois d'autonomie.</summary>
    Healthy,

    /// <summary>Tout est payé, mais moins de 30 jours d'autonomie ou un solde négatif.</summary>
    Tight,

    /// <summary>Au moins une obligation échue n'est pas payée.</summary>
    Overdue,

    /// <summary>Impayés depuis trop longtemps : délai fixe pour tout régulariser, sinon liquidation.</summary>
    Notice,

    /// <summary>Partie perdue : l'entreprise ne peut plus honorer ses obligations.</summary>
    Liquidated
}

public class Solvency
{
    public SolvencyState State { get; set; } = SolvencyState.Healthy;
    public long? NoticeUntil { get; set; }
    public long? LastRecoveryPlanAt { get; set; }

    /// <summary>Bilan rédigé au moment de la liquidation.</summary>
    public string? LiquidationReport { get; set; }

    public static string Name(SolvencyState state) => state switch
    {
        SolvencyState.Healthy => "Saine",
        SolvencyState.Tight => "Tendue",
        SolvencyState.Overdue => "Impayés",
        SolvencyState.Notice => "Mise en demeure",
        _ => "Liquidée"
    };
}
