using System;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Threading;
using Core.states;
using UI.Views;

namespace UI;

public partial class MainWindow : Window
{
    private readonly GameStateMachine _machine;
    private readonly DispatcherTimer _timer;
    private readonly Stopwatch _stopWatch;

    public MainWindow(GameStateMachine machine)
    {
        InitializeComponent();

        _machine = machine;
        _machine.StateChanged += OnStateChanged;
        _machine.ChangeState(new MenuState(_machine));

        // Environ 60 mises à jour par seconde ; le vrai temps écoulé est mesuré par le Stopwatch
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        _stopWatch = new Stopwatch();

        _timer.Tick += OnTimerTick;
        Closed += (_, _) => _timer.Stop();

        _stopWatch.Start();
        _timer.Start();
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        double deltaTime = _stopWatch.Elapsed.TotalSeconds;
        _stopWatch.Restart();

        _machine.Update(deltaTime);

        if (ScreenHost.Content is PlayingView playingView)
            playingView.Refresh();
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
    }
}
