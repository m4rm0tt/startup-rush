namespace Core.space;

/// <summary>
/// L'infrastructure d'un site, déjà agrégée : ce que ses équipements fournissent vraiment (hébergement, calcul, GPU,
/// stockage), et pourquoi c'est moins que la somme des fiches quand quelque chose manque (électricité, froid, réseau,
/// switch, contrôleur de disques). Les détails restent explicables : chaque limite a sa raison.
/// </summary>
public record InfraSummary(
    double Hosting,
    double Compute,
    double Gpu,
    double Storage,
    double Network,
    double NetworkNeeded,
    double PowerSupply,
    double PowerDraw,
    double Cooling,
    double Heat,
    double Backup,
    double Organization,
    double Security,
    bool Supervised,
    decimal MonthlyCost,
    double Factor,
    IReadOnlyList<string> Issues,
    int QuantumUnits = 0,
    double Quantum = 0)
{
    /// <summary>Le site a-t-il un équipement d'hébergement ou de calcul ?</summary>
    public bool Any => Hosting + Compute + Gpu + Storage + QuantumUnits > 0;

    /// <summary>Part de la consommation couverte par les onduleurs (0 à 1).</summary>
    public double BackupCoverage => PowerDraw <= 0 ? 1 : Math.Min(1, Backup / PowerDraw);

    /// <summary>Probabilité d'incident par jour (coupure, panne) : réduite par les onduleurs et la supervision.</summary>
    public double IncidentChance => !Any ? 0
        : Infrastructure.BaseIncidentChance * (1 - 0.9 * BackupCoverage) * (Supervised ? 0.5 : 1) * (1.2 - 0.4 * Organization);

    /// <summary>Énergie consommée par jour, en kWh.</summary>
    public double KwhPerDay => PowerDraw * 24;
}

public static class Infrastructure
{
    /// <summary>Prix de l'électricité ⚙️, en CHF par kWh.</summary>
    public const decimal KwhPrice = 0.25m;

    /// <summary>Probabilité d'incident par jour d'un site sans onduleur ni supervision ⚙️.</summary>
    public const double BaseIncidentChance = 0.03;

    /// <summary>Débit réseau consommé par unité d'hébergement servie (le stockage et le calcul en demandent peu).</summary>
    public const double NetworkPerHosting = 2;

    /// <summary>Organisation requise par baie (brassage, passe-câbles, chemins de câbles) pour un câblage parfait.</summary>
    public const double OrganizationPerRack = 2;

    /// <summary>Points de sécurité pour une cage complète.</summary>
    public const double FullSecurity = 8;

    /// <summary>Unités de calcul quantique d'une installation stabilisée (cryostat fermé) en service.</summary>
    public const double QuantumPerUnit = 20;

    /// <summary>
    /// Ce que le site fournit sans rien acheter : électricité, froid naturel (fenêtres, ventilation) et accès internet. Un
    /// local de bureau n'est pas fait pour des baies ; la salle serveur et le datacenter, si.
    /// </summary>
    public static (double Power, double Cooling, double Network) Base(string listingId) => listingId switch
    {
        RealEstate.Apartment => (3, 0.6, 50),
        "small" => (8, 1.5, 50),
        "floor" => (15, 2, 100),
        "large" => (30, 3, 200),
        "hq" => (60, 5, 400),
        "server_room" => (12, 0, 300),
        "datacenter" => (40, 0, 2_000),
        "hw_workshop" => (20, 3, 100),
        "factory" => (120, 10, 300),
        "advanced_lab" => (40, 12, 200),
        _ => (5, 1, 50),
    };

    /// <summary>Les modules d'une baie, avec leur premier emplacement.</summary>
    public static IEnumerable<(PlacedItem Module, int Slot)> Modules(Premises premises, PlacedItem rack) =>
        premises.Children(rack).Where(c => c.Socket?.StartsWith("slot:") == true).Select(c => (c, int.Parse(c.Socket![5..])));

