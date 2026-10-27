using Core.finance;
using Core.team;
using Core.work;

namespace Core.campaign;

/// <summary>Prestations : accepter (avec un délai négocié), livrer, être payé... ou être en retard.</summary>
public partial class Campaign
{
    public const double ChunkSize = 10;

    public CommandResult AcceptContract(int contractId, DeadlineOption option, bool capitalize)
    {
        var contract = FindContract(contractId);
        if (contract is not { Status: ContractStatus.Offered })
            return CommandResult.Fail("Cette demande n'est plus disponible.");

        var (price, workdays) = contract.Terms(option);
        var company = FindCompany(contract.CompanyId) ?? MainCompany;
        contract.CompanyId = company.Id;
        contract.Option = option;
        contract.AgreedPrice = price;
        contract.Capitalize = capitalize;
        contract.AcceptedAt = Minute;
        contract.Deadline = GameTime.StartOfDay(GameTime.Day(GameTime.AddWorkdays(Minute, workdays))) + Tuning.Work.DayEndHour * 60;
        contract.Status = ContractStatus.Active;

        // Le périmètre devient des tâches : design, puis développement, puis tests
        int reuse = company.ReuseLevels.GetValueOrDefault(contract.KindId);
        double factor = (capitalize ? 1 + Tuning.Contracts.CapitalizeExtraEffort : 1) * (1 - reuse * Tuning.Contracts.ReuseGainPerLevel);
        var previous = new List<int>();
        foreach (var discipline in new[] { Discipline.Design, Discipline.Code, Discipline.Test })
        {
            if (!contract.Scope.TryGetValue(discipline, out double points))
                continue;
            var current = AddChunks(WorkOwner.Contract, contract.Id, $"{contract.Title} – {Person.DisciplineName(discipline).ToLowerInvariant()}",
                discipline, points * (discipline == Discipline.Code ? factor : 1), previous, "scope");
            previous = current;
        }

        decimal deposit = Math.Round(price * (decimal)Tuning.Contracts.DepositShare);
        var invoice = AddInvoice(contract.Client, $"Acompte {contract.Title}", deposit, Tuning.Contracts.DepositPaymentDays, contract.Reliability, contract.Id, companyId: company.Id);
        contract.InvoiceIds.Add(invoice.Id);

        // Le fondateur sans affectation s'y met directement
        if (Founder.Assignment.Kind == AssignmentKind.None || Founder.Assignment.Kind == AssignmentKind.Contract && FindContract(Founder.Assignment.TargetId) is not { Status: ContractStatus.Active })
            Founder.Assignment = new Assignment { Kind = AssignmentKind.Contract, TargetId = contract.Id };

        Notify(Severity.Info, "contrat", $"Contrat signé : {contract.Client}",
            $"{price:N0} CHF, à livrer le {GameTime.FormatDate(contract.Deadline)}. Acompte de {deposit:N0} CHF facturé (paiement attendu sous {Tuning.Contracts.DepositPaymentDays} jours).", $"contract:{contract.Id}");
        return CommandResult.Success();
    }

    /// <summary>Découpe un volume de travail en tâches d'au plus 10 points, toutes dépendantes de l'étape précédente.</summary>
    public List<int> AddChunks(WorkOwner owner, int ownerId, string title, Discipline discipline, double points, List<int> dependsOn, string kind, int? moduleId = null)
    {
        var ids = new List<int>();
        int chunks = Math.Max(1, (int)Math.Ceiling(points / ChunkSize));
        for (int i = 0; i < chunks; i++)
        {
            var item = new WorkItem
            {
                Id = NewId(),
                Owner = owner,
                OwnerId = ownerId,
                Title = chunks > 1 ? $"{title} ({i + 1}/{chunks})" : title,
                Discipline = discipline,
                Effort = Math.Round(points / chunks, 2),
                DependsOn = dependsOn.ToList(),
                Kind = kind,
                ModuleId = moduleId,
            };
            Work.Add(item);
            ids.Add(item.Id);
        }
        return ids;
    }

    public CommandResult DeclineContract(int contractId)
    {
        var contract = FindContract(contractId);
        if (contract is not { Status: ContractStatus.Offered })
            return CommandResult.Fail("Cette demande n'est plus disponible.");
        contract.Status = ContractStatus.Declined;
        contract.Outcome = "Refusée.";
        return CommandResult.Success();
    }

    /// <summary>Abandonner un contrat : on rend la part d'acompte du travail non fait, et la réputation en souffre.</summary>
    public CommandResult AbandonContract(int contractId)
    {
        var contract = FindContract(contractId);
        if (contract is not { Status: ContractStatus.Active })
            return CommandResult.Fail("Ce contrat n'est pas en cours.");
        var items = WorkFor(WorkOwner.Contract, contract.Id).ToList();
        double done = items.Sum(w => w.Progress) / Math.Max(1, items.Sum(w => w.Effort));
        decimal deposit = Math.Round(contract.AgreedPrice * (decimal)Tuning.Contracts.DepositShare);
        decimal refund = Math.Round(deposit * (decimal)(1 - done));
        // Acompte pas encore payé : on l'annule plutôt que de rembourser
        var depositInvoice = Ledger.Invoices.FirstOrDefault(i => i.ContractId == contract.Id && i.Open);
        if (depositInvoice != null)
        {
            depositInvoice.WrittenOff = true;
            refund = 0;
        }
        if (refund > 0)
            AddBill($"Remboursement d'acompte : {contract.Client}", Category.Refund, refund, Minute + 10L * GameTime.MinutesPerDay);
        Close(contract, ContractStatus.Cancelled, "Abandonné par l'agence.");
        var company = FindCompany(contract.CompanyId) ?? MainCompany;
        company.Reputation = Math.Max(0, company.Reputation - 6);
        Notify(Severity.Warning, "contrat", $"Contrat abandonné : {contract.Client}", refund > 0 ? $"Acompte à rembourser : {refund:N0} CHF." : "", $"contract:{contract.Id}");
        return CommandResult.Success();
    }

