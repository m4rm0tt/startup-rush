namespace Core.campaign;

public enum Severity { Info, Good, Warning, Critical }

/// <summary>
/// Une notification du jeu. « Target » renvoie vers ce qui est concerné (« product:3 », « finance », « person:7 »)
/// pour que l'interface ouvre directement le bon endroit. Un problème a une clé : répété, il est regroupé ;
/// réglé, il est marqué résolu et quitte la liste des choses à traiter.
/// </summary>
public class Notice
{
    public int Id { get; set; }
    public long Minute { get; set; }
    public Severity Severity { get; set; }
    public string Category { get; set; } = "";
    public string Title { get; set; } = "";
    public string Detail { get; set; } = "";
    public string Target { get; set; } = "";
    public bool Read { get; set; }

    /// <summary>Clé de regroupement d'un problème (« outage:3 », « unpaid »...).</summary>
    public string Key { get; set; } = "";

    /// <summary>Nombre d'occurrences regroupées.</summary>
    public int Count { get; set; } = 1;

    public bool Resolved { get; set; }

    public bool IsProblem => Severity >= Severity.Warning;
}

/// <summary>Une entreprise du joueur.</summary>
public class Company
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Sector { get; set; } = "";
    public long FoundedAt { get; set; }
}

/// <summary>Une proposition d'investisseur, valable un temps limité.</summary>
public class InvestorOffer
{
    public string Investor { get; set; } = "";
    public decimal Amount { get; set; }
    public double Equity { get; set; }
    public long ExpiresAt { get; set; }
    public int ProductId { get; set; }
}
