using Core.campaign;

namespace Core.team;

/// <summary>Une personne disponible sur le marché de l'emploi, pour un temps limité.</summary>
public class Candidate
{
    public Person Person { get; set; } = new();
    public long ExpiresAt { get; set; }

    /// <summary>Jours ouvrés avant de pouvoir commencer (préavis).</summary>
    public int NoticeWorkdays { get; set; }
}

/// <summary>Génère les candidats : spécialité, niveau, salaire demandé (en CHF par mois, ordre de grandeur suisse romand).</summary>
public static class Recruitment
{
    private static readonly string[] FirstNames =
    [
        "Léa", "Noah", "Chloé", "Luca", "Sara", "Yanis", "Inès", "Mathis", "Elena", "Samuel", "Zoé", "Nathan",
        "Amélie", "Arthur", "Mila", "Gabriel", "Jade", "Liam", "Nora", "Hugo", "Aylin", "Rafael", "Maëlle", "Dario",
        "Anouk", "Tiago", "Lina", "Joël", "Alix", "Kenza",
    ];

    private static readonly string[] LastNames =
    [
        "Rochat", "Favre", "Bonvin", "Pittet", "Cuendet", "Monnier", "Ferreira", "Rossi", "Meylan", "Berset",
        "Nguyen", "Gashi", "Morel", "Jaquier", "Duc", "Perret", "Mendes", "Keller", "Aubert", "Bovet",
    ];

    public static string RandomName(SimRandom random) => $"{random.Pick(FirstNames)} {random.Pick(LastNames)}";

    /// <summary>Les métiers des projets, toujours ouverts au recrutement.</summary>
    public static readonly IReadOnlyList<Discipline> ProjectJobs = [Discipline.Design, Discipline.Code, Discipline.Test, Discipline.Support];

    /// <summary>
    /// Un candidat d'un des métiers ouverts (les quatre des projets, plus production et recherche quand l'entreprise en a
    /// l'usage). Sans métier ouvert en plus, les tirages sont exactement ceux d'avant (parties et bilans reproductibles).
    /// </summary>
    public static Candidate Generate(SimRandom random, long now, int id, double reputation, IReadOnlyList<Discipline>? jobs = null)
    {
        jobs ??= ProjectJobs;
        var specialty = jobs[random.Next(0, jobs.Count)];
        // Une meilleure réputation attire des profils plus expérimentés
        double seniorChance = 0.1 + reputation / 250;
        double main = random.Chance(seniorChance) ? random.Range(70, 88)
            : random.Chance(0.5) ? random.Range(45, 68)
            : random.Range(25, 44);

        var skills = new Dictionary<Discipline, double>();
        foreach (var discipline in ProjectJobs)
            skills[discipline] = discipline == specialty ? main : Math.Round(random.Range(5, main * 0.6));
        skills[specialty] = Math.Round(main);
        Person.FillSecondary(skills);

        decimal salary = Math.Round((decimal)(3_800 + main * 70 + random.Range(-300, 300)) / 50m) * 50m;
        return new Candidate
        {
            Person = new Person
            {
                Id = id,
                Name = RandomName(random),
                Appearance = Appearance.Random(random),
                Skills = skills,
                Salary = salary,
                Morale = 72,
            },
            ExpiresAt = now + GameTime.MinutesPerDay * random.Next(6, 14),
            NoticeWorkdays = random.Chance(0.4) ? 0 : random.Next(3, 11),
        };
    }
}
