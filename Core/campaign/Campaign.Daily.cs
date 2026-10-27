using Core.finance;
using Core.products;
using Core.team;
using Core.work;

namespace Core.campaign;

/// <summary>
/// Ce qui se passe chaque nuit à minuit : marché des produits, échéances, encaissements, demandes de clients,
/// moral et énergie, et la situation financière. Chaque événement périodique a une clé dans Processed :
/// il ne peut pas se déclencher deux fois, même après un rechargement.
/// </summary>
public partial class Campaign
{
    private void DailyUpdate()
    {
        long yesterday = Day - 1;
        var date = GameTime.ToDate(Minute);

        RunProducts(yesterday);
        SupportToday.Clear();

        if (date.Day == 1)
            StartMonth(GameTime.MonthKey(Minute - 1));
        if (date.Day == 25)
            IssuePayroll();
        if (date.DayOfWeek == DayOfWeek.Monday)
            StartWeek();

        CollectInvoices();
        PayDueBills();
        UpdateContracts();
        UpdateTeam();
        UpdateMarket();
        UpdateSolvency();
        CheckMilestones();
    }

    // ------------------------------------------------------------------ Produits

    private void RunProducts(long day)
    {
        foreach (var product in Products.Where(p => p.IsLive))
        {
            var result = Market.Step(product, new DayInputs(SupportToday.GetValueOrDefault(product.Id), Competitors), day);
            if (result.Revenue > 0)
            {
                if (product.Model == RevenueModel.Ads)
                    product.AdsReceivable += result.Revenue;
                else
                {
                    decimal fees = Math.Round(result.Revenue * (decimal)Tuning.Products.PaymentFees, 2);
                    var category = product.Model == RevenueModel.Commission ? Category.Commissions : Category.Subscriptions;
                    Post(result.Revenue - fees, category, $"{product.Name} : encaissements du jour", product.CompanyId);
                }
            }

            decimal hosting = product.Hosting.MonthlyCost / 30;
            decimal marketing = product.MarketingBudget / 30;
            Ledger.Accrue(Category.Hosting, hosting);
            Ledger.Accrue(Category.Marketing, marketing);
            result.Costs = Math.Round(hosting + marketing, 2);

            // Dette technique : sans maintenance, les bugs apparaissent peu à peu
            double weeklyNeed = product.Live!.ActiveModules().Sum(m => m.Def.Maintenance * m.Factor(c => c.EffortFactor));
            product.MaintenanceDebt = Math.Max(0, product.MaintenanceDebt + weeklyNeed / 7);
            double done = product.MaintenanceDone;
            product.MaintenanceDone = 0;
            double fixedBugs = Math.Min(product.Bugs, done / 3);
            product.Bugs -= fixedBugs;
            product.MaintenanceDebt = Math.Max(0, product.MaintenanceDebt - (done - fixedBugs * 3));
            double complexity = product.Live.ActiveModules().Sum(m => m.Def.Complexity);
            double bugChance = product.MaintenanceDebt * 0.03 + complexity * 0.004 * (1 + product.Users / 5000);
            if (Random.Chance(Math.Min(0.9, bugChance)))
                product.Bugs += 1;

            if (result.DowntimeHours > 0 && !Processed.Contains($"outage:{product.Id}:{Day / 3}"))
            {
                Processed.Add($"outage:{product.Id}:{Day / 3}");
                Notify(Severity.Warning, "produit", $"{product.Name} : panne de {result.DowntimeHours:0} h",
                    "Les serveurs ne suivent plus la charge. Passe à une offre d'hébergement plus grande ou optimise les performances.", $"product:{product.Id}");
            }
            else if (result.Performance < 1 && !Processed.Contains($"slow:{product.Id}:{Day / 7}"))
            {
                Processed.Add($"slow:{product.Id}:{Day / 7}");
                Notify(Severity.Warning, "produit", $"{product.Name} ralentit", $"Serveurs chargés à {Market.Load(product, product.Live) / product.Hosting.Capacity:P0}.", $"product:{product.Id}");
            }

            if (product.SupportBacklog > result.Tickets * 3 && result.Tickets > 3 && !Processed.Contains($"support:{product.Id}:{Day / 7}"))
            {
                Processed.Add($"support:{product.Id}:{Day / 7}");
                Notify(Severity.Warning, "produit", $"{product.Name} : {product.SupportBacklog:0} tickets en attente",
                    "Les utilisateurs sans réponse finissent par partir. Affecte quelqu'un au support.", $"product:{product.Id}");
            }

            // Délégation : correctifs créés automatiquement au-delà d'un seuil
            if (product.AutoFixThreshold > 0 && product.Bugs >= product.AutoFixThreshold && !WorkFor(WorkOwner.Product, product.Id).Any(w => w.Kind == "fix"))
            {
                PlanBugFixes(product.Id);
                Notify(Severity.Info, "produit", $"{product.Name} : correctifs planifiés automatiquement", $"Seuil de {product.AutoFixThreshold} bugs atteint.", $"product:{product.Id}");
            }
        }
    }

