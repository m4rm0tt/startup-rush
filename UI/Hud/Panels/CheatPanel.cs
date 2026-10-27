using Avalonia.Controls;
using Core.campaign;

namespace UI.Hud.Panels;

/// <summary>
/// Le menu de test (F12) : argent, points, déblocages, travail fini, embauche, temps, stock. Pour essayer vite une partie ;
/// la campagne est marquée « triche utilisée ».
/// </summary>
public class CheatPanel : GamePanel
{
    public override string Title => "Menu de test";
    public override string Context => Campaign.CheatsUsed ? "Triche utilisée dans cette campagne" : "F12 pour l'ouvrir ou le fermer";

    public override Control Build()
    {
        Control Group(string title, params (string Label, System.Func<CommandResult> Action)[] buttons)
        {
            var wrap = new WrapPanel();
            foreach (var (label, action) in buttons)
            {
                var button = Ui.Button(label, () => Run(action()), "tab");
                button.Margin = new Avalonia.Thickness(0, 0, 6, 6);
                wrap.Children.Add(button);
            }
            return Ui.Section(title, wrap);
        }

        return Ui.Column(8,
            Ui.Muted("Ces raccourcis marquent la campagne comme « triche utilisée ».", 13),
            Group("Argent et points",
                ("+10 000 CHF", () => Campaign.CheatMoney(10_000)),
                ("+100 000 CHF", () => Campaign.CheatMoney(100_000)),
                ("+1 000 000 CHF", () => Campaign.CheatMoney(1_000_000)),
                ("+5 points", () => Campaign.CheatPoints(5)),
                ("Payer toutes les dettes", Campaign.CheatClearDebts)),
            Group("Déblocages et travail",
                ("Tout débloquer (arbre et recherches)", Campaign.CheatUnlockAll),
                ("Finir tout le travail en file", Campaign.CheatFinishWork)),
            Group("Équipe",
                ("Embaucher et installer quelqu'un", Campaign.CheatHire),
                ("Énergie et moral à 100", Campaign.CheatRefresh)),
            Group("Projets et production",
                ("+1 000 utilisateurs", () => Campaign.CheatUsers(1_000)),
                ("+10 000 utilisateurs", () => Campaign.CheatUsers(10_000)),
                ("100 de chaque matière ici", Campaign.CheatStock)),
            Group("Temps",
                ("+1 jour", () => Campaign.CheatAdvance(1)),
                ("+7 jours", () => Campaign.CheatAdvance(7)),
                ("+30 jours", () => Campaign.CheatAdvance(30))),
            Ui.Muted($"Actuellement : {Campaign.Ledger.Cash:N0} CHF, {Campaign.DevPoints} point(s), {Campaign.People.Count} personne(s), " +
                     $"{Campaign.Unlocked.Count} nœud(s), {Campaign.ResearchDone.Count} recherche(s) faite(s).", 13));
    }
}
