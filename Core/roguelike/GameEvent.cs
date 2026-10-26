using Core.progression;
using Core.simulation;
using Core.staff;

namespace Core.roguelike;

/// <param name="Hint">Ce que le joueur sait avant de choisir.</param>
/// <param name="Resolve">Applique le choix et renvoie le texte du résultat.</param>
public record EventOption(
    string Label,
    string Hint,
    Func<GameSession, Random, string> Resolve,
    Func<GameSession, bool>? IsAvailable = null);

/// <param name="CanHappen">L'événement n'est tiré que si la situation s'y prête.</param>
public record GameEventInfo(
    string Id,
    string Title,
    string Text,
    string Icon,
    IReadOnlyList<EventOption> Options,
    Func<GameSession, bool>? CanHappen = null);

/// <summary>Un événement en cours : le joueur choisit, lit le résultat, puis reprend.</summary>
public class GameEvent : IPendingChoice
{
    public GameEventInfo Info { get; }
    public string Title => Info.Title;
    public string? Outcome { get; private set; }
    public bool IsResolved => Outcome != null;

    public GameEvent(GameEventInfo info)
    {
        Info = info;
    }

    internal void Resolve(string outcome) => Outcome = outcome;
}

/// <summary>
/// Les événements de la run. Les montants suivent la facture du mois pour rester
/// intéressants du studio miteux jusqu'au penthouse. Le dernier choix est toujours prudent.
/// </summary>
public static class EventCatalog
{
    private static double Bill(GameSession s) => Math.Max(300, s.CurrentBill.Total);
    private static double Cash(GameSession s) => s.Company.WalletBalance;
    private static bool CanPay(GameSession s, double amount) => Cash(s) >= amount;
    private static string Money(double amount) => $"{amount:N0} CHF";

    private static string Pay(GameSession s, double amount)
    {
        s.Company.Wallet.Pay(amount);
        return Money(amount);
    }

    private static string Gain(GameSession s, double amount)
    {
        s.Company.Wallet.Earn(amount);
        return Money(amount);
    }

    private static void Effect(GameSession s, string name, string description, bool positive, int months, Action<Modifiers> apply) =>
        s.AddTimedEffect(new TimedEffect(name, description, positive, months, apply));

    private static string Curse(GameSession s, string perkId)
    {
        var curse = s.Config.Perks.First(p => p.Id == perkId);
        s.AddPerk(curse);
        return curse.Name;
    }

    private static readonly EventOption Ignore = new("Passer ton chemin", "Rien ne se passe.", (_, _) => "Tu retournes à ton écran.");

