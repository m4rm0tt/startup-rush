using Core.simulation;

namespace Core.requests;

/// <summary>
/// Crée des demandes à intervalle aléatoire. Le Random est injecté :
/// la même graine produit toujours les mêmes demandes.
/// </summary>
public class RequestGenerator
{
    private static readonly string[] ClientNames =
    [
        "Boulangerie Favre", "Garage du Lac", "Fiduciaire Morel", "Club de foot Renens",
        "Cabinet Dr Rochat", "Fromagerie Gruyère", "Hôtel des Alpes", "Librairie Payot",
        "Startup NeoBank", "Commune de Morges", "Fleuriste Bloom", "Agence Immo Léman"
    ];

    private readonly Random _random;
    private readonly GameConfig _config;
    private double _timeUntilNextRequest;
    private int _nextId = 1;

    public RequestGenerator(Random random, GameConfig config)
    {
        _random = random;
        _config = config;
        _timeUntilNextRequest = config.FirstRequestDelay;
    }

    /// <summary>
    /// Fait avancer le minuteur et renvoie les demandes apparues pendant ce delta (souvent aucune).
    /// </summary>
    public List<ClientRequest> Update(double deltaTime)
    {
        var newRequests = new List<ClientRequest>();
        _timeUntilNextRequest -= deltaTime;

        while (_timeUntilNextRequest <= 0)
        {
            newRequests.Add(CreateRequest());
            _timeUntilNextRequest += NextInterval();
        }

        return newRequests;
    }

    public ClientRequest CreateRequest()
    {
        int difficulty = _random.Next(_config.MinDifficulty, _config.MaxDifficulty + 1);

        double variation = 1 + (_random.NextDouble() * 2 - 1) * _config.ValueVariation;
        double value = Math.Round(difficulty * _config.ValuePerDifficulty * variation / 5) * 5;

        double? deadline = null;
        if (_random.NextDouble() < _config.DeadlineChance)
        {
            double margin = RandomBetween(_config.MinDeadlineMargin, _config.MaxDeadlineMargin);
            deadline = Math.Round(_config.WorkDurationFor(difficulty) * margin);
        }

        var types = Enum.GetValues<RequestType>();
        var type = types[_random.Next(types.Length)];
        string client = ClientNames[_random.Next(ClientNames.Length)];

        return new ClientRequest(_nextId++, client, type, value, difficulty, deadline, _config.OfferDuration);
    }

    // Un intervalle minimum évite une boucle infinie si la config met 0
    private double NextInterval() =>
        Math.Max(0.1, RandomBetween(_config.MinRequestInterval, _config.MaxRequestInterval));

    private double RandomBetween(double min, double max) => min + _random.NextDouble() * (max - min);
}
