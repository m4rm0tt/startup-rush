// Les tests voient les nombres comme le joueur : format du jeu, pas celui de la machine qui les lance
[NUnit.Framework.SetUpFixture]
public class GameCultureSetup
{
    [NUnit.Framework.OneTimeSetUp]
    public void Install() => Core.GameCulture.Install();
}
