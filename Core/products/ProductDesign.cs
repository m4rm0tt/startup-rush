using Core.campaign;
using Core.team;

namespace Core.products;

/// <summary>Une tâche prévue pour passer de la version publiée au brouillon.</summary>
public record PlannedWork(string Title, Discipline Discipline, double Effort, int? ModuleId, string Group);

/// <summary>Un problème de conception relevé dans une version (affiché dans l'éditeur, pris en compte par la simulation).</summary>
public record DesignWarning(string Text, bool Blocking);

/// <summary>
/// L'analyse d'une version de produit : ce que valent ses modules pour le public visé, ce qu'ils coûtent,
/// et les problèmes de conception. Les réglages visuels (thème, couleur, disposition, colonnes) n'entrent pas dans la valeur.
/// </summary>
public static class ProductDesign
{
    /// <summary>Gain sur le code des modules liés à un type de prestation déjà capitalisé.</summary>
    public static readonly Dictionary<string, string[]> ReuseLinks = new()
    {
        ["vitrine"] = ["landing", "support"],
        ["shop"] = ["catalog", "checkout", "search"],
        ["mobile"] = ["profile", "notifications", "feed"],
        ["tool"] = ["dashboard", "stats"],
        ["api"] = ["export", "login"],
        ["redesign"] = [],
    };

    public static double Effort(ModuleInstance module, Discipline discipline)
    {
        var def = module.Def;
        double base0 = discipline switch
        {
            Discipline.Design => def.Design,
            Discipline.Code => def.Code,
            Discipline.Test => def.Test,
            _ => 0
        };
        return base0 * module.Factor(c => c.EffortFactor);
    }

    /// <summary>Couverture de chaque besoin (0 à 1) : chaque module comble une part de ce qui reste, donc rendement décroissant.</summary>
    public static Dictionary<Need, double> Coverage(Product product, ProductSpec spec, Func<ModuleInstance, double> quality)
    {
        var missing = Enum.GetValues<Need>().ToDictionary(n => n, _ => 1.0);
        bool hasLogin = spec.ActiveModules().Any(m => m.DefId == "login");
        foreach (var module in spec.ActiveModules())
        {
            if (module.Def.Requires == "login" && !hasLogin)
                continue;
            double value = module.Factor(c => c.ValueFactor) * quality(module);
            foreach (var (need, contribution) in module.Def.Needs)
                missing[need] *= 1 - Math.Min(0.95, contribution * value);
        }
        return missing.ToDictionary(p => p.Key, p => 1 - p.Value);
    }

    /// <summary>Attrait de la version pour le public visé (0 à 1), avant bugs, performances et concurrence.</summary>
    public static double Appeal(Product product, ProductSpec spec, Func<ModuleInstance, double> quality)
    {
        var coverage = Coverage(product, spec, quality);
        var weights = product.Archetype.Needs;
        double total = weights.Values.Sum();
        double appeal = weights.Sum(w => w.Value * coverage[w.Key]) / total;
        return appeal * Usability(product, spec) * (MissingRequired(product, spec).Count > 0 ? 0.6 : 1);
    }

    /// <summary>Pages trop chargées et modules sans intérêt pour ce public : plus de complexité sans plus de valeur.</summary>
    public static double Usability(Product product, ProductSpec spec)
    {
        double factor = 1;
        var reachable = spec.ReachablePages();
        foreach (var page in spec.Pages.Where(p => reachable.Contains(p.Id)))
        {
            int extra = page.Modules.Count(m => m.Visible) - Tuning.Products.MaxModulesPerPageBeforeClutter;
            if (extra > 0)
                factor *= 1 - 0.05 * extra;
        }
        foreach (var module in spec.ActiveModules())
            if (!IsRelevant(product, module.Def))
                factor *= 0.97;
        return Math.Max(0.4, factor);
    }

    public static bool IsRelevant(Product product, ModuleDef def) =>
        def.Needs.Any(n => product.Archetype.Needs.TryGetValue(n.Key, out double w) && w > 0);

    public static List<string> MissingRequired(Product product, ProductSpec spec)
    {
        var active = spec.ActiveModules().Select(m => m.DefId).ToHashSet();
        return product.Archetype.RequiredModules.Where(r => !active.Contains(r)).ToList();
    }

    public static List<DesignWarning> Warnings(Product product, ProductSpec spec)
    {
        var warnings = new List<DesignWarning>();
        var reachable = spec.ReachablePages();
        if (spec.Page(spec.HomePageId) is not { Visible: true })
            warnings.Add(new("La page d'accueil est masquée ou absente : personne ne peut entrer.", true));

        foreach (var missing in MissingRequired(product, spec))
            warnings.Add(new($"Module indispensable absent : {ProductCatalog.Module(missing).Name}. {product.Archetype.RequirementText}", false));

        bool hasLogin = spec.ActiveModules().Any(m => m.DefId == "login");
        foreach (var module in spec.ActiveModules().Where(m => m.Def.Requires == "login" && !hasLogin).DistinctBy(m => m.DefId))
            warnings.Add(new($"{module.Def.Name} demande un module Connexion : il ne sert à rien sans.", false));

        foreach (var page in spec.Pages.Where(p => p.Visible && !reachable.Contains(p.Id)))
            warnings.Add(new($"La page « {page.Name} » n'est liée à aucun menu : ses modules sont invisibles pour les utilisateurs.", false));

        foreach (var page in spec.Pages.Where(p => reachable.Contains(p.Id)))
        {
            int count = page.Modules.Count(m => m.Visible);
            if (count > Tuning.Products.MaxModulesPerPageBeforeClutter)
                warnings.Add(new($"« {page.Name} » est surchargée ({count} modules) : elle devient moins claire.", false));
        }

        foreach (var def in spec.ActiveModules().Select(m => m.Def).Where(d => !IsRelevant(product, d)).Distinct())
            warnings.Add(new($"{def.Name} n'intéresse pas ce public : coûts et bugs sans valeur ajoutée.", false));

        if (product.Model is RevenueModel.Subscription or RevenueModel.Freemium && !spec.ActiveModules().Any(m => m.DefId == "subscribe"))
            warnings.Add(new("Sans module Abonnement, ce modèle de revenus ne peut rien encaisser.", false));

        return warnings;
    }

