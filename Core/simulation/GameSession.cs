using Core.company;
using Core.requests;

namespace Core.simulation;

public class GameSession
{
    private readonly GameClock _gameClock;
    private readonly Company _company;
    private readonly RequestGenerator _requestGenerator;

    public Company Company => _company;
    public GameConfig Config { get; }

    public double ElapsedTime => _gameClock.Time;

    public bool IsOver => Result != null;

    /// <summary>Bilan de la partie, null tant qu'elle n'est pas terminée.</summary>
    public GameResult? Result { get; private set; }

    public event Action<GameResult>? GameOver;

    public GameSession(GameConfig? config = null, Random? random = null, string companyName = "Ma Startup")
    {
        Config = config ?? new GameConfig();
        _gameClock = new GameClock(Config.MaxDeltaTime);
        _company = new Company(Config, companyName);
        _requestGenerator = new RequestGenerator(random ?? new Random(), Config);
    }

    /// <summary>
    /// Fait avancer la partie. L'ordre des systèmes est fixe :
    /// 1. temps, 2. génération des demandes, 3. expiration des offres,
    /// 4. avancement des tâches (et paiements), 5. charges, 6. vérification de fin.
    /// </summary>
    public void Update(double deltaTime)
    {
        if (IsOver)
            return;

        double delta = _gameClock.Tick(deltaTime);
        if (delta <= 0)
            return;

        foreach (var request in _requestGenerator.Update(delta))
            _company.AddRequest(request);

        _company.UpdateRequests(delta);
        _company.UpdateTasks(delta);
        _company.PayOperatingCosts(delta);

        CheckGameOver();
    }

    private void CheckGameOver()
    {
        if (!_company.IsBankrupt)
            return;

        Result = new GameResult(
            ElapsedTime,
            _company.WalletBalance,
            _company.TotalEarned,
            _company.CompletedTaskCount,
            _company.FailedTaskCount);

        GameOver?.Invoke(Result);
    }
}
