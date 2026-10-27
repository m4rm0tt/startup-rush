using System.Text.Json.Nodes;

namespace Core.persistence;

/// <summary>
/// Passage d'une sauvegarde au format 3 (un seul local) au format 4 (plusieurs sites). Le local devient le premier site
/// (le siège), avec un identifiant neuf ; son adresse est reprise de son nom ; les loyers déjà facturés restent marqués
/// comme facturés pour ce site (aucun loyer payé deux fois). Rien d'autre ne change.
/// </summary>
public static class MigrationV3
{
    public static string FromV3(JsonObject campaign)
    {
        if (campaign["Premises"] is not JsonObject premises)
            return "";
        int id = campaign["NextId"]?.GetValue<int>() ?? 1;
        campaign["NextId"] = id + 1;
        campaign.Remove("Premises");
        premises["Id"] = id;
        string name = premises["Name"]?.GetValue<string>() ?? "";
        premises["Place"] = name.Contains(", ") ? name[(name.LastIndexOf(", ", StringComparison.Ordinal) + 2)..] : name;
        campaign["Sites"] = new JsonArray(premises);
        campaign["SiteId"] = id;

        if (campaign["Processed"] is JsonArray processed)
            foreach (string key in processed.Select(k => k?.GetValue<string>() ?? "").Where(k => k.StartsWith("rent:") && k.Count(c => c == ':') == 1).ToList())
                processed.Add($"{key}:{id}");
        return $"Ton local devient le premier site de l'entreprise : {name}.";
    }
}
