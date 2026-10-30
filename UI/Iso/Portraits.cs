using System.Collections.Generic;
using Avalonia.Media.Imaging;
using Core.team;

namespace UI.Iso;

/// <summary>Les portraits du HUD : la vue de face du personnage en couches, recadrée selon le manifeste, gardée en cache.</summary>
public static class Portraits
{
    private static readonly Dictionary<string, Bitmap> Cache = new();

    public static Bitmap Of(Appearance look)
    {
        string key = $"{look.Base}|{look.Hair}|{look.HairRamp}|{look.Skin}|{look.Outfit}|{look.OutfitTrim}";
        if (!Cache.TryGetValue(key, out var bitmap))
            Cache[key] = bitmap = RoomRenderer.Portrait(look).ToBitmap();
        return bitmap;
    }
}
