namespace Core.ai;

/// <summary>L'usage visé par un modèle : il décide du public, de la sensibilité au délai et de la taille de modèle qui convient.</summary>
public enum AiGoal { Assistant, Generation, Analysis, Api }

/// <summary>
/// Une base de modèle : sa taille décide du meilleur résultat possible (plafond), du délai de réponse, de la charge que
/// chaque utilisateur impose à la capacité d'inférence, et du calcul d'entraînement. Les grands modèles sont plus
/// puissants mais coûtent cher à entraîner et à servir.
/// </summary>
public record AiBase(string Id, string Name, string Pitch, double Ceiling, double Latency, double InferencePerThousand, double TrainingPoints, int DataFactor, string? Research = null);

/// <summary>Un jeu de données simulé (aucune donnée réelle) : sa qualité et son prix de base.</summary>
public record AiDataset(string Id, string Name, string Pitch, double Quality, decimal Price, string? Research = null);

/// <summary>Un objectif : son public, son poids d'usage, sa sensibilité au délai, et comment chaque taille de modèle lui convient.</summary>
public record AiGoalDef(AiGoal Goal, string Name, string Pitch, string SegmentId, double LatencySensitivity, double FitSmall, double FitMedium, double FitLarge)
{
    public double Fit(string baseId) => baseId switch { "small" => FitSmall, "medium" => FitMedium, _ => FitLarge };
}

public static class AiCatalog
{
    /// <summary>Calcul fourni par une unité GPU et par une unité de calcul (CPU) pendant une nuit, en points d'entraînement.</summary>
    public const double PointsPerGpuUnit = 20;

    public const double PointsPerComputeUnit = 1;

    /// <summary>Puissance d'inférence d'une unité de calcul (CPU) : un GPU vaut 1, le CPU presque rien ⚙️.</summary>
    public const double InferencePerComputeUnit = 0.03;

    /// <summary>Location externe : points d'entraînement par jour au plus, et prix du point en CHF ⚙️.</summary>
    public const double ExternalPointsPerDay = 150;

    public const decimal ExternalPointPrice = 0.5m;

    /// <summary>Inférence louée : prix d'une unité de puissance par mois, en CHF ⚙️ (le GPU interne ne coûte que l'électricité).</summary>
    public const decimal ExternalInferencePrice = 60m;

    /// <summary>Plus la validation est bonne, plus la qualité perdue en défauts cachés est faible (0,08 au plus) ⚙️.</summary>
    public const double HiddenFlaws = 0.08;

    public static readonly IReadOnlyList<AiBase> Bases =
    [
        new("small", "Petit modèle", "Léger, rapide à entraîner, peu gourmand. Plafond modeste, mais un bon spécialiste.", 0.62, 0.2, 0.5, 600, 1),
        new("medium", "Modèle moyen", "Un bon compromis. Demande la recherche « Apprentissage automatique ».", 0.78, 0.5, 1.5, 2_400, 2, "res_ml"),
        new("large", "Grand modèle", "Le plus puissant, lent et cher à servir. Demande la recherche « Grands modèles ».", 0.92, 1.0, 5.0, 9_000, 4, "res_ml_large"),
    ];

    public static readonly IReadOnlyList<AiDataset> Datasets =
    [
        new("public", "Données publiques", "Gratuit, mais bruité.", 0.35, 0m),
        new("bought", "Données achetées", "Un jeu propre, acheté auprès d'un fournisseur.", 0.6, 1_200m),
        new("curated", "Données curées", "Nettoyées et équilibrées à la main. Demande la recherche « Qualité des données ».", 0.85, 5_000m, "res_data"),
    ];

    public static readonly IReadOnlyList<AiGoalDef> Goals =
    [
        new(AiGoal.Assistant, "Assistant", "Répond à tout le monde, vite. Les grands modèles y brillent.", "ai_assist", 0.5, 0.80, 1.0, 1.1),
        new(AiGoal.Generation, "Génération", "Textes et images : la qualité avant tout, au prix du calcul.", "ai_creative", 0.2, 0.70, 1.0, 1.15),
        new(AiGoal.Analysis, "Analyse et prédiction", "Un domaine étroit : un petit modèle spécialisé suffit souvent.", "ai_analytics", 0.3, 1.10, 1.05, 1.0),
        new(AiGoal.Api, "API de modèles", "Les développeurs intègrent le modèle à leurs produits.", "ai_api", 0.4, 1.0, 1.05, 1.05),
    ];

