using System.Collections.Generic;
using Avalonia.Media;
using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Core.campaign;
using Core.finance;
using UI.Pixel;

namespace UI.Hud.Panels;

/// <summary>
/// Finances : trésorerie, créances et dettes séparées, résultat mensuel, autonomie (avec ses hypothèses),
/// situation de solvabilité et leviers de redressement.
/// </summary>
public class FinancePanel : GamePanel
{
    public override string Title => "Finances";
    public override bool Large => true;

    private decimal _loan;

    public override Control Build()
    {
        var ledger = Campaign.Ledger;
        var (runway, assumptions) = Campaign.Runway();
        string lastMonth = GameTime.ToDate(Campaign.Minute).AddMonths(-1).ToString("yyyy-MM");

        var kpis = new WrapPanel();
        void Kpi(string label, string value, IBrush? color = null, string? tip = null)
        {
            var panel = new PixelPanel { Skin = "inset", Padding = new Thickness(10, 6), Margin = new Thickness(0, 0, 6, 6), Width = 190, Child = Ui.Column(0, Ui.Muted(label, 12), Ui.Number(value, 22, color)) };
            if (tip != null)
                ToolTip.SetTip(panel, tip);
            kpis.Children.Add(panel);
        }
        Kpi("Trésorerie", Ui.Chf(ledger.Cash), Ui.Signed(ledger.Cash), $"Découvert autorisé : {ledger.Overdraft:N0} CHF");
        Kpi("Créances clients", Ui.Chf(ledger.OpenReceivables), Palette.Sky, "Factures émises mais pas encore payées : ce n'est pas de l'argent disponible.");
        Kpi("Dettes à payer", Ui.Chf(ledger.OpenBills), Palette.Ember);
        Kpi("Prêts restants", Ui.Chf(ledger.LoanDebt));
        Kpi("Charges fixes / mois", Ui.Chf(Campaign.MonthlyFixedCosts));
        Kpi("Revenus récurrents 30 j", Ui.Chf(Campaign.RecurringRevenue30), Palette.Leaf);
        Kpi("Autonomie", double.IsInfinity(runway) ? "rentable" : $"{runway:0} jours", runway < 30 ? Palette.Ember : null, assumptions);
        Kpi("Valeur estimée", Ui.Chf(Campaign.Valuation()), null,
            $"Actif net plus 3 ans du bénéfice récurrent actuel des produits. Ta part : {Campaign.FounderEquity:P0}.");

        var left = Ui.Column(8, kpis, Ui.Muted("Autonomie : " + assumptions, 12), Solvency(), MonthTable(lastMonth));
        var right = Ui.Column(8, Bills(), Invoices(), Funding());

        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,16,*") };
        grid.Children.Add(left);
        Grid.SetColumn(right, 2);
        grid.Children.Add(right);
        return grid;
    }

    private Control Solvency()
    {
        var s = Campaign.Solvency;
        var overdue = Campaign.Ledger.Overdue(Campaign.Minute).ToList();
        IBrush color = s.State switch
        {
            SolvencyState.Healthy => Palette.Leaf,
            SolvencyState.Tight => Palette.Lemon,
            SolvencyState.Overdue => Palette.Ember,
            _ => Palette.Blood
        };
        string explanation = s.State switch
        {
            SolvencyState.Healthy => "Tout est payé et plus d'un mois d'autonomie.",
            SolvencyState.Tight => "Tout est payé, mais l'autonomie est courte ou le compte est dans le découvert.",
            SolvencyState.Overdue => $"{overdue.Sum(b => b.Amount):N0} CHF échus et impayés. Au-delà de {Tuning.Finance.OverdueDaysBeforeNotice} jours de retard, une mise en demeure est envoyée.",
            SolvencyState.Notice => $"Mise en demeure : tout régulariser avant le {GameTime.FormatDate(s.NoticeUntil ?? Campaign.Minute)}, sinon liquidation.",
            _ => "L'entreprise a été liquidée.",
        };
        var column = Ui.Column(4,
            Ui.Row(Ui.Text("Situation :", 15, Palette.Fog), Ui.Tag(Core.finance.Solvency.Name(s.State), color)),
            Ui.Text(explanation, 14),
            Ui.Muted("Règle : un solde négatif n'est pas grave tant que tout est payé. Une obligation impayée (salaire, loyer, facture, échéance) ouvre la procédure : " +
                     $"impayés → mise en demeure après {Tuning.Finance.OverdueDaysBeforeNotice} jours → liquidation {Tuning.Finance.NoticeDays} jours plus tard si rien n'est régularisé. " +
                     $"Les salariés impayés depuis {Tuning.Finance.UnpaidSalaryDaysBeforeQuitting} jours démissionnent.", 12));
        if (s.State is SolvencyState.Overdue or SolvencyState.Notice)
        {
            var (allowed, condition) = Campaign.RecoveryPlanTerms();
            column.Children.Add(Ui.Section("Leviers de redressement",
                Ui.Muted("Encaisser (relancer des prestations), revendre du matériel (Aménagement), réduire marketing et hébergement, licencier (avec indemnité), " +
                         "déménager dans plus petit, ou demander un plan de redressement.", 13),
                Ui.Text(condition, 13, allowed ? Palette.Cloud : Palette.Ember),
                Ui.Button("Demander un plan de redressement", () => Screen.Confirm("Plan de redressement", condition + " Un seul plan tous les deux ans.", "Demander",
                    () => Run(Campaign.RequestRecoveryPlan()), danger: false), "gold", allowed)));
        }
        return Ui.Card(column, color);
    }

