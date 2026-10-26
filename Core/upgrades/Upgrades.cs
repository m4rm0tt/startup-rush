using Core.company;

namespace Core.upgrades;

public enum UpgradeTrackId
{
    Computer,
    Screens,
    Chair,
    Coffee,
    Internet,
    Housing,
    Office
}

/// <param name="Effect">Multiplicateur apporté par ce niveau (vitesse, clic ou demandes selon la piste),
/// ou nombre de places pour les locaux.</param>
/// <param name="MonthlyCost">Ajouté aux factures de chaque mois (loyer, abonnement).</param>
public record UpgradeLevel(string Name, double Cost, double Effect, double MonthlyCost = 0);

/// <param name="EffectLabel">Ce que multiplie l'effet, pour l'affichage.</param>
public record UpgradeTrackInfo(UpgradeTrackId Id, string Title, string EffectLabel, IReadOnlyList<UpgradeLevel> Levels);

/// <summary>Une piste d'amélioration : on achète les niveaux dans l'ordre.</summary>
public class UpgradeTrack
{
    public UpgradeTrackInfo Info { get; }
    public int Level { get; private set; }

    public UpgradeLevel Current => Info.Levels[Level];
    public UpgradeLevel? Next => Level + 1 < Info.Levels.Count ? Info.Levels[Level + 1] : null;
    public bool IsMaxed => Next == null;

    public UpgradeTrack(UpgradeTrackInfo info)
    {
        Info = info;
    }

    internal void LevelUp() => Level++;
}

/// <summary>Le matériel et le logement du joueur.</summary>
public class Upgrades
{
    private readonly Dictionary<UpgradeTrackId, UpgradeTrack> _tracks;

    public IReadOnlyList<UpgradeTrack> Tracks { get; }

    public event Action<UpgradeTrack>? Upgraded;

    public Upgrades(IEnumerable<UpgradeTrackInfo> tracks)
    {
        Tracks = tracks.Select(info => new UpgradeTrack(info)).ToList();
        _tracks = Tracks.ToDictionary(t => t.Info.Id);
    }

    public UpgradeTrack this[UpgradeTrackId id] => _tracks[id];

    /// <summary>Vitesse de travail : ordinateur × écrans × café × confort du logement.</summary>
    public double WorkSpeedMultiplier =>
        Effect(UpgradeTrackId.Computer) * Effect(UpgradeTrackId.Screens) *
        Effect(UpgradeTrackId.Coffee) * Effect(UpgradeTrackId.Housing);

    public double ClickMultiplier => Effect(UpgradeTrackId.Chair);
    public double RequestRateMultiplier => Effect(UpgradeTrackId.Internet);

    /// <summary>Nombre d'employés que les locaux peuvent accueillir.</summary>
    public int EmployeeSeats => _tracks.TryGetValue(UpgradeTrackId.Office, out var office) ? (int)office.Current.Effect : int.MaxValue;
    public double MonthlyCost => Tracks.Sum(t => t.Current.MonthlyCost);

    public bool TryBuy(UpgradeTrackId id, Wallet wallet)
    {
        var track = _tracks[id];
        if (track.Next is not { } next || !wallet.TrySpend(next.Cost))
            return false;

        track.LevelUp();
        Upgraded?.Invoke(track);
        return true;
    }

    private double Effect(UpgradeTrackId id) => _tracks.TryGetValue(id, out var track) ? track.Current.Effect : 1;
}