    public static AiBase Base(string id) => Bases.First(b => b.Id == id);
    public static AiDataset Dataset(string id) => Datasets.First(d => d.Id == id);
    public static AiGoalDef Goal(AiGoal goal) => Goals.First(g => g.Goal == goal);

    /// <summary>L'objectif qui correspond à un public d'IA (null pour un autre type de projet).</summary>
    public static AiGoalDef? GoalOfSegment(string segmentId) => Goals.FirstOrDefault(g => g.SegmentId == segmentId);

    /// <summary>
    /// Qualité d'un modèle entraîné (0 à 1) : le plafond de sa taille, fortement dépendant de la qualité des données, et
    /// l'adéquation de la taille à l'objectif. Un petit modèle spécialisé sur de bonnes données bat un gros modèle nourri de
    /// données médiocres.
    /// </summary>
    public static double Quality(AiGoal goal, string baseId, string datasetId)
    {
        var b = Base(baseId);
        return Math.Clamp(b.Ceiling * (0.4 + 0.6 * Dataset(datasetId).Quality) * Goal(goal).Fit(baseId), 0.1, 0.95);
    }

    /// <summary>Prix d'un jeu de données pour une taille de modèle : les gros modèles demandent plus de données.</summary>
    public static decimal DatasetPrice(string datasetId, string baseId) => Dataset(datasetId).Price * Base(baseId).DataFactor;
}

/// <summary>Une version entraînée d'un modèle : sa qualité de base, et ce que l'évaluation a permis de valider.</summary>
public class AiVersion
{
    public int Number { get; set; }
    public double Quality { get; set; }
    public string DatasetId { get; set; } = "public";
    public long TrainedAt { get; set; }

    /// <summary>Validation obtenue à l'évaluation (0 à 1) : 0 sans évaluation, jusqu'à 1 avec un parfait mini-jeu.</summary>
    public double Validation { get; set; }

    public bool Evaluated { get; set; }
    public long? PublishedAt { get; set; }

    /// <summary>Qualité vue par les utilisateurs : la qualité entraînée moins les défauts que l'évaluation n'a pas attrapés.</summary>
    public double Effective => Math.Clamp(Quality - AiCatalog.HiddenFlaws * (1 - Validation), 0.05, 0.95);
}

/// <summary>Un entraînement en cours : du calcul à fournir, interne (GPU du site) ou loué.</summary>
public class AiTraining
{
    public string DatasetId { get; set; } = "public";
    public double Points { get; set; }
    public double Progress { get; set; }
    public bool External { get; set; }
    public long StartedAt { get; set; }

    /// <summary>Points obtenus hier, pour annoncer une durée.</summary>
    public double LastRate { get; set; }
}

/// <summary>
/// Un modèle d'IA de la campagne : un objectif, une base, des versions entraînées. La version en service continue de servir
/// tant que la suivante n'est pas publiée. Rien n'est entraîné sur l'ordinateur du joueur : tout est simulé et sauvegardé.
/// </summary>
public class AiModel
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public AiGoal Goal { get; set; }
    public string BaseId { get; set; } = "small";
    public List<AiVersion> Versions { get; set; } = new();
    public int NextVersion { get; set; } = 1;
    public int LiveNumber { get; set; }
    public AiTraining? Training { get; set; }

    public AiBase Base => AiCatalog.Base(BaseId);
    public AiGoalDef GoalDef => AiCatalog.Goal(Goal);
    public AiVersion? Live => Versions.FirstOrDefault(v => v.Number == LiveNumber);

    /// <summary>Une version entraînée qui attend d'être évaluée ou publiée (la plus récente, plus récente que celle en service).</summary>
    public AiVersion? Candidate => Versions.Where(v => v.Number > LiveNumber).OrderByDescending(v => v.Number).FirstOrDefault();

    public string Stage => Training != null ? "Entraînement" : Candidate is { Evaluated: false } ? "À évaluer" : Candidate != null ? "Prête à publier" : Live != null ? "En service" : "À entraîner";
}
