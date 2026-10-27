using Core.finance;
using Core.products;
using Core.team;

namespace Core.campaign;

/// <summary>
/// Ce qui se passe chaque nuit à minuit : marché des projets, avis, encaissements, échéances, équipe,
/// points de développement et situation financière. Chaque événement périodique a une clé dans Processed :
/// il ne peut pas se déclencher deux fois, même après un rechargement.
/// </summary>
public partial class Campaign
{
    private void DailyUpdate()
    {
        long yesterday = Day - 1;
        var date = GameTime.ToDate(Minute);

        RunInfrastructure();
        RunSupply();
        PayResearch();
        ChannelFees();
        RunProducts(yesterday, date.DayOfWeek == DayOfWeek.Monday);
        AutoLaunch();

        if (date.Day == 1)
            StartMonth(GameTime.MonthKey(Minute - 1));
        if (date.Day == 25)
            IssuePayroll();
        if (date.DayOfWeek == DayOfWeek.Monday)
        {
            StartWeek();
            AutoImproveProjects();
        }

        CollectInvoices();
        PayDueBills();
        UpdateTeam();
        UpdateMarket();
        UpdateSolvency();
        CheckMilestones();
    }

    // ------------------------------------------------------------------ Projets

    private void RunProducts(long day, bool weekly)
    {
        bool optimization = IsUnlocked("optimization");
        foreach (var product in Products)
        {
            if (!product.IsLive)
            {
                Market.PreLaunch(product, IsUnlocked("launches"));
                decimal preMarketing = product.MarketingBudget / 30;
                if (preMarketing > 0)
                    Ledger.Accrue(Category.Marketing, preMarketing);
                // Sans employé, c'est au fondateur de travailler au terminal : pas d'alerte, l'aide du projet le dit
                if (Staff.Any() && !TeamOf(product).Any() && product.Queue.Count > 0)
                    Notify(Severity.Warning, "projet", $"{product.Name} n'avance pas", "Aucun employé dessus : affecte quelqu'un à ce projet, ou travaille au terminal.",
                        $"product:{product.Id}", key: $"noteam:{product.Id}");
                else
                    Resolve($"noteam:{product.Id}");
                continue;
            }

            if (!product.IsHardware)
                AdjustHosting(product);
            var inputs = new DayInputs(product.SupportDone, Competitors, optimization, IsUnlocked("loyalty")) { Sales = product.IsHardware ? SalesOf(product) : null };
            var result = Market.Step(product, inputs, day);
            product.SupportDone = 0;
            if (product.IsHardware)
                SettleHardware(product, result);
            if (result.Revenue > 0)
            {
                if (product.Model == RevenueModel.Ads)
                    product.AdsReceivable += result.Revenue;
                else
                {
                    decimal fees = Math.Round(result.Revenue * (decimal)Tuning.Products.PaymentFees, 2);
                    var category = product.Model switch
                    {
                        RevenueModel.Commission => Category.Commissions,
                        RevenueModel.Purchase => Category.Sales,
                        _ => Category.Subscriptions
                    };
                    Post(result.Revenue - fees, category, $"{product.Name} : encaissements du jour", product.CompanyId);
                }
            }

            decimal hosting = product.ExternalMonthlyCost / 30;
            decimal marketing = product.MarketingBudget / 30;
            Ledger.Accrue(Category.Hosting, hosting);
            Ledger.Accrue(Category.Marketing, marketing);
            result.Costs = Math.Round(hosting + marketing, 2);

            // Entretien : la maintenance faite corrige les bugs puis réduit la dette ; sans entretien, des bugs apparaissent
            product.MaintenanceDebt = Math.Max(0, product.MaintenanceDebt + Design.MaintenancePerWeek(product) / 7);
            double done = product.MaintenanceDone;
            product.MaintenanceDone = 0;
            double fixedBugs = Math.Min(product.Bugs, done / 3);
            product.Bugs -= fixedBugs;
            product.MaintenanceDebt = Math.Max(0, product.MaintenanceDebt - (done - fixedBugs * 3));
            double bugChance = product.MaintenanceDebt * 0.03 + Design.Complexity(product) * 0.002 * (1 + product.Users / 5000);
            if (Random.Chance(Math.Min(0.9, bugChance)))
                product.Bugs += 1;

            // Problèmes : un seul message par cause, qui disparaît quand c'est réglé
            if (result.DowntimeHours > 0)
                Notify(Severity.Warning, "projet", $"{product.Name} tombe en panne", "Les serveurs sont débordés.", $"product:{product.Id}", key: $"outage:{product.Id}");
            else if (result.Performance >= 0.97)
                Resolve($"outage:{product.Id}");
            if (product.SupportBacklog > result.Tickets * 4 && result.Tickets > 3)
                Notify(Severity.Warning, "projet", $"{product.Name} : le support est débordé", "Augmente l'entretien ou l'équipe du projet.", $"product:{product.Id}", key: $"support:{product.Id}");
            else if (product.SupportBacklog < result.Tickets * 1.5)
                Resolve($"support:{product.Id}");

            if (weekly)
                Reviews.Collect(product, Minute, Random, IsUnlocked);

            // Paliers d'utilisateurs : récompensés une fois, et seulement s'ils sont tenus
            AwardUsers(product);
        }
    }

