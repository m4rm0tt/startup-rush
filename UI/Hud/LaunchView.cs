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
    public static void Show(GameScreen screen, int productId)
    {
        var campaign = screen.Session.Campaign;
        var product = campaign.FindProduct(productId);
        if (product == null)
            return;
        var challenge = campaign.Challenge(productId);
        var order = Enumerable.Range(0, challenge.Items.Count).ToList();
        var picked = new HashSet<int>();
        var body = Ui.Column(6);

        void Finish(CommandResult result)
        {
            screen.CloseModal();
            screen.Toast(result.Ok ? $"{product.Name} est sorti !" : result.Message, result.Ok);
            screen.RefreshPanel();
        }

        void Paint()
        {
            body.Children.Clear();
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

        var (canDelegate, why) = campaign.CanDelegateLaunch(product);
        var content = Ui.Column(10,
            Ui.Title($"Lancer {product.Name}"),
            Ui.Text(challenge.Rule, 15),
            body,
            Ui.Muted($"Bonus : jusqu'à {Campaign.LaunchBugReduction:P0} de bugs en moins à la sortie. Sans jouer, le lancement se fait au résultat de base.", 13),
            Ui.Row(8,
                Ui.Button("Valider et lancer", () =>
                {
                    // Déploiement : l'ordre choisi (indices des étapes présentées) ; recette : les tests cochés
                    double score = challenge.Score(challenge.Ordering ? order : picked.ToList());
                    var result = campaign.LaunchWith(productId, score);
                    if (result.Ok)
                        screen.Toast($"Mini-jeu : {score:P0} de réussite, {Campaign.LaunchBugReduction * score:P0} de bugs en moins.", true);
                    Finish(result);
                }, "gold"),
                Ui.Button("Lancer sans jouer", () => Finish(campaign.LaunchWith(productId, 0))),
                Ui.Button("Déléguer", () => Finish(campaign.DelegateLaunch(productId)), "", enabled: canDelegate, tip: canDelegate ? "Un employé lance à ta place." : why),
                Ui.Button("Annuler", screen.CloseModal)));
        screen.ShowModal(content, width: 620);
    }
}
