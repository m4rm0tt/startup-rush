using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Core.campaign;
using Core.finance;
using Core.persistence;
using UI.Audio;
using UI.Game;
using UI.Hud;
using UI.Hud.Panels;
using UI.Pixel;
using UI.Scene;

namespace UI.Screens;

/// <summary>
/// L'écran de jeu : la scène occupe toute la fenêtre. Par-dessus, une barre d'état discrète en haut,
/// une barre d'outils compacte en bas, et un seul menu ouvert à la fois (avec retour). Échap ferme d'abord
/// la couche active ; seulement ensuite il ouvre la pause. Il n'abandonne jamais la partie.
/// </summary>
public class GameScreen : UserControl, IScreen
{
    public MainWindow Window { get; }
    public GameSession Session { get; }
    public SceneCanvas Scene { get; }

    private readonly Grid _hud = new();
    private readonly LayoutTransformControl _scaler;
    private readonly ContentControl _topBar = new();
    private readonly PixelPanel _panelFrame = new() { Skin = "pack:hud_window", Padding = new Thickness(12, 4, 12, 14) };
    private readonly ContentControl _panelBody = new();
    private readonly ContentControl _inspector = new() { HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Bottom };
    private readonly StackPanel _toasts = new() { Spacing = 6, Width = 380, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(10, 0, 0, 84) };
    private readonly Grid _modalLayer = new() { IsVisible = false, Background = new SolidColorBrush(Color.Parse("#962b2128")) };
    private readonly ContentControl _modalHost = new() { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
    private readonly StackPanel _toolbar = new() { Orientation = Orientation.Horizontal, Spacing = 4 };

    /// <summary>La consigne du tutoriel en cours (en haut à gauche ; en bas à gauche quand un grand menu est ouvert).</summary>
    private readonly ContentControl _coach = new() { HorizontalAlignment = HorizontalAlignment.Left, Width = 360 };

    private readonly List<GamePanel> _stack = new();
    private Action? _modalClose;

    // Terminal du fondateur : un calque à part (le temps continue), et la vitesse d'avant à restaurer
    private readonly ContentControl _terminalLayer = new() { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(0, 0, 0, 90) };
    private TerminalView? _terminal;
    private (int Speed, bool Paused) _beforeTerminal;
    private bool _pauseMenu;

    /// <summary>Le panneau ouvert au-dessus des autres (null : aucun) ; lu par les tests d'interface.</summary>
    public GamePanel? TopPanel => _stack.LastOrDefault();

    /// <summary>Le menu de pause est ouvert.</summary>
    public bool PauseMenuOpen => _pauseMenu;

    /// <summary>Le terminal du fondateur, s'il est ouvert.</summary>
    public TerminalView? OpenTerminalView => _terminal;
    private double _refreshTimer;
    private double _hudTimer;
    private int _lastPremises;
    private bool _lostShown;

    public GameScreen(MainWindow window, GameSession session)
    {
        Window = window;
        Session = session;
        Scene = new SceneCanvas(session);
        Scene.Feedback += (message, ok) =>
        {
            if (!string.IsNullOrEmpty(message))
                Toast(message, ok);
            RefreshPanel();
        };
        session.SelectionChanged += () =>
        {
            BuildInspector();
            if (_stack.LastOrDefault() is BuildPanel)
                RefreshPanel();
        };
        _lastPremises = session.Campaign.Premises.Id;

        var root = new Grid();
        root.Children.Add(Scene);

        // Interface par-dessus la scène, mise à l'échelle selon la fenêtre ou le réglage
        _hud.Children.Add(_topBar);
        _topBar.VerticalAlignment = VerticalAlignment.Top;

        var toolbarFrame = new PixelPanel { Padding = new Thickness(8, 6, 8, 8), Child = _toolbar, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(0, 0, 0, 8) };
        _toolbarFrame = toolbarFrame;
        _hud.Children.Add(toolbarFrame);
        BuildToolbar();

        _inspector.Margin = new Thickness(10, 0, 0, 10);
        _hud.Children.Add(_inspector);
        // Les bulles se posent au-dessus de la fiche de sélection (elles la recouvraient en bas à gauche)
        _inspector.SizeChanged += (_, e) => _inspectorHeight = e.NewSize.Height;
        _panelFrame.Child = new Grid { Children = { _panelBody, PanelHandle() } };
        _panelFrame.IsVisible = false;
        _hud.Children.Add(_panelFrame);
        _hud.Children.Add(_coach);
        _hud.Children.Add(_toasts);

        _hud.Children.Add(_terminalLayer);
        _modalLayer.Children.Add(_modalHost);
        _modalLayer.PointerPressed += (_, e) => e.Handled = true; // aucun clic ne traverse
        _hud.Children.Add(_modalLayer);

        _scaler = new LayoutTransformControl { Child = _hud };
        root.Children.Add(_scaler);
        // Un seul curseur de base pour tout l'écran (celui du pack) : passer de la scène à un menu ne fait plus alterner
        // la flèche du pack et celle de Windows ; les boutons et les champs gardent le leur (App.axaml)
        root.Cursor = PackSkins.Cursor("cursor_normal");
        // Un clic en cours : aucun menu ne se met à jour sous la souris entre l'appui et le relâchement
        AddHandler(PointerPressedEvent, (_, _) => _pointerDown = true, Avalonia.Interactivity.RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(PointerReleasedEvent, (_, _) => _pointerDown = false, Avalonia.Interactivity.RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(PointerCaptureLostEvent, (_, _) => _pointerDown = false, Avalonia.Interactivity.RoutingStrategies.Tunnel, handledEventsToo: true);
        // Les clics sur l'interface ne doivent pas atteindre la scène : seule la zone transparente laisse passer
        _hud.Background = null;
        Content = root;

        BuildTopBar();
        BuildInspector();
        SizeChanged += (_, _) => ApplyScale();
        ApplyScale();
    }

    private double UiScale
    {
        get
        {
            if (Window.Settings.UiScale > 0)
                return Window.Settings.UiScale;
            double w = Bounds.Width > 0 ? Bounds.Width : Window.Width;
            double h = Bounds.Height > 0 ? Bounds.Height : Window.Height;
            return UI.Pixel.PixelText.AutoScale(w, h, Window.RenderScaling);
        }
    }

    public void ApplyScale()
    {
        double s = UiScale;
        _scaler.LayoutTransform = new ScaleTransform(s, s);
        LayoutPanelFrame();
    }

    // ------------------------------------------------------------------ Boucle

    public void Tick(double deltaTime)
    {
        Session.Update(deltaTime);
        Scene.Tick(deltaTime);
        _terminal?.Tick();
        if (_terminal != null && Session.Campaign.Terminal == null)
            CloseTerminal();

        while (Session.NewNotices.Count > 0)
        {
            var notice = Session.NewNotices.Dequeue();
            // Les bulles ne montrent que ce qui compte : bonnes nouvelles et problèmes, une seule fois par problème.
            // Une proposition de grosse action attend la fin de la saisie : rien n'interrompt le terminal.
            if (notice.Key == Campaign.ReadyKey && _terminal != null)
                _deferred.Add(notice);
            else if (notice.Severity != Severity.Info && notice.Count == 1)
                ToastNotice(notice);
        }
        if (_terminal == null && _deferred.Count > 0)
        {
            foreach (var notice in _deferred.Where(n => !n.Resolved).Distinct())
                ToastNotice(notice);
            _deferred.Clear();
        }
        UpdateToasts(deltaTime);

        if (Session.Campaign.Premises.Id != _lastPremises)
        {
            _lastPremises = Session.Campaign.Premises.Id;
            Scene.ResetCamera();
        }

        _hudTimer += deltaTime;
        if (_hudTimer > 0.25)
        {
            _hudTimer = 0;
            if (Session.Campaign.CheckTutorial())
            {
                Sfx.Play(Sound.Milestone);
                BuildToolbar();
            }
            BuildCoach();
            BuildTopBar();
            if (Session.SelectionKind == SelectionKind.Person)
                BuildInspector();
        }

        _refreshTimer += deltaTime;
        // Pas de mise à jour pendant un clic ou sous un menu ouvert (liste déroulante, menu « … ») : elle reprend juste après
        if (_refreshTimer > 1 && _stack.LastOrDefault() is { AutoRefresh: true } && !TextInputFocused() && !_pointerDown && HudKit.OpenFlyout == null)
        {
            _refreshTimer = 0;
            RefreshPanel();
        }

        Sfx.PlayMusic(Session.Campaign.Solvency.State is SolvencyState.Notice or SolvencyState.Overdue ? Music.Crisis : Music.Work);

        if (Session.Campaign.Status == CampaignStatus.Liquidated && !_lostShown)
        {
            _lostShown = true;
            ShowDefeat();
        }
        else if (Session.Campaign.FinalGoalReachedAt != null && !Session.Campaign.FinalGoalAcknowledged && !_modalLayer.IsVisible)
            ShowVictory();
    }

    private bool TextInputFocused() => TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() is TextBox;

    private bool _pointerDown;

    public void Leave() => Session.Detach();

    // ------------------------------------------------------------------ Barre du haut

    private void BuildTopBar()
    {
        var c = Session.Campaign;
        var date = GameTime.ToDate(c.Minute);
        var (runway, assumptions) = c.Runway();
        var state = c.Solvency.State;

        // Barre du haut du pack (maquette « HUD compact ») : fond marine, groupes, puce de trésorerie, textes clairs
        var light = Palette.Paper;
        var left = Ui.Row(0,
            TopSegment("icon_company", Ui.Column(0, Ui.Text(c.MainCompany.Name, 16, light, bold: true, wrap: false), Ui.Text(c.Premises.Name, 12, TopMuted, wrap: false))),
            TopDivider(),
            TopSegment("icon_calendar", Ui.Row(10, Ui.Number($"{GameTime.DayName(date.DayOfWeek)} {date:dd.MM.yyyy}", 20, light), Ui.Number($"{date:HH:mm}", 20, TopAccent))));

        var speeds = Ui.Row(4);
        speeds.Children.Add(Ui.IconButton("icon_pause", PlayerTogglePause, Loc.T("top.pause"), selected: Session.PausedByPlayer));
        // Une icône par vitesse (la 4e est composée du chevron du pack, PackLibrary.SpeedFour), plus le chiffre
        for (int i = 1; i < GameSession.Speeds.Length; i++)
        {
            int speed = i;
            speeds.Children.Add(Ui.IconButton($"icon_speed_{i}", () => PlayerSetSpeed(speed),
                Loc.T("top.speed", i, GameSession.Speeds[i]), $"{i}", !Session.PausedByPlayer && Session.Speed == i));
        }
        // L'état du temps a une place fixe à droite des vitesses : l'étiquette qui apparaît (nuit accélérée, pause) ne décale
        // plus les vitesses ni le reste de la barre
        Control timeInfo = new Border
        {
            Width = 200, Tag = "time-info",
            Child = Session.AutoPause != null
                ? Ui.Tag($"PAUSE : {Session.AutoPause}", Palette.Lemon)
                : Session.Settings.FastNights && !c.AnyoneInOffice && Session.Running ? Ui.Tag(Loc.T("top.fastNight"), Palette.Steel, Palette.Snow) : null,
        };
        if (((Border)timeInfo).Child is Control info)
            info.HorizontalAlignment = HorizontalAlignment.Left;

        decimal monthly = c.RecurringRevenue30 - c.MonthlyFixedCosts;
        // Puce de trésorerie du pack (la pièce est dans sa marge gauche), résultat du mois dessous
        var chip = new PixelPanel
        {
            Skin = "pack:hud_money_chip", Padding = new Thickness(24, 2, 10, 5),
            Child = Ui.Number(Ui.Chf(c.Ledger.Cash), 20, c.Ledger.Cash < 0 ? Palette.Blood : Palette.Ink),
        };
        var cash = Ui.Column(2, chip, Ui.Text($"{Ui.SignedChf(monthly)} / mois", 12, monthly < 0 ? TopDanger : TopSuccess, wrap: false));
        ToolTip.SetTip(cash, assumptions);
        Control? solvency = null;
        if (state != SolvencyState.Healthy)
        {
            var warning = Ui.Button(state == SolvencyState.Tight ? $"{runway:0} j d'argent" : Solvency.Name(state), () => OpenRoot(new FinancePanel()), "tab");
            warning.Foreground = state == SolvencyState.Tight ? TopAccent : TopDanger;
            solvency = warning;
        }

        int problems = c.Notices.Count(n => n.IsProblem && !n.Resolved);
        var bell = Ui.IconButton("icon_bell", () => OpenRoot(new NoticesPanel()), Loc.T("top.journalTip"), problems > 0 ? Loc.T("top.toDo") : Loc.T("top.journal"),
            style: problems > 0 ? "red" : "icon");
        Control bellWithCount = bell;
        if (problems > 0)
        {
            // Pastille compteur du pack sur le coin de la cloche
            var count = new PixelPanel
            {
                Skin = "pack:hud_badge_count", Padding = new Thickness(6, 3, 6, 5), MinWidth = 24, MinHeight = 24,
                HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, -8, -8, 0),
                Child = Ui.Text(problems > 99 ? "99+" : $"{problems}", 12, PixelPanel.CenterColor("pack:hud_badge_count") is { } fill ? Palette.ReadableOn(fill) : Palette.Paper, bold: true, wrap: false),
                IsHitTestVisible = false,
            };
            bellWithCount = new Panel { Children = { bell, count } };
        }
        var settings = Ui.IconButton("icon_settings", ShowPauseMenu, Loc.T("top.menu"));

        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto,*"), Margin = new Thickness(8, 4, 8, 8) };
        grid.Children.Add(left);
        // Les vitesses restent centrées : l'emplacement de l'étiquette est compensé à gauche par un vide de même largeur
        var center = Ui.Row(10, new Border { Width = 200, Tag = "time-balance" }, speeds, timeInfo);
        center.HorizontalAlignment = HorizontalAlignment.Center;
        Grid.SetColumn(center, 1);
        grid.Children.Add(center);
        var right = Ui.Row(10, cash, solvency, bellWithCount, settings);
        right.HorizontalAlignment = HorizontalAlignment.Right;
        foreach (var child in right.Children)
            child.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(right, 2);
        grid.Children.Add(right);
        // Mise à jour sur place (4 fois par seconde) : les boutons de vitesse restent les mêmes sous la souris
        UiPatch.Apply(_topBar, new PixelPanel { Skin = "pack:hud_topbar_bg", Child = grid });
    }

    // Couleurs lisibles sur le fond marine de la barre du haut (jetons du thème « Papier »)
    // Sur la barre sombre : sable et rouge éclaircis pour dépasser 4,5:1
    private static readonly IBrush TopMuted = Palette.Brush("#d6c6aa");
    private static readonly IBrush TopAccent = Palette.Brush("#fbd769");
    private static readonly IBrush TopSuccess = Palette.Brush("#8fc062");
    private static readonly IBrush TopDanger = Palette.Brush("#f29483");

    /// <summary>Un groupe de la barre du haut : icône claire et son contenu, dans le cadre « hud_topbar_segment ».</summary>
    private static Control TopSegment(string icon, Control content)
    {
        var image = Ui.Image(Icons.Get(icon, "#fbf5e6"), 24, 24);
        image.VerticalAlignment = VerticalAlignment.Center;
        content.VerticalAlignment = VerticalAlignment.Center;
        return new PixelPanel { Skin = "pack:hud_topbar_segment", Padding = new Thickness(10, 4, 12, 6), Child = Ui.Row(8, image, content) };
    }

    /// <summary>Séparateur vertical entre deux groupes de la barre du haut.</summary>
    private static Control TopDivider() => new PixelPanel
    {
        Skin = "pack:hud_topbar_divider", Width = 6, Margin = new Thickness(6, 4), VerticalAlignment = VerticalAlignment.Stretch,
    };

    // ------------------------------------------------------------------ Barre d'outils

    /// <summary>
    /// Le nom affiché d'un outil de la barre du bas : le libellé français sert de clé (le tutoriel le désigne ainsi,
    /// CoachTool), la traduction n'est que l'affichage.
    /// </summary>
    private static string ToolName(string label) => label switch
    {
        "Projets" => Loc.T("toolbar.projects"),
        "Équipe" => Loc.T("toolbar.team"),
        "Aménagement" => Loc.T("toolbar.build"),
        "Développement" => Loc.T("toolbar.dev"),
        "Finances" => Loc.T("toolbar.finance"),
        _ => label,
    };

    /// <summary>Après un changement de langue : la barre d'outils est reconstruite (la barre du haut l'est d'elle-même).</summary>
    public void Relocalize() => BuildToolbar();

    private static readonly (string Label, string Icon, Key Key, Func<GamePanel> Create)[] Tools =
    [
        ("Projets", "screen", Key.P, () => new ProjectsPanel()),
        ("Équipe", "people", Key.T, () => new TeamPanel()),
        ("Aménagement", "chair", Key.B, () => new BuildPanel()),
        ("Développement", "star", Key.D, () => new TreePanel()),
        ("Finances", "money", Key.F, () => new FinancePanel()),
    ];

    /// <summary>Le premier projet du portefeuille sur lequel le fondateur peut travailler au terminal, ou null.</summary>
    private Core.products.Product? TerminalTarget() =>
        Session.Campaign.Products.OrderBy(p => p.Priority).FirstOrDefault(p => Session.Campaign.CanOpenTerminal(p.Id).Ok);

    /// <summary>
    /// Ouvre le terminal sur un projet. Pendant la saisie, l'entreprise tourne à vitesse normale (ou se met en pause, si le
    /// réglage d'accessibilité est choisi) ; à la fermeture, la vitesse et la pause d'avant reviennent exactement.
    /// </summary>
    public void OpenTerminal(int productId)
    {
        if (_terminal != null)
            return;
        var result = Session.Campaign.OpenTerminal(productId);
        if (!result.Ok)
        {
            Toast(result.Message, false);
            return;
        }
        _beforeTerminal = (Session.Speed, Session.PausedByPlayer);
        ClosePanels();
        Session.ClearSelection();
        Session.SetSpeed(1);
        _terminal = new TerminalView(Session, CloseTerminal, () =>
        {
            int id = Session.Campaign.Terminal?.ProductId ?? productId;
            Session.Campaign.CloseTerminal();
            CloseTerminal(restore: false);
            OpenTerminal(id);
        });
        _terminalLayer.Content = _terminal;
        UpdateAutoPause();
        BuildToolbar();
        Avalonia.Threading.Dispatcher.UIThread.Post(() => _terminal?.FocusInput());
    }

    public void CloseTerminal() => CloseTerminal(restore: true);

    public TerminalView? Terminal => _terminal;

    private void CloseTerminal(bool restore)
    {
        Session.Campaign.CloseTerminal();
        _terminal = null;
        _terminalLayer.Content = null;
        if (restore)
            Session.Restore(_beforeTerminal.Speed, _beforeTerminal.Paused);
        UpdateAutoPause();
        BuildToolbar();
    }

    private void BuildToolbar()
    {
        var fresh = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
        BuildToolbar(fresh.Children);
        // Fusion des boutons dans la barre affichée (le bouton cliqué n'est jamais détruit sous la souris)
        var merged = UiPatch.Merge(_toolbarContent, fresh);
        if (!ReferenceEquals(merged, _toolbarContent))
        {
            _toolbar.Children.Clear();
            _toolbar.Children.Add(merged);
            _toolbarContent = merged;
        }
    }

    private Control? _toolbarContent;

    private void BuildToolbar(Avalonia.Controls.Controls toolbar)
    {
        // Le bouton du travail actif : le fondateur ne développe que par là
        var target = TerminalTarget();
        var work = new Button
        {
            // Encre sur l'orange du bouton principal (5,3:1 ; le clair n'y faisait que 2,7:1)
            Content = Ui.Row(6, Ui.Image(UI.Iso.PackLibrary.Icon("icon_terminal", target != null ? "#2b2128" : "#9c8878"), 24, 24),
                Ui.Column(0, Ui.Text(Loc.T("toolbar.work"), 15, Palette.Ink, wrap: false), Ui.Text(Loc.T("toolbar.workKey"), 11, Palette.Ink, wrap: false))),
            Padding = new Thickness(10, 4, 12, 8),
            IsEnabled = target != null && _terminal == null,
        };
        work.Classes.Add("gold");
        Ui.OnClick(work, () =>
        {
            if (TerminalTarget() is { } product)
                OpenTerminal(product.Id);
        });
        ToolTip.SetTip(work, target != null ? Loc.T("toolbar.workTip", target.Name) : Loc.T("toolbar.workNone"));
        toolbar.Add(work);
        foreach (var (label, icon, key, create) in Tools)
        {
            string badge = label == "Développement" && Session.Campaign.DevPoints > 0 ? $" ({Session.Campaign.DevPoints})" : "";
            bool active = _stack.Count > 0 && _stack[0].GetType() == create().GetType();
            // Le menu ouvert a le fond orange du pack : tout son texte passe à l'encre (le sarcelle et le gris n'y font que 2,5:1)
            var content = Ui.Row(6, Ui.Icon(icon, 24), Ui.Column(0, Ui.Text(ToolName(label) + badge, 15, active ? Palette.Ink : badge != "" ? Palette.Lemon : Palette.Snow, wrap: false),
                Ui.Text(key.ToString(), 11, active ? Palette.Ink : Palette.Fog, wrap: false)));
            // Le menu que demande le tutoriel porte une étoile (sans clignoter : rien ne bouge sous la souris)
            if (CoachTool == label && !active)
                content.Children.Insert(0, Ui.Image(UI.Iso.PackLibrary.Icon("icon_star", "#ecb841"), 16, 16));
            var button = new Button { Content = content, Padding = new Thickness(10, 4, 12, 8) };
            if (active)
                button.Classes.Add("selected");
            button.Classes.Add("tab");
            Ui.OnClick(button, () =>
            {
                Sfx.Play(Sound.Select);
                Toggle(create);
            });
            ToolTip.SetTip(button, $"{label} ({key})");
            toolbar.Add(button);
        }
    }

    private void Toggle(Func<GamePanel> create)
    {
        var panel = create();
        if (_stack.Count > 0 && _stack[0].GetType() == panel.GetType())
            ClosePanels();
        else
            OpenRoot(panel);
    }

    // ------------------------------------------------------------------ Menus

    public void OpenRoot(GamePanel panel)
    {
        _stack.Clear();
        PushPanel(panel);
    }

    public void PushPanel(GamePanel panel)
    {
        panel.Attach(this);
        if (panel is FinancePanel)
            Session.Campaign.TutorialEvent("open:finances");
        else if (panel is BuildPanel)
            Session.Campaign.TutorialEvent("open:build");
        _stack.Add(panel);
        Session.BuildMode = panel is BuildPanel || _stack.Any(p => p is BuildPanel);
        if (!Session.BuildMode)
            Session.SetTool(null);
        RefreshPanel();
        BuildToolbar();
    }

    public void PopPanel()
    {
        if (_stack.Count > 1)
        {
            _stack.RemoveAt(_stack.Count - 1);
            RefreshPanel();
        }
        else
            ClosePanels();
    }

    public void ClosePanels()
    {
        _stack.Clear();
        Session.SetFinish(null);
        Session.BuildMode = false;
        Session.SetTool(null);
        Session.BuildMode = false;
        RefreshPanel();
        BuildToolbar();
    }

    public void RefreshPanel()
    {
        var panel = _stack.LastOrDefault();
        _panelFrame.IsVisible = panel != null;
        UpdateAutoPause();
        if (panel == null)
        {
            _panelBody.Content = null;
            return;
        }

        // Fenêtre du pack : titre dans la bande sarcelle, le contexte juste dessous
        var header = Ui.Column(4, WindowTitle(panel.Title, ClosePanels, _stack.Count > 1 ? PopPanel : null), Ui.Muted(panel.Context, 13));
        header.Margin = new Thickness(0, 0, 0, 8);
        header.Children[1].Margin = new Thickness(4, 0, 0, 0);

        bool samePanel = ReferenceEquals(_lastBuilt, panel);
        _lastBuilt = panel;
        var body = panel.Build();
        // La barre de défilement (20 px) se pose par-dessus le contenu : on lui laisse sa place pour ne rien couper à droite
        body.Margin = new Thickness(body.Margin.Left, body.Margin.Top, body.Margin.Right + 24, body.Margin.Bottom);
        var dock = new DockPanel();
        DockPanel.SetDock(header, Dock.Top);
        dock.Children.Add(header);
        // Ce que le menu veut garder en vue (l'objet en cours de pose) reste au-dessus de la liste, hors du défilement
        var sticky = panel.BuildHeader();
        var stickyHost = new Border { Child = sticky, Margin = new Thickness(0, 0, 24, sticky != null ? 8 : 0), Tag = "panel-sticky" };
        DockPanel.SetDock(stickyHost, Dock.Top);
        dock.Children.Add(stickyHost);
        var scroll = Ui.Scroll(body);
        scroll.Tag = "panel-scroll";
        dock.Children.Add(scroll);
        // Le même menu : mise à jour sur place. La liste défilante, sa position, le bouton sous la souris et les infobulles restent
        // les mêmes ; seules les valeurs qui ont changé sont recopiées. Un autre menu : on remplace (et il commence en haut).
        if (samePanel && _panelBody.Content is DockPanel)
            UiPatch.Apply(_panelBody, dock);
        else
            _panelBody.Content = dock;
        LayoutPanelFrame();
        if (panel.TakeFocusTag() is { } focus)
            Avalonia.Threading.Dispatcher.UIThread.Post(() => ScrollPanelTo(focus), Avalonia.Threading.DispatcherPriority.Background);
    }

    /// <summary>Fait défiler le menu ouvert jusqu'à la section marquée (Tag), en la plaçant en haut de la zone visible.</summary>
    private void ScrollPanelTo(string tag)
    {
        if ((_panelBody.Content as DockPanel)?.Children.OfType<ScrollViewer>().FirstOrDefault() is not { Content: Control content } scroll)
            return;
        var target = content.GetVisualDescendants().OfType<Control>().FirstOrDefault(c => Equals(c.Tag, tag));
        if (target?.TranslatePoint(new Point(0, 0), content) is { } at)
            scroll.Offset = new Vector(0, Math.Max(0, at.Y - 4));
    }

    private GamePanel? _lastBuilt;

    /// <summary>
    /// Poignée du pack sur le bord gauche du panneau latéral : on la tire pour l'élargir ou le rétrécir (curseur de
    /// redimensionnement du pack). La largeur est retenue dans les réglages de la machine.
    /// </summary>
    private Control PanelHandle()
    {
        var handle = new Image
        {
            Source = PackSkins.Sprite("hud_drag_handle"), Width = 16, Height = 28, Stretch = Stretch.Fill,
            HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(-14, 0, 0, 0), Cursor = PackSkins.Cursor("cursor_resize_h"),
        };
        RenderOptions.SetBitmapInterpolationMode(handle, Avalonia.Media.Imaging.BitmapInterpolationMode.None);
        ToolTip.SetTip(handle, "Tirer pour élargir ou rétrécir le panneau");
        double? from = null, start = 0;
        handle.PointerPressed += (_, e) =>
        {
            from = e.GetPosition(this).X;
            start = _panelFrame.Bounds.Width;
            e.Pointer.Capture(handle);
            e.Handled = true;
        };
        handle.PointerMoved += (_, e) =>
        {
            if (from is not double x0)
                return;
            double width = Math.Clamp(start.Value - (e.GetPosition(this).X - x0), 380, Math.Max(380, Bounds.Width * 0.6));
            _panelFrame.Width = width;
            Session.Settings.PanelWidth = Math.Round(width);
        };
        handle.PointerReleased += (_, e) =>
        {
            if (from == null)
                return;
            from = null;
            e.Pointer.Capture(null);
            Window.SaveSettings();
        };
        // Seulement pour le panneau latéral, pas pour les grands menus pleine largeur
        handle.IsVisible = false;
        _panelHandle = handle;
        return handle;
    }

    private Image? _panelHandle;

    private void LayoutPanelFrame()
    {
        var panel = _stack.LastOrDefault();
        if (panel == null)
            return;
        if (_panelHandle != null)
            _panelHandle.IsVisible = !panel.Large;
        if (panel.Large)
        {
            _panelFrame.HorizontalAlignment = HorizontalAlignment.Stretch;
            _panelFrame.Width = double.NaN;
            _panelFrame.Margin = new Thickness(40, 62, 40, 84);
        }
        else
        {
            _panelFrame.HorizontalAlignment = HorizontalAlignment.Right;
            _panelFrame.Width = Session.Settings.PanelWidth > 0 ? Session.Settings.PanelWidth : 470;
            _panelFrame.Margin = new Thickness(0, 62, 10, 84);
        }
        _panelFrame.VerticalAlignment = VerticalAlignment.Stretch;
    }

    private void UpdateAutoPause()
    {
        string? reason = null;
        if (_pauseMenu)
            reason = "menu";
        else if (_modalLayer.IsVisible)
            reason = "confirmation";
        else if (_terminal != null && Session.Settings.PauseDuringTerminal)
            reason = "saisie";
        else if (Session.Settings.PauseInLargeMenus && _stack.LastOrDefault() is { Large: true } large && !ReferenceEquals(large, _menuPauseLifted))
            reason = large.Title.ToLowerInvariant();
        Session.AutoPause = reason;
    }

    /// <summary>Le grand menu dont le joueur a relancé le temps (Espace ou une vitesse) : il ne remet plus en pause.</summary>
    private GamePanel? _menuPauseLifted;

    /// <summary>Le temps est-il arrêté par un grand menu (réglage « pause dans les grands menus ») ?</summary>
    private bool PausedByMenu => Session.AutoPause != null && Session.Settings.PauseInLargeMenus && _stack.LastOrDefault() is { Large: true } large
                                 && !ReferenceEquals(large, _menuPauseLifted) && !_pauseMenu && !_modalLayer.IsVisible;

    /// <summary>
    /// Espace ou le bouton pause : met en pause ou relance. Si c'est un grand menu qui a arrêté le temps, Espace le relance
    /// (le menu ne remet plus en pause tant qu'il reste ouvert) au lieu de poser une pause invisible par-dessus.
    /// </summary>
    public void PlayerTogglePause()
    {
        if (PausedByMenu)
        {
            _menuPauseLifted = _stack.LastOrDefault();
            if (Session.PausedByPlayer)
                Session.TogglePause();
            UpdateAutoPause();
            Sfx.Play(Sound.PauseOut);
        }
        else
            Session.TogglePause();
        Session.Campaign.TutorialEvent("speed");
        BuildTopBar();
    }

    /// <summary>Une vitesse choisie par le joueur (touche ou bouton) relance aussi le temps arrêté par un grand menu.</summary>
    public void PlayerSetSpeed(int speed)
    {
        if (PausedByMenu)
            _menuPauseLifted = _stack.LastOrDefault();
        Session.SetSpeed(speed);
        UpdateAutoPause();
        Session.Campaign.TutorialEvent("speed");
        BuildTopBar();
    }

    // ------------------------------------------------------------------ Tutoriel

    /// <summary>
    /// La consigne en cours : une phrase, la touche à utiliser, l'avancée (3/11) et « Passer ». Elle ne bloque rien et ne met
    /// jamais en pause : l'étape se valide quand le joueur fait vraiment l'action (Campaign.Tutorial.cs).
    /// </summary>
    private void BuildCoach()
    {
        var step = Session.Campaign.CurrentTutorialStep;
        if (step == null || _terminal != null && step.Target != "work")
        {
            _coach.Content = null;
            return;
        }
        bool large = _stack.LastOrDefault() is { Large: true };
        _coach.VerticalAlignment = large ? VerticalAlignment.Bottom : VerticalAlignment.Top;
        _coach.Margin = large ? new Thickness(10, 0, 0, 8) : new Thickness(10, 62, 0, 0);
        int index = Session.Campaign.TutorialIndex + 1, total = Campaign.TutorialSteps.Count;
        var head = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
        head.Children.Add(Ui.Text($"Tutoriel {index}/{total} · {step.Title}", 12, Palette.Ink, wrap: false));
        var skip = Ui.Button("Passer", () => Confirm("Passer le tutoriel ?", "Les consignes ne reviendront pas. Tout reste accessible depuis les menus.",
            "Passer", () =>
            {
                Session.Campaign.SkipTutorial();
                BuildCoach();
                BuildToolbar();
            }, danger: false), "tab");
        skip.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(skip, 1);
        head.Children.Add(skip);
        // Une grille « Auto,* » : le texte revient à la ligne (dans une rangée horizontale, il sortait de la carte)
        var body = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*"), ColumnSpacing = 8 };
        if (step.Key != null)
        {
            var key = Ui.Tag(step.Key, Palette.Lemon);
            key.VerticalAlignment = VerticalAlignment.Top;
            body.Children.Add(key);
        }
        var text = Ui.Text(step.Text, 12, Palette.Snow);
        Grid.SetColumn(text, 1);
        body.Children.Add(text);
        var bar = Ui.Bar((index - 1) / (double)total, Palette.Gold, 6);
        UiPatch.Apply(_coach, new PixelPanel
        {
            Skin = "pack:hud_notification:info", Padding = new Thickness(16, 8, 12, 12), Tag = "coach",
            Child = Ui.Column(6, head, body, bar),
        });
    }

    /// <summary>Le menu de la barre d'outils que la consigne en cours demande d'ouvrir (« Projets »), ou null.</summary>
    private string? CoachTool => Session.Campaign.CurrentTutorialStep?.Target is { } target && target.StartsWith("tool:") ? target[5..] : null;

    // ------------------------------------------------------------------ Inspecteur de la sélection

    private void BuildInspector()
    {
        var content = SelectionInspector.Build(this);
        // Mise à jour sur place : la fiche d'une personne se rafraîchit 4 fois par seconde sans détruire ses boutons ;
        // une autre sélection remplace la fiche (Tag différent)
        UiPatch.Apply(_inspector, content == null ? null
            : new PixelPanel { Padding = new Thickness(12, 10), Width = 380, Child = content, Tag = $"{Session.SelectionKind}:{Session.SelectionId}" });
    }

    // ------------------------------------------------------------------ Bulles de notification

    private readonly List<(Control Control, double Age)> _toastAges = new();

    /// <summary>Propositions arrivées pendant la saisie au terminal, montrées à sa fermeture.</summary>
    private readonly List<Notice> _deferred = new();

    /// <summary>Retour d'une action : toast sombre du pack si elle a réussi, notification « attention » sinon.</summary>
    public void Toast(string message, bool ok)
    {
        if (ok)
            AddToast(Ui.Text(message, 15, Palette.Paper), "pack:hud_toast", new Thickness(12, 6, 12, 10), null);
        else
            AddToast(Notification("warning", Ui.Text(message, 15, Palette.Ink)), "pack:hud_notification:warning", new Thickness(18, 8, 12, 12), null);
    }

    private void ToastNotice(Notice notice)
    {
        // Le sens se lit à la bande de couleur ET à l'icône, jamais à la couleur seule
        string state = notice.Severity switch
        {
            Severity.Critical => "danger",
            Severity.Warning => "warning",
            Severity.Good => "success",
            _ => "info"
        };
        var content = Ui.Column(2, Ui.Text(notice.Title, 15, Palette.Ink, bold: true), string.IsNullOrEmpty(notice.Detail) ? null : Ui.Muted(Shorten(notice.Detail, 140), 13));
        AddToast(Notification(state, content), $"pack:hud_notification:{state}", new Thickness(18, 8, 12, 12), () => Navigate(notice));
    }

    /// <summary>Icône de sens à gauche du texte d'une notification.</summary>
    private static Control Notification(string state, Control content)
    {
        var icon = Ui.Icon(state switch { "danger" => "icon_error", "warning" => "icon_warning", "success" => "icon_success", _ => "icon_info" }, 24);
        icon.VerticalAlignment = VerticalAlignment.Top;
        icon.Margin = new Thickness(0, 0, 10, 0);
        // Une grille, pas une rangée : le texte garde une largeur finie et revient à la ligne
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*") };
        grid.Children.Add(icon);
        Grid.SetColumn(content, 1);
        grid.Children.Add(content);
        return grid;
    }

    private static string Shorten(string text, int max) => text.Length <= max ? text : text[..max] + "…";

    private void AddToast(Control content, string skin, Thickness padding, Action? click)
    {
        var frame = new PixelPanel { Skin = skin, Padding = padding, Child = content };
        if (click != null)
        {
            frame.Cursor = new Cursor(StandardCursorType.Hand);
            frame.PointerPressed += (_, e) =>
            {
                click();
                _toasts.Children.Remove(frame);
                e.Handled = true;
            };
        }
        else
            frame.PointerPressed += (_, e) => e.Handled = true;
        // En bas à gauche, au-dessus de la barre d'outils (comme la maquette du pack) : la plus récente en bas
        _toasts.Children.Add(frame);
        _toastAges.Add((frame, 0));
        // Un panneau ouvert : seulement les deux plus récentes, pour ne pas couvrir son contenu (tout reste dans le journal)
        while (_toasts.Children.Count > MaxToasts)
            _toasts.Children.RemoveAt(0);
    }

    private double _inspectorHeight;
    private PixelPanel? _toolbarFrame;

    /// <summary>Un grand menu couvre l'écran : une seule bulle, sous lui à gauche de la barre d'outils (elles cachaient son bas).</summary>
    private int MaxToasts => TopPanel is { Large: true } ? 1 : _stack.Count > 0 ? 2 : 4;

    private void UpdateToasts(double deltaTime)
    {
        // Sous un grand menu, la bulle se loge à gauche de la barre d'outils ; si la place manque (1280 × 720), au-dessus
        double room = (_toolbarFrame?.Bounds.X ?? 400) - 20;
        bool beside = TopPanel is { Large: true } && room >= 260;
        _toasts.Width = beside ? Math.Min(380, room) : 380;
        _toasts.Margin = beside ? new Thickness(10, 0, 0, 6) : new Thickness(10, 0, 0, Math.Max(84, _inspectorHeight + 20));
        while (_toasts.Children.Count > MaxToasts)
            _toasts.Children.RemoveAt(0);
        for (int i = _toastAges.Count - 1; i >= 0; i--)
        {
            var (control, age) = _toastAges[i];
            age += deltaTime;
            _toastAges[i] = (control, age);
            if (age > 7 || !_toasts.Children.Contains(control))
            {
                _toasts.Children.Remove(control);
                _toastAges.RemoveAt(i);
            }
            else if (age > 6)
                control.Opacity = 7 - age;
        }
    }

    /// <summary>Ouvre le menu qui correspond à une notification (« contract:12 », « product:3 », « finance »...).</summary>
    public void Navigate(Notice notice)
    {
        notice.Read = true;
        string[] parts = notice.Target.Split(':');
        int id = parts.Length > 1 && int.TryParse(parts[1], out int value) ? value : 0;
        switch (parts[0])
        {
            case "launch" when Session.Campaign.FindProduct(id) is { } ready && Session.Campaign.CanLaunch(ready) && _terminal == null:
                OpenRoot(new ProjectsPanel());
                PushPanel(new ProjectPanel(id));
                LaunchView.Show(this, id);
                break;
            case "release" when Session.Campaign.FindProduct(id) is { } prepared && Session.Campaign.CanPublish(prepared).Ok && prepared.Queue.Count == 0 && _terminal == null:
                OpenRoot(new ProjectsPanel());
                PushPanel(new ProjectPanel(id));
                LaunchView.ShowRelease(this, id);
                break;
            case "product" or "launch" or "release" when Session.Campaign.FindProduct(id) != null:
                OpenRoot(new ProjectsPanel());
                PushPanel(new ProjectPanel(id));
                break;
            case "product" or "products" or "projects":
                OpenRoot(new ProjectsPanel());
                break;
            case "person" when Session.Campaign.FindPerson(id) != null:
                OpenRoot(new TeamPanel());
                PushPanel(new PersonPanel(id));
                break;
            case "person" or "team":
                OpenRoot(new TeamPanel());
                break;
            case "finance":
                OpenRoot(new FinancePanel());
                break;
            case "build":
                // Après un déménagement (et pour tout ce qui attend dans les cartons) : directement la section des cartons
                OpenRoot(Session.Campaign.Storage.Count > 0 && (parts.Length > 1 && parts[1] == "cartons" || notice.Detail?.Contains("cartons") == true)
                    ? BuildPanel.AtCartons() : new BuildPanel());
                break;
            case "sites":
                OpenRoot(new BuildPanel());
                PushPanel(new SitesPanel());
                break;
            case "cloud":
                OpenRoot(new ProjectsPanel());
                PushPanel(new CloudPanel());
                break;
            case "industry":
                OpenRoot(ProjectsPanel.At("production"));
                break;
            case "research":
                OpenRoot(new TreePanel());
                PushPanel(new ResearchPanel());
                break;
            case "ai":
                OpenRoot(new TreePanel());
                PushPanel(new AiPanel());
                break;
            case "lab":
                OpenRoot(new TreePanel());
                PushPanel(new LabPanel());
                break;
            case "tree" or "company":
                OpenRoot(new TreePanel());
                break;
            default:
                OpenRoot(new NoticesPanel());
                break;
        }
    }

    // ------------------------------------------------------------------ Fenêtres modales

    /// <summary>
    /// Fenêtre modale. Avec un titre : cadre « hud_modal » du pack, titre dans la barre sarcelle ; sans titre : simple panneau
    /// (mini-jeux, menu de pause, qui ont leur propre mise en page).
    /// </summary>
    public void ShowModal(Control content, Action? onClose = null, double width = 520, string? title = null)
    {
        _modalHost.Content = title == null
            ? new PixelPanel { Padding = new Thickness(22, 16), Width = width, Child = content }
            : new PixelPanel
            {
                Skin = "pack:hud_modal", Padding = new Thickness(12, 4, 12, 18), Width = width,
                Child = Ui.Column(14, WindowTitle(title, CloseModal), new Border { Padding = new Thickness(8, 0), Child = content }),
            };
        _modalLayer.IsVisible = true;
        _modalClose = onClose;
        UpdateAutoPause();
    }

    public void CloseModal()
    {
        _modalLayer.IsVisible = false;
        _modalHost.Content = null;
        _pauseMenu = false;
        var close = _modalClose;
        _modalClose = null;
        close?.Invoke();
        UpdateAutoPause();
    }

    /// <summary>Demande confirmation avant une action importante (vente, licenciement, déménagement...).</summary>
    public void Confirm(string title, string detail, string action, Action confirmed, bool danger = true)
    {
        // Composition « hud_confirm_dialog » du pack : icône de gravité, message, puis Annuler et l'action à droite
        var icon = Ui.Icon(danger ? "icon_warning" : "icon_help", 32);
        icon.VerticalAlignment = VerticalAlignment.Top;
        var message = Ui.Text(detail, 16);
        message.Width = 380;
        var buttons = Ui.Row(10, Ui.Button("Annuler", CloseModal), Ui.Button(action, () =>
        {
            CloseModal();
            confirmed();
        }, danger ? "red" : "gold"));
        buttons.HorizontalAlignment = HorizontalAlignment.Right;
        ShowModal(Ui.Column(18, Ui.Row(14, icon, message), buttons), null, 500, title);
    }

    /// <summary>Barre de titre d'une fenêtre du pack : titre clair sur la bande sarcelle, retour et fermeture à ses bouts.</summary>
    private static Grid WindowTitle(string title, Action close, Action? back = null)
    {
        var bar = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"), Height = 32 };
        if (back != null)
        {
            var backButton = SmallIconButton("icon_back", back, "Retour (Échap)");
            backButton.Margin = new Thickness(0, 0, 8, 0);
            bar.Children.Add(backButton);
        }
        var text = Ui.Text(title, 20, Palette.Paper, bold: true, wrap: false);
        text.VerticalAlignment = VerticalAlignment.Center;
        text.TextTrimming = TextTrimming.CharacterEllipsis;
        Grid.SetColumn(text, 1);
        bar.Children.Add(text);
        var closeButton = SmallIconButton("icon_close", close, "Fermer");
        Grid.SetColumn(closeButton, 2);
        bar.Children.Add(closeButton);
        return bar;
    }

    /// <summary>Petit bouton à icône du pack (hud_button_icon_small), pour les barres de titre.</summary>
    private static Button SmallIconButton(string icon, Action click, string tip)
    {
        var button = Ui.IconButton(icon, click, tip, style: "icon small");
        button.Content = Ui.Image(Icons.Get(icon, "#fbf5e6"), 16, 16);
        button.VerticalAlignment = VerticalAlignment.Center;
        return button;
    }

    private void ShowPauseMenu()
    {
        _pauseMenu = true;
        var c = Session.Campaign;
        string? saveName = null;
        var column = Ui.Column(10,
            Ui.Muted($"{c.Name} · {GameTime.Format(c.Minute)}"),
            Ui.Button(Loc.T("pause.resume"), CloseModal, "gold", icon: Ui.IconFor(Loc.French("pause.resume"))),
            Ui.Section(Loc.T("pause.save"),
                Ui.Input("", v => saveName = v, Loc.T("pause.saveName")),
                Ui.Button(Loc.T("pause.save"), () =>
                {
                    string name = string.IsNullOrWhiteSpace(saveName) ? GameTime.FormatDate(c.Minute) : saveName!;
                    Window.Busy(() =>
                    {
                        bool ok = Session.Save(SaveKind.Manual, name);
                        Toast(Session.LastSaveMessage + (ok ? $" : « {name} »" : ""), ok);
                    });
                }, icon: Ui.IconFor(Loc.French("pause.save")))),
            Ui.Button(Loc.T("pause.loadOther"), () => Leave(() => Window.ShowLoad()), "", icon: Ui.IconFor(Loc.French("pause.loadOther"))),
            Ui.Button(Loc.T("pause.settings"), () => ShowModal(SettingsScreen.SettingsForm(Window, ShowPauseSettings, ShowPauseMenu), null, 560), "", icon: Ui.IconFor(Loc.French("pause.settings"))),
            Ui.Button(Loc.T("pause.mainMenu"), () => Leave(() => Window.ShowTitle()), "", icon: Ui.IconFor(Loc.French("pause.mainMenu"))),
            Ui.Muted(Loc.T("pause.note")));
        ShowModal(column, null, 460, Loc.T("pause.title"));
        _pauseMenu = true;
        UpdateAutoPause();
    }

    private void ShowPauseSettings()
    {
        _pauseMenu = true;
        ShowModal(SettingsScreen.SettingsForm(Window, ShowPauseSettings, ShowPauseMenu), null, 560);
        _pauseMenu = true;
        ApplyScale();
    }

    private void Leave(Action then)
    {
        Session.Save(SaveKind.Quick);
        then();
    }

    /// <summary>Le bilan d'une partie : durée, finances, utilisateurs, principaux produits (réussite comme faillite).</summary>
    public static Control SummaryView(GameSummary s)
    {
        var column = Ui.Column(4,
            Ui.Pair("Durée", $"{s.Days} jours ({s.Days / 365.0:0.0} an(s))"),
            Ui.Pair("Argent", Ui.Chf(s.Cash), Ui.Signed(s.Cash)),
            Ui.Pair("Revenus des projets / mois", Ui.Chf(s.Revenue30)),
            Ui.Pair("Dépenses fixes / mois", Ui.Chf(s.MonthlyCosts)),
            s.LowestCash is { } low ? Ui.Pair("Pire creux de trésorerie", Ui.Chf(low), Ui.Signed(low)) : null,
            Ui.Pair("Utilisateurs", $"{s.Users:N0}"),
            Ui.Pair("Équipe", $"{s.People} personne(s)"),
            Ui.Pair("Valorisation estimée", Ui.Chf(s.Valuation)));
        if (s.Products.Count > 0)
        {
            column.Children.Add(Ui.Text("Principaux produits", 15, Palette.Snow, bold: true));
            foreach (var p in s.Products)
                column.Children.Add(Ui.Pair(p.Name, $"{p.Users:N0} utilisateurs · {Ui.Chf(p.Revenue30)} sur 30 jours"));
        }
        return Ui.Card(column);
    }

    /// <summary>Objectif final atteint : le bilan, une seule fois, puis la partie continue si le joueur le veut.</summary>
    private void ShowVictory()
    {
        var c = Session.Campaign;
        c.FinalGoalAcknowledged = true;
        Sfx.Play(Sound.Milestone);
        _pauseMenu = true;
        ShowModal(Ui.Column(12,
            Ui.Title("Objectif atteint !", 28),
            Ui.Text($"{c.MainCompany.Name} est valorisée {Ui.Chf(c.Valuation())} : plus de {Ui.Chf(Campaign.FinalGoalValuation)}, avec une entreprise saine.", 15),
            SummaryView(c.Summary()),
            Ui.Muted("Tu peux continuer : la partie reste ouverte, sans nouvel objectif final. Ce bilan ne reviendra pas."),
            Ui.Row(Ui.Button("Continuer la partie", CloseModal, "gold"),
                Ui.Button("Menu principal", () => Leave(() => Window.ShowTitle())))), null, 640);
        _pauseMenu = true;
        UpdateAutoPause();
    }

    private void ShowDefeat()
    {
        var c = Session.Campaign;
        _pauseMenu = true;
        ShowModal(Ui.Column(12,
            Ui.Title("Entreprise liquidée", 28),
            Ui.Text(c.Solvency.LiquidationReport ?? "", 15),
            SummaryView(c.Summary()),
            Ui.Muted("Cette campagne est terminée. Ses sauvegardes restent intactes : tu peux recharger un moment antérieur."),
            Ui.Row(Ui.Button("Charger une sauvegarde", () => Leave(() => Window.ShowLoad()), "gold"),
                Ui.Button("Nouvelle partie", () => Leave(() => Window.ShowNewCampaign())),
                Ui.Button("Menu principal", () => Leave(() => Window.ShowTitle())))), null, 640);
        _pauseMenu = true;
        UpdateAutoPause();
    }

    // ------------------------------------------------------------------ Clavier

    public bool HandleKey(Key key, KeyModifiers modifiers)
    {
        if (key == Key.Escape)
        {
            Escape();
            return true;
        }
        if (_modalLayer.IsVisible)
            return false;
        // Pendant la saisie, aucun raccourci : les touches reviennent au terminal
        if (_terminal != null)
        {
            if (key == Key.Enter && !_terminal.HandleEnter())
                _terminal.FocusInput();
            else if (key != Key.Enter)
                _terminal.FocusInput();
            return true;
        }
        if (key == Key.Enter && TerminalTarget() is { } product)
        {
            OpenTerminal(product.Id);
            return true;
        }
        // Menu de test : argent, déblocages, temps... (marque la campagne)
        if (key == Key.F12)
        {
            Toggle(() => new CheatPanel());
            return true;
        }
        if (key == Key.R && Session.Tool != null)
        {
            Session.RotateTool();
            return true;
        }
        if (key == Key.R && Session.SelectionKind == SelectionKind.Item)
        {
            var rotated = Session.Campaign.Rotate(Session.SelectionId);
            if (!rotated.Ok)
                Toast(rotated.Message, false);
            return true;
        }

        switch (key)
        {
            case Key.Space:
                PlayerTogglePause();
                return true;
            case >= Key.D1 and <= Key.D4:
                PlayerSetSpeed(key - Key.D0);
                return true;
            case Key.Left or Key.A:
                Scene.Pan(-40, 0);
                return true;
            // D ouvre le menu Développement (barre d'outils) : la vue va à droite avec la flèche seulement
            case Key.Right:
                Scene.Pan(40, 0);
                return true;
            case Key.Up or Key.W:
                Scene.Pan(0, -40);
                return true;
            case Key.Down or Key.S:
                Scene.Pan(0, 40);
                return true;
            case Key.Add or Key.OemPlus:
                Scene.ZoomBy(1);
                return true;
            case Key.Subtract or Key.OemMinus:
                Scene.ZoomBy(-1);
                return true;
            case Key.Home:
                Scene.ResetCamera();
                return true;
            case Key.N:
                OpenRoot(new NoticesPanel());
                return true;
        }
        foreach (var (_, _, toolKey, create) in Tools)
            if (key == toolKey)
            {
                Toggle(create);
                return true;
            }
        return false;
    }

    /// <summary>Échap ferme la couche la plus haute : fenêtre modale, outil, menu, sélection ; sinon ouvre la pause.</summary>
    private void Escape()
    {
        if (_terminal != null)
        {
            CloseTerminal();
            return;
        }
        if (_modalLayer.IsVisible)
        {
            if (Session.Campaign.Status != CampaignStatus.Liquidated)
                CloseModal();
            return;
        }
        if (Session.Tool != null)
        {
            Session.SetTool(null);
            return;
        }
        if (Session.Finish != null)
        {
            Session.SetFinish(null);
            Session.BuildMode = false;
            RefreshPanel();
            return;
        }
        if (_stack.Count > 0)
        {
            PopPanel();
            return;
        }
        if (Session.SelectionKind != SelectionKind.None)
        {
            Session.ClearSelection();
            return;
        }
        Sfx.Play(Sound.PauseIn);
        ShowPauseMenu();
    }
}
