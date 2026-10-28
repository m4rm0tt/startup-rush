using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Core.campaign;
using UI.Pixel;
using UI.Screens;

namespace UI.Hud;

/// <summary>
/// Les mini-jeux du lot 6, sur le même modèle que le lancement (LaunchView) : une règle en une phrase, quelques secondes, rien
/// de perdu à ne pas jouer. L'évaluation d'un modèle d'IA associe des échantillons à leur résultat ; la mise en route d'une
/// installation quantique stabilise des signaux couplés.
/// </summary>
public static class ChallengeView
{
    /// <summary>Évaluation d'une version entraînée : jusqu'à 0,08 de qualité retrouvée, jamais plus. Se joue une fois par version.</summary>
    public static void ShowEvaluation(GameScreen screen, int modelId)
    {
        var campaign = screen.Session.Campaign;
        var model = campaign.FindAiModel(modelId);
        if (model?.Candidate is not { Evaluated: false })
            return;
        var challenge = campaign.EvaluationChallenge(modelId);
        var picked = Enumerable.Repeat(-1, challenge.Items.Count).ToList();
        var body = Ui.Column(6);

        void Finish(CommandResult result, string? message = null)
        {
            screen.CloseModal();
            screen.Toast(message ?? result.Message, result.Ok);
            screen.RefreshPanel();
        }

        void Paint()
        {
            body.Children.Clear();
            for (int i = 0; i < challenge.Items.Count; i++)
            {
                int index = i;
                string label = picked[i] < 0 ? "?" : challenge.Options[picked[i]];
                var button = Ui.Button(label, () =>
                {
                    picked[index] = (picked[index] + 1) % challenge.Options.Count;
                    Paint();
                }, picked[i] < 0 ? "tab" : "tab selected");
                button.MinWidth = 110;
                body.Children.Add(Ui.Row(8, button, Ui.Text(challenge.Items[i], 15, Palette.Cloud)));
            }
        }
        Paint();

        var (canDelegate, why) = campaign.CanDelegateEvaluation(model);
        var content = Ui.Column(10,
            Ui.Title($"Évaluer {model.Name}, version {model.Candidate!.Number}"),
            Ui.Text(challenge.Rule, 15),
            Ui.Muted("Un clic sur le bouton change le résultat. ", 13),
            body,
            Ui.Muted($"Bonus : jusqu'à {AiCatalogHidden:0.00} de qualité retrouvée (les défauts cachés), pas plus. Sans jouer, tu peux publier tout de suite : la version garde ses défauts cachés.", 13),
            Ui.Row(8,
                Ui.Button("Valider l'évaluation", () =>
                {
                    double score = challenge.Score(picked);
                    var result = campaign.EvaluateAi(modelId, score);
                    Finish(result, result.Ok ? $"Évaluation : {score:P0} d'échantillons bien associés." : result.Message);
                }, "gold"),
                Ui.Button("Déléguer", () => Finish(campaign.DelegateEvaluation(modelId)), "", enabled: canDelegate, tip: canDelegate ? "Un employé évalue à ta place." : why),
                Ui.Button("Plus tard", screen.CloseModal)));
        screen.ShowModal(content, width: 640);
    }

    private static double AiCatalogHidden => Core.ai.AiCatalog.HiddenFlaws;

    /// <summary>Mise en route d'une installation quantique : ramener quatre signaux sur le motif cible (le total est conservé).</summary>
    public static void ShowQuantum(GameScreen screen, int siteId)
    {
        var campaign = screen.Session.Campaign;
        var state = campaign.QuantumSites.FirstOrDefault(q => q.SiteId == siteId);
        if (state is not { WarmupLeft: > 0, Stabilized: false })
            return;
        var challenge = campaign.QuantumChallenge(siteId);
        var values = challenge.Start.ToList();
        var body = Ui.Column(6);

        // Un « + » ajoute 1 au signal et retire 1 au suivant (et inversement) : on reste entre 0 et 9
        bool Move(int k, int direction)
        {
            int next = (k + 1) % values.Count;
            if (values[k] + direction is < 0 or > 9 || values[next] - direction is < 0 or > 9)
                return false;
            values[k] += direction;
            values[next] -= direction;
            return true;
        }

        void Paint()
        {
            body.Children.Clear();
            for (int k = 0; k < values.Count; k++)
            {
                int index = k;
                var track = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 1 };
                for (int v = 0; v <= 9; v++)
                    track.Children.Add(new Border
                    {
                        Width = 16, Height = 16,
                        Background = v == values[k] ? Palette.Ink : v == challenge.Answer[k] ? Palette.Leaf : Palette.Slate,
                    });
                body.Children.Add(Ui.Row(6, Ui.Text(challenge.Items[k], 14, Palette.Cloud, wrap: false),
                    Ui.Button("−", () => { Move(index, -1); Paint(); }, "tab"),
                    track,
                    Ui.Button("+", () => { Move(index, 1); Paint(); }, "tab"),
                    Ui.Text($"{values[k]} → {challenge.Answer[k]}", 14, values[k] == challenge.Answer[k] ? Palette.Leaf : Palette.Fog, wrap: false)));
            }
        }
        Paint();

        var content = Ui.Column(10,
            Ui.Title("Stabiliser les signaux"),
            Ui.Text(challenge.Rule, 15),
            body,
            Ui.Muted($"Bonus : jusqu'à moitié moins de jours de mise en route ({state.WarmupLeft} restants). Sans jouer, la mise en route suit son cours normal.", 13),
            Ui.Row(8,
                Ui.Button("Valider", () =>
                {
                    double score = challenge.Score(values);
                    var result = campaign.StabilizeQuantum(siteId, score);
                    screen.CloseModal();
                    screen.Toast(result.Message, result.Ok);
                    screen.RefreshPanel();
                }, "gold"),
                Ui.Button("Plus tard", screen.CloseModal)));
        screen.ShowModal(content, width: 640);
    }
}