    /// <summary>Le travail pour passer de la version publiée au brouillon, module par module.</summary>
    public static List<PlannedWork> PlanWork(Product product, IReadOnlyDictionary<string, int> reuseLevels)
    {
        var plan = new List<PlannedWork>();
        var live = product.Live;
        var draft = product.Draft;
        var liveModules = live?.AllModules.ToDictionary(m => m.Id) ?? new Dictionary<int, ModuleInstance>();

        foreach (var page in draft.Pages)
        {
            var livePage = live?.Page(page.Id);
            if (livePage == null)
                plan.Add(new($"Maquette de la page « {page.Name} »", Discipline.Design, 3, null, page.Name));
            else if (livePage.Layout != page.Layout || livePage.Theme != page.Theme || livePage.Accent != page.Accent
                     || livePage.Title != page.Title || livePage.Subtitle != page.Subtitle || livePage.Name != page.Name)
                plan.Add(new($"Retouches visuelles « {page.Name} »", Discipline.Design, 1, null, page.Name));
            if (livePage == null || !livePage.Links.OrderBy(i => i).SequenceEqual(page.Links.OrderBy(i => i)) || livePage.Visible != page.Visible)
                plan.Add(new($"Navigation « {page.Name} »", Discipline.Code, 1, null, page.Name));

            foreach (var module in page.Modules)
            {
                double reuse = ReuseFactor(module.DefId, reuseLevels);
                if (!liveModules.TryGetValue(module.Id, out var old))
                {
                    AddModule(plan, module, 1, reuse);
                }
                else if (!OptionsEqual(old, module))
                {
                    AddModule(plan, module, 0.35, reuse);
                }
                else if (old.Visible != module.Visible || old.Label != module.Label)
                {
                    plan.Add(new($"{module.Def.Name} : affichage", Discipline.Code, 0.5, module.Id, page.Name));
                }
            }
        }

        var draftIds = draft.AllModules.Select(m => m.Id).ToHashSet();
        foreach (var removed in liveModules.Values.Where(m => !draftIds.Contains(m.Id)))
            plan.Add(new($"Retrait de {removed.Def.Name}", Discipline.Code, 2, null, "Nettoyage"));

        return plan;
    }

    private static void AddModule(List<PlannedWork> plan, ModuleInstance module, double share, double reuse)
    {
        string verb = share >= 1 ? "" : " (modification)";
        foreach (var discipline in new[] { Discipline.Design, Discipline.Code, Discipline.Test })
        {
            double effort = Effort(module, discipline) * share * (discipline == Discipline.Code ? reuse : 1);
            if (effort >= 0.25)
                plan.Add(new($"{module.Def.Name}{verb}", discipline, Math.Round(effort, 1), module.Id, module.Def.Name));
        }
    }

    private static double ReuseFactor(string moduleId, IReadOnlyDictionary<string, int> reuseLevels)
    {
        int best = 0;
        foreach (var (kind, modules) in ReuseLinks)
            if (modules.Contains(moduleId) && reuseLevels.TryGetValue(kind, out int level))
                best = Math.Max(best, level);
        return 1 - best * Tuning.Contracts.ReuseGainPerLevel;
    }

    private static bool OptionsEqual(ModuleInstance a, ModuleInstance b) =>
        a.Options.Count == b.Options.Count && a.Options.All(o => b.Options.TryGetValue(o.Key, out var v) && v == o.Value);

    /// <summary>Bugs attendus d'une version : code écrit × complexité × (1 − qualité), réduits par les tests.</summary>
    public static double ExpectedBugs(IEnumerable<PlannedWork> plan, double quality)
    {
        var list = plan.ToList();
        double code = list.Where(w => w.Discipline == Discipline.Code && w.ModuleId != null).Sum(w => w.Effort);
        double test = list.Where(w => w.Discipline == Discipline.Test).Sum(w => w.Effort);
        double raw = code * 0.12 * (1.2 - quality);
        double caught = Math.Min(0.85, test / Math.Max(1, code * 0.35) * 0.7 * quality / 0.6);
        return raw * (1 - caught);
    }

    /// <summary>Nouvelle page vide avec un titre par défaut.</summary>
    public static ProductPage NewPage(Product product, string name) => new()
    {
        Id = product.NewLocalId(),
        Name = name,
        Title = name,
    };

    public static ModuleInstance NewModule(Product product, string defId)
    {
        var def = ProductCatalog.Module(defId);
        var module = new ModuleInstance { Id = product.NewLocalId(), DefId = defId, Label = def.Name };
        foreach (var option in def.Options)
            module.Options[option.Key] = option.Choices[Math.Min(1, option.Choices.Length - 1)].Value;
        return module;
    }

    /// <summary>Squelette de départ : une page d'accueil avec une présentation.</summary>
    public static ProductSpec Starter(Product product)
    {
        var home = NewPage(product, "Accueil");
        home.Title = product.Name;
        home.Subtitle = product.Archetype.Name;
        home.Modules.Add(NewModule(product, "landing"));
        return new ProductSpec { HomePageId = home.Id, Pages = [home] };
    }
}
