using Avalonia.Input;

namespace UI.Screens;

/// <summary>Un écran du jeu (titre, chargement, partie...), animé par la boucle de la fenêtre et qui reçoit le clavier.</summary>
public interface IScreen
{
    /// <summary>Appelé à chaque image avec le temps réel écoulé.</summary>
    void Tick(double deltaTime);

    /// <summary>Renvoie true si la touche a été utilisée.</summary>
    bool HandleKey(Key key, KeyModifiers modifiers);

    /// <summary>Appelé quand l'écran est quitté (ou la fenêtre fermée).</summary>
    void Leave();
}
