using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using UI.Iso;
using UI.Pixel;

namespace UI.Hud;

/// <summary>
/// Courbe d'une série de valeurs (historique d'un produit) dans le cadre de graphique du pack : cadre, grille, axe, pastille de
/// légende et point sur la dernière valeur, teintés de la couleur de la série. Le maximum est écrit en clair.
/// </summary>
public class LineChart : Control
{
    private readonly IReadOnlyList<double> _values;
    private readonly IBrush _color;
    private readonly string _label;

    public LineChart(IReadOnlyList<double> values, IBrush color, string label, double height = 90)
    {
        _values = values;
        _color = color;
        _label = label;
        Height = height;
        MinWidth = 200;
    }

    private string Hex => _color is ISolidColorBrush solid ? $"#{solid.Color.R:x2}{solid.Color.G:x2}{solid.Color.B:x2}" : "#357c82";

    public override void Render(DrawingContext context)
    {
        var size = Bounds.Size;
        using var _ = context.PushRenderOptions(new RenderOptions { BitmapInterpolationMode = BitmapInterpolationMode.None });
        var (frame, slice) = PixelPanel.Frame("pack:hud_chart_frame");
        PixelPanel.DrawNineSlice(context, frame, slice, size, 2);

        // Légende : pastille teintée, libellé et maximum
        var legend = PackLibrary.Icon("hud_chart_legend", Hex);
        context.DrawImage(legend, new Rect(0, 0, legend.PixelSize.Width, legend.PixelSize.Height), new Rect(10, 8, 12, 12));
        double max = _values.Count == 0 ? 0 : _values.Max();
        var text = UI.Pixel.PixelText.Format($"{_label} (max {max:N0})", new Typeface((FontFamily)Application.Current!.FindResource("PixelFont")!), 12, Palette.Fog);
        context.DrawText(text, new Point(28, 6));

        double left = 10, right = size.Width - 10, top = 26, bottom = size.Height - 12;
        // Grille (quarts) : motif en pointillés répété (jamais étiré) ; axe du bas étiré sur la largeur
        var grid = PackSkins.Sprite("hud_chart_grid_h");
        double step = grid.PixelSize.Width * 2;
        for (int k = 1; k <= 3; k++)
        {
            double y = Math.Round(top + (bottom - top) * k / 4);
            for (double x = left; x < right; x += step)
            {
                double w = Math.Min(step, right - x);
                context.DrawImage(grid, new Rect(0, 0, w / 2, grid.PixelSize.Height), new Rect(x, y, w, 2));
            }
        }
        var axis = PackSkins.Sprite("hud_chart_axis_h");
        context.DrawImage(axis, new Rect(0, 0, axis.PixelSize.Width, axis.PixelSize.Height), new Rect(left, bottom, right - left, 4));

        if (_values.Count < 2 || max <= 0)
            return;
        Point At(int i) => new(left + i * (right - left) / (_values.Count - 1), bottom - _values[i] / max * (bottom - top));
        var geometry = new StreamGeometry();
        using (var g = geometry.Open())
        {
            g.BeginFigure(At(0), false);
            for (int i = 1; i < _values.Count; i++)
                g.LineTo(At(i));
            g.EndFigure(false);
        }
        context.DrawGeometry(null, new Pen(_color, 2), geometry);
        // Point du pack sur la dernière valeur
        var point = PackLibrary.Icon("hud_chart_point", Hex);
        var last = At(_values.Count - 1);
        context.DrawImage(point, new Rect(0, 0, point.PixelSize.Width, point.PixelSize.Height), new Rect(last.X - 5, last.Y - 5, 10, 10));
    }
}
