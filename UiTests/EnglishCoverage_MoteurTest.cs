using System.Reflection;
using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Core.campaign;
using Core.finance;
using Core.persistence;
using Core.products;
using Core.progress;
using Core.team;
using UI;
using UI.Hud;
using UI.Hud.Panels;

namespace UiTests;

/// <summary>
/// Lot 6, zone « moteur » : les phrases du moteur de la campagne (journal, alertes, équipe, finances, tutoriel, terminal,
/// bilans, sauvegardes) et des panneaux qui les montrent, en anglais. Chaque test part du code réel (étapes du tutoriel,
/// catégories, métiers, commandes, notifications d'une vraie partie) et vérifie que rien ne reste d'apparence française.
/// </summary>
public class EnglishCoverage_MoteurTest
{
    private string _previous = "fr";

    [SetUp]
    public void SetUp() { _previous = Loc.Language; Loc.Language = "en"; Loc.Misses.Clear(); }

    [TearDown]
    public void TearDown() { Loc.Language = _previous; Loc.RecordMisses = false; Loc.Misses.Clear(); }

    /// <summary>Noms propres d'une partie (équipe, produits, entreprise, concurrents) : jamais traduits, donc écartés du contrôle.</summary>
    private static List<string> ProperNames(Campaign c)
    {
        var names = new List<string> { "Léman Ventures", "Alpine Angels", "Romandie Seed" };
        names.AddRange(c.People.Select(p => p.Name));
        names.AddRange(c.Candidates.Select(p => p.Person.Name));
        names.AddRange(c.Products.Select(p => p.Name));
        names.AddRange(c.Companies.Select(p => p.Name));
        names.AddRange(c.Competitors.Select(p => p.Name));
        names.AddRange(c.Sites.Select(s => s.Name));
        names.AddRange(c.Sites.Select(s => s.Name.Split(',')[0]));
        names.Add(c.Name);
        names.Add(c.FounderName);
        return names.Where(n => n.Length > 1).Distinct().OrderByDescending(n => n.Length).ToList();
    }

    /// <summary>Le texte tel que le joueur anglais le lit, ou null s'il est entièrement traduit.</summary>
    private static string? Untranslated(string french, IEnumerable<string>? names = null)
    {
        string shown = Loc.Translate(french);
        foreach (string name in names ?? [])
            shown = shown.Replace(name, "X");
        return Loc.LooksFrench(shown) ? $"{french}  =>  {shown}" : null;
    }

    private static void AssertTranslated(IEnumerable<string> texts, IEnumerable<string>? names = null)
    {
        var failures = texts.Where(t => !string.IsNullOrWhiteSpace(t)).Distinct().Select(t => Untranslated(t, names)).Where(t => t != null).ToList();
        Assert.That(failures, Is.Empty, string.Join("\n", failures));
    }

    private static Campaign NewCampaign(ulong seed = 42)
    {
        var campaign = Campaign.New("Moteur", "Alex", "Studio Test", FounderProfile.Generalist, seed);
        campaign.FinalGoalAcknowledged = true;
        return campaign;
    }

    // ---------------------------------------------------------------------------------------------------- Tutoriel

    [Test]
    public void EveryTutorialStepIsTranslated()
    {
        Assert.That(Campaign.TutorialSteps, Is.Not.Empty);
        var texts = new List<string>();
        foreach (var step in Campaign.TutorialSteps)
        {
            texts.Add(step.Title);
            texts.Add(step.Text);
            if (step.Key != null)
                texts.Add(step.Key);
            Assert.That(Loc.Translate(step.Title), Is.Not.EqualTo(step.Title), $"{step.Id} : titre");
            Assert.That(Loc.Translate(step.Text), Is.Not.EqualTo(step.Text), $"{step.Id} : texte");
        }
        AssertTranslated(texts);
        // Les étapes du tutoriel nomment les menus de la barre d'outils : le nom anglais est le même qu'en bas de l'écran
        Assert.That(Loc.Translate(Campaign.TutorialSteps.First(s => s.Id == "money").Text), Does.Contain("Open Finances"));
        Assert.That(Loc.Translate("Tutoriel 3/11 · Ton premier projet"), Is.EqualTo("Tutorial 3/11 · Your first project"));
        Assert.That(Loc.Translate(Campaign.TutorialSteps.First(s => s.Key == "Entrée").Key!), Is.EqualTo("Enter"));
    }

