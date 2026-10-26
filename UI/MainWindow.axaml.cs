using System;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Core.states;
using UI.ViewModels;
using UI.Views;

namespace UI;

/// <summary>
/// La boucle de jeu : un minuteur d'environ 60 images/s mesure le vrai temps écoulé,
/// fait avancer la machine à états, anime l'écran courant et joue les effets (secousse, flash, fondu).
/// </summary>
public partial class MainWindow : Window
{
    private readonly GameStateMachine _machine;
    private readonly DispatcherTimer _timer;
    private readonly Stopwatch _stopWatch = new();
    private readonly Random _random = new();
    private readonly TranslateTransform _shake = new();
    private double _shakeStrength;

    public MainWindow(GameStateMachine machine)
    {
        InitializeComponent();

        _machine = machine;
        _machine.StateChanged += OnStateChanged;
        Root.RenderTransform = _shake;
        Juice.ShakeRequested += strength => _shakeStrength = Math.Max(_shakeStrength, strength);
        Juice.FlashRequested += color =>
        {
            FlashLayer.Fill = new SolidColorBrush(color);
            FlashLayer.Opacity = 0.45;
        };

        _machine.ChangeState(new MenuState(_machine));

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _timer.Tick += OnTimerTick;
        Closed += (_, _) => _timer.Stop();
        KeyDown += OnKeyDown;

        _stopWatch.Start();
        _timer.Start();
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        double deltaTime = _stopWatch.Elapsed.TotalSeconds;
        _stopWatch.Restart();

        _machine.Update(deltaTime);
        (ScreenHost.Content as IGameScreen)?.Tick(deltaTime);
        AnimateEffects(deltaTime);
    }

    private void AnimateEffects(double deltaTime)
    {
        // La secousse s'amortit vite, sur des positions entières pour rester « pixel »
        _shakeStrength = Math.Max(0, _shakeStrength - deltaTime * 40);
        _shake.X = Math.Round((_random.NextDouble() * 2 - 1) * _shakeStrength);
        _shake.Y = Math.Round((_random.NextDouble() * 2 - 1) * _shakeStrength);

        FlashLayer.Opacity = Math.Max(0, FlashLayer.Opacity - deltaTime * 1.5);
        FadeLayer.Opacity = Math.Max(0, FadeLayer.Opacity - deltaTime * 3);
    }

    private void OnStateChanged(IGameState state)
    {
        ScreenHost.Content = state switch
        {
            MenuState menu => new MenuView(menu),
            PlayingState playing => new PlayingView(playing),
            GameOverState gameOver => new GameOverView(gameOver),
            _ => null
        };
        FadeLayer.Opacity = 1;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.F11)
        {
            WindowState = WindowState == WindowState.FullScreen ? WindowState.Normal : WindowState.FullScreen;
            e.Handled = true;
            return;
        }

        // On laisse les champs de texte recevoir leurs lettres
        if (e.Source is TextBox && e.Key != Key.Escape)
            return;

        if (ScreenHost.Content is IGameScreen screen && screen.HandleKey(e.Key))
            e.Handled = true;
    }
}
