namespace Core.campaign;

/// <summary>Un succès Steam : son nom d'API (stable, saisi tel quel dans Steamworks) et la condition qui le rend gagné.</summary>
public record AchievementDef(string ApiName, Func<Campaign, bool> Earned);

public partial class Campaign
{
    /// <summary>
    /// Les succès préparés pour Steam. Chacun est **déduit de l'état de la partie** (aucune donnée de plus dans la sauvegarde) :
    /// l'envoi à Steam sera donc sans risque à répéter (SetAchievement est idempotent) et une ancienne sauvegarde rattrape ses
    /// succès au premier chargement. Rien n'est envoyé à Steam aujourd'hui (pas de compte, d'App ID ni de SDK : voir
    /// docs/DISTRIBUTION.md) ; les titres et descriptions en français et en anglais sont dans docs/steam/achievements.json.
    /// </summary>
    public static readonly IReadOnlyList<AchievementDef> AchievementDefs =
    [
        new("ACH_FIRST_LAUNCH", c => c.Milestones.ContainsKey("first_launch")),
        new("ACH_FIRST_HIRE", c => c.Milestones.ContainsKey("first_hire")),
        new("ACH_REAL_OFFICE", c => c.Milestones.ContainsKey("office")),
        new("ACH_USERS_1K", c => c.Milestones.ContainsKey("users1k")),
        new("ACH_MRR_10K", c => c.Milestones.ContainsKey("mrr10k")),
        new("ACH_PORTFOLIO_3", c => c.Milestones.ContainsKey("portfolio3")),
        new("ACH_TEAM_10", c => c.Milestones.ContainsKey("team10")),
        new("ACH_USERS_50K", c => c.Milestones.ContainsKey("users50k")),
        new("ACH_AI_PUBLISHED", c => c.PublishedModels.Any()),
        new("ACH_HARDWARE_ONLINE", c => c.Products.Any(p => p.IsHardware && p.IsLive)),
        new("ACH_QUANTUM_ONLINE", c => c.QuantumUnitsOnline > 0),
        new("ACH_COMEBACK", c => c.LowestCash is < 1_000m && c.Ledger.Cash >= 50_000m && c.Status == CampaignStatus.Running),
        new("ACH_FINAL_GOAL", c => c.FinalGoalReachedAt != null),
    ];

    /// <summary>Les succès gagnés par cette partie à cet instant.</summary>
    public IEnumerable<string> EarnedAchievements => AchievementDefs.Where(a => a.Earned(this)).Select(a => a.ApiName);
}
