using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Core.ai;
using Core.campaign;
using Core.products;
using UI.Pixel;

namespace UI.Hud.Panels;

/// <summary>
/// L'IA simulée : créer un modèle, l'entraîner (GPU de tes sites ou calcul loué), l'évaluer, le publier, puis le brancher sur des
/// produits. Tout est simulé et sauvegardé : rien ne s'entraîne sur ton ordinateur. L'ancienne version sert tant que la
/// nouvelle n'est pas publiée.
/// </summary>
public class AiPanel : GamePanel
{
    public override string Title => "Intelligence artificielle";
    public override string Context => $"{Campaign.AiModels.Count} modèle(s), {Campaign.PublishedModels.Count()} en service";
    public override bool Large => true;

    private AiGoal _goal = AiGoal.Assistant;
    private string _base = "small";
    private readonly Dictionary<int, (string Dataset, bool External)> _drafts = new();

    public override Control Build()
    {
        var column = Ui.Column(10, Hint("ai", "Entraîne un modèle (objectif, taille, données), évalue-le, publie-le : tes produits d'IA et tes fonctions d'IA l'utilisent."));
        column.Children.Add(Capacity());

        var models = Ui.Column(8);
        foreach (var model in Campaign.AiModels)
            models.Children.Add(ModelCard(model));
        if (Campaign.AiModels.Count == 0)
            models.Children.Add(Ui.Muted("Aucun modèle pour l'instant : crée le premier ci-dessous."));
        column.Children.Add(Ui.Section("Tes modèles", models));
        column.Children.Add(NewModel());
        column.Children.Add(Products());
        return column;
    }

    // ------------------------------------------------------------------ Capacité

    private Control Capacity()
    {
        var totals = Campaign.Infra();
        var tiles = new WrapPanel();
        tiles.Children.Add(Ui.Stat("GPU des sites", $"{totals.Gpu:0.#}", tip: "Unités GPU : l'inférence passe d'abord, puis l'entraînement, puis les offres cloud."));
        tiles.Children.Add(Ui.Stat("Calcul (CPU)", $"{totals.Compute:0.#}", tip: "Sert à l'entraînement, très peu à l'inférence."));
        tiles.Children.Add(Ui.Stat("Pris cette nuit", $"{Campaign.AiGpuUsed:0.#} GPU", Palette.Gold, 170, "L'inférence et l'entraînement de la dernière nuit ne sont pas revendus."));
        var share = Ui.Row(6, Ui.Muted("Part du calcul libre pour l'entraînement :", 13),
            Ui.Choice(new[] { 0.25, 0.5, 0.75, 1.0 }.Select(v => (v, $"{v:P0}")), Campaign.TrainingShare, v => Run(Campaign.SetTrainingShare(v))));
        return Ui.Section("Capacité", tiles, share,
            Ui.Muted($"Un GPU fournit {AiCatalog.PointsPerGpuUnit:0} points d'entraînement par nuit. Louer : {AiCatalog.ExternalPointPrice:0.00} CHF le point, {AiCatalog.ExternalPointsPerDay:0} points par jour au plus.", 12));
    }

    // ------------------------------------------------------------------ Un modèle

