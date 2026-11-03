using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Core.campaign;
using UI.Pixel;
using UI.Screens;

namespace UI.Hud;

/// <summary>
/// La fenêtre de lancement : le mini-jeu de la grosse action (une règle en une phrase, quelques secondes), ou le lancement
/// sans jouer au résultat de base, ou la délégation à un employé. Rien n'est perdu à ne pas jouer.
/// </summary>
public static class LaunchView
{
    /// <summary>
    /// Le plateau interactif d'un mini-jeu (déploiement, recette, calibration) : il se redessine à chaque geste ; « score »
    /// donne le résultat de la réponse en cours (0 à 1).
    /// </summary>
    public static Control Board(LaunchChallenge challenge, out System.Func<double> score)
    {
        var order = Enumerable.Range(0, challenge.Items.Count).ToList();
        var picked = new HashSet<int>();
        var knobs = Enumerable.Repeat(5, challenge.Items.Count).ToList();
        var body = Ui.Column(6);
        if (MiniGames.Board(challenge, out var custom) is { } special)
        {
            score = custom;
            return special;
        }

        void Paint()
        {
            body.Children.Clear();
            if (challenge.Calibration)
            {
                // Trois réglages de 0 à 10 : la zone verte est la cible (pile dessus : plein effet ; à un cran : la moitié)
                for (int k = 0; k < knobs.Count; k++)
                {
                    int index = k;
                    var track = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 1 };
                    for (int v = 0; v <= 10; v++)
                        track.Children.Add(new Avalonia.Controls.Border
                        {
                            Width = 14, Height = 14,
                            Background = v == knobs[k] ? Palette.Ink : System.Math.Abs(v - challenge.Answer[k]) == 0 ? Palette.Leaf
                                : System.Math.Abs(v - challenge.Answer[k]) == 1 ? Palette.Lemon : Palette.Slate,
                        });
                    body.Children.Add(Ui.Row(6, Ui.Text(challenge.Items[k], 14, Palette.Cloud, wrap: false),
                        Ui.Button("−", () => { knobs[index] = System.Math.Max(0, knobs[index] - 1); Paint(); }, "tab"),
                        track,
                        Ui.Button("+", () => { knobs[index] = System.Math.Min(10, knobs[index] + 1); Paint(); }, "tab")));
                }
                return;
            }
            for (int k = 0; k < order.Count; k++)
            {
                int index = challenge.Ordering ? order[k] : k;
                int position = k;
                string text = challenge.Items[index];
                if (challenge.Ordering)
                    body.Children.Add(Ui.Row(6, Ui.Text($"{position + 1}.", 15, Palette.Fog, wrap: false),
                        Ui.Button("▲", () =>
                        {
                            if (position == 0)
                                return;
                            (order[position - 1], order[position]) = (order[position], order[position - 1]);
                            Paint();
                        }, "tab", enabled: position > 0),
                        Ui.Text(text, 15)));
                else
                    body.Children.Add(Ui.Button(text, () =>
                    {
                        if (!picked.Add(index))
                            picked.Remove(index);
                        Paint();
                    }, picked.Contains(index) ? "tab selected" : "tab"));
            }
        }
        Paint();
        // Déploiement : l'ordre choisi (indices des étapes présentées) ; recette : les tests cochés ; calibration : les réglages
        score = () => challenge.Score(challenge.Calibration ? knobs : challenge.Ordering ? order : picked.ToList());
        return body;
    }

    public static void Show(GameScreen screen, int productId)
    {
        var campaign = screen.Session.Campaign;
        var product = campaign.FindProduct(productId);
        if (product == null)
            return;
        var challenge = campaign.Challenge(productId);
        var body = Board(challenge, out var score);

        void Finish(CommandResult result)
        {
            screen.CloseModal();
            screen.Toast(result.Ok ? $"{product.Name} est sorti !" : result.Message, result.Ok);
            screen.RefreshPanel();
        }

        var (canDelegate, why) = campaign.CanDelegateLaunch(product);
        var content = Ui.Column(10,
            Ui.Title($"Lancer {product.Name}"),
            Ui.Text(challenge.Rule, 15),
            body,
            Ui.Muted(challenge.Calibration
                ? $"Bonus : jusqu'à 50 % de pannes en moins sur les {Tuning.Hardware.FirstSeries} premières unités. Sans jouer, la série part au réglage de base."
                : $"Bonus : jusqu'à {Campaign.LaunchBugReduction:P0} de bugs en moins à la sortie. Sans jouer, le lancement se fait au résultat de base.", 13),
            Ui.Row(8,
                Ui.Button("Valider et lancer", () =>
                {
                    double result = score();
                    var launched = campaign.LaunchWith(productId, result);
                    if (launched.Ok)
                        screen.Toast(challenge.Calibration ? $"Calibration : {result:P0}, {0.5 * result:P0} de pannes en moins sur la première série."
                            : $"Mini-jeu : {result:P0} de réussite, {Campaign.LaunchBugReduction * result:P0} de bugs en moins.", true);
                    Finish(launched);
                }, "gold"),
                Ui.Button("Lancer sans jouer", () => Finish(campaign.LaunchWith(productId, 0))),
                Ui.Button("Déléguer", () => Finish(campaign.DelegateLaunch(productId)), "", enabled: canDelegate, tip: canDelegate ? "Un employé lance à ta place." : why),
                Ui.Button("Annuler", screen.CloseModal)));
        screen.ShowModal(content, width: 620);
    }

    /// <summary>
    /// Publier la version préparée d'un projet en ligne : ce qu'elle contient, ce qu'elle change (attrait avant et après),
    /// un mini-jeu facultatif qui retire des bugs de la sortie, ou publier sans jouer.
    /// </summary>
    public static void ShowRelease(GameScreen screen, int productId)
    {
        var campaign = screen.Session.Campaign;
        var product = campaign.FindProduct(productId);
        if (product == null || !campaign.CanPublish(product).Ok)
            return;
        var challenge = campaign.ReleaseChallenge(productId);
        var body = Board(challenge, out var score);
        int version = product.Version + 1;

        void Finish(CommandResult result)
        {
            screen.CloseModal();
            screen.Toast(result.Ok ? $"{product.Name} {version} est en ligne !" : result.Message, result.Ok);
            screen.RefreshPanel();
        }

        double before = Core.products.Design.Appeal(product), after = Campaign.AppealWithStaged(product);
        double bugs = product.Staged.Sum(s => s.Bugs);
        var content = Ui.Column(10,
            Ui.Title($"Publier {product.Name} {version}"),
            Ui.Text(string.Join(" · ", product.Staged.Select(Campaign.ChangeName)), 14, Palette.Cloud),
            Ui.Row(12, Ui.Stat("Attrait", $"{before:P0} → {after:P0}", after > before ? Palette.Leaf : null, 170),
                Ui.Stat("Bugs apportés", $"≈ {bugs:0.#}", bugs > 3 ? Palette.Ember : null, 150)),
            Ui.Text(challenge.Rule, 15),
            body,
            Ui.Muted($"Bonus : jusqu'à {Campaign.LaunchBugReduction:P0} des bugs de la version en moins. Sans jouer, elle part telle quelle.", 13),
            Ui.Row(8,
                Ui.Button("Valider et publier", () =>
                {
                    double result = score();
                    var published = campaign.PublishRelease(productId, result);
                    if (published.Ok)
                        screen.Toast($"Mini-jeu : {result:P0} de réussite, {Campaign.LaunchBugReduction * result:P0} des bugs de la version en moins.", true);
                    Finish(published);
                }, "gold"),
                Ui.Button("Publier sans jouer", () => Finish(campaign.PublishRelease(productId, 0))),
                Ui.Button("Plus tard", screen.CloseModal)));
        screen.ShowModal(content, width: 640);
    }
}
