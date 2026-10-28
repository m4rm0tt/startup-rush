using Core.ai;
using Core.finance;
using Core.products;

namespace Core.campaign;

/// <summary>
/// L'IA simulée : tout reste dans la campagne, rien n'est entraîné sur l'ordinateur du joueur. Un modèle (objectif, base) est
/// entraîné avec un jeu de données, grâce aux GPU de tes sites ou à de la location externe, puis évalué (mini-jeu facultatif),
/// puis publié. La version en service continue de servir tant que la suivante n'est pas publiée. Des produits d'IA (ou des
/// fonctions d'IA dans tes applications) branchent un modèle publié ; leur inférence prend de la capacité GPU, répartie chaque
/// nuit après tes hébergements et avant les offres cloud (une même unité n'est jamais promise deux fois).
/// </summary>
public partial class Campaign
{
    public List<AiModel> AiModels { get; set; } = new();

    /// <summary>Part des GPU et du calcul laissés libres par l'inférence qui va à l'entraînement (le reste reste vendable).</summary>
    public double TrainingShare { get; set; } = 0.5;

    /// <summary>Unités GPU et de calcul prises cette nuit par l'inférence et l'entraînement (les offres cloud ne les revendent pas).</summary>
    public double AiGpuUsed { get; set; }

    public double AiComputeUsed { get; set; }

    public AiModel? FindAiModel(int id) => AiModels.FirstOrDefault(m => m.Id == id);

    /// <summary>Les modèles qui ont une version en service : ceux qu'un produit peut brancher.</summary>
    public IEnumerable<AiModel> PublishedModels => AiModels.Where(m => m.Live != null);

    public (bool Ok, string Reason) CanUseBase(AiBase aiBase) =>
        aiBase.Research is { } research && !ResearchDone.Contains(research)
            ? (false, $"Il faut la recherche « {ResearchDefOf(research).Name} ».") : (true, "");

    public (bool Ok, string Reason) CanUseDataset(AiDataset dataset) =>
        dataset.Research is { } research && !ResearchDone.Contains(research)
            ? (false, $"Il faut la recherche « {ResearchDefOf(research).Name} ».") : (true, "");

    public (CommandResult Result, AiModel? Model) CreateAiModel(string name, AiGoal goal, string baseId)
    {
        if (!IsUnlocked("ai_models"))
            return (CommandResult.Fail("À débloquer dans Développement : Intelligence artificielle."), null);
        var (ok, reason) = CanUseBase(AiCatalog.Base(baseId));
        if (!ok)
            return (CommandResult.Fail(reason), null);
        string[] names = ["Lumen", "Sillage", "Atlas", "Orion", "Nimbus", "Cobalt"];
        var model = new AiModel
        {
            Id = NewId(),
            Name = string.IsNullOrWhiteSpace(name) ? names.FirstOrDefault(n => AiModels.All(m => m.Name != n)) ?? $"Modèle {AiModels.Count + 1}" : name.Trim(),
            Goal = goal,
            BaseId = baseId,
        };
        AiModels.Add(model);
        return (CommandResult.Success(), model);
    }

    /// <summary>
    /// Lance l'entraînement d'une nouvelle version : le jeu de données est payé tout de suite, le calcul vient des GPU de tes sites
    /// (la part prévue pour l'entraînement) ou d'une location externe (0,50 CHF le point, au plus 150 points par jour).
    /// </summary>
    public CommandResult StartAiTraining(int modelId, string datasetId, bool external)
    {
        var model = FindAiModel(modelId);
        if (model == null)
            return CommandResult.Fail("Modèle inconnu.");
        if (model.Training != null)
            return CommandResult.Fail("Un entraînement est déjà en cours.");
        if (model.Candidate != null)
            return CommandResult.Fail("Une version attend d'être évaluée ou publiée : publie-la, ou jette-la.");
        var dataset = AiCatalog.Dataset(datasetId);
        var (ok, reason) = CanUseDataset(dataset);
        if (!ok)
            return CommandResult.Fail(reason);
        var totals = Infra();
        if (!external && totals.Gpu + totals.Compute <= 0)
            return CommandResult.Fail("Aucun GPU ni calcul sur tes sites : loue du calcul externe pour commencer.");
        decimal price = AiCatalog.DatasetPrice(datasetId, model.BaseId);
        if (price > 0)
        {
            var spent = Spend(price, Category.Research, $"Données d'entraînement : {model.Name} ({dataset.Name})");
            if (!spent.Ok)
                return spent;
        }
        model.Training = new AiTraining { DatasetId = datasetId, Points = model.Base.TrainingPoints, External = external, StartedAt = Minute };
        return CommandResult.Success($"Entraînement de {model.Name} lancé : {model.Base.TrainingPoints:N0} points de calcul à fournir.");
    }

