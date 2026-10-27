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

        ShowTitle();
        _stopWatch.Start();
        _timer.Start();
    }

    public IScreen? Current => ScreenHost.Content as IScreen;

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
            WindowState = WindowState == WindowState.FullScreen ? WindowState.Normal : WindowState.FullScreen;
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
