using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Core.campaign;
using Core.progress;
using UI.Pixel;

namespace UI.Hud.Panels;

/// <summary>
/// L'arbre de développement : cinq branches, des nœuds avec icône, nom court et état écrit (acquis, disponible, verrouillé).
/// Le détail d'un nœud s'affiche quand on le sélectionne. En dessous, les jalons de l'entreprise.
/// </summary>
public class TreePanel : GamePanel
{
    public override string Title => "Développement";
    public override string Context => $"{Campaign.DevPoints} point(s) à dépenser";
    public override bool Large => true;

    private string? _selected;

    public override Control Build()
    {
        var columns = new Grid { ColumnDefinitions = new ColumnDefinitions(string.Join(",", DevTree.Branches.Select(_ => "*"))) };
        for (int i = 0; i < DevTree.Branches.Length; i++)
        {
            string branch = DevTree.Branches[i];
            var column = Ui.Column(6, Ui.Text(branch.ToUpperInvariant(), 13, Palette.Gold, bold: true));
            foreach (var node in DevTree.Nodes.Where(n => n.Branch == branch))
                column.Children.Add(NodeButton(node));
            column.Margin = new Thickness(0, 0, 8, 0);
            Grid.SetColumn(column, i);
            columns.Children.Add(column);
        }

        var milestones = new WrapPanel();
        foreach (var (id, name, description) in Campaign.MilestoneDefs)
        {
            bool done = Campaign.Milestones.ContainsKey(id);
            var tile = Ui.Row(6, Ui.Icon(done ? "trophy" : "lock", 18), Ui.Column(0, Ui.Text(name, 13, done ? Palette.Gold : Palette.Fog, bold: done, wrap: false), Ui.Muted(done ? "Atteint" : description, 11)));
            tile.Margin = new Thickness(0, 0, 16, 6);
            tile.Width = 250;
            milestones.Children.Add(tile);
        }

        return Ui.Column(10,
            Hint("tree", "Les points viennent de vrais succès : un projet qui trouve son public, des paliers d'utilisateurs, un premier mois bénéficiaire."),
            Ui.Row(10, Ui.Number($"{Campaign.DevPoints}", 32, Palette.Gold), Ui.Muted("point(s) de développement", 14),
                Campaign.IsUnlocked("hardware") ? Ui.Button($"Recherche ({Campaign.Research.Count} en cours)", () => Open(new ResearchPanel()), "gold") : null),
            columns,
            Selected(),
            Ui.Section("Jalons", milestones));
    }

    private Control NodeButton(DevNode node)
    {
        bool owned = Campaign.IsUnlocked(node.Id);
        var (can, _) = Campaign.CanUnlock(node);
        string state = owned ? "✓ Acquis" : can ? $"Disponible · {node.Cost} pt" : node.Requires != null && !Campaign.IsUnlocked(node.Requires) ? "Verrouillé" : $"{node.Cost} pt";
        var content = Ui.Row(8, Ui.Icon(owned || can ? node.Icon : "lock", 26),
            Ui.Column(0, Ui.Text(node.Name, 14, owned ? Palette.Leaf : can ? Palette.Snow : Palette.Fog, bold: true), Ui.Text(state, 12, owned ? Palette.Leaf : can ? Palette.Lemon : Palette.Fog, wrap: false)));
        var button = new Button { Content = content, HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Left, Padding = new Thickness(8, 4, 8, 8) };
        button.Classes.Add(_selected == node.Id ? "selected" : "tab");
        button.Classes.Add("tab");
        button.Click += (_, _) =>
        {
            _selected = node.Id;
            Refresh();
        };
        return button;
    }

    private Control Selected()
    {
        if (_selected == null)
            return Ui.Muted("Choisis un nœud pour voir ce qu'il débloque.", 13);
        var node = DevTree.Node(_selected);
        bool owned = Campaign.IsUnlocked(node.Id);
        var (can, reason) = Campaign.CanUnlock(node);
        return Ui.Card(Ui.Row(12, Ui.Icon(node.Icon, 36),
            Ui.Column(4, Ui.Title(node.Name, 18), Ui.Text(node.Effect, 15),
                owned ? Ui.Text("Acquis.", 14, Palette.Leaf) : Ui.Row(8,
                    Ui.Button($"Débloquer ({node.Cost} pt)", () => Run(Campaign.Unlock(node.Id)), "gold", can),
                    can ? null : Ui.Muted(reason, 13)))), Palette.Gold);
    }
}
