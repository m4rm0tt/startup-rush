using Core.campaign;
using static Tests.campaign.CampaignTestKit;

namespace Tests.campaign;

/// <summary>Le menu de test : chaque raccourci fait ce qu'il annonce et marque la campagne.</summary>
public class CheatTest
{
    [Test]
    public void TestShortcutsWorkAndMarkTheCampaign()
    {
        var campaign = NewCampaign();
        Assert.That(campaign.CheatsUsed, Is.False);
        decimal cash = campaign.Ledger.Cash;
        campaign.CheatMoney(100_000);
        Assert.That(campaign.Ledger.Cash - cash, Is.EqualTo(100_000));
        Assert.That(campaign.CheatsUsed, Is.True);

        campaign.CheatUnlockAll();
        Assert.That(campaign.IsUnlocked("cleanroom") && campaign.IsUnlocked("res_chips"), Is.True);

        var product = NewProject(campaign);
        campaign.CheatFinishWork();
        Assert.That(product.Queue, Is.Empty);
        Assert.That(campaign.CanLaunch(product), Is.True);

        int people = campaign.People.Count;
        Assert.That(campaign.CheatHire().Ok, Is.True);
        Assert.That(campaign.People.Count, Is.EqualTo(people + 1));
        Assert.That(campaign.PostOf(campaign.Staff.Last()).Post, Is.Not.Null, "installé à un poste");

        campaign.CheatStock();
        Assert.That(campaign.StockAt(campaign.Premises.Id, "kit"), Is.EqualTo(100));

        long day = campaign.Day;
        campaign.CheatAdvance(7);
        Assert.That(campaign.Day, Is.EqualTo(day + 7));

        campaign.CheatClearDebts();
        Assert.That(campaign.Ledger.Bills.All(b => !b.Open), Is.True);
        Assert.That(Core.persistence.SaveStore.Deserialize(Core.persistence.SaveStore.Serialize(campaign)).CheatsUsed, Is.True, "la marque est sauvegardée");
    }
}