    // ------------------------------------------------------------------ Mois, semaine, paie

    private void StartMonth(string previousMonth)
    {
        string month = Month;
        long due5 = Minute + 4L * GameTime.MinutesPerDay;
        long due10 = Minute + 9L * GameTime.MinutesPerDay;

        if (Premises.Rent > 0 && Processed.Add($"rent:{month}"))
            AddBill($"Loyer {Premises.Name}", Category.Rent, Premises.Rent, due5);
        if (Processed.Add($"utilities:{month}"))
            AddBill("Internet, électricité, assurances", Category.Utilities, Tuning.Finance.Utilities + Premises.Rent * 0.04m, due10);

        foreach (var (category, amount) in Ledger.Accrued.Where(a => a.Value > 0.5m).ToList())
            if (Processed.Add($"accrued:{category}:{previousMonth}"))
                AddBill(category == Category.Hosting ? $"Hébergement {previousMonth}" : $"Marketing {previousMonth}", category, amount, due10);
        Ledger.Accrued.Clear();

        if (Ledger.Cash < 0 && Processed.Add($"interest:{month}"))
            AddBill("Intérêts du découvert", Category.Interest, -Ledger.Cash * Tuning.Finance.OverdraftMonthlyRate, due10);

        foreach (var loan in Ledger.Loans.ToList())
        {
            if (!Processed.Add($"loan:{loan.Id}:{month}"))
                continue;
            decimal payment = Math.Min(loan.MonthlyPayment, loan.Remaining);
            AddBill(loan.Kind == LoanKind.Bank ? "Remboursement du prêt bancaire" : "Échéance du plan de redressement", Category.LoanRepayment, payment, Minute + 14L * GameTime.MinutesPerDay);
            loan.Remaining -= payment;
            loan.MonthsLeft--;
            if (loan.Remaining <= 0.5m)
                Ledger.Loans.Remove(loan);
        }

        foreach (var product in Products.Where(p => p.AdsReceivable > 1))
            if (Processed.Add($"ads:{product.Id}:{previousMonth}"))
            {
                AddInvoice("Régie publicitaire", $"{product.Name} : publicité {previousMonth}", product.AdsReceivable, 30, 0.97, null, Category.Ads, product.CompanyId);
                product.AdsReceivable = 0;
            }

        Notify(Severity.Info, "finance", $"Nouveau mois : {GameTime.ToDate(Minute):MMMM yyyy}", MonthSummary(previousMonth), "finance");
    }

    public string MonthSummary(string month)
    {
        if (!Ledger.Months.TryGetValue(month, out var totals))
            return "Aucun mouvement le mois passé.";
        decimal income = totals.Where(t => Ledger.IsIncome(t.Key) && t.Key is not (Category.Funding or Category.LoanReceived or Category.DepositRefund)).Sum(t => t.Value);
        decimal spent = totals.Where(t => !Ledger.IsIncome(t.Key)).Sum(t => t.Value);
        return $"Recettes d'exploitation {income:N0} CHF, dépenses {-spent:N0} CHF.";
    }