    // ---------------------------------------------------------------------------------------------------- Calendrier, métiers, finances

    [Test]
    public void DaysMonthsAndDatesAreTranslated()
    {
        foreach (var day in Enum.GetValues<DayOfWeek>())
            Assert.That(Loc.Translate(GameTime.DayName(day)), Is.Not.EqualTo(GameTime.DayName(day)), day.ToString());
        Assert.That(Loc.Translate(GameTime.Format(0)), Is.EqualTo("Mon 04.01.2027 00:00"));
        Assert.That(Loc.Translate("Partie · " + GameTime.Format(GameTime.StartMinute)), Is.EqualTo("Partie · Mon 04.01.2027 08:00"));
        var months = Enumerable.Range(1, 12).Select(m => new DateTime(2027, m, 1).ToString("MMMM")).ToList();
        AssertTranslated(months);
        Assert.That(Loc.Translate($"Bilan de {months[1]}"), Is.EqualTo("February summary"));
        Assert.That(Loc.Translate(GameTime.Format(0).Replace("Lun", "Mar")), Does.StartWith("Tue "));
    }

    [AvaloniaTest]
    public void JobsLevelsTitlesAndFinanceNamesAreTranslated()
    {
        var texts = new List<string>();
        foreach (var discipline in Enum.GetValues<Discipline>())
        {
            texts.Add(Person.DisciplineName(discipline));
            texts.Add(Person.DisciplineName(discipline).ToLowerInvariant());
            texts.Add(Person.SpecialtyName(discipline));
            foreach (double skill in new[] { 10.0, 50.0, 80.0 })
            {
                var person = new Person { Skills = new() { [discipline] = skill } };
                texts.Add(person.Level);
                texts.Add(person.Title);
                texts.Add($"Léa · {person.Title}");
                texts.Add($"{person.Title} · 4 100 CHF/mois");
            }
        }
        texts.Add(new Person { IsFounder = true, Skills = new() { [Discipline.Code] = 50 } }.Title);
        texts.AddRange(Enum.GetValues<SolvencyState>().Select(Solvency.Name));
        texts.AddRange(Enum.GetValues<Category>().Select(FinancePanel.CategoryName));
        foreach (var (_, name, description) in Campaign.MilestoneDefs)
        {
            texts.Add(name);
            texts.Add(description);
        }
        AssertTranslated(texts, ["Léa"]);
        Assert.That(Loc.Translate(new Person { Skills = new() { [Discipline.Code] = 50 } }.Title), Is.EqualTo("Mid-level developer"));
        Assert.That(Loc.Translate(Solvency.Name(SolvencyState.Healthy)), Is.EqualTo("Healthy"));
    }