    /// <summary>Abandonne l'entraînement en cours (le jeu de données payé n'est pas rendu).</summary>
    public CommandResult CancelAiTraining(int modelId)
    {
        var model = FindAiModel(modelId);
        if (model?.Training == null)
            return CommandResult.Fail("Aucun entraînement en cours.");
        model.Training = null;
        return CommandResult.Success("Entraînement abandonné.");
    }

    /// <summary>Jette la version entraînée qui n'a pas été publiée.</summary>
    public CommandResult DiscardAiCandidate(int modelId)
    {
        var model = FindAiModel(modelId);
        if (model?.Candidate is not { } candidate)
            return CommandResult.Fail("Aucune version à jeter.");
        model.Versions.Remove(candidate);
        return CommandResult.Success("Version jetée : la version en service ne change pas.");
    }

    // ------------------------------------------------------------------ Évaluation (mini-jeu) et publication

    private static readonly string[] SampleSubjects = ["Avis client", "Mesure du capteur", "Image test", "Message reçu", "Ligne de journal", "Échantillon vocal"];

    /// <summary>
    /// Évaluation : associer chaque échantillon de test à son résultat selon une règle claire (moins de 5 : négatif, 5 : douteux,
    /// plus de 5 : positif). Tiré d'un hasard séparé, il se rejoue à l'identique et ne touche pas à la simulation.
    /// </summary>
    public LaunchChallenge EvaluationChallenge(int modelId)
    {
        var model = FindAiModel(modelId) ?? throw new ArgumentException("Modèle inconnu.");
        var random = new SimRandom(Seed ^ ((ulong)modelId * 0x9E3779B97F4A7C15UL) ^ ((ulong)(model.Candidate?.Number ?? 0) * 0xA24BAED4963EE407UL) ^ 0xA1);
        var scores = SampleSubjects.Select(_ => random.Next(1, 10)).ToList();
        if (!scores.Contains(5))
            scores[random.Next(0, scores.Count)] = 5;
        var items = SampleSubjects.Select((s, i) => $"{s} : score {scores[i]}").ToList();
        var answer = scores.Select(n => n < 5 ? 0 : n == 5 ? 1 : 2).ToList();
        return new LaunchChallenge("match", "Moins de 5 : négatif. 5 : douteux. Plus de 5 : positif. Associe chaque échantillon à son résultat.", items, answer)
        {
            Options = ["Négatif", "Douteux", "Positif"],
        };
    }

    /// <summary>Applique le score d'évaluation à la version prête : jusqu'à 0,08 de qualité retrouvée, jamais plus.</summary>
    public CommandResult EvaluateAi(int modelId, double score)
    {
        var candidate = FindAiModel(modelId)?.Candidate;
        if (candidate == null)
            return CommandResult.Fail("Aucune version à évaluer.");
        if (candidate.Evaluated)
            return CommandResult.Fail("Cette version est déjà évaluée.");
        candidate.Validation = Math.Clamp(score, 0, 1);
        candidate.Evaluated = true;
        return CommandResult.Success($"Évaluation : {candidate.Validation:P0} de validation.");
    }

    public (bool Ok, string Reason) CanDelegateEvaluation(AiModel model)
    {
        if (model.Candidate is not { Evaluated: false })
            return (false, "Aucune version à évaluer.");
        if (!IsUnlocked("delegation"))
            return (false, "À débloquer dans Développement : Délégation des lancements.");
        if (Delegate() == null)
            return (false, "Il faut un employé présent pour évaluer à ta place.");
        return (true, "");
    }

    /// <summary>Un employé évalue sans mini-jeu : un quart de validation, la moitié avec les procédures de publication.</summary>
    public CommandResult DelegateEvaluation(int modelId)
    {
        var model = FindAiModel(modelId);
        if (model == null)
            return CommandResult.Fail("Modèle inconnu.");
        var (ok, reason) = CanDelegateEvaluation(model);
        if (!ok)
            return CommandResult.Fail(reason);
        var who = Delegate()!;
        var result = EvaluateAi(modelId, IsUnlocked("procedures") ? 0.5 : 0.25);
        if (result.Ok)
            Notify(Severity.Info, "ia", $"{who.Name} a évalué {model.Name}", "Sans mini-jeu.", "ai");
        return result;
    }