    private void Close(Contract contract, ContractStatus status, string outcome)
    {
        contract.Status = status;
        contract.Outcome = outcome;
        Work.RemoveAll(w => w.Owner == WorkOwner.Contract && w.OwnerId == contract.Id);
        foreach (var person in People.Where(p => p.Assignment.Is(AssignmentKind.Contract, contract.Id)))
        {
            person.Assignment = Assignment.None;
            person.CurrentWorkItemId = null;
        }
    }

    private void DeliverContract(Contract contract)
    {
        var items = WorkFor(WorkOwner.Contract, contract.Id).ToList();
        double quality = items.Sum(w => w.QualityPoints) / Math.Max(1e-6, items.Sum(w => w.Progress));

        // Trop loin de l'attendu : le client demande des corrections une fois
        if (quality < contract.ExpectedQuality - 0.08 && !contract.ReworkRequested)
        {
            contract.ReworkRequested = true;
            double total = contract.TotalEffort;
            var code = AddChunks(WorkOwner.Contract, contract.Id, $"{contract.Title} – corrections", Discipline.Code, Math.Max(2, total * 0.1), [], "rework");
            AddChunks(WorkOwner.Contract, contract.Id, $"{contract.Title} – nouvelle recette", Discipline.Test, Math.Max(1, total * 0.05), code, "rework");
            Notify(Severity.Warning, "contrat", $"{contract.Client} demande des corrections",
                $"Qualité livrée {quality:P0}, attendue {contract.ExpectedQuality:P0}. Des tâches de correction sont ajoutées ; l'échéance ne bouge pas.", $"contract:{contract.Id}");
            return;
        }

        var company = FindCompany(contract.CompanyId) ?? MainCompany;
        int late = Math.Max(0, GameTime.WorkdaysBetween(contract.Deadline, Minute));
        decimal penaltyRate = Math.Min((decimal)Tuning.Contracts.MaxLatePenalty, late * (decimal)Tuning.Contracts.LatePenaltyPerWorkday);
        decimal qualityDiscount = quality < contract.ExpectedQuality ? Math.Min(0.3m, (decimal)((contract.ExpectedQuality - quality) * 1.5)) : 0;
        decimal deposit = Math.Round(contract.AgreedPrice * (decimal)Tuning.Contracts.DepositShare);
        contract.Penalty = Math.Round(contract.AgreedPrice * (penaltyRate + qualityDiscount));
        decimal balance = Math.Max(0, contract.AgreedPrice - deposit - contract.Penalty);
        contract.DeliveredQuality = Math.Round(quality, 3);
        contract.DeliveredAt = Minute;

        if (balance > 0)
        {
            var invoice = AddInvoice(contract.Client, $"Solde {contract.Title}", balance, Tuning.Contracts.InvoicePaymentDays, contract.Reliability, contract.Id, companyId: company.Id);
            contract.InvoiceIds.Add(invoice.Id);
        }

        double gain = 3 + 4 * Math.Clamp(quality / contract.ExpectedQuality - 0.8, -0.5, 0.5) - late * 0.6 + Math.Min(3, contract.TotalEffort / 80);
        company.Reputation = Math.Clamp(company.Reputation + gain, 0, 100);
        if (contract.Capitalize)
            company.ReuseLevels[contract.KindId] = Math.Min(Tuning.Contracts.MaxReuseLevel, company.ReuseLevels.GetValueOrDefault(contract.KindId) + 1);

        string detail = $"Qualité {quality:P0} (attendue {contract.ExpectedQuality:P0}). Solde facturé : {balance:N0} CHF, payable sous {Tuning.Contracts.InvoicePaymentDays} jours.";
        if (contract.Penalty > 0)
            detail += $" Pénalités et réductions : {contract.Penalty:N0} CHF.";
        Close(contract, ContractStatus.Completed, late > 0 ? $"Livré avec {late} jour(s) de retard." : "Livré à temps.");
        Notify(Severity.Good, "contrat", $"Livré : {contract.Client}", detail, $"contract:{contract.Id}");
    }

    private void CancelByClient(Contract contract)
    {
        var company = FindCompany(contract.CompanyId) ?? MainCompany;
        company.Reputation = Math.Max(0, company.Reputation - 10);
        Close(contract, ContractStatus.Cancelled, "Rompu par le client pour retard.");
        Notify(Severity.Critical, "contrat", $"{contract.Client} rompt le contrat",
            "Trop de retard : le solde ne sera jamais payé et la réputation de l'agence baisse.", $"contract:{contract.Id}");
    }
}