    [Test]
    public void TerminalWordsStayTypableAndEveryTerminalSentenceIsTranslated()
    {
        // Les mots à taper ne se traduisent jamais : ce qui est affiché doit rester ce que le joueur doit écrire
        foreach (var word in new[] { TerminalWords.Design, TerminalWords.Code, TerminalWords.Test, TerminalWords.Support, TerminalWords.Research }.SelectMany(w => w))
            Assert.That(Loc.Translate(word), Is.EqualTo(word), $"mot à taper « {word} »");

        // Les raisons de refus et de fin de session : vrais scénarios
        var c = NewCampaign();
        var texts = new List<string>();
        texts.Add(c.TerminalTitle(Campaign.ResearchTerminal));
        texts.Add($"terminal · {c.TerminalTitle(Campaign.ResearchTerminal)}");
        texts.Add(c.CanOpenTerminal(Campaign.ResearchTerminal).Reason);
        texts.Add(c.CanOpenTerminal(12345).Reason);
        c.CreateProject("Appli", "freelances", RevenueModel.Subscription);
        int id = c.Products[0].Id;
        texts.Add(c.TerminalTitle(id));
        var open = c.CanOpenTerminal(id);
        Assert.That(open.Ok, Is.True, open.Reason);
        Assert.That(c.OpenTerminal(id).Ok, Is.True);
        texts.Add(c.CanOpenTerminal(id).Reason);
        texts.Add(c.Terminal!.Stage);
        for (int i = 0; i < 80 && c.Terminal is { Finished: false }; i++)
        {
            c.SubmitWord(c.Terminal.Word, 1.0);
            texts.Add(c.Terminal!.Stage);
        }
        texts.Add(c.Terminal!.EndReason);
        c.CloseTerminal();
        c.Status = CampaignStatus.Liquidated;
        texts.Add(c.CanOpenTerminal(id).Reason);
        c.Status = CampaignStatus.Running;
        c.Founder.TrainingDiscipline = Discipline.Code;
        texts.Add(c.CanOpenTerminal(id).Reason);
        c.Founder.TrainingDiscipline = null;
        c.Founder.VacationUntil = c.Minute + 10_000;
        texts.Add(c.CanOpenTerminal(id).Reason);
        c.Founder.VacationUntil = 0;
        c.Advance(14 * 60);
        texts.Add(c.CanOpenTerminal(id).Reason);
        // Recherche : session de préparation
        c.CheatUnlockAll();
        c.CheatMoney(1_000_000);
        AssertTranslated(texts, ProperNames(c));
        Assert.That(Loc.Translate("Session complète."), Is.EqualTo("Session complete."));
    }

    // ---------------------------------------------------------------------------------------------------- Sauvegardes

    [Test]
    public void SaveLabelsAndMessagesAreTranslated()
    {
        string root = Path.Combine(Path.GetTempPath(), "startuprush_uitests", Guid.NewGuid().ToString("N"));
        var store = new SaveStore(root);
        var campaign = NewCampaign();
        store.Save(campaign, SaveKind.Auto);
        store.Save(campaign, SaveKind.Quick);
        string folder = store.CampaignFolder(campaign.Id);
        File.Copy(Path.Combine(folder, "exit.json"), Path.Combine(folder, SaveStore.EmergencyName + ".json"));
        var entries = store.List(campaign.Id);
        Assert.That(entries.Select(e => e.Label), Has.Some.StartWith("Sauvegarde d'urgence"));
        AssertTranslated(entries.Select(e => e.Label));
        AssertTranslated(entries.Where(e => e.Summary != null).Select(e => e.Summary!.Situation));
        campaign.Status = CampaignStatus.Liquidated;
        AssertTranslated([SaveStore.Summarize(campaign).Situation]);
        Assert.That(Loc.Translate(entries.First(e => e.Label.StartsWith("Sauvegarde d'urgence")).Label), Is.EqualTo("Emergency save (after an error)"));
        Assert.That(Loc.Translate("Saine"), Is.EqualTo("Healthy"));
        Assert.That(Loc.Translate("Partie sauvegardée : « Ma partie »"), Is.EqualTo("Game saved: “Ma partie”"));

        // Fichiers abîmés : les messages de chargement
        string bad = Path.Combine(folder, "manual-casse.json");
        File.WriteAllText(bad, "{ pas du json");
        var (loaded, message) = store.LoadWithRecovery(bad);
        var texts = new List<string> { message, "Partie chargée.", "Sauvegarde automatique (04.01.2027)", "Partie sauvegardée", "Sauvegarde impossible : accès refusé" };
        foreach (string text in texts.Where(t => t != ""))
            Assert.That(Loc.Translate(text), Is.Not.EqualTo(text), text);
    }

    // ---------------------------------------------------------------------------------------------------- Messages du moteur

