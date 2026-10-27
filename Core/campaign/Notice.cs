namespace Core.campaign;

public enum Severity { Info, Good, Warning, Critical }

/// <summary>
/// Une notification du jeu. « Target » renvoie vers ce qui est concerné (« contract:12 », « product:3 », « finance »)
/// pour que l'interface ouvre directement le bon menu.
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
}

/// <summary>Une entreprise du joueur : un nom, un secteur, une réputation et ses composants réutilisables.</summary>
public class Company
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Sector { get; set; } = "";
    public long FoundedAt { get; set; }

    /// <summary>Réputation d'agence (0 à 100) : elle attire des demandes plus grosses et des candidats plus expérimentés.</summary>
    public double Reputation { get; set; } = 5;

    /// <summary>Niveau de code réutilisable par type de prestation (0 à 3).</summary>
    public Dictionary<string, int> ReuseLevels { get; set; } = new();
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