    private Control ModelCard(AiModel model)
    {
        var live = model.Live;
        var head = Ui.Row(8, Ui.Icon("ai", 24), Ui.Text(model.Name, 17, Palette.Snow, bold: true),
            Ui.Tag(model.GoalDef.Name, Palette.Sky), Ui.Tag(model.Base.Name, Palette.Steel, Palette.Snow),
            Ui.Tag(model.Stage.ToUpperInvariant(), model.Training != null ? Palette.Lemon : model.Candidate != null ? Palette.Gold : live != null ? Palette.Leaf : Palette.Steel,
                model.Live == null && model.Candidate == null && model.Training == null ? Palette.Snow : Palette.Ink));
        var body = Ui.Column(5, head);

        if (live != null)
            body.Children.Add(Ui.Row(12, Ui.Text($"Version {live.Number} en service", 14, Palette.Leaf, bold: true),
                Ui.Muted($"qualité vue par les utilisateurs {Campaign.AiQualityFor(model):P0} · délai {Latency(model.Base)} · {model.Base.InferencePerThousand:0.#} GPU pour 1 000 utilisateurs", 13)));
        if (model.Versions.Count > 0)
            body.Children.Add(Ui.Muted(string.Join(" · ", model.Versions.OrderBy(v => v.Number).Select(v =>
                $"v{v.Number} : {v.Quality:P0}{(v.Evaluated ? $", validée à {v.Validation:P0}" : "")} ({AiCatalog.Dataset(v.DatasetId).Name.ToLowerInvariant()})")), 12));

        if (model.Training is { } training)
        {
            double perDay = training.External ? AiCatalog.ExternalPointsPerDay : training.LastRate;
            string eta = perDay > 0 ? $"~{Math.Ceiling((training.Points - training.Progress) / perDay)} j" : "rien ne l'alimente cette nuit";
            body.Children.Add(Ui.Bar(training.Progress / training.Points, Palette.Gold, 8));
            body.Children.Add(Ui.Muted($"Entraînement : {training.Progress:N0} / {training.Points:N0} points · {eta} · {(training.External ? $"calcul loué, jusqu'à {AiCatalog.ExternalPointsPerDay * (double)AiCatalog.ExternalPointPrice:0} CHF par jour" : "sur tes GPU")}", 13));
            body.Children.Add(Ui.Button("Abandonner l'entraînement", () => Screen.Confirm("Abandonner ?", "Le jeu de données payé n'est pas rendu.", "Abandonner", () => Run(Campaign.CancelAiTraining(model.Id)))));
        }
        else if (model.Candidate is { } candidate)
        {
            body.Children.Add(Ui.Text($"Version {candidate.Number} entraînée : qualité {candidate.Quality:P0}" +
                (candidate.Evaluated ? $", validation {candidate.Validation:P0} → {candidate.Effective:P0} vue des utilisateurs." : $". Sans évaluation, {candidate.Effective:P0} vus des utilisateurs."), 14));
            var (canDelegate, why) = Campaign.CanDelegateEvaluation(model);
            body.Children.Add(Ui.Row(6,
                !candidate.Evaluated ? Ui.Button("Évaluer (mini-jeu)", () => ChallengeView.ShowEvaluation(Screen, model.Id), "gold") : null,
                !candidate.Evaluated ? Ui.Button("Déléguer l'évaluation", () => Run(Campaign.DelegateEvaluation(model.Id)), "", canDelegate, canDelegate ? null : why) : null,
                Ui.Button(live == null ? "Publier" : $"Publier (remplace la v{live.Number})", () => Run(Campaign.PublishAi(model.Id), $"{model.Name} version {candidate.Number} est en service."), candidate.Evaluated ? "gold" : ""),
                Ui.Button("Jeter", () => Run(Campaign.DiscardAiCandidate(model.Id)))));
        }
        else
            body.Children.Add(TrainingChoice(model));

        return Ui.Card(body);
    }

    private static string Latency(AiBase aiBase) => aiBase.Latency switch { <= 0.3 => "court", <= 0.6 => "moyen", _ => "long" };

    /// <summary>Choisir le jeu de données et d'où vient le calcul, avec le coût et la durée annoncés avant de lancer.</summary>
    private Control TrainingChoice(AiModel model)
    {
        var draft = _drafts.GetValueOrDefault(model.Id, ("public", false));
        var dataset = AiCatalog.Dataset(draft.Item1);
        if (!Campaign.CanUseDataset(dataset).Ok)
            dataset = AiCatalog.Datasets[0];
        decimal price = AiCatalog.DatasetPrice(dataset.Id, model.BaseId);
        var totals = Campaign.Infra();
        double capacity = totals.Gpu * Campaign.TrainingShare * AiCatalog.PointsPerGpuUnit + totals.Compute * Campaign.TrainingShare * AiCatalog.PointsPerComputeUnit;
        double perDay = draft.Item2 ? AiCatalog.ExternalPointsPerDay : capacity;
        string duration = perDay > 0 ? $"~{Math.Ceiling(model.Base.TrainingPoints / perDay)} jours" : "impossible sans GPU : loue du calcul";
        double expected = AiCatalog.Quality(model.Goal, model.BaseId, dataset.Id);

        var data = new WrapPanel();
        foreach (var d in AiCatalog.Datasets)
        {
            var (ok, why) = Campaign.CanUseDataset(d);
            var button = Ui.Button($"{d.Name} ({AiCatalog.DatasetPrice(d.Id, model.BaseId):N0} CHF)", () =>
            {
                _drafts[model.Id] = (d.Id, draft.Item2);
                Refresh();
            }, d.Id == dataset.Id ? "tab selected" : "tab", ok, ok ? d.Pitch : why);
            button.Margin = new Thickness(0, 0, 4, 4);
            data.Children.Add(button);
        }
        return Ui.Column(6,
            Ui.Muted("Nouvelle version : choisis les données et le calcul.", 13),
            data,
            Ui.Row(8, Ui.Muted("Calcul :", 13), Ui.Choice([(false, "Mes GPU"), (true, "Calcul loué")], draft.Item2, v =>
            {
                _drafts[model.Id] = (dataset.Id, v);
                Refresh();
            })),
            Ui.Muted($"Qualité attendue : {expected:P0} · {model.Base.TrainingPoints:N0} points · {duration} · données : {price:N0} CHF", 13),
            Ui.Button("Entraîner une nouvelle version", () => Run(Campaign.StartAiTraining(model.Id, dataset.Id, draft.Item2)), "gold"));
    }

    // ------------------------------------------------------------------ Nouveau modèle

    private Control NewModel()
    {
        var goals = Ui.Choice(AiCatalog.Goals.Select(g => (g.Goal, g.Name)), _goal, v =>
        {
            _goal = v;
            Refresh();
        });
        var bases = new WrapPanel();
        foreach (var b in AiCatalog.Bases)
        {
            var (ok, why) = Campaign.CanUseBase(b);
            var button = Ui.Button(b.Name, () =>
            {
                _base = b.Id;
                Refresh();
            }, b.Id == _base ? "tab selected" : "tab", ok, ok ? b.Pitch : why);
            button.Margin = new Thickness(0, 0, 4, 4);
            bases.Children.Add(button);
        }
        var goal = AiCatalog.Goal(_goal);
        var chosen = AiCatalog.Base(_base);
        if (!Campaign.CanUseBase(chosen).Ok)
            chosen = AiCatalog.Bases[0];
        return Ui.Section("Nouveau modèle",
            Ui.Row(8, Ui.Muted("Objectif :", 13), goals),
            Ui.Muted(goal.Pitch, 13),
            Ui.Row(8, Ui.Muted("Taille :", 13), bases),
            Ui.Muted($"{chosen.Pitch} Plafond de qualité {chosen.Ceiling:P0}.", 13),
            Ui.Button("Créer ce modèle", () => Run(Campaign.CreateAiModel("", _goal, chosen.Id).Result), "gold"));
    }

    // ------------------------------------------------------------------ Produits qui utilisent un modèle

    private Control Products()
    {
        var list = Ui.Column(8);
        foreach (var product in Campaign.Products.Where(p => p.AiModelId != 0 || p.ArchetypeId == "ai" || p.Level("ai_feature") > 0 || p.Level("ai_core") > 0))
        {
            var model = Campaign.FindAiModel(product.AiModelId);
            double load = Campaign.AiLoad(product);
            double capacity = Campaign.InferenceCapacity(product);
            var line = Ui.Column(5,
                Ui.Row(8, Ui.IconTile(product.Archetype.Icon, Palette.At(product.Color), 28), Ui.Text(product.Name, 15, Palette.Snow, bold: true),
                    Ui.Tag(model != null ? $"Modèle : {model.Name}" : "Sans modèle", model != null ? Palette.Sky : Palette.Ember)),
                Ui.Row(6, Ui.Muted("Modèle utilisé :", 13), Campaign.PublishedModels.Any()
                    ? Ui.Choice(Campaign.PublishedModels.Select(m => (m.Id, m.Name)), product.AiModelId, id => Run(Campaign.SetProductAiModel(product.Id, id)))
                    : Ui.Muted("aucun modèle publié", 13)));
            if (load > 0)
            {
                line.Children.Add(Ui.Bar(Math.Min(1, capacity / Math.Max(0.01, load)), capacity >= load ? Palette.Leaf : Palette.Ember, 8));
                line.Children.Add(Ui.Muted($"Inférence : {capacity:0.#} de puissance pour {load:0.#} demandés " +
                    $"({product.InferenceInternal:0.#} sur tes GPU, {product.InferenceRented:0.#} loués, {(decimal)product.InferenceRented * AiCatalog.ExternalInferencePrice:N0} CHF par mois)", 13));
                line.Children.Add(Ui.Row(6, Ui.Muted("Location :", 13),
                    Ui.Choice([(true, "Autorisée"), (false, "Interdite")], product.InferenceExternal, v => Run(Campaign.SetInference(product.Id, v, product.InferenceCap))),
                    Ui.Button("−", () => Run(Campaign.SetInference(product.Id, product.InferenceExternal, product.InferenceCap - 100m)), "tab", product.InferenceCap >= 100m),
                    Ui.Text($"plafond {product.InferenceCap:N0} CHF/mois", 14, Palette.Snow, wrap: false),
                    Ui.Button("+", () => Run(Campaign.SetInference(product.Id, product.InferenceExternal, product.InferenceCap + 100m)), "tab")));
            }
            line.Children.Add(Ui.Button("Ouvrir le projet", () => Open(new ProjectPanel(product.Id)), "tab"));
            list.Children.Add(Ui.Card(line));
        }
        if (list.Children.Count == 0)
            list.Children.Add(Ui.Muted("Aucun produit n'utilise encore un modèle. Crée un produit d'IA, ou ajoute « Fonctions d'IA » à une application."));
        return Ui.Section("Produits qui utilisent un modèle", list,
            Ui.Button("Créer un produit d'IA", () => Open(new NewProjectPanel("ai")), "gold", Campaign.TypeAvailable("ai")));
    }
}
