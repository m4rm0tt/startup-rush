using Core.space;

namespace Core.campaign;

/// <summary>Le studio d'un site : vidéo (caméra, fond vert, lumière) et audio (micro, enceintes, acoustique), et ce qui manque.</summary>
public record StudioSummary(double Video, double Audio, double Score, bool Staffed, IReadOnlyList<string> Missing)
{
    /// <summary>Bonus de qualité du contenu pour un studio complet ; le marketing en gagne la moitié ⚙️.</summary>
    public const double MaxBonus = 0.3;

    public double Bonus => MaxBonus * Score;
}

/// <summary>La zone de test XR d'un site : un casque seul sert au prototype ; avec balises, tapis et quelqu'un sur place, la zone est complète.</summary>
public record XrSummary(bool Headset, int Stations, bool Mat, bool Staffed, double Capacity, IReadOnlyList<string> Missing)
{
    /// <summary>Part des défauts d'un produit XR que la zone complète fait disparaître à chaque amélioration ⚙️.</summary>
    public const double MaxDefectCut = 0.6;

    public double DefectCut => MaxDefectCut * Capacity;
}

/// <summary>
/// Une installation quantique d'un site : les cryostats fermés pris en charge (avec leur baie de contrôle et leur support
/// cryogénique) passent une mise en route avant de fournir du calcul. Un mini-jeu de signaux peut la raccourcir.
/// </summary>
public class QuantumSite
{
    public int SiteId { get; set; }

    /// <summary>Installations stabilisées en service.</summary>
    public int Commissioned { get; set; }

    /// <summary>Jours restants de la mise en route de la prochaine installation (0 : aucune en cours).</summary>
    public int WarmupLeft { get; set; }

    /// <summary>Le mini-jeu a déjà raccourci cette mise en route.</summary>
    public bool Stabilized { get; set; }
}

/// <summary>
/// Studio audiovisuel, zone XR, laboratoire avancé et quantique : des installations qui servent vraiment. Le studio améliore
/// le contenu et la portée du marketing (plafonnés), la zone XR réduit les défauts des produits XR, les installations quantiques
/// vendent du calcul spécialisé et accélèrent un peu la recherche de calcul intensif. Rien n'accélère « tout ».
/// </summary>
public partial class Campaign
{
    public List<QuantumSite> QuantumSites { get; set; } = new();

    /// <summary>Jours de mise en route d'une installation quantique stabilisée ⚙️.</summary>
    public const int QuantumWarmupDays = 14;

    private static int Count(Premises site, string defId) => site.Items.Count(i => i.DefId == defId);

    private bool Staffed(Premises site) => People.Any(p => SiteOf(p) == site);

    public StudioSummary StudioOf(Premises site)
    {
        bool camera = Count(site, "camera_tripod") > 0, green = Count(site, "greenscreen_backdrop") > 0;
        int lights = Math.Min(2, Count(site, "softbox_light"));
        bool mic = Count(site, "mic_stand") > 0, monitors = Count(site, "studio_monitors") > 0;
        bool acoustic = Count(site, "partition_acoustic") > 0;
        double video = (camera ? 0.35 : 0) + (green ? 0.25 : 0) + 0.2 * lights;
        double audio = (mic ? 0.5 : 0) + (monitors ? 0.3 : 0) + (acoustic ? 0.2 : 0);
        bool staffed = Staffed(site);
        var missing = new List<string>();
        if (!camera) missing.Add("caméra");
        if (!green) missing.Add("fond vert");
        if (lights < 2) missing.Add(lights == 0 ? "éclairage" : "un second éclairage");
        if (!mic) missing.Add("micro");
        if (!monitors) missing.Add("enceintes");
        if (!acoustic) missing.Add("séparation acoustique");
        double score = (0.6 * video + 0.4 * audio) * (staffed ? 1 : 0.5);
        return new StudioSummary(video, audio, score, staffed, missing);
    }

    /// <summary>Le meilleur studio de l'entreprise (les studios de plusieurs sites ne s'additionnent pas).</summary>
    public StudioSummary BestStudio() => Sites.Select(StudioOf).OrderByDescending(s => s.Score).FirstOrDefault() ?? StudioOf(Headquarters);

    public XrSummary XrOf(Premises site)
    {
        bool headset = Count(site, "xr_headset_stand") > 0, mat = Count(site, "xr_play_mat") > 0;
        int stations = Math.Min(2, Count(site, "xr_base_station"));
        bool staffed = Staffed(site);
        var missing = new List<string>();
        if (!headset) missing.Add("casque XR");
        if (stations < 2) missing.Add(stations == 0 ? "balises de suivi" : "une seconde balise");
        if (!mat) missing.Add("tapis de zone");
        if (!staffed) missing.Add("quelqu'un sur place");
        double capacity = ((headset ? 0.4 : 0) + 0.15 * stations + (mat ? 0.3 : 0)) * (staffed ? 1 : 0.5);
        return new XrSummary(headset, stations, mat, staffed, capacity, missing);
    }

    public XrSummary BestXr() => Sites.Select(XrOf).OrderByDescending(x => x.Capacity).FirstOrDefault() ?? XrOf(Headquarters);