    private Control MonthTable(string lastMonth)
    {
        var months = Campaign.Ledger.Months.Keys.OrderByDescending(k => k).Take(4).ToList();
        if (months.Count == 0)
            return Ui.Muted("Aucun mouvement pour l'instant.");
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*" + string.Concat(Enumerable.Repeat(",110", months.Count))) };
        int row = 0;
        void AddRow(string label, Func<string, decimal?> value, bool bold = false)
        {
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            var title = Ui.Text(label, 13, bold ? Palette.Snow : Palette.Fog, bold);
            Grid.SetRow(title, row);
            grid.Children.Add(title);
            for (int i = 0; i < months.Count; i++)
            {
                decimal? v = value(months[i]);
                var cell = Ui.Text(v == null ? months[i] : $"{v:N0}", 13, v == null ? Palette.Gold : Ui.Signed(v.Value), bold, wrap: false);
                cell.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right;
                Grid.SetRow(cell, row);
                Grid.SetColumn(cell, i + 1);
                grid.Children.Add(cell);
            }
            row++;
        }
        decimal Get(string month, Category category) => Campaign.Ledger.Months[month].GetValueOrDefault(category);
        AddRow("Mois", _ => null, true);
        foreach (var category in Enum.GetValues<Category>().Where(c => months.Any(m => Get(m, c) != 0)))
            AddRow(CategoryName(category), m => Get(m, category));
        AddRow("Résultat d'exploitation", m => Campaign.Ledger.Months[m]
            .Where(t => t.Key is not (Category.Funding or Category.LoanReceived or Category.DepositRefund or Category.Deposit or Category.LoanRepayment)).Sum(t => t.Value), true);
        AddRow("Variation de trésorerie", m => Campaign.Ledger.Months[m].Values.Sum(), true);
        return Ui.Section("Mois par mois (mouvements réels)", grid,
            Ui.Muted("Le résultat d'exploitation exclut financements, prêts et garanties.", 12));
    }

    public static string CategoryName(Category category) => category switch
    {
        Category.Contracts => "Prestations",
        Category.Subscriptions => "Abonnements",
        Category.Ads => "Publicité",
        Category.Commissions => "Commissions",
        Category.Funding => "Levée de fonds",
        Category.LoanReceived => "Prêt reçu",
        Category.Resale => "Reventes",
        Category.DepositRefund => "Garantie rendue",
        Category.Salaries => "Salaires",
        Category.FounderDraw => "Rémunération fondateur",
        Category.Rent => "Loyer",
        Category.Utilities => "Charges et services",
        Category.Equipment => "Matériel",
        Category.Hosting => "Hébergement",
        Category.Marketing => "Marketing",
        Category.Training => "Formations",
        Category.Severance => "Indemnités",
        Category.Interest => "Intérêts",
        Category.LoanRepayment => "Remboursements",
        Category.Deposit => "Garantie versée",
        Category.Moving => "Déménagement",
        Category.Fees => "Frais",
        Category.Refund => "Remboursements clients",
        _ => "Registre du commerce"
    };

