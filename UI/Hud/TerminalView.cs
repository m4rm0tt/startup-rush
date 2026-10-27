using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Core.campaign;
using UI.Audio;
using UI.Game;
using UI.Iso;
using UI.Pixel;

namespace UI.Hud;

/// <summary>
/// Le terminal du fondateur : un mot s'affiche, on le tape, on valide (Espace ou Entrée), le suivant apparaît. C'est un jeu
/// de saisie entièrement simulé : rien n'est exécuté sur l'ordinateur. Chaque mot juste apporte un peu de travail à la vraie
/// tâche du projet (Campaign.SubmitWord) ; le temps est mesuré en temps réel, jamais multiplié par la vitesse du jeu.
/// Une faute se corrige sans pénalité. On peut fermer à tout moment : le travail validé reste acquis.
/// </summary>
public class TerminalView : UserControl
{
    private static readonly IBrush Text = Palette.Brush("#8fe3cc");
    private static readonly IBrush Dim = Palette.Brush("#3a9a98");
    private static readonly IBrush Ok = Palette.Brush("#7ef08a");
    private static readonly IBrush Error = Palette.Brush("#ff6157");
    private static readonly IBrush Warn = Palette.Brush("#ffbf4a");
    private static readonly IBrush Surface = Palette.Brush("#162235");

    private readonly GameSession _session;
    private readonly Action _close;
    private readonly Action _again;
    private readonly TextBox _input = new();
    private readonly TextBlock _word = new();
    private readonly TextBlock _stage = new();
    private readonly TextBlock _stats = new();
    private readonly TextBlock _status = new();
    private readonly StackPanel _segments = new() { Orientation = Orientation.Horizontal, Spacing = 0 };
    private readonly StackPanel _actions = new() { Orientation = Orientation.Horizontal, Spacing = 8 };
    private readonly Stopwatch _shown = Stopwatch.StartNew();
    private bool _keyDown;
    private bool _wasRunning = true;

    private Campaign Campaign => _session.Campaign;

    public TerminalView(GameSession session, Action close, Action again)
    {
        _session = session;
        _close = close;
        _again = again;
        var mono = (FontFamily)Application.Current!.FindResource("TermFont")!;

        _input.FontFamily = mono;
        _input.FontSize = 24;
        _input.Background = Palette.Brush("#1f2f47");
        _input.Foreground = Text;
        _input.BorderBrush = Dim;
        _input.CaretBrush = Ok;
        _input.Width = 360;
        _input.HorizontalAlignment = HorizontalAlignment.Left;
        _input.AddHandler(KeyDownEvent, OnInputKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);
        _input.KeyUp += (_, e) =>
        {
            if (e.Key is Key.Enter or Key.Space)
                _keyDown = false;
        };
        _input.TextChanged += (_, _) => Paint();

        foreach (var block in new[] { _word, _stage, _stats, _status })
        {
            block.FontFamily = mono;
            block.Foreground = Text;
        }
        _word.FontSize = 36;
        _stage.FontSize = 18;
        _stage.Foreground = Dim;
        _stats.FontSize = 18;
        _status.FontSize = 16;
        _status.Foreground = Dim;
        _status.TextWrapping = TextWrapping.Wrap;

        var title = new TextBlock
        {
            FontFamily = mono, FontSize = 18, Foreground = Palette.Brush("#fbf5e6"),
            Text = $"terminal · {Campaign.FindProduct(Campaign.Terminal!.ProductId)?.Name}",
        };
        var body = new StackPanel
        {
            Spacing = 8,
            Children =
            {
                Line("term_mark_prompt", _stage),
                _word,
                _input,
                _segments,
                Line("term_mark_info", _stats),
                _status,
                _actions,
            },
        };
        var surface = new Border { Background = Surface, Padding = new Thickness(14, 10), Child = body };
        Content = new PixelPanel
        {
            Skin = "pack:term_frame",
            Padding = new Thickness(10, 34, 10, 12),
            Width = 560,
            Child = new Grid { Children = { surface, new Border { Margin = new Thickness(30, -30, 0, 0), Child = title, VerticalAlignment = VerticalAlignment.Top } } },
        };
        Paint();
    }

