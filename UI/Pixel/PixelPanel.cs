using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace UI.Pixel;

/// <summary>
/// Un panneau habillé d'un cadre pixel en 9 parties (Kenney Pixel UI Pack recoloré) :
/// les coins restent nets, les bords et le centre s'étirent. Un pixel du cadre vaut
/// deux unités d'interface, quelle que soit la taille du panneau.
/// </summary>
public class PixelPanel : Decorator, Avalonia.Rendering.ICustomHitTest
{
    /// <summary>Tout le cadre capte la souris : un clic sur un menu n'atteint jamais la scène derrière.</summary>
    public bool HitTest(Point point) => Skin != "none" && new Rect(Bounds.Size).Contains(point);

    private const int Slice = 6;   // pixels de chaque bord de l'image source
    private const double Unit = 2; // unités d'interface par pixel du cadre

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
        if (Skin == "none" || size.Width < 2 * Slice * Unit || size.Height < 2 * Slice * Unit)
            return;

        var image = Atlas.Skin(Skin);
        using (context.PushRenderOptions(new RenderOptions { BitmapInterpolationMode = BitmapInterpolationMode.None }))
            DrawNineSlice(context, image, size);

        // Le fond sans le biseau ni l'ombre du bas
        if (Background != null)
            context.FillRectangle(Background, new Rect(3 * Unit, 3 * Unit, size.Width - 6 * Unit, size.Height - 8 * Unit));

        double thickness = BorderThickness.Left;
        if (BorderBrush != null && thickness > 0)
        {
            var outline = new Rect(0, 0, size.Width, size.Height - 2 * Unit).Deflate(thickness / 2);
            context.DrawRectangle(new Pen(BorderBrush, thickness), outline);
        }
    }

    private static void DrawNineSlice(DrawingContext context, Bitmap image, Size size)
    {
        double w = image.PixelSize.Width;
        double h = image.PixelSize.Height;
        double edge = Slice * Unit;

        // Colonnes et rangées : bord, milieu étiré, bord
        double[] sourceX = [0, Slice, w - Slice, w];
        double[] sourceY = [0, Slice, h - Slice, h];
        double[] targetX = [0, edge, size.Width - edge, size.Width];
        double[] targetY = [0, edge, size.Height - edge, size.Height];

        for (int row = 0; row < 3; row++)
        for (int column = 0; column < 3; column++)
        {
            var source = new Rect(sourceX[column], sourceY[row], sourceX[column + 1] - sourceX[column], sourceY[row + 1] - sourceY[row]);
            var target = new Rect(targetX[column], targetY[row], targetX[column + 1] - targetX[column], targetY[row + 1] - targetY[row]);
            context.DrawImage(image, source, target);
        }
    }
}