    private static readonly Regex[] OwnTitles =
    [
        new(@"^Bilan de "), new(@"^Facture jamais payée"), new(@"^Factures impayées"), new(@"n'a pas de poste$"), new(@"n'a plus le moral$"),
        new(@"démissionne$"), new(@"reste finalement$"), new(@"^Mise en demeure"), new(@"^Faillite"), new(@"^L'argent va manquer"),
        new(@"^Jalon : "), new(@"dans les cartons$"), new(@"^Bienvenue chez "), new(@"point.? de développement$"), new(@"^Tutoriel terminé"),
        new(@"^Objectif atteint"), new(@"n'avance pas$"), new(@"tombe en panne$"), new(@"le support est débordé$"), new(@"veut investir$"),
        new(@"^Plan de redressement"), new(@"rejoint l'équipe$"), new(@"nouvelle version$"), new(@" est parti·e\.$"), new(@"a été licencié"),
    ];

    private static IEnumerable<string> OwnNoticeTexts(Campaign c) =>
        c.Notices.Where(n => OwnTitles.Any(r => r.IsMatch(n.Title))).SelectMany(n => new[] { n.Title, n.Detail, n.Category });

    [Test]
    public void DailyNoticesBillsAndTheLedgerAreTranslatedInARealGame()
    {
        var c = NewCampaign();
        c.CheatMoney(40_000);
        c.CreateProject("Appli", "freelances", RevenueModel.Subscription);
        c.CreateProject("Jeu", "casual", RevenueModel.Purchase);
        foreach (var candidate in c.Candidates.Take(2).ToList())
            Assert.That(c.Hire(candidate.Person.Id).Ok, Is.True);
        // 100 jours : paies, loyers, mois, jalons, démissions éventuelles, factures
        for (int i = 0; i < 100; i++)
        {
            c.AdvanceDays(1);
            if (i == 70)
                c.TakeLoan(5_000);
        }
        var texts = new List<string>(OwnNoticeTexts(c));
        texts.AddRange(c.Ledger.Bills.Select(b => b.Label));
        texts.AddRange(c.Ledger.Entries.Select(e => e.Label));
        texts.AddRange(c.People.Select(p => p.Status));
        texts.Add(c.MonthSummary(c.Ledger.Months.Keys.First()));
        texts.Add(c.Runway().Assumptions);
        texts.Add(c.Notices.First(n => n.Title.StartsWith("Bienvenue")).Detail);
        var names = ProperNames(c);
        AssertTranslated(texts, names);
        Assert.That(c.Notices.Select(n => n.Title), Has.Some.StartsWith("Bilan de "));
        Assert.That(c.Notices.Select(n => n.Title), Has.Some.StartsWith("Jalon : "));
        Assert.That(Loc.Translate(c.Notices.First(n => n.Title.StartsWith("Bilan de ")).Title), Does.EndWith(" summary"));
    }

    [Test]
    public void BankruptcyAndTheReportAreTranslated()
    {
        var c = NewCampaign(7UL);
        foreach (var candidate in c.Candidates.Take(3).ToList())
            c.Hire(candidate.Person.Id);
        c.CheatMoney(-c.Ledger.Cash - c.Ledger.Overdraft - 100_000);
        for (int i = 0; i < 400 && c.Status == CampaignStatus.Running; i++)
            c.AdvanceDays(1);
        Assert.That(c.Status, Is.EqualTo(CampaignStatus.Liquidated));
        var texts = new List<string>(OwnNoticeTexts(c));
        texts.AddRange(c.Notices.Where(n => n.Severity == Severity.Critical).SelectMany(n => new[] { n.Title, n.Detail }));
        foreach (var line in c.Solvency.LiquidationReport!.Split('\n'))
            texts.Add(line);
        texts.Add(c.Solvency.LiquidationReport!);
        AssertTranslated(texts.Where(t => !t.StartsWith("Impayés :")), ProperNames(c));
        // La ligne des impayés cite des libellés de dettes : au moins son début est traduit
        string unpaid = c.Solvency.LiquidationReport!.Split('\n').First(l => l.StartsWith("Impayés :"));
        Assert.That(Loc.Translate(unpaid), Does.StartWith("Unpaid: "));
        Assert.That(c.Notices.Select(n => n.Title), Has.Some.EqualTo("Faillite"));
    }

