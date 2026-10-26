using System;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Threading;
using Core.company;
using Core.simulation;

namespace UI;

public partial class MainWindow : Window
{
    private GameSession _session;
    private DispatcherTimer _timer;
    private Stopwatch _stopWatch;
    
    public MainWindow(GameSession session)
    {
        InitializeComponent();
        
        _session = session;
        
        _session.Company.BalanceChanged += OnBalanceChanged;
        
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _stopWatch = new Stopwatch();
        
        _timer.Tick += OnTimerTick;
        
        _stopWatch.Start();
        
        _timer.Start();

        BalanceText.Text = $"Solde : {_session.Company.WalletBalance} CHF";
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        Console.WriteLine("Tick !");
        
        double deltaTime = _stopWatch.Elapsed.TotalSeconds;
        _stopWatch.Restart();
        
        _session.Update(deltaTime);
    }

    private void OnBalanceChanged(double balance)
    {
        BalanceText.Text = $"Solde : {balance:F2} CHF";
    }
}