    private void IssuePayroll()
    {
        string month = Month;
        long due = Minute + 12 * 60;
        if (Processed.Add($"draw:{month}"))
            AddBill($"Rémunération de {Founder.Name}", Category.FounderDraw, Tuning.Finance.FounderDraw, due);
        foreach (var person in Staff.Where(p => p.HiredAt <= Minute))
        {
            if (!Processed.Add($"salary:{person.Id}:{month}"))
                continue;
            // Premier mois : salaire au prorata des jours depuis l'arrivée
            double share = Math.Clamp((Minute - Math.Max(person.StartsAt, person.HiredAt)) / (30.0 * GameTime.MinutesPerDay), 0, 1);
            if (share > 0.02)
                AddBill($"Salaire de {person.Name}", Category.Salaries, person.Salary * (decimal)share, due, person.Id);
        }
    }

    private void StartWeek()
    {
        AutosaveDue = true;
        foreach (var person in People)
            person.PointsThisWeek = 0;
        Candidates.RemoveAll(c => c.ExpiresAt <= Minute);
        int wanted = 3 + Math.Min(3, Staff.Count() / 3);
        while (Candidates.Count < wanted)
            Candidates.Add(Recruitment.Generate(Random, Minute, NewId(), MainCompany.Reputation));
    }

    // ------------------------------------------------------------------ Encaissements et paiements

    private void CollectInvoices()
    {
        foreach (var invoice in Ledger.Invoices.Where(i => i.Open))
        {
            if (invoice.PaysAt is long paysAt && paysAt <= Minute)
            {
                invoice.PaidAt = Minute;
                Post(invoice.Amount, invoice.Category, $"Encaissé : {invoice.Label}", invoice.CompanyId);
            }
            else if (invoice.PaysAt == null && Minute > invoice.DueAt + 60L * GameTime.MinutesPerDay)
            {
                invoice.WrittenOff = true;
                Notify(Severity.Warning, "finance", $"Créance irrécouvrable : {invoice.Client}",
                    $"{invoice.Amount:N0} CHF ne seront jamais payés ({invoice.Label}).", "finance");
            }
            else if (Minute > invoice.DueAt && Processed.Add($"late-invoice:{invoice.Id}"))
            {
                Notify(Severity.Info, "finance", $"{invoice.Client} paie en retard", $"{invoice.Amount:N0} CHF attendus depuis le {GameTime.FormatDate(invoice.DueAt)}.", "finance");
            }
        }
        if (Ledger.Invoices.Count > 300)
            Ledger.Invoices.RemoveAll(i => !i.Open && i.IssuedAt < Minute - 365L * GameTime.MinutesPerDay);
    }

    /// <summary>Paie les dettes échues dans l'ordre de priorité, tant que le découvert autorisé le permet.</summary>
    public void PayDueBills()
    {
        foreach (var bill in Ledger.Bills.Where(b => b.Open && b.DueAt <= Minute).OrderBy(b => b.Priority).ThenBy(b => b.DueAt).ToList())
        {
            if (!Ledger.CanSpend(bill.Amount))
            {
                if (Processed.Add($"unpaid:{bill.Id}"))
                    Notify(Severity.Critical, "finance", $"Impayé : {bill.Label}",
                        $"{bill.Amount:N0} CHF n'ont pas pu être payés. Trouve de l'argent avant la mise en demeure.", "finance");
                continue;
            }
            bill.PaidAt = Minute;
            Post(-bill.Amount, bill.Category, bill.Label, bill.CompanyId);
            if (bill.PersonId is int id && FindPerson(id) is { } person)
                person.UnpaidDays = 0;
        }
        if (Ledger.Bills.Count > 400)
            Ledger.Bills.RemoveAll(b => !b.Open && b.IssuedAt < Minute - 365L * GameTime.MinutesPerDay);
    }