    /// <summary>Met la version prête en service : l'ancienne servait jusque-là ; les produits qui utilisent le modèle passent à la nouvelle.</summary>
    public CommandResult PublishAi(int modelId)
    {
        var model = FindAiModel(modelId);
        if (model?.Candidate is not { } candidate)
            return CommandResult.Fail("Aucune version prête à publier.");
        model.LiveNumber = candidate.Number;
        candidate.PublishedAt = Minute;
        foreach (var product in Products.Where(p => p.AiModelId == model.Id))
            RefreshAiComponents(product);
        Notify(Severity.Good, "ia", $"{model.Name} version {candidate.Number} est en service", $"Qualité : {candidate.Effective:P0}.", "ai");
        Award("ai:published", 1, "Premier modèle d'IA publié.");
        return CommandResult.Success();
    }

    // ------------------------------------------------------------------ Produits qui utilisent un modèle

    /// <summary>Branche un modèle publié sur un produit (produit d'IA, ou application avec des fonctions d'IA).</summary>
    public CommandResult SetProductAiModel(int productId, int modelId)
    {
        var product = FindProduct(productId);
        var model = FindAiModel(modelId);
        if (product == null || model == null)
            return CommandResult.Fail("Projet ou modèle inconnu.");
        if (model.Live == null)
            return CommandResult.Fail("Ce modèle n'a pas encore de version en service.");
        product.AiModelId = modelId;
        RefreshAiComponents(product);
        return CommandResult.Success($"{product.Name} utilise maintenant {model.Name}.");
    }

    /// <summary>L'inférence : prendre de la capacité en interne, ou louer sous le plafond mensuel.</summary>
    public CommandResult SetInference(int productId, bool external, decimal cap)
    {
        var product = FindProduct(productId);
        if (product == null)
            return CommandResult.Fail("Projet inconnu.");
        product.InferenceExternal = external;
        product.InferenceCap = Math.Max(0, cap);
        return CommandResult.Success();
    }

    /// <summary>Réglage du partage du calcul libre entre l'entraînement et ce que tu vends (de 10 à 100 %).</summary>
    public CommandResult SetTrainingShare(double share)
    {
        TrainingShare = Math.Clamp(share, 0.1, 1);
        return CommandResult.Success();
    }

    /// <summary>Qualité vue par les utilisateurs : celle de la version en service, moins une pénalité de délai selon l'objectif et la taille.</summary>
    public static double AiQualityFor(AiModel model)
    {
        if (model.Live is not { } live)
            return 0.1;
        return Math.Clamp(live.Effective - model.GoalDef.LatencySensitivity * model.Base.Latency * 0.08, 0.05, 0.95);
    }

    /// <summary>La qualité du modèle devient celle des composants d'IA du produit (et celle d'un modèle retiré, la plus basse).</summary>
    private void RefreshAiComponents(Product product)
    {
        double quality = FindAiModel(product.AiModelId) is { Live: not null } model ? AiQualityFor(model) : 0.1;
        foreach (var component in product.Components.Where(c => c.DefId is "ai_core" or "ai_feature"))
            component.Quality = Math.Round(quality, 3);
    }

    /// <summary>Relie au meilleur modèle publié un produit qui vient de recevoir un composant d'IA (sans toucher à un lien encore valide).</summary>
    private void AutoLinkAi(Product product)
    {
        if (product.AiModelId != 0 && FindAiModel(product.AiModelId)?.Live != null)
            return;
        if (PublishedModels.OrderByDescending(AiQualityFor).FirstOrDefault() is { } best)
        {
            product.AiModelId = best.Id;
            RefreshAiComponents(product);
        }
    }

    /// <summary>Puissance d'inférence demandée par les utilisateurs d'un produit (un produit d'IA en consomme pleinement, une fonction d'IA un tiers).</summary>
    public double AiLoad(Product product)
    {
        if (FindAiModel(product.AiModelId) is not { Live: not null } model)
            return 0;
        double usage = product.ArchetypeId == "ai" ? (product.Level("ai_core") > 0 ? 1 : 0) : product.Level("ai_feature") > 0 ? 0.35 : 0;
        return (product.Users + product.Sellers) / 1000 * model.Base.InferencePerThousand * usage;
    }

    public double InferenceCapacity(Product product) => product.InferenceInternal + product.InferenceRented;

    private AiService? AiInputs(Product product)
    {
        double load = AiLoad(product);
        return load > 0 ? new AiService(InferenceCapacity(product), load) : null;
    }

