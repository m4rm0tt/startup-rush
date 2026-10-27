using Core.campaign;
using Core.products;
using Core.space;

namespace Tests.campaign;

/// <summary>Outils communs aux tests : campagnes reproductibles et petits raccourcis.</summary>
public static class CampaignTestKit
{
    public static Campaign NewCampaign(ulong seed = 42, FounderProfile profile = FounderProfile.Developer) =>
        Campaign.New("Test", "Alex", "Studio Test", profile, seed);

    /// <summary>Crée un projet avec ses composants de départ.</summary>
    public static Product NewProject(Campaign campaign, string segment = "freelances", string[]? starters = null)
    {
        var type = ProductCatalog.Type(ProductCatalog.Segment(segment).TypeId);
        var (result, product) = campaign.CreateProject("Projet test", segment, type.Models[0], starters: starters);
        Assert.That(result.Ok, Is.True, result.Message);
        return product!;
    }

    /// <summary>Joue jusqu'à ce que la file du projet soit vide (au plus 120 jours).</summary>
    public static void FinishQueue(Campaign campaign, Product product)
    {
        for (int i = 0; i < 120 && product.Queue.Count > 0; i++)
            campaign.AdvanceDays(1);
        Assert.That(product.Queue, Is.Empty, "la file aurait dû se vider");
    }

    /// <summary>Installe un poste complet (bureau, chaise, PC) à l'emplacement donné.</summary>
    public static PlacedItem AddWorkstation(Campaign campaign, int x, int y, string computer = "pc")
    {
        var desk = new PlacedItem { Id = campaign.NewId(), DefId = "desk", X = x, Y = y };
        campaign.Premises.Items.Add(desk);
        campaign.Premises.Items.Add(new PlacedItem { Id = campaign.NewId(), DefId = "chair", X = x, Y = y - 1 });
        campaign.Premises.Items.Add(new PlacedItem { Id = campaign.NewId(), DefId = computer, X = x, Y = y, HostId = desk.Id });
        campaign.InvalidateLayout();
        return desk;
    }
}
