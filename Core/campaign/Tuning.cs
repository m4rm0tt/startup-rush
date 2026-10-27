namespace Core.campaign;

/// <summary>
/// Toutes les valeurs d'équilibrage au même endroit. Ce sont des réglages proposés, pas des décisions de design :
/// DESIGN_SIMULATION.md explique d'où elles viennent et BALANCE_REPORT.md ce que les simulations en ont montré.
/// </summary>
public static class Tuning
{
    public static class Start
    {
        /// <summary>Économies du fondateur placées dans l'entreprise : environ trois mois de charges fixes sans aucun revenu.</summary>
        public const decimal Cash = 7_000m;

        /// <summary>Découvert bancaire autorisé : un solde légèrement négatif ne bloque rien.</summary>
        public const decimal Overdraft = 2_000m;
    }

    public static class Work
    {
        public const int DayStartHour = 9;
        public const int LunchHour = 12;
        public const int DayEndHour = 17;

        /// <summary>Heures supplémentaires (horaire étendu) : de 17 h à 20 h.</summary>
        public const int ExtendedEndHour = 20;

        /// <summary>Un point de travail = une heure d'une personne de niveau 50 sur un ordinateur standard.</summary>
        public const double PointsPerHourAtLevel50 = 1.0;

        /// <summary>Niveau minimal dans une discipline pour pouvoir y travailler.</summary>
        public const int MinimumSkill = 15;

        /// <summary>Au-delà de ce nombre de personnes sur une même cible, la coordination coûte (sans responsable).</summary>
        public const int CoordinationFreeTeam = 3;

        public const int CoordinationFreeTeamWithLead = 6;
        public const double CoordinationLossPerExtraPerson = 0.12;

        /// <summary>Énergie perdue par heure travaillée (0-100), puis en heures supplémentaires.</summary>
        public const double EnergyPerHour = 1.6;

        public const double EnergyPerOvertimeHour = 3.2;
        public const double EnergyNightRecovery = 38;
        public const double EnergyWeekendRecovery = 30;
    }

    public static class Contracts
    {
        public const int MaxOpenOffers = 6;

        /// <summary>Part payée à la signature, facturée avec 10 jours de délai de paiement.</summary>
        public const double DepositShare = 0.3;

        public const int DepositPaymentDays = 10;
        public const int InvoicePaymentDays = 30;
        public const double LatePenaltyPerWorkday = 0.02;
        public const double MaxLatePenalty = 0.3;

        /// <summary>Jours ouvrés de retard au-delà desquels le client rompt le contrat.</summary>
        public const int CancelAfterLateWorkdays = 12;

        /// <summary>Effort supplémentaire pour rendre le code réutilisable, et gain sur les projets suivants du même type.</summary>
        public const double CapitalizeExtraEffort = 0.15;

        public const double ReuseGainPerLevel = 0.1;
        public const int MaxReuseLevel = 3;
    }

    public static class Products
    {
        /// <summary>Taux d'abandon quotidien de base des utilisateurs actifs.</summary>
        public const double BaseDailyChurn = 0.006;

        /// <summary>Visiteurs organiques (référencement, curiosité) par jour pour un attrait moyen ; ils augmentent avec l'attrait.</summary>
        public const double OrganicAwarenessPerDay = 25;

        public const double WordOfMouth = 0.012;
        public const int MaxModulesPerPageBeforeClutter = 5;

        /// <summary>Frais de paiement prélevés sur les encaissements par carte.</summary>
        public const double PaymentFees = 0.03;

        public const int HistoryDays = 400;
    }

    public static class Finance
    {
        /// <summary>Rémunération minimale du fondateur, logement compris (l'appartement n'a donc pas de loyer séparé).</summary>
        public const decimal FounderDraw = 2_200m;

        public const decimal Utilities = 160m;
        public const decimal OverdraftMonthlyRate = 0.01m;

        /// <summary>Jours de retard d'une obligation avant la mise en demeure.</summary>
        public const int OverdueDaysBeforeNotice = 20;

        /// <summary>Durée de la mise en demeure : sans régularisation, l'entreprise est liquidée.</summary>
        public const int NoticeDays = 30;

        /// <summary>Jours de salaires impayés avant qu'un employé démissionne.</summary>
        public const int UnpaidSalaryDaysBeforeQuitting = 14;

        public const decimal SeverancePayMonths = 1m;
        public const double ResaleShare = 0.5;

        /// <summary>Prêt bancaire : plafond en mois de revenus moyens, durée, taux annuel.</summary>
        public const decimal LoanMonthsOfRevenue = 3m;

        public const int LoanMonths = 12;
        public const decimal LoanYearlyRate = 0.07m;

        /// <summary>Plan de redressement : surcoût sur les dettes échelonnées, et part minimale des charges fixes couverte par les revenus récurrents.</summary>
        public const decimal RecoveryPlanSurcharge = 0.1m;

        public const int RecoveryPlanMonths = 6;
        public const decimal RecoveryPlanMinCoverage = 0.5m;
        public const int RecoveryPlanCooldownDays = 720;
    }
}