    // ------------------------------------------------------------------ Chaque nuit

    /// <summary>
    /// Répartit le GPU et le calcul restants après tes hébergements : d'abord l'inférence des produits en ligne (avec 25 % de
    /// marge), puis l'entraînement (la part prévue), pendant que la location externe prend le reste. Ce qui n'est pas pris reste
    /// vendable par les offres cloud.
    /// </summary>
    private void AllocateAi(InfraTotals totals)
    {
        double gpuLeft = totals.Gpu, computeLeft = totals.Compute;
        double gpuUsed = 0, computeUsed = 0;

        foreach (var product in Products.OrderBy(p => p.Priority))
        {
            product.InferenceInternal = 0;
            product.InferenceRented = 0;
            if (product.AiModelId == 0)
                continue;
            RefreshAiComponents(product);
            double load = AiLoad(product);
            if (!product.IsLive || load <= 0)
                continue;
            double want = Math.Max(0.2, load / 0.75);
            double fromGpu = Math.Min(gpuLeft, want);
            gpuLeft -= fromGpu;
            gpuUsed += fromGpu;
            double fromCompute = Math.Min(computeLeft, (want - fromGpu) / AiCatalog.InferencePerComputeUnit);
            computeLeft -= fromCompute;
            computeUsed += fromCompute;
            product.InferenceInternal = Math.Round(fromGpu + fromCompute * AiCatalog.InferencePerComputeUnit, 2);
            double missing = want - product.InferenceInternal;
            if (product.InferenceExternal && missing > 0.01)
                product.InferenceRented = Math.Round(Math.Min(missing, (double)(product.InferenceCap / AiCatalog.ExternalInferencePrice)), 2);
            if (InferenceCapacity(product) < load * 1.05)
                Notify(Severity.Warning, "ia", $"{product.Name} : réponses trop lentes",
                    "Pas assez de GPU pour tous les utilisateurs : ajoute des GPU, ou relève le plafond de location.", "ai", key: $"inference:{product.Id}");
            else
                Resolve($"inference:{product.Id}");
        }

        double trainGpu = gpuLeft * TrainingShare, trainCompute = computeLeft * TrainingShare;
        foreach (var model in AiModels.Where(m => m.Training != null).ToList())
        {
            var training = model.Training!;
            double need = training.Points - training.Progress, got;
            if (training.External)
            {
                got = Math.Min(need, AiCatalog.ExternalPointsPerDay);
                Ledger.Accrue(Category.Hosting, Math.Round((decimal)got * AiCatalog.ExternalPointPrice, 2));
            }
            else
            {
                double capacity = trainGpu * AiCatalog.PointsPerGpuUnit + trainCompute * AiCatalog.PointsPerComputeUnit;
                got = Math.Min(need, capacity);
                double fromGpu = Math.Min(trainGpu, got / AiCatalog.PointsPerGpuUnit);
                double fromCompute = Math.Min(trainCompute, (got - fromGpu * AiCatalog.PointsPerGpuUnit) / AiCatalog.PointsPerComputeUnit);
                trainGpu -= fromGpu;
                trainCompute -= fromCompute;
                gpuUsed += fromGpu;
                computeUsed += fromCompute;
            }
            training.Progress += got;
            training.LastRate = got;
            if (training.Progress >= training.Points - 1e-6)
                FinishTraining(model);
            else if (got <= 0)
                Notify(Severity.Warning, "ia", $"L'entraînement de {model.Name} n'avance pas", "Pas de GPU libre : ajoute-en, augmente la part d'entraînement, ou loue du calcul.", "ai", key: $"training:{model.Id}");
            else
                Resolve($"training:{model.Id}");
        }
        AiGpuUsed = gpuUsed;
        AiComputeUsed = computeUsed;
    }

    private void FinishTraining(AiModel model)
    {
        var training = model.Training!;
        model.Training = null;
        var version = new AiVersion
        {
            Number = model.NextVersion++,
            Quality = Math.Round(AiCatalog.Quality(model.Goal, model.BaseId, training.DatasetId), 3),
            DatasetId = training.DatasetId,
            TrainedAt = Minute,
        };
        model.Versions.Add(version);
        Resolve($"training:{model.Id}");
        Notify(Severity.Good, "ia", $"{model.Name} : version {version.Number} entraînée",
            $"Qualité {version.Quality:P0}. Évalue-la (mini-jeu facultatif) puis publie-la.", "ai");
    }
}
