using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Core.campaign;
using Core.team;
using Core.work;
using UI.Pixel;

namespace UI.Hud.Panels;

/// <summary>Projets clients : demandes à étudier (avec négociation du délai), contrats en cours, historique.</summary>
public class ProjectsPanel : GamePanel
{
    public override string Title => "Projets clients";
    public override string Context => $"{Campaign.MainCompany.Name} · réputation {Campaign.MainCompany.Reputation:0}/100";

    private readonly Dictionary<int, DeadlineOption> _options = new();
    private readonly HashSet<int> _capitalize = new();

    public override Control Build()
    {
        var column = Ui.Column(8);

        var active = Campaign.ActiveContracts.ToList();
        var activeList = Ui.Column(8);
        foreach (var contract in active)
            activeList.Children.Add(ActiveCard(contract));
        if (active.Count == 0)
            activeList.Children.Add(Ui.Muted("Aucun contrat en cours."));
        column.Children.Add(Ui.Section($"En cours ({active.Count})", activeList));

        var offers = Campaign.Offers.OrderBy(o => o.OfferExpiresAt).ToList();
        var offerList = Ui.Column(8);
        foreach (var offer in offers)
            offerList.Children.Add(OfferCard(offer));
        if (offers.Count == 0)
            offerList.Children.Add(Ui.Muted("Aucune demande pour l'instant. Elles arrivent les jours ouvrés, plus souvent quand la réputation monte."));
        column.Children.Add(Ui.Section($"Demandes ({offers.Count})", offerList));

        var history = Campaign.Contracts.Where(c => c.Status is ContractStatus.Completed or ContractStatus.Cancelled).OrderByDescending(c => c.DeliveredAt ?? c.AcceptedAt).Take(8).ToList();
        if (history.Count > 0)
        {
            var list = Ui.Column(4);
            foreach (var c in history)
                list.Children.Add(Ui.Column(0, Ui.Text($"{c.Client} — {c.Title}", 14, c.Status == ContractStatus.Completed ? Palette.Cloud : Palette.Ember),
                    Ui.Muted($"{c.Outcome} {(c.Status == ContractStatus.Completed ? $"Qualité {c.DeliveredQuality:P0}, pénalités {c.Penalty:N0} CHF." : "")}", 12)));
            column.Children.Add(Ui.Section("Historique", list));
        }
        return column;
    }

    private Control OfferCard(Contract offer)
    {
        var option = _options.GetValueOrDefault(offer.Id, DeadlineOption.Standard);
        bool capitalize = _capitalize.Contains(offer.Id);
        var (price, workdays) = offer.Terms(option);
        var company = Campaign.MainCompany;
        int reuse = company.ReuseLevels.GetValueOrDefault(offer.KindId);
        double speed = Campaign.People.Where(p => p.Assignment.Kind == AssignmentKind.None || p.IsFounder)
            .Sum(p => Campaign.WorkRate(p, p.BestDiscipline) * 60 * 7);
        double days = offer.TotalEffort / System.Math.Max(1, speed);

        string scope = string.Join(", ", offer.Scope.Select(s => $"{Person.DisciplineName(s.Key).ToLowerInvariant()} {s.Value:0}"));
        var card = Ui.Column(4,
            Ui.Row(Ui.Text(offer.Client, 16, Palette.Snow, bold: true), Ui.Tag(offer.Title, Palette.Sky)),
            Ui.Muted($"Travail : {offer.TotalEffort:0} points ({scope}). Qualité attendue {offer.ExpectedQuality:P0}." +
                     (reuse > 0 ? $" Code réutilisable niveau {reuse} : −{reuse * 10} % de développement." : "")),
            Ui.Muted($"Estimation : environ {days:0.0} jours ouvrés avec la personne disponible la plus rapide. Paiement client {(offer.Reliability >= 0.85 ? "fiable" : offer.Reliability >= 0.7 ? "parfois en retard" : "peu fiable")}."),
            Ui.Choice([(DeadlineOption.Safe, "Délai sûr"), (DeadlineOption.Standard, "Standard"), (DeadlineOption.Rush, "Rapide")], option, o =>
            {
                _options[offer.Id] = o;
                Refresh();
            }),
            Ui.Pair("Prix", Ui.Chf(price), Palette.Lemon),
            Ui.Pair("Délai", $"{workdays} jours ouvrés (le client rompt après {Tuning.Contracts.CancelAfterLateWorkdays} jours de retard)"),
            Ui.Pair("Acompte à la signature", Ui.Chf(System.Math.Round(price * (decimal)Tuning.Contracts.DepositShare))),
            Ui.Choice([(false, "Code jetable"), (true, "Capitaliser (+15 % de dev)")], capitalize, v =>
            {
                if (v)
                    _capitalize.Add(offer.Id);
                else
                    _capitalize.Remove(offer.Id);
                Refresh();
            }),
            Ui.Muted($"Expire le {GameTime.FormatDate(offer.OfferExpiresAt)}."),
            Ui.Row(Ui.Button("Accepter", () => Run(Campaign.AcceptContract(offer.Id, option, capitalize), $"Contrat signé avec {offer.Client}."), "green"),
                Ui.Button("Refuser", () => Run(Campaign.DeclineContract(offer.Id)), "red")));
        return Ui.Card(card);
    }