    private void AwardUsers(Product product)
    {
        if (product.Users >= Tuning.Projects.AdoptionForPoints)
            Award($"adopted:{product.Id}", 2, $"{product.Name} a trouvé son public ({Tuning.Projects.AdoptionForPoints} utilisateurs actifs).");
        if (product.Users >= 500)
            Award($"users500:{product.Id}", 1, $"{product.Name} dépasse 500 utilisateurs actifs.");
        if (product.Users >= 5_000)
            Award($"users5k:{product.Id}", 2, $"{product.Name} dépasse 5 000 utilisateurs actifs.");
        if (product.Users >= 50_000)
            Award($"users50k:{product.Id}", 3, $"{product.Name} dépasse 50 000 utilisateurs actifs.");
        if (product.ArchetypeId == "game" && product.TotalBuyers >= 1_000)
            Award($"sold1k:{product.Id}", 1, $"{product.Name} s'est vendu à 1 000 exemplaires.");
    }

    // ------------------------------------------------------------------ Mois, semaine, paie

    private void StartMonth(string previousMonth)
    {
        string month = Month;
        long due5 = Minute + 4L * GameTime.MinutesPerDay;
        long due10 = Minute + 9L * GameTime.MinutesPerDay;

        foreach (var site in Sites.Where(s => s.Rent > 0))
            if (Processed.Add($"rent:{month}:{site.Id}"))
                AddBill($"Loyer {site.Name}", Category.Rent, site.Rent, due5);
        if (Processed.Add($"utilities:{month}"))
            AddBill("Internet, électricité, assurances", Category.Utilities, Tuning.Finance.Utilities + Sites.Sum(s => s.Rent) * 0.04m, due10);

        foreach (var (category, amount) in Ledger.Accrued.Where(a => a.Value > 0.5m).ToList())
            if (Processed.Add($"accrued:{category}:{previousMonth}"))
                AddBill(category switch
                {
                    Category.Hosting => $"Hébergement {previousMonth}",
                    Category.Energy => $"Électricité et réseau des sites {previousMonth}",
                    Category.Research => $"Budget de recherche {previousMonth}",
                    Category.ChannelFees => $"Frais des canaux de vente {previousMonth}",
                    _ => $"Marketing {previousMonth}"
                }, category, amount, due10);
        Ledger.Accrued.Clear();

        if (Ledger.Cash < 0 && Processed.Add($"interest:{month}"))
            AddBill("Intérêts du découvert", Category.Interest, -Ledger.Cash * Tuning.Finance.OverdraftMonthlyRate, due10);

        foreach (var loan in Ledger.Loans.ToList())
        {
            if (!Processed.Add($"loan:{loan.Id}:{month}"))
                continue;
            decimal payment = Math.Min(loan.MonthlyPayment, loan.Remaining);
            AddBill(loan.Kind == LoanKind.Bank ? "Remboursement du prêt" : "Échéance du plan de redressement", Category.LoanRepayment, payment, Minute + 14L * GameTime.MinutesPerDay);
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

        // Premier mois bénéficiaire : un vrai jalon
        if (Ledger.Months.TryGetValue(previousMonth, out var totals) && OperatingResult(totals) > 0)
            Award("profit", 2, "Premier mois bénéficiaire.");

        Notify(Severity.Info, "finance", $"Bilan de {GameTime.ToDate(Minute - 1):MMMM}", MonthSummary(previousMonth), "finance");
    }

    public static decimal OperatingResult(Dictionary<Category, decimal> totals) =>
        totals.Where(t => t.Key is not (Category.Funding or Category.LoanReceived or Category.DepositRefund or Category.Deposit or Category.LoanRepayment)).Sum(t => t.Value);

    public string MonthSummary(string month)
    {
        if (!Ledger.Months.TryGetValue(month, out var totals))
            return "Aucun mouvement.";
        decimal result = OperatingResult(totals);
        return result >= 0 ? $"Bénéfice : {result:N0} CHF." : $"Perte : {-result:N0} CHF.";
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
            double share = Math.Clamp((Minute - Math.Max(person.StartsAt, person.HiredAt)) / (30.0 * GameTime.MinutesPerDay), 0, 1);
            if (share > 0.02)
                AddBill($"Salaire de {person.Name}", Category.Salaries, person.Salary * (decimal)share, due, person.Id);
        }
    }

