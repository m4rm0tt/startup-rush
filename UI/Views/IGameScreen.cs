using Avalonia.Input;

namespace UI.Views;

/// <summary>Un écran du jeu, animé par la boucle de la fenêtre et qui reçoit le clavier.</summary>
public interface IGameScreen
{
    /// <summary>Appelé à chaque image, après la mise à jour du jeu.</summary>
    void Tick(double deltaTime);

    /// <summary>Renvoie true si la touche a été utilisée.</summary>
    bool HandleKey(Key key);
}