    // ------------------------------------------------------------------ Contrats

    private void UpdateContracts()
    {
        foreach (var offer in Offers.Where(o => o.OfferExpiresAt <= Minute).ToList())
        {
            offer.Status = ContractStatus.Expired;
            offer.Outcome = "Le client a choisi quelqu'un d'autre.";
        }

        foreach (var contract in ActiveContracts.ToList())
        {
            int late = GameTime.WorkdaysBetween(contract.Deadline, Minute);
            if (late == 1 && Processed.Add($"late:{contract.Id}"))
                Notify(Severity.Warning, "contrat", $"Retard : {contract.Client}",
                    $"Échéance dépassée. Pénalité de {Tuning.Contracts.LatePenaltyPerWorkday:P0} par jour ouvré ; le client rompt après {Tuning.Contracts.CancelAfterLateWorkdays} jours.", $"contract:{contract.Id}");
            if (late == Tuning.Contracts.CancelAfterLateWorkdays - 3 && Processed.Add($"late-warning:{contract.Id}"))
                Notify(Severity.Critical, "contrat", $"{contract.Client} menace de rompre",
                    "Encore 3 jours ouvrés de retard et le contrat est annulé sans paiement du solde.", $"contract:{contract.Id}");
            if (late >= Tuning.Contracts.CancelAfterLateWorkdays)
                CancelByClient(contract);
        }

        if (GameTime.IsWorkday(Minute) && Offers.Count() < Tuning.Contracts.MaxOpenOffers
            && Random.Chance(ContractMarket.DailyOfferChance(MainCompany.Reputation)))
        {
            var offer = ContractMarket.Generate(Random, NewId(), Minute, MainCompany.Reputation, TeamSpeedEstimate());
            offer.CompanyId = MainCompany.Id;
            Contracts.Add(offer);
            Notify(Severity.Info, "contrat", $"Nouvelle demande : {offer.Client}", $"{offer.Title}, {offer.Price:N0} CHF, {offer.DurationWorkdays} jours ouvrés.", $"contract:{offer.Id}");
        }

        // On garde l'historique récent, pas une liste qui grandit sans fin
        Contracts.RemoveAll(c => c.Status is ContractStatus.Expired or ContractStatus.Declined && c.OfferExpiresAt < Minute - 30L * GameTime.MinutesPerDay);
        if (Contracts.Count(c => c.Status is ContractStatus.Completed or ContractStatus.Cancelled) > 60)
        {
            var old = Contracts.Where(c => c.Status is ContractStatus.Completed or ContractStatus.Cancelled).OrderBy(c => c.AcceptedAt).First();
            Contracts.Remove(old);
        }
    }

    /// <summary>Points par jour qu'une personne seule abat à peu près (sert à calibrer les délais demandés).</summary>
    private double TeamSpeedEstimate() => Math.Max(5, WorkRate(Founder, Founder.BestDiscipline) * 60 * 7);

    // ------------------------------------------------------------------ Équipe

