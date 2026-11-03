using System;
using System.Collections.Generic;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Core.campaign;
using Core.persistence;
using UI.Audio;
using UI.Game;
using UI.Screens;

namespace UI;

/// <summary>
/// La fenêtre et la boucle : un minuteur d'environ 60 images/s mesure le vrai temps écoulé et le donne à l'écran courant.
/// Elle porte aussi la navigation entre écrans, les réglages et le dossier des sauvegardes.
/// </summary>
public partial class MainWindow : Window
{
    private readonly DispatcherTimer _timer;
    private readonly Stopwatch _stopWatch = new();
    private readonly HashSet<Key> _heldKeys = new();

    public SaveStore Store { get; }
    public Settings Settings { get; }
    public string SettingsPath { get; }

    public MainWindow() : this(new SaveStore(SaveStore.DefaultRoot), new Settings(), Settings.DefaultPath)
    {
    }

    public MainWindow(SaveStore store, Settings settings, string settingsPath)
    {
        InitializeComponent();
        Store = store;
        Settings = settings;
        SettingsPath = settingsPath;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _timer.Tick += OnTimerTick;
        Closing += OnClosing;
        Closed += (_, _) => _timer.Stop();
        // En tunnel : le jeu voit les touches avant les contrôles (sauf la saisie de texte)
        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
        AddHandler(KeyUpEvent, (_, e) => _heldKeys.Remove(e.Key), RoutingStrategies.Tunnel);

        if (settings.Fullscreen)
            WindowState = WindowState.FullScreen;
        UI.Pixel.PixelText.Large = settings.LargeText;
        Loc.Language = settings.Language;
        CrashLog.Window = this;

        ShowTitle();
        _stopWatch.Start();
        _timer.Start();
    }

    public IScreen? Current => ScreenHost.Content as IScreen;

    /// <summary>
    /// Une opération qui fait attendre (chargement, sauvegarde) : le curseur d'attente du pack s'affiche d'abord, le travail
    /// suit au prochain passage du fil d'interface, puis le curseur revient.
    /// </summary>
    public void Busy(Action work)
    {
        Cursor = UI.Pixel.PackSkins.Cursor("cursor_busy");
        IsBusy = true;
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            try
            {
                work();
            }
            finally
            {
                Cursor = null;
                IsBusy = false;
            }
        }, Avalonia.Threading.DispatcherPriority.Background);
    }

    /// <summary>Une opération longue est en cours (curseur d'attente affiché).</summary>
    public bool IsBusy { get; private set; }

    /// <summary>Plein écran ou fenêtré, et retenu dans les réglages.</summary>
    public void SetFullscreen(bool on)
    {
        WindowState = on ? WindowState.FullScreen : WindowState.Normal;
        Settings.Fullscreen = on;
        SaveSettings();
    }

    /// <summary>
    /// Langue de l'interface : retenue dans les réglages ; l'écran de jeu reconstruit sa barre d'outils (la barre du haut se
    /// reconstruit d'elle-même), les écrans de menu se reconstruisent par leur appelant.
    /// </summary>
    public void SetLanguage(string language)
    {
        Loc.Language = language;
        Settings.Language = Loc.Language;
        SaveSettings();
        (Current as Screens.GameScreen)?.Relocalize();
    }

    /// <summary>Texte agrandi : les tailles déjà affichées sont recalculées tout de suite, sans relancer le jeu.</summary>
    public void SetLargeText(bool on)
    {
        Settings.LargeText = on;
        UI.Pixel.PixelText.Large = on;
        SaveSettings();
        foreach (var text in Avalonia.VisualTree.VisualExtensions.GetVisualDescendants(this))
        {
            if (text is TextBlock block)
                block.CoerceValue(TextBlock.FontSizeProperty);
            else if (text is Avalonia.Controls.Primitives.TemplatedControl control)
                control.CoerceValue(Avalonia.Controls.Primitives.TemplatedControl.FontSizeProperty);
            else if (text is Avalonia.Controls.Presenters.TextPresenter presenter)
                presenter.CoerceValue(Avalonia.Controls.Documents.TextElement.FontSizeProperty);
        }
    }

    public void SaveSettings()
    {
        Settings.Save(SettingsPath);
        Sfx.MusicVolume = Settings.MusicVolume;
        Sfx.SoundVolume = Settings.SoundVolume;
    }

    private void Show(Control screen, Music music)
    {
        Current?.Leave();
        ScreenHost.Content = screen;
        FadeLayer.Opacity = 1;
        Sfx.PlayMusic(music);
        _heldKeys.Clear();
    }

    public void ShowTitle(string? message = null) => Show(new TitleScreen(this, message), Music.Menu);

    public void ShowNewCampaign() => Show(new NewCampaignScreen(this), Music.Menu);

    public void ShowLoad() => Show(new LoadScreen(this), Music.Menu);

    public void ShowSettings(Action back) => Show(new SettingsScreen(this, back), Music.Menu);

    public GameScreen StartGame(Campaign campaign, string? savePath, string? message = null)
    {
        var screen = new GameScreen(this, new GameSession(campaign, Store, Settings, savePath));
        Show(screen, Music.Work);
        if (!string.IsNullOrEmpty(message))
            screen.Toast(message, true);
        if (!string.IsNullOrEmpty(campaign.MigrationNote))
        {
            screen.Toast(campaign.MigrationNote, true);
            campaign.MigrationNote = "";
        }
        return screen;
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        double deltaTime = _stopWatch.Elapsed.TotalSeconds;
        _stopWatch.Restart();
        Current?.Tick(deltaTime);
        FadeLayer.Opacity = Math.Max(0, FadeLayer.Opacity - deltaTime * 4);
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        // Fermer la fenêtre en pleine partie : on sauvegarde l'état exact pour pouvoir reprendre
        if (Current is GameScreen game)
            game.Session.Save(SaveKind.Quick);
        Current?.Leave();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (!_heldKeys.Add(e.Key) && e.Key is not (Key.Left or Key.Right or Key.Up or Key.Down or Key.W or Key.A or Key.S or Key.D))
        {
            e.Handled = e.Source is not TextBox;
            return;
        }

        if (e.Key == Key.F11)
        {
            SetFullscreen(WindowState != WindowState.FullScreen);
            e.Handled = true;
            return;
        }

        // Les champs de texte gardent leurs touches, sauf Échap
        if (e.Source is TextBox && e.Key != Key.Escape)
            return;

        if (Current?.HandleKey(e.Key, e.KeyModifiers) == true)
            e.Handled = true;
    }
}