    /// <summary>Renommée de l'entreprise (0 à 100), tirée de l'audience de ses projets : elle attire de meilleurs candidats.</summary>
    public double Renown => Math.Min(100, Products.Sum(p => p.Users) / 100);

    private void StartWeek()
    {
        AutosaveDue = true;
        foreach (var person in People)
            person.PointsThisWeek = 0;
        Candidates.RemoveAll(c => c.ExpiresAt <= Minute);
        int wanted = 3 + Math.Min(3, Staff.Count() / 3) + (IsUnlocked("hiring") ? 2 : 0) + SiteComfort().Recruits;
        double renown = Renown + (IsUnlocked("hiring") ? 25 : 0);
        while (Candidates.Count < wanted)
            Candidates.Add(Recruitment.Generate(Random, Minute, NewId(), renown));
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
                Notify(Severity.Info, "finance", $"Facture jamais payée : {invoice.Client}", $"{invoice.Amount:N0} CHF perdus.", "finance");
            }
        }
        if (Ledger.Invoices.Count > 300)
            Ledger.Invoices.RemoveAll(i => !i.Open && i.IssuedAt < Minute - 365L * GameTime.MinutesPerDay);
    }

    /// <summary>Paie automatiquement les dettes échues, dans l'ordre de priorité, tant que le découvert le permet.</summary>
    public void PayDueBills()
    {
        foreach (var bill in Ledger.Bills.Where(b => b.Open && b.DueAt <= Minute).OrderBy(b => b.Priority).ThenBy(b => b.DueAt).ToList())
        {
            if (!Ledger.CanSpend(bill.Amount))
            {
                Notify(Severity.Critical, "finance", "Factures impayées", "Plus assez d'argent pour payer ce qui est dû. Ouvre Finances.", "finance", key: "unpaid");
                continue;
            }
            bill.PaidAt = Minute;
            Post(-bill.Amount, bill.Category, bill.Label, bill.CompanyId);
            if (bill.PersonId is int id && FindPerson(id) is { } person)
                person.UnpaidDays = 0;
        }
        if (!Ledger.Overdue(Minute).Any())
            Resolve("unpaid");
        if (Ledger.Bills.Count > 400)
            Ledger.Bills.RemoveAll(b => !b.Open && b.IssuedAt < Minute - 365L * GameTime.MinutesPerDay);
    }

    // ------------------------------------------------------------------ Équipe

    private void UpdateTeam()
    {
        bool weekend = !GameTime.IsWorkday(Minute);
        var overdueSalaries = Ledger.Overdue(Minute).Where(b => b.Category == Category.Salaries).Select(b => b.PersonId).ToHashSet();

        foreach (var person in People.ToList())
        {
            person.Activity = person.TrainingDiscipline != null ? Activity.Training : Activity.Away;
            person.Path.Clear();
            var (site, post) = PostOf(person);
            var comfort = SiteComfort(site);
            person.Position = site.Outside;
            // Les pauses se font sur place : boissons, collations et sièges rendent un peu d'énergie chaque jour
            double recovery = weekend ? Tuning.Work.EnergyWeekendRecovery : Tuning.Work.EnergyNightRecovery + comfort.Break - space.Layout.BaseBreak;
            // Ton lit, dans ta chambre : tu récupères un peu mieux la nuit (si la chambre est encore l'un de tes sites)
            if (person.IsFounder)
                recovery += Sites.Max(space.Layout.NightRecovery);
            person.Energy = OnVacation(person) ? 100 : Math.Min(100, person.Energy + recovery * (person.Energy < 30 ? 0.8 : 1));

            if (overdueSalaries.Contains(person.Id))
                person.UnpaidDays++;

            bool hasPost = post != null && space.Layout.Check(site, post) == space.WorkstationIssue.None;
            double target = 58 + comfort.Morale + (post == null ? 0 : space.Layout.PostMorale(site, post));
            if (!person.IsFounder)
            {
                target += Math.Clamp((double)((person.Salary - MarketSalary(person)) / MarketSalary(person)) * 40, -12, 12);
                target -= person.UnpaidDays * 4;
            }
            if (!hasPost && Minute >= person.StartsAt)
                target -= 15;
            if (person.Schedule == WorkSchedule.Extended)
                target -= 6;
            if (person.Energy < 30)
                target -= 10;
            if (OnVacation(person))
                target += 8;
            person.Morale = Math.Clamp(person.Morale + (target - person.Morale) * 0.1, 0, 100);

            if (!hasPost && !person.IsFounder && Minute >= person.StartsAt)
                Notify(Severity.Warning, "équipe", $"{person.Name} n'a pas de poste", "Installe un bureau complet dans Aménagement.", "build", key: $"nodesk:{person.Id}");
            else
                Resolve($"nodesk:{person.Id}");

            if (person.IsFounder)
                continue;

            person.LowMoraleDays = person.Morale < 25 ? person.LowMoraleDays + 1 : 0;
            if (person.Morale < 35)
                Notify(Severity.Warning, "équipe", $"{person.Name} n'a plus le moral", "Un congé, une augmentation ou de meilleures conditions aideraient.", $"person:{person.Id}", key: $"morale:{person.Id}");
            else if (person.Morale > 45)
                Resolve($"morale:{person.Id}");

            if (person.LeavesAt == null && (person.LowMoraleDays >= 10 || person.UnpaidDays >= Tuning.Finance.UnpaidSalaryDaysBeforeQuitting))
            {
                person.LeavesAt = person.UnpaidDays >= Tuning.Finance.UnpaidSalaryDaysBeforeQuitting ? Minute : GameTime.AddWorkdays(Minute, 10);
                Notify(Severity.Critical, "équipe", $"{person.Name} démissionne",
                    person.UnpaidDays >= Tuning.Finance.UnpaidSalaryDaysBeforeQuitting ? "Salaire impayé : départ immédiat." : $"Départ le {GameTime.FormatDate(person.LeavesAt.Value)}, sauf si son moral remonte.",
                    $"person:{person.Id}", key: $"leave:{person.Id}");
            }
            else if (person.LeavesAt != null && person.LeavesAt > Minute && person.Morale > 45)
            {
                person.LeavesAt = null;
                Resolve($"leave:{person.Id}");
                Notify(Severity.Good, "équipe", $"{person.Name} reste finalement", "", $"person:{person.Id}");
            }

            if (person.LeavesAt is long leaves && leaves <= Minute)
                RemovePerson(person, $"{person.Name} est parti·e.");
        }

        Candidates.RemoveAll(c => c.ExpiresAt <= Minute);
    }

    /// <summary>Salaire de référence pour un niveau : le jeu le propose et le paie, sans négociation.</summary>
    public static decimal MarketSalary(Person person) => 3_800m + (decimal)person.Skill(person.BestDiscipline) * 70m;

    private void RemovePerson(Person person, string message)
    {
        People.Remove(person);
        foreach (var seat in Sites.SelectMany(s => s.Items).Where(i => i.AssignedPersonId == person.Id))
            seat.AssignedPersonId = null;
        foreach (var notice in Notices.Where(n => n.Key.EndsWith($":{person.Id}") && n.Target == $"person:{person.Id}"))
            notice.Resolved = true;
        Notify(Severity.Warning, "équipe", message, "", "team");
    }

    // ------------------------------------------------------------------ Concurrence

    private void UpdateMarket()
    {
        foreach (var competitor in Competitors)
        {
            competitor.Appeal = Math.Min(0.75, competitor.Appeal + 0.0004);
            bool relevant = Products.Any(p => p.IsLive && p.SegmentId == competitor.SegmentId);
            if (competitor.UpgradeAt is long at && at <= Minute)
            {
                competitor.Appeal = Math.Min(0.8, competitor.Appeal + competitor.UpgradeGain);
                competitor.UpgradeAt = null;
                if (relevant)
                    Notify(Severity.Info, "marché", $"{competitor.Name} sort une nouvelle version", "Tes utilisateurs vont comparer.", "projects");
            }
            else if (competitor.UpgradeAt == null && Random.Chance(1 / 120.0))
            {
                competitor.UpgradeAt = Minute + Random.Next(10, 21) * (long)GameTime.MinutesPerDay;
                competitor.UpgradeGain = Math.Round(Random.Range(0.04, 0.1), 2);
                if (relevant)
                    Notify(Severity.Info, "marché", $"Rumeur : {competitor.Name} prépare une nouvelle version", $"Sortie vers le {GameTime.FormatDate(competitor.UpgradeAt.Value)}.", "projects");
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
        Tuning.Finance.FounderDraw + Tuning.Finance.Utilities + Sites.Sum(s => s.Rent) * 1.04m
        + Staff.Sum(p => p.Salary)
        + Products.Sum(p => p.ExternalMonthlyCost + p.MarketingBudget) + Infra().MonthlyEnergy
        + Ledger.Loans.Sum(l => Math.Min(l.MonthlyPayment, l.Remaining));

    /// <summary>Revenus des projets sur les 30 derniers jours.</summary>
    public decimal RecurringRevenue30 =>
        Products.Sum(p => p.History.Skip(Math.Max(0, p.History.Count - 30)).Sum(d => d.Revenue)) + CloudRevenue30;

    /// <summary>
    /// Jours d'autonomie : argent disponible (avec le découvert, moins ce qui est dû, plus 85 % des factures attendues
    /// sous 30 jours) divisé par la perte quotidienne (charges fixes moins revenus des projets).
    /// </summary>
    public (double Days, string Assumptions) Runway()
    {
        decimal burn = MonthlyFixedCosts - RecurringRevenue30;
        decimal soon = Ledger.Invoices.Where(i => i.Open && i.DueAt <= Minute + 30L * GameTime.MinutesPerDay).Sum(i => i.Amount);
        decimal available = Ledger.Cash + Ledger.Overdraft - Ledger.OpenBills + soon * 0.85m;
        string assumptions = $"Argent {Ledger.Cash:N0} + découvert {Ledger.Overdraft:N0} − à payer {Ledger.OpenBills:N0} + factures attendues {soon * 0.85m:N0}. " +
                             $"Dépenses {MonthlyFixedCosts:N0}/mois, revenus des projets {RecurringRevenue30:N0}/mois.";
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
                Notify(Severity.Good, "finance", "Mise en demeure levée", "Toutes les factures sont payées.", "finance");
            Resolve("notice");
            Solvency.NoticeUntil = null;
            Solvency.State = Runway().Days < 30 || Ledger.Cash < 0 ? SolvencyState.Tight : SolvencyState.Healthy;
            if (Solvency.State == SolvencyState.Tight && previous == SolvencyState.Healthy)
                Notify(Severity.Warning, "finance", "L'argent va manquer", "Moins de 30 jours d'autonomie au rythme actuel.", "finance", key: "tight");
            if (Solvency.State == SolvencyState.Healthy)
                Resolve("tight");
            return;
        }

        long oldest = overdue.Min(b => b.DueAt);
        decimal total = overdue.Sum(b => b.Amount);
        int overdueDays = (int)((Minute - oldest) / GameTime.MinutesPerDay);

        if (Solvency.State == SolvencyState.Notice && Solvency.NoticeUntil is long until)
        {
            long left = (until - Minute) / GameTime.MinutesPerDay;
            if (Minute >= until)
                Liquidate(total, overdue);
            else
                Notify(Severity.Critical, "finance", $"Faillite dans {left} jours", $"{total:N0} CHF à payer avant le {GameTime.FormatDate(until)}.", "finance", key: "notice");
            return;
        }

        Solvency.State = SolvencyState.Overdue;
        if (overdueDays >= Tuning.Finance.OverdueDaysBeforeNotice && total >= 500)
        {
            Solvency.State = SolvencyState.Notice;
            Solvency.NoticeUntil = Minute + Tuning.Finance.NoticeDays * (long)GameTime.MinutesPerDay;
            Notify(Severity.Critical, "finance", "Mise en demeure",
                $"{total:N0} CHF impayés. Sans paiement avant le {GameTime.FormatDate(Solvency.NoticeUntil.Value)}, c'est la faillite.", "finance", key: "notice");
        }
    }

    private void Liquidate(decimal overdueTotal, List<Bill> overdue)
    {
        Solvency.State = SolvencyState.Liquidated;
        Status = CampaignStatus.Liquidated;
        var lines = new List<string>
        {
            $"Faillite prononcée le {GameTime.FormatDate(Minute)}, après {Day} jours.",
            $"Impayés : {overdueTotal:N0} CHF ({string.Join(", ", overdue.Select(b => b.Label).Distinct().Take(4))}).",
            $"Argent : {Ledger.Cash:N0} CHF. Dépenses : {MonthlyFixedCosts:N0} CHF par mois pour {RecurringRevenue30:N0} CHF de revenus.",
            $"Équipe : {People.Count} personne(s). Projets en ligne : {Products.Count(p => p.IsLive)}.",
        };
        Solvency.LiquidationReport = string.Join("\n", lines);
        Notify(Severity.Critical, "finance", "Faillite", Solvency.LiquidationReport, "finance");
    }

    // ------------------------------------------------------------------ Jalons

    public static readonly (string Id, string Name, string Description)[] MilestoneDefs =
    [
        ("first_launch", "Premier lancement", "Sortir un premier projet."),
        ("first_hire", "On est deux", "Embaucher une première personne."),
        ("office", "De vrais locaux", "Quitter l'appartement."),
        ("users1k", "Mille utilisateurs", "1 000 utilisateurs actifs sur un projet."),
        ("mrr10k", "10 000 CHF par mois", "10 000 CHF de revenus des projets sur 30 jours."),
        ("portfolio3", "Un vrai portefeuille", "Trois projets en ligne qui ont trouvé leur public."),
        ("team10", "Une vraie équipe", "Être 10 dans l'entreprise."),
        ("users50k", "Cinquante mille", "50 000 utilisateurs actifs au total."),
    ];

    private void CheckMilestones()
    {
        bool Reach(string id, bool condition, int points)
        {
            if (!condition || Milestones.ContainsKey(id))
                return false;
            Milestones[id] = Minute;
            var def = MilestoneDefs.First(m => m.Id == id);
            Notify(Severity.Good, "entreprise", $"Jalon : {def.Name}", def.Description, "tree");
            if (points > 0)
                Award($"milestone:{id}", points, def.Name);
            return true;
        }

        if (Reach("first_launch", Products.Any(p => p.IsLive), 0))
            GiveReward("trophy_project");
        Reach("first_hire", Staff.Any(), 1);
        Reach("office", Sites.Any(s => s.ListingId != space.RealEstate.Apartment), 1);
        Reach("users1k", Products.Any(p => p.Users >= 1_000), 1);
        if (Reach("mrr10k", RecurringRevenue30 >= 10_000, 2))
            GiveReward("award_plaque");
        Reach("portfolio3", Products.Count(p => p.IsLive && p.Users >= Tuning.Projects.AdoptionForPoints) >= 3, 1);
        Reach("team10", People.Count >= 10, 2);
        Reach("users50k", Products.Sum(p => p.Users) >= 50_000, 2);
    }

    /// <summary>La récompense d'un jalon (trophée, plaque) arrive dans les cartons, à poser où l'on veut.</summary>
    private void GiveReward(string defId)
    {
        var def = space.FurnitureCatalog.Get(defId);
        Storage.Add(new StoredItem { Id = NewId(), DefId = defId });
        Notify(Severity.Good, "local", $"{def.Name} dans les cartons", "Pose-le depuis Aménagement : il apporte un peu de moral à l'équipe.", "build");
    }
}
