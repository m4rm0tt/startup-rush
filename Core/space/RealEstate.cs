namespace Core.space;

/// <summary>Un local à louer : surface, loyer, garantie et équipements fixes.</summary>
public record Listing(
    string Id,
    string Name,
    string Description,
    int Width,
    int Height,
    int DoorX,
    int LivingWidth,
    int MaxDesks,
    decimal Rent,
    int DepositMonths,
    decimal MovingCost,
    (string DefId, int X, int Y)[] Fixtures)
{
    public decimal Deposit => Rent * DepositMonths;
}

public static class RealEstate
{
    public const string Apartment = "apartment";

    /// <summary>Coût de déménagement par meuble transporté, en plus du forfait du local.</summary>
    public const decimal MovingCostPerItem = 35m;

    public static readonly IReadOnlyList<Listing> Listings =
    [
        new(Apartment, "Ton appartement, Prilly",
            "3 pièces. Le salon et la chambre restent privés ; le reste accueille 3 postes au plus. Loyer compris dans ta rémunération.",
            14, 9, 3, 6, 3, 0m, 0, 0m,
            [("bed", 0, 0), ("kitchen", 2, 0), ("fridge", 5, 0), ("home_sofa", 0, 5), ("dining", 3, 4)]),
        new("small", "Petit bureau, Flon",
            "Un bureau de 60 m² au centre-ville. Kitchenette. Jusqu'à 6-7 postes.",
            16, 10, 2, 0, 0, 2_300m, 3, 900m,
            [("kitchenette", 0, 0)]),
        new("floor", "Plateau, Malley",
            "130 m² ouverts près de la gare. Kitchenette. Jusqu'à 15 postes.",
            22, 13, 3, 0, 0, 5_400m, 3, 1_800m,
            [("kitchenette", 0, 0)]),
        new("large", "Étage complet, Renens",
            "300 m² sur un étage. Jusqu'à 30 postes.",
            30, 16, 4, 0, 0, 12_500m, 3, 3_500m,
            [("kitchenette", 0, 0), ("kitchenette", 2, 0)]),
    ];

    public static Listing Get(string id) => Listings.First(l => l.Id == id);

    /// <summary>Crée le local vide d'une annonce, avec ses équipements fixes.</summary>
    public static Premises Create(Listing listing, Func<int> nextId)
    {
        var premises = new Premises
        {
            ListingId = listing.Id,
            Name = listing.Name,
            Width = listing.Width,
            Height = listing.Height,
            DoorX = listing.DoorX,
            LivingWidth = listing.LivingWidth,
            MaxDesks = listing.MaxDesks,
            Rent = listing.Rent,
            Deposit = listing.Deposit,
        };
        foreach (var (defId, x, y) in listing.Fixtures)
            premises.Items.Add(new PlacedItem { Id = nextId(), DefId = defId, X = x, Y = y });
        return premises;
    }
}
