using System.Collections.Generic;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using UI.Iso;

namespace UI.Pixel;

/// <summary>
/// Un panneau habillé d'un composant 9-slice du HUD « Papier » du pack (assets/hud/components) : les coins restent nets,
/// les bords et le centre s'étirent, selon les marges « nine_slice » du manifeste. Un pixel du composant vaut deux
/// unités d'interface, quelle que soit la taille du panneau. Les états (survol, appui, désactivé) sont d'autres images.
/// </summary>
public class PixelPanel : Decorator, Avalonia.Rendering.ICustomHitTest
{
    /// <summary>Tout le cadre capte la souris : un clic sur un menu n'atteint jamais la scène derrière.</summary>
    public bool HitTest(Point point) => Skin != "none" && new Rect(Bounds.Size).Contains(point);

    /// <summary>Unités d'interface par pixel du composant (2 ; 1 pour les petites pièces comme les barres).</summary>
    public double Scale { get; set; } = 2;

    /// <summary>Nom de cadre du jeu → composant du pack et état.</summary>
    private static readonly Dictionary<string, (string Component, string? State)> Skins = new()
    {
        ["panel"] = ("hud_panel_main", null),
        ["inset"] = ("hud_panel_inset", null),
        ["button"] = ("hud_button_secondary", "normal"),
        ["button_hover"] = ("hud_button_secondary", "hover"),
        ["button_pressed"] = ("hud_button_secondary", "pressed"),
        ["button_disabled"] = ("hud_button_secondary", "disabled"),
        ["gold"] = ("hud_button_primary", "normal"),
        ["gold_hover"] = ("hud_button_primary", "hover"),
        ["gold_pressed"] = ("hud_button_primary", "pressed"),
        ["green"] = ("hud_button_primary", "normal"),
        ["green_hover"] = ("hud_button_primary", "hover"),
        ["green_pressed"] = ("hud_button_primary", "pressed"),
        ["red"] = ("hud_button_destructive", "normal"),
        ["red_hover"] = ("hud_button_destructive", "hover"),
        ["red_pressed"] = ("hud_button_destructive", "pressed"),
    };

    private static readonly Dictionary<string, (Bitmap Image, int[] Slice)> Cache = new();

    /// <summary>L'image et les marges 9-slice d'un cadre (chargées une fois).</summary>
    public static (Bitmap Image, int[] Slice) Frame(string skin)
    {
        if (Cache.TryGetValue(skin, out var frame))
            return frame;
        // « pack:term_frame » ou « pack:hud_tabs:active » : un composant du pack donné par son identifiant (et son état)
        (string component, string? state) = skin.StartsWith("pack:")
            ? skin[5..].Split(':') is [var id, var st0] ? (id, st0) : (skin[5..], null)
            : Skins.TryGetValue(skin, out var s) ? s : Skins["panel"];
        var asset = PackLibrary.Asset(component);
        string file = PackLibrary.StateFile(asset, state);
        // Un sprite sans marges (case, curseur de glissière) s'étire en entier
        int[] margins = asset.TryGetProperty("nine_slice", out var slice)
            ? [slice[0].GetInt32(), slice[1].GetInt32(), slice[2].GetInt32(), slice[3].GetInt32()]
            : [0, 0, 0, 0];
        return Cache[skin] = (PackLibrary.Png(file).ToBitmap(), margins);
    }

    public static readonly StyledProperty<string> SkinProperty =
        AvaloniaProperty.Register<PixelPanel, string>(nameof(Skin), "panel");

    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        Border.BackgroundProperty.AddOwner<PixelPanel>();

    public static readonly StyledProperty<IBrush?> BorderBrushProperty =
        Border.BorderBrushProperty.AddOwner<PixelPanel>();

    public static readonly StyledProperty<Thickness> BorderThicknessProperty =
        Border.BorderThicknessProperty.AddOwner<PixelPanel>();

    static PixelPanel()
    {
        AffectsRender<PixelPanel>(SkinProperty, BackgroundProperty, BorderBrushProperty, BorderThicknessProperty);
    }

    /// <summary>Nom du cadre (panel, inset, button, gold...), ou « none » pour ne rien dessiner.</summary>
    public string Skin
    {
        get => GetValue(SkinProperty);
        set => SetValue(SkinProperty, value);
    }

    /// <summary>Remplace la couleur de fond du cadre.</summary>
    public IBrush? Background
    {
        get => GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    /// <summary>Contour coloré par-dessus le cadre (rareté, sélection).</summary>
    public IBrush? BorderBrush
    {
        get => GetValue(BorderBrushProperty);
        set => SetValue(BorderBrushProperty, value);
    }

    public Thickness BorderThickness
    {
        get => GetValue(BorderThicknessProperty);
        set => SetValue(BorderThicknessProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        var size = Bounds.Size;
        if (Skin == "none")
            return;
        var (image, slice) = Frame(Skin);
        double unit = Scale;
        if (size.Width < (slice[0] + slice[2]) * unit || size.Height < (slice[1] + slice[3]) * unit)
            return;
        using (context.PushRenderOptions(new RenderOptions { BitmapInterpolationMode = BitmapInterpolationMode.None }))
            DrawNineSlice(context, image, slice, size, unit);

        // Le fond, à l'intérieur des bords
        if (Background != null)
            context.FillRectangle(Background, new Rect(slice[0] * unit, slice[1] * unit,
                size.Width - (slice[0] + slice[2]) * unit, size.Height - (slice[1] + slice[3]) * unit));

        double thickness = BorderThickness.Left;
        if (BorderBrush != null && thickness > 0)
        {
            var outline = new Rect(0, 0, size.Width, size.Height - unit).Deflate(thickness / 2);
            context.DrawRectangle(new Pen(BorderBrush, thickness), outline);
        }
    }

    private static void DrawNineSlice(DrawingContext context, Bitmap image, int[] slice, Size size, double unit)
    {
        double w = image.PixelSize.Width;
        double h = image.PixelSize.Height;
        int l = slice[0], t = slice[1], r = slice[2], b = slice[3];

        // Colonnes et rangées : bord, milieu étiré, bord
        double[] sourceX = [0, l, w - r, w];
        double[] sourceY = [0, t, h - b, h];
        double[] targetX = [0, l * unit, size.Width - r * unit, size.Width];
        double[] targetY = [0, t * unit, size.Height - b * unit, size.Height];

        for (int row = 0; row < 3; row++)
            for (int column = 0; column < 3; column++)
            {
                var source = new Rect(sourceX[column], sourceY[row], sourceX[column + 1] - sourceX[column], sourceY[row + 1] - sourceY[row]);
                var target = new Rect(targetX[column], targetY[row], targetX[column + 1] - targetX[column], targetY[row + 1] - targetY[row]);
                if (source.Width > 0 && source.Height > 0 && target.Width > 0 && target.Height > 0)
                    context.DrawImage(image, source, target);
            }
    }
}
