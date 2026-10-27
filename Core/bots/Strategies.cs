using Core.products;
using Core.work;

namespace Core.bots;

/// <summary>Les stratégies contrastées mesurées par la campagne d'équilibrage (BALANCE_REPORT.md).</summary>
public static class Strategies
{
    public static readonly IReadOnlyList<Strategy> All =
    [
        new("freelance", "Freelance prudent : seulement des prestations, délai sûr, personne d'embauché.")
        {
            Deadline = DeadlineOption.Safe, MaxStaff = 0,
        },
        new("agence", "Agence : prestations en continu, code capitalisé, embauches dès que la trésorerie le permet.")
        {
            Capitalize = true, MaxStaff = 5, HireAbove = 25_000, MaxContracts = 1, UpgradeComputers = true,
        },
        new("produit_tot", "Produit lancé tôt : un SaaS minimal dès la 2e semaine, le fondateur dessus, quelques prestations pour survivre.")
        {
            ProductDay = 10, FounderOnContracts = false, MaxStaff = 2, HireAbove = 30_000, MarketingBudget = 300,
            Modules = ["login", "dashboard", "subscribe"],
        },
        new("produit_soigne", "Produit très soigné : prestations d'abord, puis un SaaS complet avec support et correctifs.")
        {
            ProductDay = 60, MaxStaff = 3, HireAbove = 30_000, MarketingBudget = 600,
            Modules = ["landing", "login", "dashboard", "subscribe", "support", "export"],
        },
        new("croissance", "Croissance financée : embauches rapides et marketing agressif sur un produit communautaire.")
        {
            ProductDay = 20, MaxStaff = 6, HireAbove = 12_000, MarketingBudget = 3_000, Segment = "creators", Model = RevenueModel.Ads, CarefulHiring = false,
            Modules = ["login", "feed", "profile", "search"], LaterModules = ["messaging", "notifications", "reviews"],
        },
        new("embauche_rapide", "Embauche rapide sans produit : beaucoup de salaires, des prestations pour les payer.")
        {
            MaxStaff = 6, HireAbove = 6_000, MaxContracts = 1, Deadline = DeadlineOption.Rush, CarefulHiring = false, Borrow = false,
        },
        new("marketing", "Marketing agressif sur une place de marché, sans gros soin du produit.")
        {
            ProductDay = 15, MaxStaff = 3, HireAbove = 20_000, MarketingBudget = 8_000, Segment = "crafts", Model = RevenueModel.Commission, CarefulHiring = false,
            Modules = ["login", "listings", "checkout", "catalog"], FixBugs = false,
        },
        new("sous_qualite", "Sous-investissement dans la qualité : SaaS sans tests de support ni correctifs, promesses rapides.")
        {
            ProductDay = 20, MaxStaff = 2, HireAbove = 25_000, MarketingBudget = 1_000, FixBugs = false, KeepSupport = false, CarefulHiring = false, Borrow = false,
            Deadline = DeadlineOption.Rush, Modules = ["login", "dashboard", "subscribe", "search", "messaging", "feed", "stats"],
        },
        new("mixte", "Stratégie mixte : prestations pour financer, SaaS PME bien construit, croissance mesurée.")
        {
            ProductDay = 40, MaxStaff = 4, HireAbove = 30_000, MarketingBudget = 1_000, Segment = "smb", Capitalize = true, UpgradeComputers = true,
            Modules = ["landing", "login", "dashboard", "subscribe", "support"],
        },
    ];
}