    /// <summary>Installations quantiques stabilisées en service, tous sites réunis.</summary>
    public int QuantumUnitsOnline => QuantumSites.Sum(q => q.Commissioned);

    public int QuantumOnline(Premises site) =>
        Math.Min(QuantumSites.FirstOrDefault(q => q.SiteId == site.Id)?.Commissioned ?? 0, SiteInfra(site).QuantumUnits);

    /// <summary>Chaque nuit : le studio du jour fixe le bonus des produits.</summary>
    private void RunStudio()
    {
        double bonus = BestStudio().Bonus;
        foreach (var product in Products)
            product.StudioBonus = Math.Round(bonus, 3);
    }

    /// <summary>
    /// Chaque nuit : les cryostats fermés pris en charge démarrent l'un après l'autre (14 jours), si le froid et l'énergie du site
    /// suivent ; une installation retirée sort du compte.
    /// </summary>
    private void RunQuantum()
    {
        foreach (var site in Sites)
        {
            var infra = SiteInfra(site);
            var state = QuantumSites.FirstOrDefault(q => q.SiteId == site.Id);
            if (infra.QuantumUnits == 0)
            {
                if (state != null)
                    QuantumSites.Remove(state);
                continue;
            }
            if (state == null)
                QuantumSites.Add(state = new QuantumSite { SiteId = site.Id });
            state.Commissioned = Math.Min(state.Commissioned, infra.QuantumUnits);
            if (state.Commissioned >= infra.QuantumUnits)
            {
                state.WarmupLeft = 0;
                continue;
            }
            if (infra.Factor < 0.99)
            {
                Notify(Severity.Warning, "labo", $"{site.Name} : la mise en route quantique est à l'arrêt",
                    "Le froid ou l'énergie du site ne suffisent pas : ajoute un climatiseur ou une armoire électrique.", "sites", key: $"quantum:{site.Id}");
                continue;
            }
            Resolve($"quantum:{site.Id}");
            if (state.WarmupLeft <= 0)
            {
                state.WarmupLeft = QuantumWarmupDays;
                state.Stabilized = false;
                Notify(Severity.Info, "labo", $"{site.Name} : mise en route d'une installation quantique",
                    $"{QuantumWarmupDays} jours. Un mini-jeu de signaux peut la raccourcir de moitié au mieux.", "lab");
            }
            else if (--state.WarmupLeft <= 0)
            {
                state.Commissioned++;
                Notify(Severity.Good, "labo", $"{site.Name} : installation quantique en service",
                    $"{Core.space.Infrastructure.QuantumPerUnit:0} unités de calcul quantique à vendre ou à utiliser.", "lab");
                Award("quantum:first", 3, "Première installation quantique en service.");
            }
        }
    }

    /// <summary>
    /// Mini-jeu de mise en route : quatre signaux à ramener sur le motif cible ; chaque « + » sur un signal retire 1 au suivant,
    /// donc le total est conservé. Le motif est toujours atteignable. Tiré d'un hasard séparé.
    /// </summary>
    public LaunchChallenge QuantumChallenge(int siteId)
    {
        var random = new SimRandom(Seed ^ ((ulong)siteId * 0xC2B2AE3D27D4EB4FUL) ^ ((ulong)(QuantumSites.FirstOrDefault(q => q.SiteId == siteId)?.Commissioned ?? 0) * 0x165667B19E3779F9UL) ^ 0x51);
        var start = Enumerable.Range(0, 4).Select(_ => random.Next(2, 8)).ToList();
        var target = start.ToList();
        int moves = 0;
        for (int guard = 0; guard < 200 && moves < 6; guard++)
        {
            int i = random.Next(0, 4), next = (i + 1) % 4;
            if (target[i] >= 9 || target[next] <= 0)
                continue;
            target[i]++;
            target[next]--;
            moves++;
        }
        string[] channels = ["Signal A", "Signal B", "Signal C", "Signal D"];
        return new LaunchChallenge("signals", "Ramène chaque signal sur sa valeur cible. Un « + » ajoute 1 au signal et retire 1 au suivant : le total reste le même.", channels, target)
        {
            Start = start,
        };
    }

    /// <summary>Applique le score du mini-jeu : la mise en route restante raccourcit jusqu'à moitié, une seule fois par mise en route.</summary>
    public CommandResult StabilizeQuantum(int siteId, double score)
    {
        var state = QuantumSites.FirstOrDefault(q => q.SiteId == siteId);
        if (state == null || state.WarmupLeft <= 0)
            return CommandResult.Fail("Aucune mise en route en cours sur ce site.");
        if (state.Stabilized)
            return CommandResult.Fail("Les signaux de cette mise en route sont déjà stabilisés.");
        int before = state.WarmupLeft;
        state.WarmupLeft = Math.Max(1, (int)Math.Ceiling(before * (1 - 0.5 * Math.Clamp(score, 0, 1))));
        state.Stabilized = true;
        return CommandResult.Success($"Signaux stabilisés : {before - state.WarmupLeft} jour(s) gagné(s), il en reste {state.WarmupLeft}.");
    }
}
