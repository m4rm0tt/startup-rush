using System.Text.Json.Nodes;
using Core.campaign;
using Core.products;

namespace Core.persistence;

/// <summary>
/// Passage d'une sauvegarde au format 1 (prestations clients, éditeur de pages) au format 2 (projets à composants).
/// Rien n'est inventé ni perdu en argent : factures et dettes restent telles quelles, les contrats en cours sont
/// arrêtés sans pénalité, les modules publiés deviennent des composants, la version en cours repart en file
/// avec son avancement. Le fichier d'origine est gardé à côté.
/// </summary>
public static class Migration
{
    /// <summary>Correspondance ancien module → composant (même idée, même nom de clé quand c'est possible).</summary>
    private static readonly Dictionary<string, string> ModuleToComponent = new()
    {
        ["landing"] = "ui", ["login"] = "login", ["profile"] = "profile", ["catalog"] = "catalog", ["search"] = "search",
        ["feed"] = "feed", ["subscribe"] = "subscribe", ["checkout"] = "checkout", ["reviews"] = "reviews",
        ["messaging"] = "messaging", ["support"] = "support", ["dashboard"] = "dashboard", ["export"] = "export",
        ["notifications"] = "notifications", ["listings"] = "listings", ["stats"] = "stats",
    };

    public record Pending(int ProductId, string DefId, double Fraction);

    /// <summary>Transforme le JSON d'une campagne au format 1. Renvoie les améliorations à remettre en file après chargement.</summary>
    public static (List<Pending> Pending, List<string> Notes) FromV1(JsonObject campaign)
    {
        var notes = new List<string>();
        var pending = new List<Pending>();

        // Contrats : arrêtés sans pénalité ; leurs factures déjà émises restent dues
        var contracts = campaign["Contracts"] as JsonArray ?? new JsonArray();
        int active = contracts.Count(c => c?["Status"]?.GetValue<string>() == "Active");
        campaign.Remove("Contracts");
        if (active > 0)
            notes.Add($"{active} prestation(s) en cours arrêtée(s) sans pénalité ; les factures déjà émises restent dues et seront encaissées.");

        // Ancien travail : celui des produits est repris plus bas, celui des contrats disparaît
        var work = campaign["Work"] as JsonArray ?? new JsonArray();
        var workById = work.Where(w => w != null).ToDictionary(w => w!["Id"]!.GetValue<int>(), w => w!);
        campaign["Work"] = new JsonArray();

        var unlocked = new JsonArray();
        foreach (var node in campaign["Products"] as JsonArray ?? new JsonArray())
        {
            if (node is not JsonObject product)
                continue;
            int productId = product["Id"]!.GetValue<int>();
            string archetype = product["ArchetypeId"]?.GetValue<string>() ?? "saas";
            if (archetype == "social" || archetype == "marketplace")
                unlocked.Add("community");
            if (archetype == "marketplace")
                unlocked.Add("marketplace");
            if (product["SegmentId"]?.GetValue<string>() == "associations")
                product["SegmentId"] = "freelances";

            var quality = product["ModuleQuality"] as JsonObject;
            var components = new JsonArray();
            var have = new HashSet<string>();
            foreach (var module in Modules(product["Live"]))
            {
                if (!ModuleToComponent.TryGetValue(module.DefId, out var defId) || !have.Add(defId))
                    continue;
                double q = quality?[module.Id.ToString()]?.GetValue<double>() ?? 0.6;
                components.Add(new JsonObject { ["DefId"] = defId, ["Level"] = 1, ["Quality"] = q });
            }
            product["Components"] = components;
            product["Queue"] = new JsonArray();
            if (product["Live"] != null)
            {
                product["LaunchedAt"] = product["PublishedAt"]?.DeepClone();
                product["Version"] = product["LiveVersion"]?.GetValue<int>() ?? 1;
            }

            // Couleur : l'accent de la page d'accueil devient la couleur du projet
            var draftPages = product["Draft"]?["Pages"] as JsonArray;
            if (draftPages?.FirstOrDefault()?["Accent"] is JsonNode accent)
                product["Color"] = accent.GetValue<int>();

            // Version en fabrication : chaque nouveau module repart en file avec son avancement
            if (product["Build"] is JsonObject build)
            {
                var ids = (build["WorkItemIds"] as JsonArray ?? new JsonArray()).Select(i => i!.GetValue<int>()).ToList();
                foreach (var module in Modules(build["Spec"]))
                {
                    if (!ModuleToComponent.TryGetValue(module.DefId, out var defId) || have.Contains(defId))
                        continue;
                    have.Add(defId);
                    var items = ids.Where(workById.ContainsKey).Select(i => workById[i])
                        .Where(w => w["ModuleId"]?.GetValue<int>() == module.Id).ToList();
                    double effort = items.Sum(w => w["Effort"]!.GetValue<double>());
                    double progress = items.Sum(w => w["Progress"]!.GetValue<double>());
                    pending.Add(new Pending(productId, defId, effort > 0 ? progress / effort : 0));
                }
                notes.Add($"{product["Name"]} : la version en cours est remise en file, avancement conservé.");
            }
            foreach (string old in new[] { "Live", "Draft", "Build", "LiveVersion", "PublishedAt", "ModuleQuality", "Optimizations", "AutoFixThreshold" })
                product.Remove(old);
        }

        // Affectations : un produit (développement, support, maintenance) → ce projet ; un contrat → automatique
        foreach (var person in campaign["People"] as JsonArray ?? new JsonArray())
        {
            if (person?["Assignment"] is not JsonObject assignment)
                continue;
            string kind = assignment["Kind"]?.GetValue<string>() ?? "None";
            assignment["Kind"] = kind is "Product" or "Support" or "Maintenance" ? "Project" : "None";
            if (assignment["Kind"]!.GetValue<string>() == "None")
                assignment["TargetId"] = 0;
            person["CurrentWorkItemId"] = null;
        }

        // Notifications qui renvoyaient vers des contrats
        foreach (var notice in campaign["Notices"] as JsonArray ?? new JsonArray())
            if (notice?["Target"]?.GetValue<string>() is { } target && target.StartsWith("contract"))
                notice["Target"] = "";

        campaign["Unlocked"] = new JsonArray(unlocked.Select(u => u!.GetValue<string>()).Distinct().Select(u => (JsonNode?)JsonValue.Create(u)).ToArray());
        campaign["DevPoints"] ??= progress.DevTree.StartingPoints;
        notes.Insert(0, "Sauvegarde mise à jour vers la nouvelle version du jeu (projets à composants, sans prestations).");
        return (pending, notes);
    }