    [Test]
    public void CommandMessagesOfTheWholeEngineAreTranslated()
    {
        var c = NewCampaign();
        var messages = new List<string>();
        c.CreateProject("Appli", "freelances", RevenueModel.Subscription);

        // Chaque commande de la campagne, appelée avec des arguments vides ou inconnus : les refus
        var calls = 0;
        foreach (var method in typeof(Campaign).GetMethods(BindingFlags.Public | BindingFlags.Instance).Where(m => m.ReturnType == typeof(CommandResult)))
        {
            var args = new List<object?>();
            bool supported = true;
            foreach (var parameter in method.GetParameters())
            {
                var type = Nullable.GetUnderlyingType(parameter.ParameterType) ?? parameter.ParameterType;
                if (parameter.HasDefaultValue) args.Add(parameter.DefaultValue);
                else if (Nullable.GetUnderlyingType(parameter.ParameterType) != null) args.Add(null);
                else if (type == typeof(string)) args.Add("zzz");
                else if (type == typeof(int)) args.Add(-1);
                else if (type == typeof(long)) args.Add(-1L);
                else if (type == typeof(decimal)) args.Add(-1m);
                else if (type == typeof(double)) args.Add(-1.0);
                else if (type == typeof(bool)) args.Add(false);
                else if (type.IsEnum) args.Add(Activator.CreateInstance(type));
                else { supported = false; break; }
            }
            if (!supported)
                continue;
            try
            {
                var result = (CommandResult)method.Invoke(c, args.ToArray())!;
                calls++;
                messages.Add(result.Message);
            }
            catch (TargetInvocationException)
            {
            }
        }
        Assert.That(calls, Is.GreaterThan(40));

        // Les vrais gestes de l'équipe et des finances
        var candidate = c.Candidates[0];
        messages.Add(c.Hire(candidate.Person.Id).Message);
        messages.Add(c.Hire(candidate.Person.Id).Message);
        var hired = c.Staff.First();
        messages.Add(c.Fire(c.Founder.Id).Message);
        messages.Add(c.Assign(hired.Id, c.Products[0].Id).Message);
        messages.Add(c.SetLead(hired.Id, true).Message);
        messages.Add(c.Train(hired.Id, Discipline.Code).Message);
        messages.Add(c.Train(hired.Id, Discipline.Code).Message);
        messages.Add(c.Vacation(hired.Id).Message);
        messages.Add(c.Vacation(c.Founder.Id).Message);
        messages.Add(c.Raise(c.Founder.Id).Message);
        messages.Add(c.Raise(hired.Id).Message);
        messages.Add(c.Raise(hired.Id).Message);
        messages.Add(c.Raise(hired.Id).Message);
        messages.Add(c.TakeLoan(1).Message);
        messages.Add(c.TakeLoan(100_000_000).Message);
        messages.Add(c.AcceptInvestor().Message);
        messages.Add(c.RequestRecoveryPlan().Message);
        messages.Add(c.Spend(10_000_000m, Category.Equipment, "x").Message);
        messages.Add(c.Unlock(DevTree.Nodes.First().Id).Message);
        messages.Add(c.Unlock(DevTree.Nodes.First().Id).Message);
        messages.Add(c.Unlock(DevTree.Nodes.Last().Id).Message);
        messages.AddRange(c.LoanTerms().Condition is { } cond ? [cond] : []);
        messages.Add(c.RecoveryPlanTerms().Condition);

        // Menu de test
        messages.Add(c.CheatMoney(10_000).Message);
        messages.Add(c.CheatPoints(5).Message);
        messages.Add(c.CheatUnlockAll().Message);
        messages.Add(c.CheatFinishWork().Message);
        messages.Add(c.CheatHire().Message);
        messages.Add(c.CheatRefresh().Message);
        messages.Add(c.CheatStock().Message);
        messages.Add(c.CheatUsers(1000).Message);
        messages.Add(c.CheatClearDebts().Message);
        messages.Add(c.CheatAdvance(3).Message);
        messages.Add(c.Unlock(DevTree.Nodes.First().Id).Message);

        // Noms d'entreprise refusés
        foreach (string name in new[] { "", "a", new string('x', 200) })
            messages.Add(Campaign.CompanyNameError(name) ?? "");

        AssertTranslated(messages.Where(m => m != ""), ProperNames(c));
    }

