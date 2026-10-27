using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using UI.Pixel;

namespace UI.Hud;

/// <summary>Courbe simple d'une série de valeurs (historique d'un produit), avec son maximum indiqué.</summary>
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

    public override void Render(DrawingContext context)
    {
        var size = Bounds.Size;
        context.FillRectangle(Palette.Ink, new Rect(size));
        double max = _values.Count == 0 ? 0 : _values.Max();
        var text = new FormattedText($"{_label} (max {max:N0})", System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            new Typeface((FontFamily)Application.Current!.FindResource("PixelFont")!), 12, Palette.Fog);
        context.DrawText(text, new Point(6, 4));
        if (_values.Count < 2 || max <= 0)
            return;
        double top = 22, bottom = size.Height - 6;
        var geometry = new StreamGeometry();
        using (var g = geometry.Open())
        {
            for (int i = 0; i < _values.Count; i++)
            {
                var p = new Point(6 + i * (size.Width - 12) / (_values.Count - 1), bottom - _values[i] / max * (bottom - top));
                if (i == 0)
                    g.BeginFigure(p, false);
                else
                    g.LineTo(p);
            }
            g.EndFigure(false);
        }
        context.DrawGeometry(null, new Pen(_color, 2), geometry);
    }
}