    private void UpdateTeam()
    {
        bool weekend = !GameTime.IsWorkday(Minute);
        double shared = space.Layout.SharedMorale(Premises);
        var overdueSalaries = Ledger.Overdue(Minute).Where(b => b.Category == Category.Salaries).Select(b => b.PersonId).ToHashSet();

        foreach (var person in People.ToList())
        {
            person.Activity = person.TrainingDiscipline != null ? Activity.Training : Activity.Away;
            person.Path.Clear();
            person.Position = Premises.Outside;
            person.Energy = Math.Min(100, person.Energy + (weekend ? Tuning.Work.EnergyWeekendRecovery : Tuning.Work.EnergyNightRecovery) * (person.Energy < 30 ? 0.8 : 1));

            if (overdueSalaries.Contains(person.Id))
                person.UnpaidDays++;

            var desk = Premises.DeskOf(person.Id);
            bool hasPost = desk != null && space.Layout.Check(Premises, desk) == space.WorkstationIssue.None;
            double target = 58 + Math.Min(10, shared) + (desk?.Def.Morale ?? 0);
            if (!person.IsFounder)
            {
                decimal market = 3_800m + (decimal)person.Skill(person.BestDiscipline) * 70m;
                target += Math.Clamp((double)((person.Salary - market) / market) * 40, -12, 12);
                target -= person.UnpaidDays * 4;
            }
            if (!hasPost && Minute >= person.StartsAt)
                target -= 15;
            if (person.Schedule == WorkSchedule.Extended)
                target -= 6;
            if (person.Energy < 30)
                target -= 10;
            person.Morale = Math.Clamp(person.Morale + (target - person.Morale) * 0.1, 0, 100);

            if (person.IsFounder)
                continue;

            person.LowMoraleDays = person.Morale < 25 ? person.LowMoraleDays + 1 : 0;
            if (person.LeavesAt == null && (person.LowMoraleDays >= 10 || person.UnpaidDays >= Tuning.Finance.UnpaidSalaryDaysBeforeQuitting))
            {
                person.LeavesAt = person.UnpaidDays >= Tuning.Finance.UnpaidSalaryDaysBeforeQuitting ? Minute : GameTime.AddWorkdays(Minute, 10);
                Notify(Severity.Critical, "équipe", $"{person.Name} démissionne",
                    person.UnpaidDays >= Tuning.Finance.UnpaidSalaryDaysBeforeQuitting
                        ? "Salaire impayé depuis trop longtemps : départ immédiat."
                        : $"Moral au plus bas. Départ le {GameTime.FormatDate(person.LeavesAt.Value)} sauf si ses conditions s'améliorent nettement.", $"person:{person.Id}");
            }
            else if (person.LeavesAt != null && person.LeavesAt > Minute && person.Morale > 45)
            {
                person.LeavesAt = null;
                Notify(Severity.Good, "équipe", $"{person.Name} reste finalement", "Son moral est remonté.", $"person:{person.Id}");
            }

            if (person.LeavesAt is long leaves && leaves <= Minute)
                RemovePerson(person, $"{person.Name} est partie·i.");
        }

        Candidates.RemoveAll(c => c.ExpiresAt <= Minute);
    }

    private void RemovePerson(Person person, string message)
    {
        People.Remove(person);
        foreach (var desk in Premises.Desks.Where(d => d.AssignedPersonId == person.Id))
            desk.AssignedPersonId = null;
        Notify(Severity.Warning, "équipe", message, "", "team");
    }

    // ------------------------------------------------------------------ Concurrence

    private void UpdateMarket()
    {
        foreach (var competitor in Competitors)
        {
            competitor.Appeal = Math.Min(0.75, competitor.Appeal + 0.0004);
            if (competitor.UpgradeAt is long at && at <= Minute)
            {
                competitor.Appeal = Math.Min(0.8, competitor.Appeal + competitor.UpgradeGain);
                competitor.UpgradeAt = null;
                if (Products.Any(p => p.IsLive && p.SegmentId == competitor.SegmentId))
                    Notify(Severity.Warning, "marché", $"{competitor.Name} sort sa nouvelle version",
                        $"Son attrait passe à {competitor.Appeal:P0} sur « {ProductCatalog.Segment(competitor.SegmentId).Name} ».", "market");
            }
            else if (competitor.UpgradeAt == null && Random.Chance(1 / 120.0))
            {
                competitor.UpgradeAt = Minute + Random.Next(10, 21) * (long)GameTime.MinutesPerDay;
                competitor.UpgradeGain = Math.Round(Random.Range(0.04, 0.1), 2);
                if (Products.Any(p => p.IsLive && p.SegmentId == competitor.SegmentId))
                    Notify(Severity.Info, "marché", $"Rumeur : {competitor.Name} prépare une nouvelle version",
                        $"Sortie prévue vers le {GameTime.FormatDate(competitor.UpgradeAt.Value)}. Prépare une réponse (qualité, fonctions, prix).", "market");
            }
        }

        if (Investor is { } offer && offer.ExpiresAt <= Minute)
            Investor = null;
        if (!InvestorRoundDone && Investor == null && Day % 7 == 0)
            OfferInvestment();
    }

