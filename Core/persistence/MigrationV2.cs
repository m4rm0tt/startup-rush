using System.Text.Json.Nodes;
using Core.campaign;
using Core.space;
using Core.team;

namespace Core.persistence;

/// <summary>
/// Passage d'une sauvegarde au format 2 (tycoon, local en grille vue de dessus) au format 3 (plans de pièces du pack, vue
/// isométrique, fondateur au terminal). Rien n'est perdu ni inventé en argent : le local devient le plan correspondant,
/// chaque meuble devient son équivalent du pack et part dans les cartons au prix payé, puis chacun reçoit un poste complet
/// avec ce qu'il y a dans les cartons (sans aucun achat). Les fondateurs perdent seulement leur production passive.
/// </summary>
public static class MigrationV2
{
    /// <summary>Un ancien meuble : ce qu'il devient dans le pack, avec le prix payé réparti entre les pièces.</summary>
    private static readonly Dictionary<string, (string Id, decimal Paid)[]> Map = new()
    {
        ["desk"] = [("desk_simple", 180m)],
        ["desk_pro"] = [("desk_tech", 460m)],
        ["chair"] = [("chair_budget", 60m)],
        ["chair_ergo"] = [("chair_ergonomic", 390m)],
        ["laptop"] = [("laptop_old", 0m)],
        ["pc"] = [("tower_office", 1_060m), ("monitor_basic", 150m), ("keyboard_mouse", 40m)],
        ["workstation"] = [("workstation_dev", 2_910m), ("monitor_basic", 150m), ("keyboard_mouse", 40m)],
        ["plant"] = [("plant_floor_medium", 45m)],
        ["coffee"] = [("coffee_machine", 650m)],
        ["sofa"] = [("sofa", 900m)],
        ["whiteboard"] = [("whiteboard_mobile", 220m)],
        ["bookshelf"] = [("bookcase", 320m)],
        ["rug"] = [("rug_small", 120m)],
    };

    /// <summary>Ce qu'il faut refaire après la lecture : le local, les cartons, les postes.</summary>
    public record Pending(string ListingId, List<(string Id, decimal Paid)> Items, int Unknown);

    /// <summary>Transforme le JSON d'une campagne au format 2 (le local et les apparences) ; le reste se lit tel quel.</summary>
    public static Pending FromV2(JsonObject campaign)
    {
        var items = new List<(string, decimal)>();
        int unknown = 0;
        void Take(JsonNode? node)
        {
            string defId = node?["DefId"]?.GetValue<string>() ?? "";
            if (Map.TryGetValue(defId, out var parts))
                items.AddRange(parts);
            else if (defId is not ("bed" or "kitchen" or "fridge" or "home_sofa" or "dining" or "kitchenette"))
                unknown++;
        }

        string listing = campaign["Premises"]?["ListingId"]?.GetValue<string>() ?? RealEstate.Apartment;
        foreach (var item in campaign["Premises"]?["Items"]?.AsArray() ?? [])
            Take(item);
        foreach (var item in campaign["Storage"]?.AsArray() ?? [])
            Take(item);
        campaign.Remove("Premises");
        campaign.Remove("Storage");

        // Apparences : l'ancien personnage (Look 0 à 4) devient une base du pack ; le fondateur, l'avatar joueur
        foreach (var person in campaign["People"]?.AsArray() ?? [])
        {
            if (person is not JsonObject p)
                continue;
            int look = p["Look"]?.GetValue<int>() ?? 0;
            int id = p["Id"]?.GetValue<int>() ?? 0;
            bool founder = p["IsFounder"]?.GetValue<bool>() ?? false;
            p.Remove("Look");
            p["Appearance"] = new JsonObject
            {
                ["Base"] = founder ? "player_m" : $"employee_{(id % 2 == 0 ? 'f' : 'm')}_0{look % 5 + 1}",
                ["Hair"] = Appearance.Hairs[(id + look) % Appearance.Hairs.Length],
                ["HairRamp"] = Appearance.HairRamps[id % 4],
                ["Skin"] = Appearance.Skins[(id / 2) % Appearance.Skins.Length],
            };
        }
        return new Pending(RealEstate.Listings.Any(l => l.Id == listing) ? listing : RealEstate.Apartment, items, unknown);
    }

    /// <summary>Après lecture : le local du plan correspondant, les meubles dans les cartons, puis un poste pour chacun.</summary>
    public static string Finish(Campaign campaign, Pending pending)
    {
        campaign.Premises = RealEstate.Create(RealEstate.Get(pending.ListingId), campaign.NewId);
        campaign.Storage = pending.Items.Select(i => new StoredItem { Id = campaign.NewId(), DefId = i.Id, PaidPrice = i.Paid }).ToList();
        campaign.InvalidateLayout();
        campaign.SeatEveryone(buy: false);

        int seated = campaign.People.Count(p => campaign.Premises.SeatOf(p.Id) is { } post && Layout.Check(campaign.Premises, post) == WorkstationIssue.None);
        var notes = new List<string>
        {
            $"Nouveaux locaux en vue isométrique : {campaign.Premises.Name}. Tes meubles ont leurs équivalents du pack, au prix payé ; "
            + $"{seated} personne(s) sur {campaign.People.Count} ont un poste complet, {campaign.Storage.Count} objet(s) restent dans les cartons.",
            "Le fondateur ne développe plus tout seul : ouvre le terminal sur un projet pour y travailler.",
        };
        if (seated < campaign.People.Count)
            notes.Add("Installe les postes qui manquent dans Aménagement.");
        if (pending.Unknown > 0)
            notes.Add($"{pending.Unknown} objet(s) inconnu(s) n'ont pas pu être repris.");
        return string.Join(" ", notes);
    }
}
