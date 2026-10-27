namespace Core.space;

/// <summary>
/// Un type de local à louer : un plan de pièce du pack, son loyer et sa garantie, et les adresses où il est proposé. Chaque
/// adresse est une offre : on peut louer plusieurs locaux du même type, tant qu'il reste des adresses libres.
/// </summary>
public record Listing(
    string Id,
    string Kind,
    string Description,
    string PlanId,
    int MaxDesks,
    decimal Rent,
    int DepositMonths,
    decimal MovingCost,
    string[] Places,
    string? UnlockNode = null)
{
    public decimal Deposit => Rent * DepositMonths;

    public RoomPlan Plan => Pack.Plan(PlanId);

    /// <summary>Le nom d'une offre : « Petit bureau, Flon ».</summary>
    public string NameAt(string place) => $"{Kind}, {place}";

    /// <summary>Le nom de la première adresse (pour l'affichage d'un type de local).</summary>
    public string Name => NameAt(Places[0]);

    /// <summary>Un site technique (salle serveur, datacenter) n'accueille pas de bureaux.</summary>
    public bool Technical => MaxDesks < 0;
}

/// <summary>Une offre de location : un type de local à une adresse.</summary>
public record Offer(Listing Listing, string Place)
{
    public string Name => Listing.NameAt(Place);
}

public static class RealEstate
{
    public const string Apartment = "apartment";

    /// <summary>Coût de déménagement par meuble transporté, en plus du forfait du local.</summary>
    public const decimal MovingCostPerItem = 35m;

    /// <summary>Ouvrir un site de plus coûte la moitié du forfait de déménagement (installation, raccordements).</summary>
    public const decimal OpeningShare = 0.5m;

    public static readonly IReadOnlyList<Listing> Listings =
    [
        new(Apartment, "Ta chambre",
            "Ta chambre de passionné : lit, rangement et un bureau modeste. 3 postes au plus. Loyer compris dans ta rémunération.",
            "room_bedroom_start", 3, 0m, 0, 0m, ["Prilly"]),
        new("small", "Petit bureau",
            "Un premier local au centre-ville, avec un coin cuisine et un coin réunion. Jusqu'à 6-7 postes.",
            "room_startup_small", 0, 2_300m, 3, 900m, ["Flon", "Ouchy", "Morges"]),
        new("floor", "Bureau d'équipe",
            "Un vrai bureau près de la gare : cuisine, coin détente, salle de réunion. Jusqu'à 15 postes.",
            "room_office_medium", 0, 5_400m, 3, 1_800m, ["Malley", "Ecublens"]),
        new("large", "Grand open space",
            "Un plateau ouvert avec cafétéria, cloisons et grande salle de réunion. Jusqu'à 30 postes.",
            "room_open_space", 0, 12_500m, 3, 3_500m, ["Renens", "Nyon"]),
        new("server_room", "Salle serveur",
            "Une petite salle technique : plancher technique, climatisation et armoire électrique livrées. Place pour quatre baies.",
            "room_server_compact", -1, 1_800m, 3, 1_200m, ["Crissier", "Bussigny"], "server_room"),
        new("datacenter", "Datacenter",
            "Une salle d'hébergement à grande échelle : deux climatiseurs, deux armoires électriques, cage de sécurité livrés.",
            "room_datacenter", -1, 16_000m, 3, 6_000m, ["Gland", "Yverdon"], "datacenter"),
        new("hq", "Siège",
            "Un étage complet : accueil, cafétéria, sanitaires, salles de réunion et cloisons livrés. Jusqu'à 60 postes.",
            "room_hq_floor", 0, 26_000m, 3, 8_000m, ["Lausanne-Gare"], "multisite"),
    ];

    public static Listing Get(string id) => Listings.First(l => l.Id == id);

    /// <summary>Les offres libres : chaque adresse d'un type de local qui n'est pas déjà l'un de tes sites.</summary>
    public static IEnumerable<Offer> Offers(IEnumerable<Premises> sites)
    {
        var taken = sites.Select(s => (s.ListingId, s.Place)).ToHashSet();
        return Listings.SelectMany(l => l.Places.Select(p => new Offer(l, p))).Where(o => !taken.Contains((o.Listing.Id, o.Place)));
    }

    /// <summary>Équipement de poste : la location ne le fournit pas (il vient des cartons ou s'achète).</summary>
    private static bool IsWorkGear(FurnitureDef def) =>
        def.Kind is FurnitureKind.Desk or FurnitureKind.Chair or FurnitureKind.Laptop or FurnitureKind.Tower or FurnitureKind.Monitor
            or FurnitureKind.Keyboard or FurnitureKind.Personal or FurnitureKind.Coffee or FurnitureKind.Supervision
            or FurnitureKind.Rack or FurnitureKind.RackModule or FurnitureKind.Server or FurnitureKind.Backup;

    /// <summary>
    /// Crée le local d'une offre à partir de son plan. Ta chambre est livrée avec tes affaires (lit, bureau, vieille tour) ;
    /// un local loué n'est livré qu'avec ce qui fait partie du lieu (cuisine, rangements, cloisons, climatisation, armoire
    /// électrique, déco) : les postes, baies et serveurs du plan ne sont qu'un aperçu. Ce qui est livré reste au local et ne se
    /// revend pas ; ton matériel de départ te suit, sans valeur de revente. Les personnages du plan ne sont jamais repris.
    /// </summary>
    public static Premises Create(Listing listing, Func<int> nextId, string? place = null)
    {
        var plan = listing.Plan;
        place ??= listing.Places[0];
        var premises = new Premises
        {
            Id = nextId(),
            ListingId = listing.Id,
            PlanId = plan.Id,
            Place = place,
            Name = listing.NameAt(place),
            Width = plan.Width,
            Height = plan.Depth,
            EntranceX = plan.Entrance.X,
            EntranceY = plan.Entrance.Y,
            EntranceWall = plan.EntranceWall,
            MaxDesks = Math.Max(0, listing.MaxDesks),
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
        premises.Items.AddRange(WallItems(plan, nextId));
        return premises;
    }

    /// <summary>Les objets muraux du plan (affiches, horloge, stores, planning) : fixés au local, livrés avec lui.</summary>
    public static IEnumerable<PlacedItem> WallItems(RoomPlan plan, Func<int> nextId) =>
        plan.WallItems.Where(w => Pack.Has(w.Asset)).Select(w => new PlacedItem
        {
            Id = nextId(), DefId = w.Asset, X = w.Wall == "left" ? 0 : w.Index, Y = w.Wall == "left" ? w.Index : 0,
            Wall = w.Wall, Facing = Layout.WallFacing(w.Wall), Included = true,
        }).ToList();
}