    // ------------------------------------------------------------------ Situation financière

    /// <summary>Charges fixes mensuelles : salaires, rémunération, loyer, services, hébergement, marketing, prêts.</summary>
    public decimal MonthlyFixedCosts =>
        Tuning.Finance.FounderDraw + Tuning.Finance.Utilities + Premises.Rent * 1.04m
        + Staff.Sum(p => p.Salary)
        + Products.Sum(p => p.Hosting.MonthlyCost + p.MarketingBudget)
        + Ledger.Loans.Sum(l => Math.Min(l.MonthlyPayment, l.Remaining));

    /// <summary>Revenus récurrents des 30 derniers jours (produits uniquement : les prestations ne sont pas récurrentes).</summary>
    public decimal RecurringRevenue30 =>
        Products.Sum(p => p.History.Skip(Math.Max(0, p.History.Count - 30)).Sum(d => d.Revenue));

    /// <summary>
    /// Jours d'autonomie estimés : trésorerie et découvert, plus les factures clients attendues dans les 30 jours
    /// (seulement si leur client est fiable), divisés par la consommation nette quotidienne.
    /// </summary>
    public (double Days, string Assumptions) Runway()
    {
        decimal burn = MonthlyFixedCosts - RecurringRevenue30;
        decimal soon = Ledger.Invoices.Where(i => i.Open && i.DueAt <= Minute + 30L * GameTime.MinutesPerDay && i.DueAt >= Minute - 15L * GameTime.MinutesPerDay).Sum(i => i.Amount);
        decimal available = Ledger.Cash + Ledger.Overdraft - Ledger.OpenBills + soon * 0.85m;
        string assumptions = $"Trésorerie {Ledger.Cash:N0} + découvert {Ledger.Overdraft:N0} − dettes ouvertes {Ledger.OpenBills:N0} + 85 % des factures attendues sous 30 jours ({soon:N0}). " +
                             $"Consommation : charges fixes {MonthlyFixedCosts:N0}/mois − revenus récurrents {RecurringRevenue30:N0}/mois. Les prestations futures ne sont pas comptées.";
        if (burn <= 0)
            return (double.PositiveInfinity, assumptions);
        return (Math.Max(0, (double)(available / (burn / 30))), assumptions);
    }

    private void UpdateSolvency()
    {
        var overdue = Ledger.Overdue(Minute).ToList();
        var previous = Solvency.State;

        if (overdue.Count == 0)
        {
            if (Solvency.State == SolvencyState.Notice)
                Notify(Severity.Good, "finance", "Mise en demeure levée", "Toutes les dettes échues sont payées.", "finance");
            Solvency.NoticeUntil = null;
            Solvency.State = Runway().Days < 30 || Ledger.Cash < 0 ? SolvencyState.Tight : SolvencyState.Healthy;
            if (Solvency.State == SolvencyState.Tight && previous == SolvencyState.Healthy)
                Notify(Severity.Warning, "finance", "Trésorerie tendue", "Moins de 30 jours d'autonomie au rythme actuel.", "finance");
            return;
        }

        long oldest = overdue.Min(b => b.DueAt);
        decimal total = overdue.Sum(b => b.Amount);
        int overdueDays = (int)((Minute - oldest) / GameTime.MinutesPerDay);

        if (Solvency.State == SolvencyState.Notice && Solvency.NoticeUntil is long until)
        {
            if (Minute >= until)
                Liquidate(total, overdue);
            else if ((until - Minute) / GameTime.MinutesPerDay is 10 or 3 && Processed.Add($"notice-left:{until}:{(until - Minute) / GameTime.MinutesPerDay}"))
                Notify(Severity.Critical, "finance", $"Liquidation dans {(until - Minute) / GameTime.MinutesPerDay} jours",
                    $"Il reste {total:N0} CHF d'impayés à régulariser.", "finance");
            return;
        }

        Solvency.State = SolvencyState.Overdue;
        if (overdueDays >= Tuning.Finance.OverdueDaysBeforeNotice && total >= 500)
        {
            Solvency.State = SolvencyState.Notice;
            Solvency.NoticeUntil = Minute + Tuning.Finance.NoticeDays * (long)GameTime.MinutesPerDay;
            Notify(Severity.Critical, "finance", "Mise en demeure",
                $"{total:N0} CHF impayés depuis {overdueDays} jours. Sans régularisation avant le {GameTime.FormatDate(Solvency.NoticeUntil.Value)}, l'entreprise sera liquidée. " +
                "Pistes : encaisser, vendre du matériel, réduire les charges, licencier, plan de redressement.", "finance");
        }
    }