    // ---------------------------------------------------------------------------------------------------- Interface

    private static List<string> ScreenTexts(Window window)
    {
        var texts = new List<string>();
        foreach (var visual in window.GetVisualDescendants())
        {
            if (visual is TextBlock { Text: { Length: > 0 } text })
                texts.Add(text);
            if (visual is Control control && ToolTip.GetTip(control) is string tip)
                texts.Add(Loc.Translate(tip));
            if (visual is TextBox { PlaceholderText: { Length: > 0 } placeholder })
                texts.Add(Loc.Translate(placeholder));
        }
        return texts;
    }

    /// <summary>
    /// Notifications écrites par d'autres zones (lancements, industrie, sites, catalogue du mobilier) : le journal les montre,
    /// mais leur traduction n'est pas dans cette zone.
    /// </summary>
    private static readonly Regex OtherZones = new(@"est sorti|est prêt à sortir|premiers utilisateurs|Lance-le quand|clients repartent|^Rent : |Trophée|Plaque", RegexOptions.Compiled);

    /// <summary>Les textes visibles d'un écran, hors noms propres, qui ressemblent encore à du français.</summary>
    private static List<string> FrenchLeft(Window window, Campaign campaign, params string[] allowed)
    {
        var names = ProperNames(campaign).Concat(allowed).OrderByDescending(n => n.Length).ToList();
        return ScreenTexts(window).Distinct().Where(t => !OtherZones.IsMatch(t)).Where(t =>
        {
            string shown = t;
            foreach (string name in names)
                shown = shown.Replace(name, "X");
            return Loc.LooksFrench(shown);
        }).ToList();
    }

    [AvaloniaTest]
    public void MoteurPanelsHaveNoFrenchLeftInEnglish()
    {
        var (window, game, campaign) = PanelsTest.Advanced();
        window.SetLanguage("en");
        // Une partie qui a de quoi alerter : retard de paie, moral bas, investisseur, prêt
        var staff = campaign.Staff.ToList();
        campaign.CheatHire();
        campaign.CheatHire();
        foreach (var person in campaign.Staff)
            person.LeavesAt = campaign.Minute + 5 * GameTime.MinutesPerDay;
        campaign.Ledger.Cash = -2_000;
        campaign.AdvanceDays(3);
        campaign.TakeLoan(1_000);

        var failures = new List<string>();
        void Scan(string label, GamePanel panel)
        {
            game.OpenRoot(panel);
            game.Tick(0.3);
            Dispatcher.UIThread.RunJobs();
            foreach (string text in FrenchLeft(window, campaign, "Marketing"))
                failures.Add($"{label} : {text}");
        }

        Scan("Équipe", new TeamPanel());
        Scan("Fiche d'un employé", new PersonPanel(campaign.Staff.First().Id));
        Scan("Fiche du fondateur", new PersonPanel(campaign.Founder.Id));
        Scan("Finances", new FinancePanel());
        Scan("Journal à traiter", new NoticesPanel());
        Scan("Journal bonnes nouvelles", new NoticesPanel().ShowGoodNews());
        Scan("Menu de test", new CheatPanel());

        // Finances en difficulté : mise en demeure, plan de redressement
        campaign.Ledger.Cash = -1_000_000;
        campaign.AdvanceDays(40);
        Scan("Finances en difficulté", new FinancePanel());
        Scan("Journal en difficulté", new NoticesPanel());

        // Investisseur et prêt
        campaign.Ledger.Cash = 500_000;
        campaign.Investor = new InvestorOffer { Investor = "Léman Ventures", Amount = 100_000, Equity = 0.15, ExpiresAt = campaign.Minute + 5000, ProductId = campaign.Products[0].Id };
        Scan("Finances avec un investisseur", new FinancePanel());

        window.Close();
        Assert.That(failures.Distinct(), Is.Empty, string.Join("\n", failures.Distinct()));
    }