    private Control Bills()
    {
        var open = Campaign.Ledger.Bills.Where(b => b.Open).OrderBy(b => b.DueAt).Take(12).ToList();
        var list = Ui.Column(2);
        foreach (var bill in open)
        {
            bool late = bill.DueAt < Campaign.Minute;
            list.Children.Add(Ui.Pair($"{GameTime.FormatDate(bill.DueAt)} · {bill.Label}", Ui.Chf(bill.Amount), late ? Palette.Blood : null));
        }
        if (open.Count == 0)
            list.Children.Add(Ui.Muted("Rien à payer pour l'instant."));
        var upcoming = Ui.Muted($"Prochaines échéances fixes : loyer le 1er (payable le 5), salaires et rémunération le 25, hébergement et marketing du mois écoulé le 1er (payables le 10).", 12);
        return Ui.Section("Dettes à payer (échéance)", list, upcoming);
    }

    private Control Invoices()
    {
        var open = Campaign.Ledger.Invoices.Where(i => i.Open).OrderBy(i => i.DueAt).Take(12).ToList();
        var list = Ui.Column(2);
        foreach (var invoice in open)
        {
            bool late = invoice.DueAt < Campaign.Minute;
            list.Children.Add(Ui.Pair($"{GameTime.FormatDate(invoice.DueAt)} · {invoice.Client} · {invoice.Label}", Ui.Chf(invoice.Amount) + (late ? " (en retard)" : ""), late ? Palette.Ember : Palette.Sky));
        }
        if (open.Count == 0)
            list.Children.Add(Ui.Muted("Aucune facture en attente."));
        return Ui.Section("Factures clients en attente (échéance)", list, Ui.Muted("Un client peu fiable peut payer tard, parfois jamais (créance irrécouvrable après 60 jours).", 12));
    }

    private Control Funding()
    {
        var column = Ui.Column(6);
        var (max, condition) = Campaign.LoanTerms();
        if (_loan <= 0 || _loan > max)
            _loan = max;
        column.Children.Add(Ui.Text("Prêt bancaire", 14, Palette.Snow, bold: true));
        column.Children.Add(Ui.Muted(condition, 13));
        if (max > 0)
        {
            var amounts = new[] { 0.25m, 0.5m, 1m }.Select(f => Math.Floor(max * f / 500m) * 500m).Where(a => a >= 500).Distinct();
            column.Children.Add(Ui.Choice(amounts.Select(a => (a, Ui.Chf(a))), _loan, a => { _loan = a; Refresh(); }));
            column.Children.Add(Ui.Button($"Emprunter {_loan:N0} CHF", () => Screen.Confirm("Emprunter ?",
                $"{_loan:N0} CHF tout de suite, remboursés en {Tuning.Finance.LoanMonths} mensualités avec {Tuning.Finance.LoanYearlyRate:P0} d'intérêts par an.", "Emprunter",
                () => Run(Campaign.TakeLoan(_loan))), "gold"));
        }
        foreach (var loan in Campaign.Ledger.Loans)
            column.Children.Add(Ui.Pair(loan.Kind == LoanKind.Bank ? "Prêt bancaire" : "Plan de redressement", $"{loan.Remaining:N0} CHF, {loan.MonthlyPayment:N0}/mois"));

        column.Children.Add(Ui.Text("Investisseurs", 14, Palette.Snow, bold: true));
        if (Campaign.Investor is { } offer)
        {
            column.Children.Add(Ui.Card(Ui.Column(4,
                Ui.Text($"{offer.Investor} propose {offer.Amount:N0} CHF contre {offer.Equity:P0} du capital.", 14, Palette.Lemon),
                Ui.Muted($"Valable jusqu'au {GameTime.FormatDate(offer.ExpiresAt)}. Une seule levée possible.", 12),
                Ui.Row(Ui.Button("Accepter", () => Screen.Confirm("Lever des fonds ?", $"Ta part passera de {Campaign.FounderEquity:P0} à {Campaign.FounderEquity * (1 - offer.Equity):P0}.", "Accepter",
                        () => Run(Campaign.AcceptInvestor()), danger: false), "green"),
                    Ui.Button("Refuser", () => { Campaign.DeclineInvestor(); Refresh(); }, "red"))), Palette.Gold));
        }
        else
            column.Children.Add(Ui.Muted(Campaign.InvestorRoundDone
                ? "Levée de fonds déjà faite."
                : "Un investisseur se manifeste quand un produit a 1 500 utilisateurs actifs ou 100 clients payants et a grandi de 20 % en 30 jours.", 13));
        return Ui.Section("Financement", column);
    }
}