    private Control ActiveCard(Contract contract)
    {
        var items = Campaign.WorkFor(WorkOwner.Contract, contract.Id).ToList();
        double effort = items.Sum(w => w.Effort), progress = items.Sum(w => w.Progress);
        var team = Campaign.People.Where(p => p.Assignment.Is(AssignmentKind.Contract, contract.Id)).ToList();
        int left = GameTime.WorkdaysBetween(Campaign.Minute, contract.Deadline);
        double speed = team.Sum(p => Campaign.WorkRate(p, p.BestDiscipline) * 60 * 7);
        double need = (effort - progress) / System.Math.Max(0.1, speed);
        bool risky = team.Count == 0 || need > left;

        var disciplines = Ui.Column(2);
        foreach (var group in items.GroupBy(w => w.Discipline))
        {
            double done = group.Sum(w => w.Progress), total = group.Sum(w => w.Effort);
            disciplines.Children.Add(Ui.Row(Ui.Text(Person.DisciplineName(group.Key), 13, Palette.Fog), Ui.Bar(done / total, Palette.Sky, 6, 160), Ui.Muted($"{done:0}/{total:0}", 12)));
        }

        var card = Ui.Column(4,
            Ui.Row(Ui.Text(contract.Client, 16, Palette.Snow, bold: true), Ui.Tag(contract.Title, Palette.Sky), contract.ReworkRequested ? Ui.Tag("Corrections", Palette.Ember) : null),
            Ui.Bar(progress / System.Math.Max(1, effort), Palette.Leaf, 10),
            disciplines,
            Ui.Pair("Échéance", $"{GameTime.FormatDate(contract.Deadline)} ({(left >= 0 ? $"{left} j ouvrés" : $"{-left} j de RETARD")})", left < 0 ? Palette.Blood : left < 3 ? Palette.Lemon : null),
            Ui.Pair("Équipe", team.Count == 0 ? "personne !" : string.Join(", ", team.Select(p => p.Name)), team.Count == 0 ? Palette.Blood : null),
            Ui.Muted(team.Count == 0 ? "Affecte quelqu'un dans Équipe : sans personne, rien n'avance."
                : $"Au rythme actuel : ~{need:0.0} jours ouvrés restants. Goulot : {Campaign.Bottleneck(WorkOwner.Contract, contract.Id)}."),
            risky && team.Count > 0 ? Ui.Text("Risque de retard : renforce l'équipe ou passe en horaire étendu.", 13, Palette.Ember) : null,
            Ui.Row(Ui.Button("Affecter le fondateur", () => Run(Campaign.Assign(Campaign.Founder.Id, AssignmentKind.Contract, contract.Id))),
                Ui.Button("Abandonner", () => Screen.Confirm($"Abandonner {contract.Client} ?",
                    "La réputation baisse, et l'acompte correspondant au travail non fait doit être remboursé.", "Abandonner",
                    () => Run(Campaign.AbandonContract(contract.Id))), "red")));
        return Ui.Card(card, risky ? Palette.Ember : null);
    }
}
