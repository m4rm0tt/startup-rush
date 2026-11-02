using System;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace UI.Hud;

/// <summary>
/// Un bouton qui vient d'apparaître sous le curseur ne prend pas un appui arrivé dans le délai d'un double-clic.
/// Exemple du retour de test : un premier clic sur un objet ouvre sa fiche, dont le bouton « Déplacer » se trouvait sous
/// la souris ; le second clic du double-clic l'actionnait, et l'objet partait en déplacement sans que le joueur l'ait voulu.
/// Le délai part du moment où le bouton est affiché (Loaded) : un bouton gardé par la mise à jour sur place (UiPatch) n'est
/// pas concerné, seul un bouton nouveau l'est. Les clics par programme (tests, parcours de captures) ne passent pas par ici.
/// </summary>
public static class ClickGuard
{
    /// <summary>Délai pendant lequel un bouton tout juste affiché ignore la souris (borne haute du double-clic).</summary>
    public static readonly TimeSpan ArmDelay = TimeSpan.FromMilliseconds(350);

    private static readonly ConditionalWeakTable<Button, object> ShownAt = new();

    private static bool _installed;

    public static void Install()
    {
        if (_installed)
            return;
        _installed = true;
        Control.LoadedEvent.AddClassHandler<Button>((button, _) =>
        {
            ShownAt.AddOrUpdate(button, Environment.TickCount64);
        });
        InputElement.PointerPressedEvent.AddClassHandler<Button>((button, e) =>
        {
            if (Blocked(button))
                e.Handled = true;
        }, RoutingStrategies.Tunnel);
    }

    /// <summary>Ce bouton est-il encore trop récent pour prendre un appui ?</summary>
    public static bool Blocked(Button button) =>
        ShownAt.TryGetValue(button, out var at) && Environment.TickCount64 - (long)at < ArmDelay.TotalMilliseconds;
}