    private static IEnumerable<(int Id, string DefId)> Modules(JsonNode? spec)
    {
        foreach (var page in spec?["Pages"] as JsonArray ?? new JsonArray())
        foreach (var module in page?["Modules"] as JsonArray ?? new JsonArray())
            if (module?["DefId"]?.GetValue<string>() is { } defId)
                yield return (module["Id"]!.GetValue<int>(), defId);
    }

    /// <summary>Après chargement : remet en file les améliorations en cours, avec leur avancement.</summary>
    public static void Finish(Campaign campaign, List<Pending> pending, List<string> notes)
    {
        foreach (var item in pending)
        {
            var product = campaign.FindProduct(item.ProductId);
            if (product == null || !ProductCatalog.HasComponent(item.DefId))
                continue;
            if (!campaign.QueueComponent(product.Id, item.DefId).Ok)
                continue;
            var queued = product.Queue[^1];
            foreach (var work in queued.WorkItemIds.Select(campaign.FindWork).Where(w => w != null))
            {
                work!.Progress = Math.Round(work.Effort * Math.Clamp(item.Fraction, 0, 1), 2);
                work.QualityPoints = work.Progress * 0.6;
            }
        }
        foreach (var product in campaign.Products)
            campaign.CheckQueue(product);
        campaign.MigrationNote = string.Join(" ", notes);
    }
}
