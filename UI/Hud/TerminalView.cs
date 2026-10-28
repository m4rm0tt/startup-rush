using System;
using System.Collections.Generic;
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

    private readonly GameSession _session;
    private readonly Action _close;
    private readonly Action _again;
    private readonly TextBox _input = new();
    private readonly PixelPanel _inputFrame = new() { Skin = "pack:term_input:normal", Width = 380, HorizontalAlignment = HorizontalAlignment.Left };
    private readonly StackPanel _word = new() { Orientation = Orientation.Horizontal };
    private readonly TextBlock _done = new(), _rest = new();
    private readonly Image _cursor = new() { Width = 18, Height = 36, Stretch = Stretch.Fill };
    private readonly StackPanel _log = new() { Spacing = 2 };
    private readonly ScrollViewer _logScroll = new() { MaxHeight = 140, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled };
    private readonly List<(string Mark, string Text, IBrush Color)> _lines = new();
    private readonly TextBlock _stage = new();
    private readonly TextBlock _stats = new();
    private readonly TextBlock _status = new();
    private readonly Image _statusMark = new() { Width = 12, Height = 24, Stretch = Stretch.Fill, VerticalAlignment = VerticalAlignment.Top };
    private readonly StackPanel _segments = new() { Orientation = Orientation.Horizontal, Spacing = 0 };
    private readonly StackPanel _actions = new() { Orientation = Orientation.Horizontal, Spacing = 8 };
    private readonly Stopwatch _shown = Stopwatch.StartNew();
    private bool _keyDown;
    private bool _wasRunning = true;

    private Campaign Campaign => _session.Campaign;

    /// <summary>Saisie simplifiée (réglage d'accessibilité) : le mot s'écrit tout seul, Entrée le valide.</summary>
    private bool Assisted => _session.Settings.SimplifiedTyping;

    public TerminalView(GameSession session, Action close, Action again)
    {
        _session = session;
        _close = close;
        _again = again;
        var mono = (FontFamily)Application.Current!.FindResource("TermFont")!;

        // Champ de saisie du pack : chevron d'invite dans sa marge gauche, texte en police mono
        _input.FontFamily = mono;
        _input.FontSize = 24;
        _input.Background = Brushes.Transparent;
        _input.BorderThickness = new Thickness(0);
        _input.Foreground = Text;
        _input.CaretBrush = Ok;
        _input.Padding = new Thickness(24, 2, 8, 4);
        _input.Classes.Add("bare");
        _inputFrame.Child = _input;
        _input.GotFocus += (_, _) => _inputFrame.Skin = "pack:term_input:focus";
        _input.LostFocus += (_, _) => _inputFrame.Skin = _input.IsEnabled ? "pack:term_input:normal" : "pack:term_input:disabled";
        _input.AddHandler(KeyDownEvent, OnInputKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);
        _input.KeyUp += (_, e) =>
        {
            if (e.Key is Key.Enter or Key.Space)
                _keyDown = false;
        };
        _input.TextChanged += (_, _) => Paint();
        _input.IsReadOnly = Assisted;

        foreach (var block in new[] { _done, _rest, _stage, _stats, _status })
        {
            block.FontFamily = mono;
            block.Foreground = Text;
        }
        // Le mot à taper : la partie déjà juste, le curseur du pack à la position atteinte, puis le reste
        _done.FontSize = 36;
        _rest.FontSize = 36;
        _done.Foreground = Ok;
        RenderOptions.SetBitmapInterpolationMode(_cursor, Avalonia.Media.Imaging.BitmapInterpolationMode.None);
        _cursor.Source = PackSkins.Sprite(_session.Settings.TerminalCursor switch { "bar" => "term_cursor_bar", "underline" => "term_cursor_underline", _ => "term_cursor_block" });
        _cursor.VerticalAlignment = VerticalAlignment.Center;
        _word.Children.Add(_done);
        _word.Children.Add(_cursor);
        _word.Children.Add(_rest);
        _logScroll.Content = _log;
        _logScroll.Classes.Add("term");
        _stage.FontSize = 18;
        _stage.Foreground = Dim;
        _stats.FontSize = 18;
        _status.FontSize = 16;
        _status.Foreground = Dim;
        _status.TextWrapping = TextWrapping.Wrap;

        var title = new PixelPanel
        {
            Skin = "pack:term_titlebar", Padding = new Thickness(8, 2, 8, 4), HorizontalAlignment = HorizontalAlignment.Left,
            Child = new TextBlock { FontFamily = mono, FontSize = 18, Foreground = Palette.Brush("#fbf5e6"), Text = $"terminal · {Campaign.FindProduct(Campaign.Terminal!.ProductId)?.Name}" },
        };
        // La barre de progression sur une bande de sélection, comme la ligne en cours d'un vrai terminal
        var progress = new PixelPanel { Skin = "pack:term_selection", Padding = new Thickness(4, 2), Child = _segments, HorizontalAlignment = HorizontalAlignment.Left };
        var body = new StackPanel
        {
            Spacing = 8,
            Children =
            {
                _logScroll,
                Line("term_mark_prompt", _stage),
                _word,
                _inputFrame,
                progress,
                Line("term_mark_info", _stats),
                new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Children = { _statusMark, _status } },
                _actions,
            },
        };
        _status.MaxWidth = 470;
        var surface = new PixelPanel { Skin = "pack:term_surface", Padding = new Thickness(14, 10), Child = body };
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
        Children = { Mark(mark), text },
    };

    private static Image Mark(string mark)
    {
        var image = new Image { Source = PackSkins.Sprite(mark), Width = 12, Height = 24, Stretch = Stretch.Fill };
        RenderOptions.SetBitmapInterpolationMode(image, Avalonia.Media.Imaging.BitmapInterpolationMode.None);
        return image;
    }

    /// <summary>Ajoute une ligne au journal du terminal (mots validés, fautes, fin de session) et le fait défiler en bas.</summary>
    private void Log(string mark, string text, IBrush color)
    {
        _lines.Add((mark, text, color));
        if (_lines.Count > 40)
            _lines.RemoveAt(0);
        _log.Children.Clear();
        var mono = (FontFamily)Application.Current!.FindResource("TermFont")!;
        foreach (var (m, t, c) in _lines)
            _log.Children.Add(Line(m, new TextBlock { Text = t, FontFamily = mono, FontSize = 16, Foreground = c }));
        _logScroll.ScrollToEnd();
    }

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
        if (_keyDown)
            return;
        _keyDown = true;
        Submit();
    }

    /// <summary>Valide le mot du champ (Espace, Entrée, ou le parcours de captures) : même chemin que la frappe.</summary>
    public void Submit()
    {
        if (Campaign.Terminal is not { Finished: false } || !_session.Running && _session.AutoPause != "saisie")
            return;
        string typed = _input.Text ?? "";
        if (typed.Trim().Length == 0)
            return;
        // En saisie simplifiée, on valide seulement un mot entièrement écrit
        if (Assisted && typed != Campaign.Terminal.Word)
            return;
        string expected = Campaign.Terminal.Word;
        var result = Campaign.SubmitWord(typed, _shown.Elapsed.TotalSeconds, Assisted);
        Sfx.Play(result.Correct ? Sound.Click : Sound.Error);
        if (result.Correct)
            Log("term_mark_ok", expected, Ok);
        else
            Log("term_mark_error", $"{typed.Trim()} ≠ {expected}", Error);
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
        // Curseur qui clignote toutes les 0,5 s (fixe si les effets sont réduits)
        _cursor.Opacity = _session.Settings.ReduceEffects || (int)(_shown.Elapsed.TotalSeconds * 2) % 2 == 0 ? 1 : 0;
        bool running = _session.Running || _session.AutoPause == "saisie";
        if (Assisted && running && Campaign.Terminal is { Finished: false } session)
        {
            // Le mot s'écrit lettre après lettre, au rythme réglé : la validation reste un geste du joueur
            int letters = Math.Min(session.Word.Length, (int)(_shown.Elapsed.TotalSeconds * Tuning.Terminal.AssistedCharsPerSecond));
            string shown = session.Word[..letters];
            if (_input.Text != shown)
                _input.Text = shown;
        }
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
        if (session.Finished)
        {
            _done.Text = "";
            _rest.Text = "—";
            _cursor.IsVisible = false;
        }
        else
        {
            // Juste jusqu'ici : en vert ; une faute : tout le mot en rouge, le curseur reste au début
            int good = matching ? typed.Trim().Length : 0;
            _done.Text = session.Word[..good];
            _rest.Text = session.Word[good..];
            _rest.Foreground = typed.Length == 0 || matching ? Text : Error;
        }
        _stage.Text = session.Stage;
        _stats.Text = $"{session.WordsTyped}/{session.Length} mots · {session.Points:0.0} pts · rendement {session.LastYield * 100:0} % · fautes {session.Errors}";

        // [####▒---] : bouts, mots faits, mot en cours à moitié, mots restants (cellules mono du pack)
        _segments.Children.Clear();
        _segments.Children.Add(Mark("term_progress_cap_left"));
        for (int i = 0; i < session.Length; i++)
            _segments.Children.Add(Mark(i < session.WordsTyped ? "term_progress_full"
                : i == session.WordsTyped && typed.Trim().Length > 0 && !session.Finished ? "term_progress_half" : "term_progress_empty"));
        _segments.Children.Add(Mark("term_progress_cap_right"));

        _actions.Children.Clear();
        if (session.Finished)
        {
            _input.IsEnabled = false;
            _inputFrame.Skin = "pack:term_input:disabled";
            if (_status.Text != session.EndReason)
                Log("term_mark_warning", session.EndReason, Warn);
            _status.Text = session.EndReason;
            _status.Foreground = Warn;
            _statusMark.Source = PackSkins.Sprite("term_mark_warning");
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
                : Assisted ? "Saisie simplifiée : le mot s'écrit tout seul, Entrée le valide (rendement de la frappe lente). Échap : fermer."
                : "L'entreprise continue à vitesse normale. Espace ou Entrée : valider. Échap : fermer (le travail validé reste acquis).";
            _status.Foreground = paused ? Warn : Dim;
            _statusMark.Source = PackSkins.Sprite(paused ? "term_mark_warning" : "term_mark_running");
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
