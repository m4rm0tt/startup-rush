using Core.company;
using Core.simulation;

namespace Core.requests;

/// <summary>
/// Crée des demandes à intervalle aléatoire. Le Random est injecté :
/// la même graine produit toujours les mêmes demandes.
/// La taille des contrats et leur fréquence suivent les PlayerStats (réputation, internet, équipe).
/// </summary>
public class RequestGenerator
{
    private static readonly string[] ClientNames =
    [
        "Boulangerie Favre", "Garage du Lac", "Fiduciaire Morel", "Club de foot Renens",
        "Cabinet Dr Rochat", "Fromagerie Gruyère", "Hôtel des Alpes", "Librairie Payot",
        "Startup NeoBank", "Commune de Morges", "Fleuriste Bloom", "Agence Immo Léman",
        "Chocolats Suchard", "CFF Cargo", "Montres Horlogia", "Assurances Helvetia",
        "Banque du Rhône", "Nestlé Labs", "Swiss Airways", "ONU Genève"
    ];

    private readonly Random _random;
    private readonly GameConfig _config;
    private readonly PlayerStats _stats;
    private double _timeUntilNextRequest;
    private int _nextId = 1;

    public RequestGenerator(Random random, GameConfig config, PlayerStats? stats = null)
    {
        _random = random;
        _config = config;
        _stats = stats ?? new PlayerStats(config);
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
        int maxDifficulty = Math.Max(1, _stats.MaxDifficulty);
        int minDifficulty = Math.Max(1, maxDifficulty - _config.DifficultySpread);
        int difficulty = _random.Next(minDifficulty, maxDifficulty + 1);

        double variation = 1 + (_random.NextDouble() * 2 - 1) * _config.ValueVariation;
        double value = RoundNicely(difficulty * _config.ValuePerDifficulty * _stats.ValueMultiplier * variation);

        // L'échéance tient compte de la vitesse actuelle, sinon un bon PC rendrait les échéances inutiles
        double? deadline = null;
        if (_random.NextDouble() < _config.DeadlineChance)
        {
            double margin = RandomBetween(_config.MinDeadlineMargin, _config.MaxDeadlineMargin) * _stats.DeadlineMultiplier;
            deadline = Math.Ceiling(_config.WorkDurationFor(difficulty) / _stats.WorkSpeed * margin +
                                    _config.DeadlineReactionTime);
        }

        // Les plus gros clients n'arrivent qu'avec la réputation
        int clientPool = Math.Min(ClientNames.Length, 6 + maxDifficulty * 2);
        string client = ClientNames[_random.Next(clientPool)];

        var types = Enum.GetValues<RequestType>();
        var type = types[_random.Next(types.Length)];

        return new ClientRequest(_nextId++, client, type, value, difficulty, deadline,
            _config.OfferDuration * _stats.OfferDurationMultiplier);
    }

    // Un intervalle minimum évite une boucle infinie si la config met 0
    private double NextInterval() =>
        Math.Max(0.1, RandomBetween(_config.MinRequestInterval, _config.MaxRequestInterval) / _stats.RequestRateMultiplier);

    private double RandomBetween(double min, double max) => min + _random.NextDouble() * (max - min);

    // 5 CHF près pour les petits montants, 3 chiffres significatifs pour les gros
    private static double RoundNicely(double value)
    {
        if (value < 1000)
            return Math.Round(value / 5) * 5;

        double magnitude = Math.Pow(10, Math.Floor(Math.Log10(value)) - 2);
        return Math.Round(value / magnitude) * magnitude;
    }
}
