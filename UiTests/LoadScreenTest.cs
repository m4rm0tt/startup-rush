using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Core.campaign;
using Core.persistence;
using UI;

namespace UiTests;

/// <summary>Lot 2 des sauvegardes : chaque partie a son nom et sa date dans la liste, et se supprime après confirmation.</summary>
public class LoadScreenTest
{
    private static Button ButtonWith(Window window, string text) =>
        window.GetVisualDescendants().OfType<Button>().First(b => b.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == text));

    private static List<string> Texts(Window window) =>
        window.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text ?? "").Where(t => t != "").ToList();

    [AvaloniaTest]
    public void APartyIsDeletedOnlyAfterConfirmation()
    {
        string root = Path.Combine(Path.GetTempPath(), "startuprush_uitests", Guid.NewGuid().ToString("N"), "campaigns");
        var store = new SaveStore(root);
        var a = Campaign.New("Partie A", "Alex", "Studio Léman", FounderProfile.Generalist, 1);
        var b = Campaign.New("Partie B", "Sam", "Atelier Rhône", FounderProfile.Generalist, 2);
        store.Save(a, SaveKind.Manual, "Début");
        store.Save(b, SaveKind.Auto);
        var window = new MainWindow(store, new Settings(), Path.Combine(root, "settings.json")) { Width = 1600, Height = 900 };
        window.Show();
        window.ShowLoad();
        Dispatcher.UIThread.RunJobs();
        Assert.That(Texts(window), Has.Some.Contains("Partie A — Studio Léman").And.Some.Contains("Partie B — Atelier Rhône"));

        Thread.Sleep(400);
        window.GetVisualDescendants().OfType<Button>().Where(x => x.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == "Supprimer la partie")).Last()
            .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
        Assert.That(Texts(window), Has.Some.Contains("Supprimer cette partie ?"));
        Assert.That(store.List(), Has.Count.EqualTo(2), "rien n'est supprimé avant la confirmation");

        Thread.Sleep(400);
        ButtonWith(window, "Annuler").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
        Assert.That(store.List(), Has.Count.EqualTo(2));

        Thread.Sleep(400);
        window.GetVisualDescendants().OfType<Button>().Where(x => x.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == "Supprimer la partie")).Last()
            .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
        Thread.Sleep(400);
        ButtonWith(window, "Supprimer").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
        Assert.That(store.List(), Has.Count.EqualTo(1));
        Assert.That(Texts(window), Has.None.Contains(store.List()[0].Summary!.CampaignId == a.Id ? "Partie B —" : "Partie A —"));
        window.Close();
    }
}