    [AvaloniaTest]
    public void TheGameScreenTutorialTerminalAndSummariesAreInEnglish()
    {
        string root = Path.Combine(Path.GetTempPath(), "startuprush_uitests", Guid.NewGuid().ToString("N"));
        var window = new MainWindow(new SaveStore(root), new Settings(), Path.Combine(root, "settings.json"));
        var campaign = NewCampaign();
        campaign.TutorialOn = true;
        window.Show();
        var game = window.StartGame(campaign, null);
        window.SetLanguage("en");
        Dispatcher.UIThread.RunJobs();
        game.Session.Settings.PauseInLargeMenus = true;
        var failures = new List<string>();
        void Scan(string label)
        {
            game.Tick(0.3);
            Dispatcher.UIThread.RunJobs();
            foreach (string text in FrenchLeft(window, campaign))
                failures.Add($"{label} : {text}");
        }

        // Le coach, étape par étape (les vraies étapes du moteur)
        for (int i = 0; i < Campaign.TutorialSteps.Count; i++)
        {
            campaign.TutorialIndex = i;
            Scan($"tutoriel {Campaign.TutorialSteps[i].Id}");
            var step = Campaign.TutorialSteps[i];
            Assert.That(ScreenTexts(window), Has.Some.Contains(Loc.Translate(step.Title)), $"le coach montre « {step.Title} »");
        }
        campaign.TutorialOn = false;

        // Barre du haut, barre d'outils, notifications, panneau d'un grand menu qui met le temps en pause
        Scan("écran de jeu");
        game.OpenRoot(new FinancePanel());
        Scan("pause automatique d'un grand menu");
        Assert.That(ScreenTexts(window), Has.Some.StartsWith("PAUSE: finances"));
        game.OpenRoot(new TeamPanel());
        Scan("équipe");

        // Terminal : titre, étape, statistiques, raisons de fin
        campaign.CreateProject("Appli", "freelances", RevenueModel.Subscription);
        game.ClosePanels();
        game.OpenTerminal(campaign.Products[0].Id);
        Scan("terminal");
        var terminal = game.OpenTerminalView!;
        for (int i = 0; i < 60 && campaign.Terminal is { Finished: false }; i++)
        {
            terminal.SetInput(campaign.Terminal.Word);
            terminal.Submit();
        }
        Scan("terminal : fin de session");
        Assert.That(ScreenTexts(window), Has.Some.StartsWith("terminal · Appli"));
        game.CloseTerminal();

        // Menu pause, bilans de réussite et de faillite
        game.HandleKey(Avalonia.Input.Key.Escape, Avalonia.Input.KeyModifiers.None);
        Scan("menu pause");
        game.HandleKey(Avalonia.Input.Key.Escape, Avalonia.Input.KeyModifiers.None);
        var summary = UI.Screens.GameScreen.SummaryView(campaign.Summary());
        var host = new Window { Content = summary };
        host.Show();
        Dispatcher.UIThread.RunJobs();
        foreach (string text in FrenchLeft(host, campaign))
            failures.Add($"bilan : {text}");
        host.Close();

        // Bilans : réussite (valorisation atteinte) puis faillite
        campaign.FinalGoalReachedAt = campaign.Minute;
        campaign.FinalGoalAcknowledged = false;
        Scan("bilan de réussite");
        Assert.That(ScreenTexts(window), Does.Contain("Goal reached!"));
        campaign.Status = CampaignStatus.Liquidated;
        campaign.Solvency.LiquidationReport = "Faillite prononcée le 17.03.2027, après 72 jours.\nÉquipe : 2 personne(s). Projets en ligne : 1.";
        Scan("bilan de faillite");
        Assert.That(ScreenTexts(window), Does.Contain("Company liquidated"));
        campaign.Status = CampaignStatus.Running;

        // Une notification devient une bulle
        campaign.Hire(campaign.Candidates[0].Person.Id);
        game.Toast("Ce candidat n'est plus disponible.", false);
        Scan("toast");

        window.Close();
        Assert.That(failures.Distinct(), Is.Empty, string.Join("\n", failures.Distinct()));
    }
}
