using Core.finance;
using Core.space;

namespace Core.campaign;

/// <summary>
/// Finitions du site affiché : refaire tout le sol, ou habiller les deux murs du fond. Les travaux se paient d'un coup (par
/// case ou par segment de mur), restent au local et sont sauvegardés avec lui ; un déménagement repart du plan du nouveau local.
/// </summary>
public partial class Campaign
{
    public CommandResult SetFinish(string finishId)
    {
        if (!Finishes.Has(finishId))
            return CommandResult.Fail("Finition inconnue.");
        var finish = Finishes.Get(finishId);
        if (finish.UnlockNode is { } node && !IsUnlocked(node))
            return CommandResult.Fail($"À débloquer : {UnlockName(node)}.");
        if (Finishes.IsCurrent(Premises, finish))
            return CommandResult.Fail($"Le local a déjà : {finish.Name}.");
        var paid = Spend(Finishes.Cost(Premises, finish), Category.Equipment, $"Travaux : {finish.Name}", Premises.Id);
        if (!paid.Ok)
            return paid;
        if (finish.Kind == FinishKind.Floor)
        {
            // Tout le sol est refait : les sols posés avec des modules disparaissent sous le nouveau
            Premises.FloorMaterial = finish.Id;
            Premises.FloorPatches.Clear();
        }
        else
            Premises.WallPattern = finish.Id;
        InvalidateLayout();
        return CommandResult.Success(finish.Kind == FinishKind.Floor ? $"Nouveau sol : {finish.Name}." : $"Nouveaux murs : {finish.Name}.");
    }

    // ------------------------------------------------------------------ Modules de pièce

    /// <summary>Ce que coûte un module : chaque objet et accessoire au prix du catalogue, plus son sol par case.</summary>
    public static decimal ModuleCost(RoomModule module) =>
        module.Objects.Sum(o => FurnitureCatalog.Get(o.Asset).Price + o.Children.Sum(c => FurnitureCatalog.Get(c.Asset).Price))
        + module.Floors.Sum(f => Finishes.FloorPrice(f.Material) * f.Width * f.Depth);

    /// <summary>Ce qui manque pour poser un module (objet à débloquer), ou null.</summary>
    public string? ModuleLock(RoomModule module) =>
        module.Objects.Select(o => FurnitureCatalog.Get(o.Asset)).Concat(module.Objects.SelectMany(o => o.Children).Select(c => FurnitureCatalog.Get(c.Asset)))
            .FirstOrDefault(d => d.UnlockNode is { } n && !IsUnlocked(n)) is { } locked ? $"{locked.Name} : à débloquer ({UnlockName(locked.UnlockNode!)})" : null;

    /// <summary>
    /// Pose un module de pièce sur le site affiché, à la première place où tous ses objets tiennent (contre le bon mur si le
    /// pack le demande) : chaque objet et accessoire est acheté et posé, sa zone de sol est posée. Tout ou rien.
    /// </summary>
    public CommandResult InstallModule(string moduleId)
    {
        if (Pack.FindModule(moduleId) is not { } module)
            return CommandResult.Fail("Module inconnu.");
        if (ModuleLock(module) is { } why)
            return CommandResult.Fail(why);
        decimal cost = ModuleCost(module);
        if (!Ledger.CanSpend(cost))
            return CommandResult.Fail($"Il faut {cost:N0} CHF pour ce module.");
        var site = Premises;
        // Contre le mur demandé d'abord ; si les plans meublés n'y laissent pas la place, ailleurs dans le local
        var spots = Enumerable.Range(0, Math.Max(0, site.Height - module.Depth + 1))
            .SelectMany(v => Enumerable.Range(0, Math.Max(0, site.Width - module.Width + 1)).Select(u => (U: u, V: v)))
            .OrderBy(s => module.AgainstWall == "left" && s.U == 0 || module.AgainstWall == "right" && s.V == 0 ? 0 : 1).ToList();
        foreach (var (u0, v0) in spots)
            {
                if (!ModuleFits(site, module, u0, v0))
                    continue;
                foreach (var o in module.Objects)
                {
                    var placed = BuyAndPlace(o.Asset, u0 + o.U, v0 + o.V, o.Facing, freeStanding: true);
                    if (!placed.Ok)
                        continue;
                    var host = site.Items.Last();
                    foreach (var swatch in o.Swatches)
                        host.Swatches[swatch.Key] = swatch.Value;
                    foreach (var child in o.Children)
                        BuyAndAttach(child.Asset, host.Id, child.Socket);
                }
                foreach (var floor in module.Floors)
                {
                    decimal price = Finishes.FloorPrice(floor.Material) * floor.Width * floor.Depth;
                    if (price > 0 && !Spend(price, Category.Equipment, $"Sol : {module.Name}", site.Id).Ok)
                        continue;
                    site.FloorPatches.Add(new FloorPatch
                    {
                        Material = floor.Material, U = u0 + floor.U, V = v0 + floor.V, Width = floor.Width, Depth = floor.Depth,
                        Swatches = new Dictionary<string, string>(floor.Swatches),
                    });
                }
                InvalidateLayout();
                return CommandResult.Success($"{module.Name} installé·e ({cost:N0} CHF).");
            }
        return CommandResult.Fail($"Pas de place libre de {module.Width} × {module.Depth} cases pour ce module (entrée et passages compris).");
    }

    /// <summary>Tous les objets du module tiennent-ils à cette place (posés à l'essai, puis retirés) ?</summary>
    private static bool ModuleFits(Premises site, RoomModule module, int u0, int v0)
    {
        var trial = new List<PlacedItem>();
        bool ok = true;
        foreach (var o in module.Objects)
        {
            var def = FurnitureCatalog.Get(o.Asset);
            if (!Layout.CanPlace(site, def, u0 + o.U, v0 + o.V, o.Facing, freeStanding: true).Ok)
            {
                ok = false;
                break;
            }
            var temp = new PlacedItem { Id = -2000 - trial.Count, DefId = o.Asset, X = u0 + o.U, Y = v0 + o.V, Facing = o.Facing };
            site.Items.Add(temp);
            trial.Add(temp);
        }
        foreach (var temp in trial)
            site.Items.Remove(temp);
        return ok;
    }

    /// <summary>Remise des sols ou des murs du site affiché tels que le plan les prévoit (sans frais, sans remboursement des travaux).</summary>
    public CommandResult ResetFinish(FinishKind kind)
    {
        if (kind == FinishKind.Floor ? Premises.FloorMaterial == null && Premises.FloorPatches.Count == 0 : Premises.WallPattern == null)
            return CommandResult.Fail(kind == FinishKind.Floor ? "Les sols sont déjà ceux du plan." : "Les murs sont déjà ceux du plan.");
        if (kind == FinishKind.Floor)
        {
            Premises.FloorMaterial = null;
            Premises.FloorPatches.Clear();
        }
        else
            Premises.WallPattern = null;
        InvalidateLayout();
        return CommandResult.Success(kind == FinishKind.Floor ? "Sols remis comme sur le plan." : "Murs remis comme sur le plan.");
    }
}
