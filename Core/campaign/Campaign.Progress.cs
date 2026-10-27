using Core.progress;

namespace Core.campaign;

/// <summary>L'arbre de développement : dépenser ses points pour débloquer de nouvelles possibilités.</summary>
public partial class Campaign
{
    public (bool Ok, string Reason) CanUnlock(DevNode node)
    {
        if (IsUnlocked(node.Id))
            return (false, "Déjà acquis.");
        if (node.Requires != null && !IsUnlocked(node.Requires))
            return (false, $"Il faut d'abord : {DevTree.Node(node.Requires).Name}.");
        if (DevPoints < node.Cost)
            return (false, $"Il faut {node.Cost} point{(node.Cost > 1 ? "s" : "")} (tu en as {DevPoints}).");
        return (true, "");
    }

    public CommandResult Unlock(string nodeId)
    {
        var node = DevTree.Node(nodeId);
        var (ok, reason) = CanUnlock(node);
        if (!ok)
            return CommandResult.Fail(reason);
        DevPoints -= node.Cost;
        Unlocked.Add(node.Id);
        // Le recrutement élargi agit tout de suite, pas seulement lundi prochain
        if (node.Id == "hiring")
            for (int i = 0; i < 2; i++)
                Candidates.Add(team.Recruitment.Generate(Random, Minute, NewId(), Renown + 25));
        return CommandResult.Success($"{node.Name} débloqué.");
    }
}