    private void Liquidate(decimal overdueTotal, List<Bill> overdue)
    {
        Solvency.State = SolvencyState.Liquidated;
        Status = CampaignStatus.Liquidated;
        var lines = new List<string>
        {
            $"Liquidation prononcée le {GameTime.FormatDate(Minute)}, après {Day} jours d'activité.",
            $"Impayés au moment de la liquidation : {overdueTotal:N0} CHF ({string.Join(", ", overdue.Select(b => b.Label).Distinct().Take(5))}).",
            $"Trésorerie : {Ledger.Cash:N0} CHF (découvert autorisé {Ledger.Overdraft:N0}). Créances ouvertes : {Ledger.OpenReceivables:N0} CHF. Prêts restants : {Ledger.LoanDebt:N0} CHF.",
            $"Charges fixes mensuelles : {MonthlyFixedCosts:N0} CHF, revenus récurrents : {RecurringRevenue30:N0} CHF par mois.",
            $"Équipe au moment de la liquidation : {People.Count} personne(s). Produits publiés : {Products.Count(p => p.IsLive)}. Contrats livrés : {Contracts.Count(c => c.Status == ContractStatus.Completed)}.",
        };
        Solvency.LiquidationReport = string.Join("\n", lines);
        Notify(Severity.Critical, "finance", "Entreprise liquidée", Solvency.LiquidationReport, "finance");
    }

    // ------------------------------------------------------------------ Jalons

    public static readonly (string Id, string Name, string Description)[] MilestoneDefs =
    [
        ("first_contract", "Premier client", "Livrer une première prestation."),
        ("first_launch", "En ligne", "Publier la première version d'un produit."),
        ("first_hire", "On est deux", "Embaucher une première personne."),
        ("office", "De vrais locaux", "Quitter l'appartement pour un bureau."),
        ("paying100", "Cent clients", "Avoir 100 clients payants sur un produit."),
        ("mrr10k", "10 000 CHF récurrents", "Dépasser 10 000 CHF de revenus récurrents sur 30 jours."),
        ("team10", "Une vraie équipe", "Être 10 dans l'entreprise."),
        ("users10k", "Dix mille utilisateurs", "Atteindre 10 000 utilisateurs actifs sur un produit."),
    ];

    private void CheckMilestones()
    {
        void Reach(string id, bool condition)
        {
            if (!condition || Milestones.ContainsKey(id))
                return;
            Milestones[id] = Minute;
            var def = MilestoneDefs.First(m => m.Id == id);
            Notify(Severity.Good, "entreprise", $"Jalon atteint : {def.Name}", def.Description, "company");
        }

        Reach("first_contract", Contracts.Any(c => c.Status == ContractStatus.Completed));
        Reach("first_launch", Products.Any(p => p.IsLive));
        Reach("first_hire", Staff.Any());
        Reach("office", Premises.ListingId != space.RealEstate.Apartment);
        Reach("paying100", Products.Any(p => p.Paying >= 100));
        Reach("mrr10k", RecurringRevenue30 >= 10_000);
        Reach("team10", People.Count >= 10);
        Reach("users10k", Products.Any(p => p.Users >= 10_000));
    }
}
