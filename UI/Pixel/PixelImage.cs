using System;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace UI.Pixel;

/// <summary>Une image affichée agrandie sans lissage : chaque pixel reste un carré net.</summary>
public class PixelImage : Image
{
    protected override Type StyleKeyOverride => typeof(Image);

    public PixelImage()
    {
        RenderOptions.SetBitmapInterpolationMode(this, BitmapInterpolationMode.None);
    }
}