    public static IReadOnlyList<GameEventInfo> All { get; } =
    [
        new("prince", "Un mail d'un prince", "Un prince exilé te promet une fortune si tu l'aides à débloquer son héritage.", "mail",
        [
            new("Envoyer 10 % de ton cash", "Il paraît que ça marche parfois.", (s, r) =>
            {
                double stake = Math.Floor(Cash(s) * 0.1);
                Pay(s, stake);
                return r.NextDouble() < 0.05
                    ? $"Incroyable : le prince était réel. Tu reçois {Gain(s, stake * 25)} !"
                    : $"Le prince a disparu avec tes {Money(stake)}. Évidemment.";
            }, s => Cash(s) > 10),
            new("Supprimer le mail", "Ta mère t'a appris à te méfier.", (_, _) => "Mail supprimé. Sage décision."),
        ]),

        new("smokingpc", "Ton PC fume", "Une odeur de plastique brûlé sort de ta tour. L'écran clignote.", "fire",
        [
            new("Le faire réparer", "Coûte la moitié d'une facture.", (s, _) =>
                $"Le réparateur repart avec {Pay(s, Bill(s) * 0.5)}. Tout remarche.", s => CanPay(s, Bill(s) * 0.5)),
            new("Taper dessus", "Une chance sur deux.", (s, r) =>
            {
                if (r.NextDouble() < 0.5)
                    return "Un bon coup sur le côté et il repart. Respect.";
                Effect(s, "PC en rade", "Vitesse −30 %", false, 1, m => m.WorkSpeed *= 0.7);
                return "Il ne fume plus... mais il rame. Vitesse −30 % pendant un mois.";
            }),
        ]),

        new("hackathon", "Hackathon ce week-end", "48 heures de code, de pizza et de Red Bull. Gros prix à la clé.", "trophy",
        [
            new("Participer", "40 % de chance de gagner gros, sinon tu seras épuisé.", (s, r) =>
            {
                if (r.NextDouble() < 0.4)
                {
                    s.Company.Reputation.Add(10);
                    return $"Tu gagnes ! {Gain(s, Bill(s) * 3)} et +10 de réputation.";
                }

                Effect(s, "Épuisé", "Vitesse −15 %", false, 1, m => m.WorkSpeed *= 0.85);
                return "Deuxième place. Tu rentres lessivé : vitesse −15 % pendant un mois.";
            }),
            new("Rester chez toi", "Personne ne gagne de hackathon en dormant.", (_, _) => "Tu fais une bonne nuit de sommeil."),
        ]),

        new("mom", "Maman appelle", "Ça fait trois semaines que tu ne l'as pas appelée. Elle a l'air inquiète.", "heart",
        [
            new("Décrocher", "Ça fait du bien au moral.", (s, _) =>
            {
                Effect(s, "Moral au top", "Vitesse +20 %", true, 1, m => m.WorkSpeed *= 1.2);
                return "Elle est fière de toi. Vitesse +20 % pendant un mois.";
            }),
            new("La rappeler plus tard", "Tu es en plein rush.", (s, _) =>
            {
                Effect(s, "Culpabilité", "Vitesse −5 %", false, 1, m => m.WorkSpeed *= 0.95);
                return "Tu ne la rappelles pas. Un petit poids sur la conscience : vitesse −5 % ce mois-ci.";
            }),
        ]),

        new("influencer", "Un influenceur te contacte", "« Je fais une story sur toi pour une petite facture, et les clients pleuvent. »", "megaphone",
        [
            new("Payer une facture", "Demandes +60 % pendant deux mois.", (s, _) =>
            {
                string paid = Pay(s, Bill(s));
                Effect(s, "Buzz", "Demandes +60 %", true, 2, m => m.RequestRate *= 1.6);
                return $"Tu paies {paid}. Ton téléphone n'arrête plus de sonner.";
            }, s => CanPay(s, Bill(s))),
            Ignore,
        ]),

        new("casino", "Casino en ligne", "Une pub clignote : « Doublez votre mise ! ». Tu as un mauvais pressentiment.", "dice",
        [
            new("Miser 25 % de ton cash", "45 % de chance de ×2,2.", (s, r) =>
            {
                double stake = Math.Floor(Cash(s) * 0.25);
                Pay(s, stake);
                return r.NextDouble() < 0.45
                    ? $"Jackpot ! Tu empoches {Gain(s, stake * 2.2)}."
                    : $"Perdu. {Money(stake)} envolés.";
            }, s => Cash(s) > 20),
            new("Fermer l'onglet", "La maison gagne toujours.", (_, _) => "Tu fermes l'onglet. Ton banquier respire."),
        ]),

        new("grandma", "Le coup de pouce de mamie", "Mamie a glissé une enveloppe dans ta boîte aux lettres. « Pour tes factures, mon grand. »", "heart",
        [
            new("Accepter l'enveloppe", "Une facture offerte.", (s, _) => $"Merci mamie ! +{Gain(s, Bill(s))}."),
            new("Lui rendre", "Tu veux réussir seul.", (s, _) =>
            {
                s.Company.Reputation.Add(8);
                return "Tu lui rends l'enveloppe. Ta fierté te pousse : +8 de réputation.";
            }),
        ], s => s.Career.Stage <= CareerStage.Freelancer),

        new("vc", "Un VC frappe à la porte", "Costume trop cher, sourire trop blanc. Il veut entrer au capital de ta boîte.", "suit",
        [
            new("Accepter son argent", "Gros chèque, mais il prendra sa part pour toujours.", (s, _) =>
            {
                string gain = Gain(s, Math.Max(10_000, s.NetWorth * 0.3));
                string curse = Curse(s, "dilution");
                return $"Tu encaisses {gain}. Malédiction : {curse}.";
            }),
            new("Le mettre dehors", "Tu restes seul maître à bord.", (_, _) => "Il repart vexé. Ta boîte reste à toi."),
        ], s => s.Company.IsFounded),

        new("burnout", "Burn-out en approche", "Tu ne dors plus, tu manges devant ton écran. Ton corps lâche.", "skull",
        [
            new("Partir en vacances", "Coûte une facture, puis vitesse +25 % pendant un mois.", (s, _) =>
            {
                string paid = Pay(s, Bill(s));
                Effect(s, "Reposé", "Vitesse +25 %", true, 1, m => m.WorkSpeed *= 1.25);
                return $"Une semaine au soleil pour {paid}. Tu reviens en pleine forme.";
            }, s => CanPay(s, Bill(s))),
            new("Serrer les dents", "Vitesse −30 % pendant deux mois.", (s, _) =>
            {
                Effect(s, "Burn-out", "Vitesse −30 %", false, 2, m => m.WorkSpeed *= 0.7);
                return "Tu continues... au ralenti. Vitesse −30 % pendant deux mois.";
            }),
        ], s => s.Month >= 6),

        new("leak", "Fuite de données", "Des hackers ont volé ta base clients et demandent une rançon.", "lock",
        [
            new("Payer la rançon", "Deux factures.", (s, _) => $"Tu paies {Pay(s, Bill(s) * 2)}. Les données ne fuitent pas.",
                s => CanPay(s, Bill(s) * 2)),
            new("Refuser", "Ta réputation va souffrir.", (s, _) =>
            {
                s.Company.Reputation.Add(-30);
                Effect(s, "Scandale", "Demandes −25 %", false, 1, m => m.RequestRate *= 0.75);
                return "Les données fuitent dans la presse : −30 de réputation et demandes −25 % pendant un mois.";
            }),
        ], s => s.Company.IsFounded),

        new("intern", "Un stagiaire motivé", "Un étudiant sonne chez toi avec son CV et des croissants. Il veut apprendre.", "people",
        [
            new("L'embaucher gratuitement", "Un stagiaire sans frais d'embauche.", (s, _) =>
            {
                s.Staff.Add(EmployeeRole.Intern);
                return "Bienvenue dans l'équipe ! (son salaire reste à payer chaque mois)";
            }, s => s.HasFreeSeat),
            new("Lui dire non", "Tu prends juste les croissants.", (_, _) => "Il repart, les croissants restent. Délicieux."),
        ], s => s.Company.IsFounded && s.IsUnlocked(Feature.Hiring)),

        new("cryptobro", "Ton pote crypto", "« Frérot, le $MOON va faire ×10, fais-moi confiance. »", "rocket",
        [
            new("Mettre 20 % de ton cash", "35 % de chance de ×3.", (s, r) =>
            {
                double stake = Math.Floor(Cash(s) * 0.2);
                Pay(s, stake);
                return r.NextDouble() < 0.35
                    ? $"TO THE MOON ! Tu récupères {Gain(s, stake * 3)}."
                    : $"Le $MOON est à zéro. Adieu {Money(stake)}.";
            }, s => Cash(s) > 20),
            new("Lui couper la parole", "Tu as déjà entendu ce discours.", (_, _) => "Il part vendre son rêve à quelqu'un d'autre."),
        ]),

        new("conference", "Salon tech à Genève", "Tous les grands noms seront là. L'entrée n'est pas donnée.", "trophy",
        [
            new("Y aller", "Une demi-facture, puis un atout rare au choix.", (s, _) =>
            {
                string paid = Pay(s, Bill(s) * 0.5);
                s.QueueDraft("Rencontres au salon", Rarity.Rare);
                return $"Tu paies {paid} et tu fais des rencontres décisives.";
            }, s => CanPay(s, Bill(s) * 0.5)),
            Ignore,
        ]),

        new("buyout", "Offre de rachat", "Un fonds veut racheter ta plus petite boîte, au double de ce que tu y as mis.", "suit",
        [
            new("Vendre", "Tu encaisses ×2, la boîte disparaît.", (s, _) =>
            {
                var business = s.Businesses.Businesses.Where(b => b.IsOwned).MinBy(b => b.TotalInvested)!;
                double price = business.TotalInvested * 2;
                s.Businesses.Sell(business);
                return $"{business.Info.Name} vendue pour {Gain(s, price)}.";
            }),
            new("Refuser", "Tes boîtes ne sont pas à vendre.", (_, _) => "Tu raccroches au nez du fonds."),
        ], s => s.Businesses.Businesses.Any(b => b.IsOwned)),
    ];
}
