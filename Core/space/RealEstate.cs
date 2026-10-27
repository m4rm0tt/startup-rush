namespace Core.space;

/// <summary>Un local à louer : un plan de pièce du pack, avec son loyer et sa garantie.</summary>
public record Listing(
    string Id,
    string Name,
    string Description,
    string PlanId,
    int MaxDesks,
    decimal Rent,
    int DepositMonths,
    decimal MovingCost)
{
    public decimal Deposit => Rent * DepositMonths;

    public RoomPlan Plan => Pack.Plan(PlanId);
}

public static class RealEstate
{
    public const string Apartment = "apartment";

    /// <summary>Coût de déménagement par meuble transporté, en plus du forfait du local.</summary>
    public const decimal MovingCostPerItem = 35m;

    public static readonly IReadOnlyList<Listing> Listings =
    [
        new(Apartment, "Ta chambre, Prilly",
            "Ta chambre de passionné : lit, rangement et un bureau modeste. 3 postes au plus. Loyer compris dans ta rémunération.",
            "room_bedroom_start", 3, 0m, 0, 0m),
        new("small", "Petit bureau, Flon",
            "Un premier local au centre-ville, avec un coin cuisine et un coin réunion. Jusqu'à 6-7 postes.",
            "room_startup_small", 0, 2_300m, 3, 900m),
        new("floor", "Bureau d'équipe, Malley",
            "Un vrai bureau près de la gare : cuisine, coin détente, salle de réunion. Jusqu'à 15 postes.",
            "room_office_medium", 0, 5_400m, 3, 1_800m),
        new("large", "Grand open space, Renens",
            "Un plateau ouvert avec cafétéria, cloisons et grande salle de réunion. Jusqu'à 30 postes.",
            "room_open_space", 0, 12_500m, 3, 3_500m),
    ];

    public static Listing Get(string id) => Listings.First(l => l.Id == id);

    /// <summary>Équipement de poste : la location ne le fournit pas (il vient des cartons ou s'achète).</summary>
    private static bool IsWorkGear(FurnitureDef def) =>
        def.Kind is FurnitureKind.Desk or FurnitureKind.Chair or FurnitureKind.Laptop or FurnitureKind.Tower or FurnitureKind.Monitor
            or FurnitureKind.Keyboard or FurnitureKind.Personal or FurnitureKind.Coffee;

    /// <summary>
    /// Crée le local d'une annonce à partir de son plan. Ta chambre est livrée avec tes affaires (lit, bureau, vieille tour) ;
    /// un bureau loué n'est livré qu'avec ce qui fait partie du lieu (cuisine, rangements, cloisons, déco) : les postes du plan
    /// ne sont qu'un aperçu. Ce qui est livré reste au local et ne se revend pas ; ton matériel de départ te suit, sans valeur de revente. Les personnages du plan ne sont jamais repris.
    /// </summary>
    public static Premises Create(Listing listing, Func<int> nextId)
    {
        var plan = listing.Plan;
        var premises = new Premises
        {
            ListingId = listing.Id,
            PlanId = plan.Id,
            Name = listing.Name,
            Width = plan.Width,
            Height = plan.Depth,
            EntranceX = plan.Entrance.X,
            EntranceY = plan.Entrance.Y,
            EntranceWall = plan.EntranceWall,
            MaxDesks = listing.MaxDesks,
            Rent = listing.Rent,
            Deposit = listing.Deposit,
        };
        bool home = listing.Id == Apartment;
        foreach (var o in plan.Objects)
        {
            var def = FurnitureCatalog.Get(o.Asset);
            if (!home && IsWorkGear(def))
                continue;
            var item = new PlacedItem
            {
                Id = nextId(), DefId = o.Asset, X = o.U, Y = o.V, Facing = o.Facing, Included = !(home && IsWorkGear(def)),
                Swatches = new Dictionary<string, string>(o.Swatches),
            };
            premises.Items.Add(item);
            foreach (var child in o.Children)
            {
                if (!home && IsWorkGear(FurnitureCatalog.Get(child.Asset)))
                    continue;
                premises.Items.Add(new PlacedItem
                {
                    Id = nextId(), DefId = child.Asset, HostId = item.Id, Socket = child.Socket,
                    Included = !(home && IsWorkGear(FurnitureCatalog.Get(child.Asset))),
                });
            }
        }
        return premises;
    }
}
