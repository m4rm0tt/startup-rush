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
            Premises.FloorMaterial = finish.Id;
        else
            Premises.WallPattern = finish.Id;
        InvalidateLayout();
        return CommandResult.Success(finish.Kind == FinishKind.Floor ? $"Nouveau sol : {finish.Name}." : $"Nouveaux murs : {finish.Name}.");
    }

    /// <summary>Remise des sols ou des murs du site affiché tels que le plan les prévoit (sans frais, sans remboursement des travaux).</summary>
    public CommandResult ResetFinish(FinishKind kind)
    {
        if (kind == FinishKind.Floor ? Premises.FloorMaterial == null : Premises.WallPattern == null)
            return CommandResult.Fail(kind == FinishKind.Floor ? "Les sols sont déjà ceux du plan." : "Les murs sont déjà ceux du plan.");
        if (kind == FinishKind.Floor)
            Premises.FloorMaterial = null;
        else
            Premises.WallPattern = null;
        InvalidateLayout();
        return CommandResult.Success(kind == FinishKind.Floor ? "Sols remis comme sur le plan." : "Murs remis comme sur le plan.");
    }
}
