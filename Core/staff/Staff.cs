using Core.company;

namespace Core.staff;

public enum EmployeeRole
{
    Intern,
    Developer,
    Salesperson,
    ProjectManager
}

/// <param name="ExtraCapacity">Tâches simultanées en plus par employé.</param>
/// <param name="SpeedBonus">Vitesse de travail en plus par employé (0.1 = +10 %).</param>
/// <param name="RequestRateBonus">Demandes en plus par employé (0.3 = +30 %).</param>
/// <param name="AutoAccept">Accepte tout seul les demandes faisables à temps.</param>
public record EmployeeRoleInfo(
    EmployeeRole Role,
    string Title,
    double HiringCost,
    double MonthlySalary,
    int ExtraCapacity,
    double SpeedBonus,
    double RequestRateBonus,
    bool AutoAccept,
    string Description);

/// <summary>Les employés de la boîte. Ils remplacent la capacité fixe du solo.</summary>
public class Staff
{
    private readonly Dictionary<EmployeeRole, int> _counts = new();

    public IReadOnlyList<EmployeeRoleInfo> Roles { get; }

    public Staff(IReadOnlyList<EmployeeRoleInfo> roles)
    {
        Roles = roles;
    }

    public int Count(EmployeeRole role) => _counts.GetValueOrDefault(role);
    public int TotalCount => _counts.Values.Sum();

    public double MonthlySalaries => Roles.Sum(r => r.MonthlySalary * Count(r.Role));
    public int ExtraCapacity => Roles.Sum(r => r.ExtraCapacity * Count(r.Role));
    public double SpeedMultiplier => 1 + Roles.Sum(r => r.SpeedBonus * Count(r.Role));
    public double RequestRateMultiplier => 1 + Roles.Sum(r => r.RequestRateBonus * Count(r.Role));
    public bool AutoAccept => Roles.Any(r => r.AutoAccept && Count(r.Role) > 0);

    public EmployeeRoleInfo Info(EmployeeRole role) => Roles.First(r => r.Role == role);

    public bool Hire(EmployeeRole role, Wallet wallet, double costMultiplier = 1)
    {
        if (!wallet.TrySpend(Info(role).HiringCost * costMultiplier))
            return false;

        Add(role);
        return true;
    }

    /// <summary>Ajoute un employé sans frais d'embauche (cadeau d'un événement).</summary>
    public void Add(EmployeeRole role) => _counts[role] = Count(role) + 1;

    public bool Fire(EmployeeRole role)
    {
        if (Count(role) == 0)
            return false;

        _counts[role]--;
        return true;
    }
}
