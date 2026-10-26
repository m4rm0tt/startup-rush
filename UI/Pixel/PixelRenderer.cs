using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace UI.Pixel;

/// <summary>
/// Rend une scène vectorielle en vrai pixel art : dessin en basse résolution,
/// puis chaque pixel est ramené à la palette avec un tramage de Bayer (dégradés en damier, façon rétro).
/// L'image obtenue est affichée agrandie sans lissage.
/// </summary>
public sealed class PixelRenderer
{
    // Matrice de Bayer 4×4 : décale légèrement chaque pixel pour que les dégradés deviennent des motifs
    private static readonly int[] Bayer = [0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5];
    private const double DitherStrength = 16;

    private readonly int _width;
    private readonly int _height;
    private readonly Canvas _root;
    private readonly Control _scene;
    private readonly RenderTargetBitmap _target;
    private readonly int[] _pixels;

    public WriteableBitmap Output { get; }

    /// <param name="scene">Le dessin, en unités de scène.</param>
    /// <param name="logicalSize">Taille du dessin en unités de scène.</param>
    /// <param name="pixelScale">Combien d'unités de scène pour un pixel (2 = deux fois moins de pixels).</param>
    public PixelRenderer(Control scene, Size logicalSize, double pixelScale)
    {
        _width = (int)(logicalSize.Width / pixelScale);
        _height = (int)(logicalSize.Height / pixelScale);

        // La scène est réduite par une transformation dans un conteneur à la taille finale en pixels
        scene.RenderTransform = new ScaleTransform(1 / pixelScale, 1 / pixelScale);
        scene.RenderTransformOrigin = RelativePoint.TopLeft;
        _scene = scene;
        _root = new Canvas { Width = _width, Height = _height };
        _root.Children.Add(scene);
        RenderOptions.SetEdgeMode(_root, EdgeMode.Aliased);

        var dpi = new Vector(96, 96);
        _target = new RenderTargetBitmap(new PixelSize(_width, _height), dpi);
        Output = new WriteableBitmap(new PixelSize(_width, _height), dpi, PixelFormat.Bgra8888, AlphaFormat.Premul);
        _pixels = new int[_width * _height];
    }

    public void Render()
    {
        // Hors de la fenêtre, personne ne relance la mise en page quand le décor change : on le force
        _scene.InvalidateMeasure();
        _root.InvalidateMeasure();
        var size = new Size(_width, _height);
        _root.Measure(size);
        _root.Arrange(new Rect(size));
        _target.Render(_root);

        using var frame = Output.Lock();
        _target.CopyPixels(frame);
        PixelArt.Read(frame, _pixels, _width, _height);
        Quantize();
        PixelArt.WriteTo(frame, _pixels, _width, _height);
    }

    private void Quantize()
    {
        for (int y = 0; y < _height; y++)
        {
            int row = y * _width;
            int bayerRow = (y & 3) * 4;
            for (int x = 0; x < _width; x++)
            {
                int c = _pixels[row + x];
                int offset = (int)((Bayer[bayerRow + (x & 3)] / 16.0 - 0.47) * DitherStrength);
                int r = Math.Clamp((c >> 16 & 0xff) + offset, 0, 255);
                int g = Math.Clamp((c >> 8 & 0xff) + offset, 0, 255);
                int b = Math.Clamp((c & 0xff) + offset, 0, 255);
                _pixels[row + x] = unchecked((int)(0xff000000 | Palette.Nearest(r, g, b)));
            }
        }
    }
}