    public static InfraSummary Of(Premises premises)
    {
        var issues = new List<string>();
        var (power, cooling, network) = Base(premises.ListingId);
        double hosting = 0, compute = 0, gpu = 0, storage = 0, draw = 0, heat = 0, backup = 0, organization = 0, security = 0, coolers = 0;
        decimal monthly = 0;
        bool supervised = false;
        int racks = 0;

        void Add(FurnitureDef def, double heatFactor = 1)
        {
            hosting += def.Hosting;
            compute += def.Compute;
            gpu += def.Gpu;
            storage += def.Storage;
            draw += def.Power;
            heat += def.Power * heatFactor;
        }

        foreach (var item in premises.Items)
        {
            var def = item.Def;
            if (def.Kind == FurnitureKind.RackModule)
                continue; // comptés avec leur baie
            power += def.PowerSupply;
            cooling += def.Cooling;
            network += def.Network;
            backup += def.Backup;
            organization += def.Organization;
            security += def.Security;
            monthly += def.MonthlyCost;
            supervised |= def.Kind == FurnitureKind.Supervision;
            // Le froid consomme selon la chaleur à évacuer (au moins un quart), sans chauffer la salle
            if (def.Cooling > 0)
                coolers += def.Power;
            else if (def.Kind != FurnitureKind.Rack)
                Add(def);
            if (def.Kind != FurnitureKind.Rack)
                continue;

            racks++;
            var modules = Modules(premises, item).ToList();
            bool switched = modules.Any(m => m.Module.DefId == "module_network_switch_1u");
            bool controller = modules.Any(m => m.Module.DefId == "module_storage_2u");
            int blanks = modules.Count(m => m.Module.DefId == "module_blank_1u");
            double rackHeat = (def.Id == "rack_high_density" ? 0.8 : 1) * (1 - Math.Min(0.09, blanks * 0.03));
            foreach (var (module, _) in modules)
            {
                var m = module.Def;
                backup += m.Backup;
                organization += m.Organization;
                if (m.Hosting + m.Compute + m.Gpu + m.Storage > 0)
                {
                    if (!switched)
                        continue;
                    if (m.Id == "module_storage_jbod_4u" && !controller)
                        continue;
                }
                Add(m, rackHeat);
            }
            if (!switched && modules.Any(m => m.Module.Def.Hosting + m.Module.Def.Compute + m.Module.Def.Gpu + m.Module.Def.Storage > 0))
                issues.Add($"{def.Name} sans switch : ses serveurs ne sont pas reliés au réseau.");
            if (!controller && modules.Any(m => m.Module.DefId == "module_storage_jbod_4u"))
                issues.Add($"{def.Name} : un tiroir de disques demande un stockage 2U dans la même baie.");
        }

        // Le faux plancher distribue mieux le froid qui existe (Finishes) : il n'en crée pas
        cooling *= 1 + Finishes.CoolingBonus(premises);
        if (coolers > 0)
            draw += coolers * Math.Clamp(cooling > 0 ? heat / cooling : 1, 0.25, 1);
        double orgScore = racks == 0 ? Math.Min(1, organization / 2) : Math.Min(1, organization / (racks * OrganizationPerRack));
        double powerFactor = draw <= power ? 1 : power / draw;
        double coolFactor = heat <= cooling ? 1 : cooling <= 0 ? 0.3 : Math.Max(0.3, cooling / heat);
        if (powerFactor < 1)
            issues.Add($"Puissance insuffisante : {draw:0.#} kW demandés pour {power:0.#} kW disponibles (armoire électrique).");
        if (coolFactor < 1)
            issues.Add($"Capacité de refroidissement insuffisante : {heat:0.#} kW de chaleur pour {cooling:0.#} kW évacués (climatiseur).");
        // Un câblage en désordre fait perdre jusqu'à 10 % ; l'organisation le rattrape, plafonnée
        double factor = Math.Min(powerFactor, coolFactor) * (0.9 + 0.1 * (racks == 0 ? 1 : orgScore));
        double networkNeeded = hosting * factor * NetworkPerHosting;
        double networkFactor = networkNeeded <= network ? 1 : network / networkNeeded;
        if (networkFactor < 1)
            issues.Add($"Débit réseau insuffisant : l'hébergement demande {networkNeeded:0} pour {network:0} (routeur, fibre).");

        // Quantique : chaque cryostat fermé demande sa baie de contrôle et son support cryogénique ; le froid et l'énergie du site comptent
        int Count(string id) => premises.Items.Count(i => i.DefId == id);
        int closed = Count("quantum_cryostat_closed"), controls = Count("quantum_control_rack"), cryo = Count("cryo_support_unit");
        int quantumUnits = Math.Min(closed, Math.Min(controls, cryo));
        if (closed > quantumUnits)
            issues.Add($"Cryostat fermé sans {(controls < cryo ? "baie de contrôle micro-ondes" : "support cryogénique")} : {closed - quantumUnits} installation(s) ne peuvent pas démarrer.");

        return new InfraSummary(hosting * factor * networkFactor, compute * factor, gpu * factor, storage * factor, network, networkNeeded,
            power, draw, cooling, heat, backup, orgScore, Math.Min(1, security / FullSecurity), supervised, monthly, factor, issues,
            quantumUnits, quantumUnits * QuantumPerUnit * factor);
    }
}