    private static Control Line(string mark, TextBlock text) => new StackPanel
    {
        Orientation = Orientation.Horizontal, Spacing = 6,
        Children = { new Image { Source = PackLibrary.Thumbnail(mark), Width = 12, Height = 24, Stretch = Stretch.Uniform }, text },
    };

    public void FocusInput() => _input.Focus();

    /// <summary>Redessine après des mots validés hors du clavier (parcours de captures).</summary>
    public void Repaint() => Paint();

    /// <summary>Met un début de mot dans le champ (parcours de captures).</summary>
    public void SetInput(string text) => _input.Text = text;

    /// <summary>Espace ou Entrée valident le mot ; maintenir la touche n'en valide qu'un.</summary>
    private void OnInputKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key is not (Key.Enter or Key.Space))
            return;
        e.Handled = true;
        if (_keyDown || Campaign.Terminal is not { Finished: false } || !_session.Running && _session.AutoPause != "saisie")
            return;
        _keyDown = true;
        string typed = _input.Text ?? "";
        if (typed.Trim().Length == 0)
            return;
        var result = Campaign.SubmitWord(typed, _shown.Elapsed.TotalSeconds);
        Sfx.Play(result.Correct ? Sound.Click : Sound.Error);
        if (result.Correct)
        {
            _input.Text = "";
            _shown.Restart();
        }
        Paint();
    }

    /// <summary>À chaque image : la vraie pause du jeu met la saisie en attente, sans rien perdre.</summary>
    public void Tick()
    {
        bool running = _session.Running || _session.AutoPause == "saisie";
        if (running != _wasRunning)
        {
            _wasRunning = running;
            _input.IsEnabled = running && Campaign.Terminal is { Finished: false };
            if (!running)
                _shown.Stop();
            else
                _shown.Start();
            Paint();
        }
    }

    private void Paint()
    {
        var session = Campaign.Terminal;
        if (session == null)
            return;
        string typed = _input.Text ?? "";
        bool matching = session.Word.StartsWith(typed.Trim(), StringComparison.Ordinal);
        _word.Text = session.Finished ? "—" : session.Word;
        _word.Foreground = typed.Length == 0 ? Text : matching ? Ok : Error;
        _stage.Text = session.Stage;
        _stats.Text = $"{session.WordsTyped}/{session.Length} mots · {session.Points:0.0} pts · rendement {session.LastYield * 100:0} % · fautes {session.Errors}";

        _segments.Children.Clear();
        for (int i = 0; i < session.Length; i++)
            _segments.Children.Add(new Image
            {
                Source = PackLibrary.Thumbnail(i < session.WordsTyped ? "term_progress_full" : "term_progress_empty"),
                Width = 18, Height = 18, Stretch = Stretch.Uniform,
            });

        _actions.Children.Clear();
        if (session.Finished)
        {
            _input.IsEnabled = false;
            _status.Text = session.EndReason;
            _status.Foreground = Warn;
            var (ok, reason) = Campaign.CanOpenTerminal(session.ProductId);
            if (ok)
                _actions.Children.Add(Ui.Button("Continuer (Entrée)", _again, "gold"));
            else if (reason != "Le terminal est déjà ouvert.")
                _status.Text += " " + reason;
            _actions.Children.Add(Ui.Button("Fermer (Échap)", _close));
        }
        else
        {
            bool paused = !_session.Running && _session.AutoPause != "saisie";
            _status.Text = paused ? "Jeu en pause : la saisie reprendra avec le temps."
                : _session.AutoPause == "saisie" ? "L'entreprise est en pause pendant la saisie (réglage d'accessibilité)."
                : "L'entreprise continue à vitesse normale. Espace ou Entrée : valider. Échap : fermer (le travail validé reste acquis).";
            _status.Foreground = paused ? Warn : Dim;
            _actions.Children.Add(Ui.Button("Fermer", _close));
        }
    }

    /// <summary>Entrée après la fin d'une session : en ouvrir une autre (si c'est possible).</summary>
    public bool HandleEnter()
    {
        if (Campaign.Terminal is { Finished: true } session && Campaign.CanOpenTerminal(session.ProductId).Ok)
        {
            _again();
            return true;
        }
        return false;
    }
}
