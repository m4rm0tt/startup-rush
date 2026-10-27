using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
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
    private readonly PixelPanel _panelFrame = new() { Padding = new Thickness(14, 10) };
    private readonly ContentControl _panelBody = new();
    private readonly ContentControl _inspector = new() { HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Bottom };
    private readonly StackPanel _toasts = new() { Spacing = 6, Width = 380, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 60, 10, 0) };
    private readonly Grid _modalLayer = new() { IsVisible = false, Background = new SolidColorBrush(Color.Parse("#a0181425")) };
    private readonly ContentControl _modalHost = new() { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
    private readonly StackPanel _toolbar = new() { Orientation = Orientation.Horizontal, Spacing = 4 };

    private readonly List<GamePanel> _stack = new();
    private Action? _modalClose;
    private bool _pauseMenu;
    private double _refreshTimer;
    private double _hudTimer;
    private string _lastPremises;
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
        _lastPremises = session.Campaign.Premises.ListingId;

        var root = new Grid();
        root.Children.Add(Scene);

        // Interface par-dessus la scène, mise à l'échelle selon la fenêtre ou le réglage
        _hud.Children.Add(_topBar);
        _topBar.VerticalAlignment = VerticalAlignment.Top;

        var toolbarFrame = new PixelPanel { Padding = new Thickness(8, 6, 8, 8), Child = _toolbar, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(0, 0, 0, 8) };
        _hud.Children.Add(toolbarFrame);
        BuildToolbar();

        _inspector.Margin = new Thickness(10, 0, 0, 10);
        _hud.Children.Add(_inspector);
        _panelFrame.Child = _panelBody;
        _panelFrame.IsVisible = false;
        _hud.Children.Add(_panelFrame);
        _hud.Children.Add(_toasts);

        _modalLayer.Children.Add(_modalHost);
        _modalLayer.PointerPressed += (_, e) => e.Handled = true; // aucun clic ne traverse
        _hud.Children.Add(_modalLayer);

        _scaler = new LayoutTransformControl { Child = _hud };
        root.Children.Add(_scaler);
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
            return Math.Clamp(Math.Min(w / 1500, h / 860), 0.72, 1.5);
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

        while (Session.NewNotices.Count > 0)
        {
            var notice = Session.NewNotices.Dequeue();
            // Les bulles ne montrent que ce qui compte : bonnes nouvelles et problèmes, une seule fois par problème
            if (notice.Severity != Severity.Info && notice.Count == 1)
                ToastNotice(notice);
        }
        UpdateToasts(deltaTime);

        if (Session.Campaign.Premises.ListingId != _lastPremises)
        {
            _lastPremises = Session.Campaign.Premises.ListingId;
            Scene.ResetCamera();
        }

        _hudTimer += deltaTime;
        if (_hudTimer > 0.25)
        {
            _hudTimer = 0;
            BuildTopBar();
            if (Session.SelectionKind == SelectionKind.Person)
                BuildInspector();
        }

        _refreshTimer += deltaTime;
        if (_refreshTimer > 1 && _stack.LastOrDefault() is { AutoRefresh: true } && !TextInputFocused())
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
    }

    private bool TextInputFocused() => TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() is TextBox;

    public void Leave() => Session.Detach();

    // ------------------------------------------------------------------ Barre du haut

    private void BuildTopBar()
    {
        var c = Session.Campaign;
        var date = GameTime.ToDate(c.Minute);
        var (runway, assumptions) = c.Runway();
        var state = c.Solvency.State;

        var left = Ui.Row(14,
            Ui.Column(0, Ui.Text(c.MainCompany.Name, 16, Palette.Snow, bold: true, wrap: false), Ui.Muted(c.Premises.Name, 12)),
            Ui.Column(0, Ui.Number($"{GameTime.DayName(date.DayOfWeek)} {date:dd.MM.yyyy}", 20), Ui.Number($"{date:HH:mm}", 20, Palette.Lemon)));

        var speeds = Ui.Row(4);
        var pause = Ui.Button(Session.PausedByPlayer ? "▶" : "❚❚", Session.TogglePause, Session.PausedByPlayer ? "tab selected" : "tab", tip: "Pause (Espace)");
        speeds.Children.Add(pause);
        for (int i = 1; i < GameSession.Speeds.Length; i++)
        {
            int speed = i;
            speeds.Children.Add(Ui.Button(new string('›', i), () => Session.SetSpeed(speed),
                !Session.PausedByPlayer && Session.Speed == i ? "tab selected" : "tab", tip: $"Vitesse {i} ({GameSession.Speeds[i]:0} min de jeu par seconde) — touche {i}"));
        }
        Control timeInfo = Session.AutoPause != null
            ? Ui.Tag($"PAUSE : {Session.AutoPause}", Palette.Lemon)
            : Session.Settings.FastNights && !c.AnyoneInOffice && Session.Running ? Ui.Tag("Nuit accélérée", Palette.Steel, Palette.Snow) : new Control();

        var stateColor = state switch
        {
            SolvencyState.Healthy => Palette.Leaf,
            SolvencyState.Tight => Palette.Lemon,
            SolvencyState.Overdue => Palette.Ember,
            _ => Palette.Blood
        };
        decimal monthly = c.RecurringRevenue30 - c.MonthlyFixedCosts;
        var cash = Ui.Column(0, Ui.Number(Ui.Chf(c.Ledger.Cash), 22, Ui.Signed(c.Ledger.Cash)), Ui.Text($"{Ui.SignedChf(monthly)} / mois", 12, Ui.Signed(monthly), wrap: false));
        ToolTip.SetTip(cash, assumptions);
        Control? solvency = null;
        if (state != SolvencyState.Healthy)
        {
            var warning = Ui.Button(state == SolvencyState.Tight ? $"{runway:0} j d'argent" : Solvency.Name(state), () => OpenRoot(new FinancePanel()), "tab");
            warning.Foreground = stateColor;
            solvency = warning;
        }

        int problems = c.Notices.Count(n => n.IsProblem && !n.Resolved);
        var bell = Ui.Button(problems > 0 ? $"À traiter ({problems})" : "Journal", () => OpenRoot(new NoticesPanel()), problems > 0 ? "red" : "tab", tip: "Journal (N)");

        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto,*"), Margin = new Thickness(10, 6) };
        grid.Children.Add(left);
        var center = Ui.Row(10, speeds, timeInfo);
        center.HorizontalAlignment = HorizontalAlignment.Center;
        Grid.SetColumn(center, 1);
        grid.Children.Add(center);
        var right = Ui.Row(10, cash, solvency, bell);
        right.HorizontalAlignment = HorizontalAlignment.Right;
        Grid.SetColumn(right, 2);
        grid.Children.Add(right);
        _topBar.Content = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#e6262b44")),
            BorderBrush = Palette.Slate,
            BorderThickness = new Thickness(0, 0, 0, 2),
            Child = grid,
        };
    }

    // ------------------------------------------------------------------ Barre d'outils

    private static readonly (string Label, string Icon, Key Key, Func<GamePanel> Create)[] Tools =
    [
        ("Projets", "screen", Key.P, () => new ProjectsPanel()),
        ("Équipe", "people", Key.T, () => new TeamPanel()),
        ("Aménagement", "chair", Key.B, () => new BuildPanel()),
        ("Développement", "star", Key.D, () => new TreePanel()),
        ("Finances", "money", Key.F, () => new FinancePanel()),
    ];

    private void BuildToolbar()
    {
        _toolbar.Children.Clear();
        foreach (var (label, icon, key, create) in Tools)
        {
            string badge = label == "Développement" && Session.Campaign.DevPoints > 0 ? $" ({Session.Campaign.DevPoints})" : "";
            var content = Ui.Row(6, Ui.Icon(icon, 24), Ui.Column(0, Ui.Text(label + badge, 15, badge != "" ? Palette.Lemon : Palette.Snow, wrap: false), Ui.Muted(key.ToString(), 11)));
            var button = new Button { Content = content, Padding = new Thickness(10, 4, 12, 8) };
            bool active = _stack.Count > 0 && _stack[0].GetType() == create().GetType();
            if (active)
                button.Classes.Add("selected");
            button.Classes.Add("tab");
            button.Click += (_, _) =>
            {
                Sfx.Play(Sound.Select);
                Toggle(create);
            };
            ToolTip.SetTip(button, $"{label} ({key})");
            _toolbar.Children.Add(button);
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

        var header = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"), Margin = new Thickness(0, 0, 0, 8) };
        if (_stack.Count > 1)
        {
            var back = Ui.Button("‹ Retour", PopPanel, "tab", tip: "Retour (Échap)");
            back.Margin = new Thickness(0, 0, 10, 0);
            header.Children.Add(back);
        }
        var titles = Ui.Column(0, Ui.Title(panel.Title, 22), Ui.Muted(panel.Context, 13));
        Grid.SetColumn(titles, 1);
        header.Children.Add(titles);
        var close = Ui.Button("✕", ClosePanels, "tab", tip: "Fermer");
        Grid.SetColumn(close, 2);
        header.Children.Add(close);

        // On garde la position de défilement quand le contenu est reconstruit
        double offset = (_panelBody.Content as DockPanel)?.Children.OfType<ScrollViewer>().FirstOrDefault()?.Offset.Y ?? 0;
        bool samePanel = ReferenceEquals(_lastBuilt, panel);
        _lastBuilt = panel;
        var body = panel.Build();
        var dock = new DockPanel();
        DockPanel.SetDock(header, Dock.Top);
        dock.Children.Add(header);
        var scroll = Ui.Scroll(body);
        dock.Children.Add(scroll);
        _panelBody.Content = dock;
        if (samePanel)
            scroll.Offset = new Vector(0, offset);
        LayoutPanelFrame();
    }

    private GamePanel? _lastBuilt;

    private void LayoutPanelFrame()
    {
        var panel = _stack.LastOrDefault();
        if (panel == null)
            return;
        if (panel.Large)
        {
            _panelFrame.HorizontalAlignment = HorizontalAlignment.Stretch;
            _panelFrame.Width = double.NaN;
            _panelFrame.Margin = new Thickness(40, 62, 40, 84);
        }
        else
        {
            _panelFrame.HorizontalAlignment = HorizontalAlignment.Right;
            _panelFrame.Width = 470;
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
        else if (Session.Settings.PauseInLargeMenus && _stack.LastOrDefault() is { Large: true } large)
            reason = large.Title.ToLowerInvariant();
        Session.AutoPause = reason;
    }

    // ------------------------------------------------------------------ Inspecteur de la sélection

    private void BuildInspector()
    {
        var content = SelectionInspector.Build(this);
        _inspector.Content = content == null ? null : new PixelPanel { Padding = new Thickness(12, 10), Width = 360, Child = content };
    }

    // ------------------------------------------------------------------ Bulles de notification

    private readonly List<(Control Control, double Age)> _toastAges = new();

    public void Toast(string message, bool ok) => AddToast(Ui.Text(message, 15, ok ? Palette.Snow : Palette.Lemon), ok ? null : Palette.Ember, null);

    private void ToastNotice(Notice notice)
    {
        var color = notice.Severity switch
        {
            Severity.Critical => Palette.Blood,
            Severity.Warning => Palette.Ember,
            Severity.Good => Palette.Leaf,
            _ => Palette.Sky
        };
        var content = Ui.Column(2, Ui.Text(notice.Title, 15, Palette.Snow, bold: true), string.IsNullOrEmpty(notice.Detail) ? null : Ui.Muted(Shorten(notice.Detail, 140), 13));
        AddToast(content, color, () => Navigate(notice));
    }

    private static string Shorten(string text, int max) => text.Length <= max ? text : text[..max] + "…";

    private void AddToast(Control content, IBrush? border, Action? click)
    {
        var frame = new PixelPanel { Padding = new Thickness(10, 6, 10, 10), Child = content, BorderBrush = border, BorderThickness = new Thickness(border != null ? 3 : 0) };
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
        _toasts.Children.Insert(0, frame);
        _toastAges.Add((frame, 0));
        while (_toasts.Children.Count > 4)
            _toasts.Children.RemoveAt(_toasts.Children.Count - 1);
    }

    private void UpdateToasts(double deltaTime)
    {
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
            case "product" when Session.Campaign.FindProduct(id) != null:
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
                OpenRoot(new BuildPanel());
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

    public void ShowModal(Control content, Action? onClose = null, double width = 520)
    {
        _modalHost.Content = new PixelPanel { Padding = new Thickness(22, 16), Width = width, Child = content };
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
        ShowModal(Ui.Column(12,
            Ui.Title(title),
            Ui.Text(detail, 16),
            Ui.Row(Ui.Button(action, () =>
            {
                CloseModal();
                confirmed();
            }, danger ? "red" : "gold"), Ui.Button("Annuler", CloseModal))));
    }

    private void ShowPauseMenu()
    {
        _pauseMenu = true;
        var c = Session.Campaign;
        string? saveName = null;
        var column = Ui.Column(10,
            Ui.Title("Pause", 28),
            Ui.Muted($"{c.Name} · {GameTime.Format(c.Minute)}"),
            Ui.Button("Reprendre", CloseModal, "gold"),
            Ui.Section("Sauvegarder",
                Ui.Input("", v => saveName = v, "Nom de la sauvegarde"),
                Ui.Button("Sauvegarder", () =>
                {
                    string name = string.IsNullOrWhiteSpace(saveName) ? GameTime.FormatDate(c.Minute) : saveName!;
                    bool ok = Session.Save(SaveKind.Manual, name);
                    Toast(Session.LastSaveMessage + (ok ? $" : « {name} »" : ""), ok);
                })),
            Ui.Button("Charger une autre partie", () => Leave(() => Window.ShowLoad())),
            Ui.Button("Réglages", () => ShowModal(SettingsScreen.SettingsForm(Window, ShowPauseSettings, ShowPauseMenu), null, 560)),
            Ui.Button("Menu principal", () => Leave(() => Window.ShowTitle())),
            Ui.Muted("Quitter le menu ou la partie enregistre automatiquement une sauvegarde de sortie. Rien n'est jamais effacé."));
        ShowModal(column, null, 460);
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

    private void ShowDefeat()
    {
        var c = Session.Campaign;
        _pauseMenu = true;
        ShowModal(Ui.Column(12,
            Ui.Title("Entreprise liquidée", 28),
            Ui.Text(c.Solvency.LiquidationReport ?? "", 15),
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

        switch (key)
        {
            case Key.Space:
                Session.TogglePause();
                return true;
            case >= Key.D1 and <= Key.D4:
                Session.SetSpeed(key - Key.D0);
                return true;
            case Key.Left or Key.A:
                Scene.Pan(-40, 0);
                return true;
            case Key.Right or Key.D:
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
