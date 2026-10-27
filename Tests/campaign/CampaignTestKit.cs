using Core.campaign;
using Core.space;
using Core.work;

namespace Tests.campaign;

/// <summary>Outils communs aux tests : campagnes reproductibles et petits raccourcis.</summary>
public static class CampaignTestKit
{
    public static Campaign NewCampaign(ulong seed = 42, FounderProfile profile = FounderProfile.Developer) =>
        Campaign.New("Test", "Alex", "Studio Test", profile, seed);

    /// <summary>Une demande simple, déterministe, ajoutée à la campagne.</summary>
    public static Contract AddOffer(Campaign campaign, double design = 6, double code = 10, double test = 4, decimal price = 2_000m, int workdays = 10, double reliability = 1)
    {
        var contract = new Contract
        {
            Id = campaign.NewId(),
            CompanyId = campaign.MainCompany.Id,
            Client = "Client Test",
            KindId = "vitrine",
            Title = "Site vitrine",
            Scope = new() { [Core.team.Discipline.Design] = design, [Core.team.Discipline.Code] = code, [Core.team.Discipline.Test] = test },
            ExpectedQuality = 0.5,
            Price = price,
            DurationWorkdays = workdays,
            Reliability = reliability,
            OfferExpiresAt = campaign.Minute + 10L * GameTime.MinutesPerDay,
            Status = ContractStatus.Offered,
        };
        campaign.Contracts.Add(contract);
        return contract;
    }

    /// <summary>Installe un poste complet (bureau, chaise, PC) dans le local actuel, à l'emplacement libre donné.</summary>
    public static PlacedItem AddWorkstation(Campaign campaign, int x, int y, string computer = "pc")
    {
        var desk = new PlacedItem { Id = campaign.NewId(), DefId = "desk", X = x, Y = y };
        campaign.Premises.Items.Add(desk);
        campaign.Premises.Items.Add(new PlacedItem { Id = campaign.NewId(), DefId = "chair", X = x, Y = y - 1 });
        campaign.Premises.Items.Add(new PlacedItem { Id = campaign.NewId(), DefId = computer, X = x, Y = y, HostId = desk.Id });
        campaign.InvalidateLayout();
        return desk;
    }

    /// <summary>Avance jusqu'au prochain lundi 8 h (les tests de production partent d'un jour ouvré).</summary>
    public static void ToNextMonday(Campaign campaign)
    {
        do
            campaign.Advance(GameTime.MinutesPerDay);
        while (GameTime.ToDate(campaign.Minute).DayOfWeek != DayOfWeek.Monday);
        campaign.Advance(8 * 60 - GameTime.MinuteOfDay(campaign.Minute));
    }
}
