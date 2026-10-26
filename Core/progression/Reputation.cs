namespace Core.progression;

/// <param name="MaxDifficulty">Difficulté maximale des demandes proposées à ce niveau.</param>
/// <param name="ValueMultiplier">Multiplie la valeur des demandes.</param>
public record ReputationLevel(string Title, double PointsRequired, int MaxDifficulty, double ValueMultiplier);

/// <summary>
/// Monte quand le joueur livre à temps, baisse quand il échoue.
/// Le niveau décide de la taille des contrats proposés.
/// </summary>
public class Reputation
{
    private readonly IReadOnlyList<ReputationLevel> _levels;

    public double Points { get; private set; }
    public int LevelIndex { get; private set; }
    public ReputationLevel Current => _levels[LevelIndex];
    public ReputationLevel? Next => LevelIndex + 1 < _levels.Count ? _levels[LevelIndex + 1] : null;

    public event Action<ReputationLevel>? LevelChanged;

    public Reputation(IReadOnlyList<ReputationLevel> levels)
    {
        _levels = levels;
    }

    public double ProgressToNext => Next == null
        ? 1
        : Math.Clamp((Points - Current.PointsRequired) / (Next.PointsRequired - Current.PointsRequired), 0, 1);

    public void Add(double points)
    {
        Points = Math.Max(0, Points + points);

        int index = 0;
        while (index + 1 < _levels.Count && Points >= _levels[index + 1].PointsRequired)
            index++;

        if (index == LevelIndex)
            return;

        LevelIndex = index;
        LevelChanged?.Invoke(Current);
    }
}
