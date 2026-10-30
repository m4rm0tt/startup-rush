using Avalonia;
using Avalonia.Headless;

[assembly: AvaloniaTestApplication(typeof(UiTests.TestApp))]

namespace UiTests;

/// <summary>L'application du jeu, sans fenêtre réelle (Avalonia.Headless) et avec le vrai rendu Skia pour les images.</summary>
public static class TestApp
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<UI.App>().UseSkia().UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}
