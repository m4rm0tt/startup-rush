using Core.campaign;
using Core.products;
using Core.space;
using Core.team;

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

    /// <summary>Le fondateur tape au plus « max » mots justes, à vitesse normale, sur ce projet. Renvoie le nombre tapé.</summary>
    public static int TypeWords(Campaign campaign, Product product, int max)
    {
        int typed = 0;
        while (typed < max && campaign.OpenTerminal(product.Id).Ok)
        {
            while (typed < max && campaign.Terminal is { Finished: false } session)
            {
                Assert.That(campaign.SubmitWord(session.Word, session.Word.Length / 4.0).Correct, Is.True);
                typed++;
            }
            campaign.CloseTerminal();
        }
        return typed;
    }

    /// <summary>
    /// Joue jusqu'à ce que la file du projet soit vide (au plus 120 jours) : chaque jour, le fondateur tape au terminal,
    /// puis la journée passe (les employés éventuels travaillent tout seuls).
    /// </summary>
    public static void FinishQueue(Campaign campaign, Product product)
    {
        for (int i = 0; i < 120 && product.Queue.Count > 0; i++)
        {
            TypeWords(campaign, product, 200);
            campaign.AdvanceDays(1);
        }
        Assert.That(product.Queue, Is.Empty, "la file aurait dû se vider");
    }

    /// <summary>Embauche le meilleur développeur disponible, présent dès aujourd'hui, et lui installe un poste complet.</summary>
    public static Person HireDeveloper(Campaign campaign)
    {
        campaign.Ledger.Cash += 10_000;
        var candidate = campaign.Candidates.OrderByDescending(c => c.Person.Skill(Discipline.Code)).First();
        candidate.NoticeWorkdays = 0;
        Assert.That(campaign.Hire(candidate.Person.Id).Ok, Is.True);
        var post = campaign.InstallWorkstation(buy: true);
        Assert.That(post, Is.Not.Null, "pas la place d'un poste");
        campaign.Seat(candidate.Person, post!);
        return candidate.Person;
    }
}
