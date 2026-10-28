using Core.finance;
using Core.industry;
using Core.progress;
using Core.space;
using Core.team;
using Core.work;

namespace Core.campaign;

/// <summary>
/// Le menu de test : des raccourcis pour essayer le jeu sans attendre (argent, points, déblocages, temps, équipe, stock).
/// Chaque usage marque la campagne (CheatsUsed) : une sauvegarde de test se reconnaît, et les bilans ne s'en servent pas.
/// </summary>
public partial class Campaign
{
    /// <summary>Le menu de test a servi dans cette campagne.</summary>
    public bool CheatsUsed { get; set; }

    private CommandResult Cheated(string message)
    {
        CheatsUsed = true;
        return CommandResult.Success(message);
    }

    public CommandResult CheatMoney(decimal amount)
    {
        Post(amount, Category.Funding, "Menu de test : argent");
        return Cheated($"+{amount:N0} CHF.");
    }

    public CommandResult CheatPoints(int points)
    {
        DevPoints += points;
        return Cheated($"+{points} points de développement.");
    }

    /// <summary>Tous les nœuds de l'arbre et toutes les recherches.</summary>
    public CommandResult CheatUnlockAll()
    {
        Unlocked.UnionWith(DevTree.Nodes.Select(n => n.Id));
        ResearchDone.UnionWith(ResearchCatalog.Select(r => r.Id));
        Research.Clear();
        return Cheated("Tout l'arbre et toutes les recherches sont débloqués.");
    }

    /// <summary>Termine tout le travail en file de tous les projets (et les recherches en cours).</summary>
    public CommandResult CheatFinishWork()
    {
        foreach (var item in Work.Where(w => !w.Done))
        {
            item.QualityPoints += item.Remaining * 0.7;
            item.Progress = item.Effort;
        }
        foreach (var product in Products)
            CheckQueue(product);
        foreach (var project in Research.ToList())
            CompleteResearch(project);
        foreach (var model in AiModels.Where(m => m.Training != null).ToList())
            FinishTraining(model);
        foreach (var quantum in QuantumSites.Where(q => q.WarmupLeft > 1))
            quantum.WarmupLeft = 1;
        foreach (var person in People)
            person.CurrentWorkItemId = null;
        return Cheated("Tout le travail en file est terminé.");
    }

    /// <summary>Embauche tout de suite le meilleur candidat, avec un poste complet sur le site affiché.</summary>
    public CommandResult CheatHire()
    {
        if (Candidates.Count == 0)
            Candidates.Add(Recruitment.Generate(Random, Minute, NewId(), Renown));
        var candidate = Candidates.OrderByDescending(c => c.Person.Skill(c.Person.BestDiscipline)).First();
        candidate.NoticeWorkdays = 0;
        var result = Hire(candidate.Person.Id);
        if (!result.Ok)
            return result;
        if (PostOf(candidate.Person).Post == null && InstallWorkstation(buy: true) is { } post)
            Seat(candidate.Person, post);
        return Cheated($"{candidate.Person.Name} est embauché·e et installé·e.");
    }

    public CommandResult CheatRefresh()
    {
        foreach (var person in People)
        {
            person.Energy = 100;
            person.Morale = 100;
        }
        return Cheated("Énergie et moral à 100 pour tout le monde.");
    }

    /// <summary>100 de chaque matière au stock du site affiché (sans tenir compte de la place).</summary>
    public CommandResult CheatStock()
    {
        foreach (var material in IndustryCatalog.Materials)
            AddStock(Premises.Id, material.Id, 100);
        return Cheated($"100 de chaque matière à {Premises.Name}.");
    }

    /// <summary>Des utilisateurs (ou acheteurs informés) en plus sur chaque projet en ligne.</summary>
    public CommandResult CheatUsers(double users)
    {
        foreach (var product in Products.Where(p => p.IsLive))
        {
            if (product.IsHardware)
                product.Aware = Math.Min(product.Segment.Size, product.Aware + users);
            else
                product.Users += users;
        }
        return Cheated($"+{users:N0} utilisateurs sur chaque projet en ligne.");
    }

    /// <summary>Paie toutes les factures en retard et remet la situation financière au propre.</summary>
    public CommandResult CheatClearDebts()
    {
        decimal due = Ledger.Bills.Where(b => b.Open).Sum(b => b.Amount);
        if (due > 0)
            Post(due, Category.Funding, "Menu de test : dettes couvertes");
        PayDueBills();
        foreach (var bill in Ledger.Bills.Where(b => b.Open).ToList())
        {
            bill.PaidAt = Minute;
            Post(-bill.Amount, bill.Category, $"Payé : {bill.Label}");
        }
        UpdateSolvency();
        return Cheated("Toutes les factures sont payées.");
    }

    public CommandResult CheatAdvance(int days)
    {
        AdvanceDays(days);
        return Cheated($"{days} jour(s) passé(s).");
    }
}
