using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Core.campaign;
using Core.finance;
using UI.Pixel;

namespace UI.Hud.Panels;

/// <summary>
/// Finances : d'abord l'argent, la tendance du mois, les grosses dépenses et la marge. Les créances, dettes, tableaux
/// mensuels et financements sont repliés dans les détails.
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
        decimal monthlyBalance = Campaign.RecurringRevenue30 - Campaign.MonthlyFixedCosts;

        var stats = new WrapPanel();
        stats.Children.Add(Ui.Stat("Argent", Ui.Chf(ledger.Cash), Ui.Signed(ledger.Cash), 190));
        stats.Children.Add(Ui.Stat("Chaque mois", Ui.SignedChf(monthlyBalance), Ui.Signed(monthlyBalance), 190, "Revenus des projets (30 derniers jours) moins dépenses fixes."));
        stats.Children.Add(Ui.Stat("Revenus / mois", Ui.Chf(Campaign.RecurringRevenue30), Palette.Leaf, 190));
        stats.Children.Add(Ui.Stat("Dépenses / mois", Ui.Chf(Campaign.MonthlyFixedCosts), Palette.Ember, 190));
        stats.Children.Add(Ui.Stat("Tiens encore", double.IsInfinity(runway) ? "sans limite" : $"{runway:0} jours", runway < 60 ? Palette.Ember : null, 190, assumptions));

        var column = Ui.Column(10, stats);
        var situation = Situation();
        if (situation != null)
            column.Children.Add(situation);

        column.Children.Add(Ui.Section("Où va l'argent chaque mois", Costs()));
        column.Children.Add(Ui.Section("D'où il vient", Incomes()));
        column.Children.Add(Ui.Details("Détails : factures, mois par mois, financement", Ui.Column(10, Bills(), MonthTable(), Funding())));
        return column;
    }

    private Control? Situation()
    {
        var s = Campaign.Solvency;
        if (s.State == SolvencyState.Healthy)
            return null;
        var (text, color) = s.State switch
        {
            SolvencyState.Tight => ("L'argent va manquer : moins de 30 jours devant toi.", Palette.Lemon),
            SolvencyState.Overdue => ("Des factures n'ont pas pu être payées. Au bout de 20 jours, c'est la mise en demeure.", Palette.Ember),
            SolvencyState.Notice => ($"Mise en demeure : tout payer avant le {GameTime.FormatDate(s.NoticeUntil ?? Campaign.Minute)}, sinon faillite.", Palette.Blood),
            _ => ("Faillite.", Palette.Blood)
        };
        var actions = Ui.Muted("Pour réagir : baisser le marketing (Projets → Réglages), fermer un projet qui perd de l'argent, " +
                               "revendre du matériel (Aménagement), réduire l'équipe ou déménager plus petit.", 13);
        var column = Ui.Column(6, Ui.Text(text, 16, color, bold: true), actions);
        if (s.State is SolvencyState.Overdue or SolvencyState.Notice)
        {
            var (allowed, condition) = Campaign.RecoveryPlanTerms();
            column.Children.Add(Ui.Row(8, Ui.Button("Plan de redressement", () => Screen.Confirm("Plan de redressement", condition, "Demander",
                () => Run(Campaign.RequestRecoveryPlan()), danger: false), "gold", allowed), Ui.Muted(condition, 12)));
        }
        return Ui.Card(column, color);
    }

    private Control Costs()
    {
        var items = new List<(string Label, decimal Amount)>
        {
            ("Ta rémunération", Tuning.Finance.FounderDraw),
            ("Salaires", Campaign.Staff.Sum(p => p.Salary)),
            ("Loyer et charges", Campaign.Premises.Rent * 1.04m + Tuning.Finance.Utilities),
            ("Serveurs", Campaign.Products.Where(p => p.IsLive).Sum(p => p.Hosting.MonthlyCost)),
            ("Marketing", Campaign.Products.Sum(p => p.MarketingBudget)),
            ("Remboursements", Campaign.Ledger.Loans.Sum(l => Math.Min(l.MonthlyPayment, l.Remaining))),
        };
        return Bars(items.Where(i => i.Amount > 0).OrderByDescending(i => i.Amount).ToList(), Palette.Ember);
    }

    private Control Incomes()
    {
        var items = Campaign.Products.Select(p => (p.Name, p.History.Skip(Math.Max(0, p.History.Count - 30)).Sum(d => d.Revenue))).Where(i => i.Item2 > 0).OrderByDescending(i => i.Item2).ToList();
        return items.Count == 0 ? Ui.Muted("Aucun revenu pour l'instant : sors un premier projet.", 13) : Bars(items, Palette.Leaf);
    }

    private static Control Bars(List<(string Label, decimal Amount)> items, IBrush color)
    {
        decimal max = items.Count == 0 ? 1 : items.Max(i => i.Amount);
        var column = Ui.Column(4);
        foreach (var (label, amount) in items)
        {
            var row = new Grid { ColumnDefinitions = new ColumnDefinitions("180,*,110") };
            row.Children.Add(Ui.Text(label, 14, Palette.Cloud, wrap: false));
            var bar = Ui.Bar((double)(amount / Math.Max(1, max)), color, 10);
            bar.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center;
            Grid.SetColumn(bar, 1);
            row.Children.Add(bar);
            var value = Ui.Text(Ui.Chf(amount), 14, Palette.Snow, wrap: false);
            value.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right;
            Grid.SetColumn(value, 2);
            row.Children.Add(value);
            column.Children.Add(row);
        }
        return column;
    }

    private Control Bills()
    {
        var list = Ui.Column(2);
        foreach (var bill in Campaign.Ledger.Bills.Where(b => b.Open).OrderBy(b => b.DueAt).Take(10))
            list.Children.Add(Ui.Pair($"{GameTime.FormatDate(bill.DueAt)} · {bill.Label}", Ui.Chf(bill.Amount), bill.DueAt < Campaign.Minute ? Palette.Blood : null));
        foreach (var invoice in Campaign.Ledger.Invoices.Where(i => i.Open).OrderBy(i => i.DueAt).Take(6))
            list.Children.Add(Ui.Pair($"{GameTime.FormatDate(invoice.DueAt)} · à recevoir : {invoice.Label}", Ui.Chf(invoice.Amount), Palette.Sky));
        if (list.Children.Count == 0)
            list.Children.Add(Ui.Muted("Rien à payer ni à recevoir.", 13));
        return Ui.Section("À payer et à recevoir", list, Ui.Muted("Tout est payé automatiquement à l'échéance, s'il y a l'argent.", 12));
    }

    private Control MonthTable()
    {
        var months = Campaign.Ledger.Months.Keys.OrderByDescending(k => k).Take(4).ToList();
        if (months.Count == 0)
            return Ui.Muted("Aucun mois terminé.", 13);
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
        AddRow("Mois", _ => null, true);
        foreach (var category in Enum.GetValues<Category>().Where(c => months.Any(m => Campaign.Ledger.Months[m].GetValueOrDefault(c) != 0)))
            AddRow(CategoryName(category), m => Campaign.Ledger.Months[m].GetValueOrDefault(category));
        AddRow("Résultat", m => Campaign.OperatingResult(Campaign.Ledger.Months[m]), true);
        return Ui.Section("Mois par mois", grid);
    }

    public static string CategoryName(Category category) => category switch
    {
        Category.Contracts => "Prestations (ancienne version)",
        Category.Subscriptions => "Abonnements",
        Category.Ads => "Publicité",
        Category.Commissions => "Commissions",
        Category.Sales => "Ventes",
        Category.Funding => "Levée de fonds",
        Category.LoanReceived => "Prêt reçu",
        Category.Resale => "Reventes",
        Category.DepositRefund => "Garantie rendue",
        Category.Salaries => "Salaires",
        Category.FounderDraw => "Ta rémunération",
        Category.Rent => "Loyer",
        Category.Utilities => "Charges",
        Category.Equipment => "Matériel",
        Category.Hosting => "Serveurs",
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

    private Control Funding()
    {
        var column = Ui.Column(6);
        var (max, condition) = Campaign.LoanTerms();
        if (_loan <= 0 || _loan > max)
            _loan = max;
        column.Children.Add(Ui.Text("Prêt bancaire", 14, Palette.Snow, bold: true));
        column.Children.Add(Ui.Muted(condition, 13));
        if (max > 0)
            column.Children.Add(Ui.Button($"Emprunter {_loan:N0} CHF", () => Screen.Confirm("Emprunter ?",
                $"{_loan:N0} CHF tout de suite, puis {_loan * (1 + Tuning.Finance.LoanYearlyRate) / Tuning.Finance.LoanMonths:N0} CHF par mois pendant {Tuning.Finance.LoanMonths} mois.", "Emprunter",
                () => Run(Campaign.TakeLoan(_loan))), "gold"));
        foreach (var loan in Campaign.Ledger.Loans)
            column.Children.Add(Ui.Pair(loan.Kind == LoanKind.Bank ? "Prêt en cours" : "Plan de redressement", $"{loan.Remaining:N0} CHF ({loan.MonthlyPayment:N0}/mois)"));
        column.Children.Add(Ui.Text("Investisseurs", 14, Palette.Snow, bold: true));
        if (Campaign.Investor is { } offer)
            column.Children.Add(Ui.Card(Ui.Column(4,
                Ui.Text($"{offer.Investor} propose {offer.Amount:N0} CHF contre {offer.Equity:P0} de l'entreprise.", 14, Palette.Lemon),
                Ui.Row(Ui.Button("Accepter", () => Screen.Confirm("Lever des fonds ?", $"Ta part passe à {Campaign.FounderEquity * (1 - offer.Equity):P0}.", "Accepter",
                    () => Run(Campaign.AcceptInvestor()), danger: false), "green"), Ui.Button("Refuser", () => { Campaign.DeclineInvestor(); Refresh(); }, "red"))), Palette.Gold));
        else
            column.Children.Add(Ui.Muted(Campaign.InvestorRoundDone ? "Levée déjà faite." : "Un investisseur viendra si un projet décolle (1 500 utilisateurs ou 100 payants, +20 % en un mois).", 13));
        return Ui.Section("Financement", column);
    }
}
