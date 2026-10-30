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
}
