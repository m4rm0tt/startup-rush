namespace Core.campaign;

/// <summary>
/// Toutes les valeurs d'équilibrage au même endroit. Ce sont des réglages proposés, pas des décisions de design :
/// DESIGN_SIMULATION.md explique d'où elles viennent et BALANCE_REPORT.md ce que les simulations en ont montré.
/// </summary>
public static class Tuning
{
    public static class Start
    {
        /// <summary>
        /// Économies et petit prêt d'honneur : environ 8 mois de charges fixes. De quoi sortir un premier projet
        /// (2 à 4 semaines) et voir ses premiers résultats avant d'être à court.
        /// </summary>
        public const decimal Cash = 18_000m;

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

        /// <summary>
        /// Niveau minimal dans une discipline pour y travailler. 0 : tout le monde peut tout faire (plus lentement et moins bien
        /// hors de sa spécialité), pour qu'un projet ne soit jamais bloqué faute d'une compétence.
        /// </summary>
        public const int MinimumSkill = 0;

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

    public static class Projects
    {
        /// <summary>Part du temps de l'équipe d'un projet en ligne consacrée au support et à la maintenance, selon l'entretien choisi.</summary>
        public const double UpkeepLow = 0.1;

        public const double UpkeepNormal = 0.25;
        public const double UpkeepHigh = 0.45;

        /// <summary>Utilisateurs actifs à atteindre pour qu'un lancement rapporte des points de développement (pas de points pour un projet vide).</summary>
        public const int AdoptionForPoints = 50;

        /// <summary>
        /// Multiplicateur d'effort des composants. Les trois composants de départ d'une application demandent environ
        /// 92 points : quelque 380 mots au terminal pour le fondateur seul (une douzaine de minutes de saisie).
        /// </summary>
        public const double EffortScale = 2.2;
    }

    /// <summary>Le terminal du fondateur : son seul moyen de produire (voir Campaign.Terminal.cs).</summary>
    public static class Terminal
    {
        /// <summary>Points de travail d'un mot validé pour un niveau 50 sur un poste standard, à vitesse normale.</summary>
        public const double PointsPerWord = 0.25;

        /// <summary>Mots d'une session (20 à 45 secondes) ; on peut en enchaîner une autre aussitôt.</summary>
        public const int WordsPerSession = 24;

        /// <summary>Mots d'une session avec les macros (arbre « Outils du boss »).</summary>
        public const int WordsPerSessionWithMacros = 32;

        /// <summary>Vitesse de frappe (caractères par seconde) : lente, normale, rapide, et le bonus au plus.</summary>
        public const double SlowCharsPerSecond = 1.5;

        public const double FastCharsPerSecond = 6.5;
        /// <summary>Production au terminal à distance (Supervision à distance), sans poste complet : celle d'un vieux portable.</summary>
        public const double RemoteOutput = 0.85;

        /// <summary>Saisie simplifiée (accessibilité) : lettres écrites par seconde avant que le mot puisse être validé.</summary>
        public const double AssistedCharsPerSecond = 3;

        public const double SlowFactor = 0.85;
        public const double FastFactor = 1.2;

        /// <summary>Énergie dépensée par mot validé (environ 240 mots avant d'être épuisé, en partant reposé).</summary>
        public const double EnergyPerWord = 0.35;

        /// <summary>Heures où le fondateur peut travailler au terminal.</summary>
        public const int FirstHour = 7;

        public const int LastHour = 23;
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
        public const decimal FounderDraw = 2_000m;

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
