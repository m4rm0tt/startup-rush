using Avalonia.Controls;
using Core.campaign;
using UI.Audio;
using UI.Game;
using UI.Screens;

namespace UI.Hud;

/// <summary>
/// Un menu de gestion ouvrable. Il se construit à partir de l'état réel de la campagne et appelle ses commandes :
/// il n'a pas de données à lui. « Large » occupe presque toute la fenêtre (et peut mettre le temps en pause).
/// </summary>
public abstract class GamePanel
{
    protected GameScreen Screen { get; private set; } = null!;
    protected GameSession Session => Screen.Session;
    protected Campaign Campaign => Session.Campaign;

    public abstract string Title { get; }

    /// <summary>Contexte affiché sous le titre (entreprise, produit...).</summary>
    public virtual string Context => Campaign.MainCompany.Name;

    public virtual bool Large => false;

    /// <summary>Reconstruit le contenu régulièrement pour suivre la simulation (désactivé pendant une saisie).</summary>
    public virtual bool AutoRefresh => true;

    public void Attach(GameScreen screen) => Screen = screen;

    public abstract Control Build();

    protected void Refresh() => Screen.RefreshPanel();

    protected void Open(GamePanel panel) => Screen.PushPanel(panel);

    /// <summary>
    /// Une aide d'une phrase, affichée seulement à la première ouverture de cet outil, avec un bouton pour la fermer.
    /// Renvoie null une fois lue : rien n'oblige à la lire pour jouer.
    /// </summary>
    protected Control? Hint(string key, string text)
    {
        var settings = Screen.Window.Settings;
        if (settings.SeenHints.Contains(key))
            return null;
        var row = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto") };
        row.Children.Add(Ui.Icon("star", 20));
        var label = Ui.Text(text, 14, UI.Pixel.Palette.Lemon);
        label.Margin = new Avalonia.Thickness(8, 0);
        Grid.SetColumn(label, 1);
        row.Children.Add(label);
        var ok = Ui.Button("Compris", () =>
        {
            settings.SeenHints.Add(key);
            Screen.Window.SaveSettings();
            Refresh();
        }, "tab");
        Grid.SetColumn(ok, 2);
        row.Children.Add(ok);
        return Ui.Card(row, UI.Pixel.Palette.Lemon);
    }

    /// <summary>Exécute une commande, affiche son message, puis redessine le menu.</summary>
    protected void Run(CommandResult result, string? success = null)
    {
        if (!result.Ok)
        {
            Sfx.Play(Sound.Error);
            Screen.Toast(result.Message, false);
        }
        else if (!string.IsNullOrEmpty(success ?? result.Message))
            Screen.Toast(success ?? result.Message, true);
        Refresh();
    }
}
