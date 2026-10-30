using System.Collections.Generic;
using Avalonia.Media.Imaging;
using UI.Iso;

namespace UI.Hud;

/// <summary>
/// Les composants de projet du jeu → leurs cartes du pack (« comp_* » : aperçu abstrait 48×32 et pictogramme). Les composants
/// sans équivalent dans le pack (graphismes, sons, matériel…) gardent leur icône du jeu.
/// </summary>
public static class ComponentArt
{
    private static readonly Dictionary<string, string> PackIds = new()
    {
        ["ui"] = "comp_home", ["performance"] = "comp_infrastructure", ["support"] = "comp_support", ["login"] = "comp_auth",
        ["notifications"] = "comp_notifications", ["cloudsave"] = "comp_savegame", ["dashboard"] = "comp_admin", ["subscribe"] = "comp_subscription",
        ["search"] = "comp_search", ["messaging"] = "comp_messaging", ["export"] = "comp_integration", ["stats"] = "comp_analytics",
        ["gameplay"] = "comp_engine", ["content"] = "comp_progression", ["leaderboard"] = "comp_leaderboard", ["multiplayer"] = "comp_netplay",
        ["feed"] = "comp_feed", ["profile"] = "comp_profile", ["moderation"] = "comp_security", ["listings"] = "comp_inventory",
        ["catalog"] = "comp_catalog", ["checkout"] = "comp_payment", ["reviews"] = "comp_reviews", ["ai_core"] = "comp_processing",
        ["ai_feature"] = "comp_automation", ["api"] = "comp_api", ["storage"] = "comp_storage", ["game_menu"] = "comp_game_menu",
        ["inventory"] = "comp_inventory",
    };

    private static readonly Dictionary<string, Bitmap> Previews = new();

    /// <summary>L'identifiant de la carte du pack pour ce composant, s'il en a une.</summary>
    public static string? PackId(string componentId) => PackIds.GetValueOrDefault(componentId);

    /// <summary>L'aperçu abstrait du composant (maquette sans texte), ou null.</summary>
    public static Bitmap? Preview(string componentId)
    {
        if (PackId(componentId) is not { } id)
            return null;
        if (!Previews.TryGetValue(id, out var bitmap))
            Previews[id] = bitmap = PackLibrary.Png(PackLibrary.Asset(id).GetProperty("preview").GetProperty("file").GetString()!).ToBitmap();
        return bitmap;
    }

    /// <summary>Le pictogramme du composant : celui de sa carte du pack, sinon l'icône du jeu.</summary>
    public static string Icon(string componentId, string fallback) =>
        PackId(componentId) is { } id ? PackLibrary.Asset(id).GetProperty("icon").GetProperty("id").GetString()! : fallback;
}
