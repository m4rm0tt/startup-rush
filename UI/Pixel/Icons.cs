using System.Collections.Generic;
using Avalonia.Media.Imaging;

namespace UI.Pixel;

/// <summary>
/// Les icônes 12×12 du jeu (atouts, événements, HUD), dessinées en ASCII art.
/// Lettres : voir PixelArt.Inks (K = contour, Y = or, R = rouge, G = vert, B = bleu, w = blanc...).
/// </summary>
public static class Icons
{
    private static readonly Dictionary<string, string[]> Art = new()
    {
        ["coin"] =
        [
            "....KKKK....", "..KKYYYYKK..", ".KYYyyyyYYK.", ".KYyYKKKYYK.", "KYYyYKYYYYYK", "KYYyYKKKYYYK",
            "KYYyYYYKYYYK", "KYYyYKKKYYYK", ".KYyYYYYYYK.", ".KYYYYYYYYK.", "..KKYYYYKK..", "....KKKK....",
        ],
        ["money"] =
        [
            "............", "KKKKKKKKKKKK", "KGGGGGGGGGGK", "KGDGGDDGGDGK", "KGGGDGGDGGGK", "KGGDGDDGDGGK",
            "KGGGDGGDGGGK", "KGDGGDDGGDGK", "KGGGGGGGGGGK", "KKKKKKKKKKKK",
        ],
        ["coffee"] =
        [
            "...c..c.....", "..c..c......", "...c..c.....", "............", "KKKKKKKK....", "KNNNNNNKKK..",
            "KwwwwwwK.K..", "KwwwwwwK.K..", "KwwwwwwKKK..", "KwwwwwwK....", ".KKKKKK.....",
        ],
        ["hand"] =
        [
            "....KK......", "...KSSK.....", "...KSSK.....", "...KSSKKK...", "...KSSKSSKK.", ".KKKSSKSSKSK",
            "KSSKSSSSSSSK", "KSSSSSSSSSSK", ".KSSSSSSSSK.", "..KSSSSSSSK.", "...KSSSSSK..", "....KKKKK...",
        ],
        ["link"] =
        [
            "KKKKKKKKKKKK", "KBBBBBBBBBBK", "KBwwBBBBBBBK", "KBBBBBBBBBBK", "KBwwBwwwwBBK", "KBwwBwwBwwBK",
            "KBwwBwwBwwBK", "KBwwBwwBwwBK", "KBwwBwwBwwBK", "KBBBBBBBBBBK", "KKKKKKKKKKKK",
        ],
        ["home"] =
        [
            ".....KK.....", "....KRRK....", "...KRRRRK...", "..KRRRRRRK..", ".KRRRRRRRRK.", "KKKKKKKKKKKK",
            ".KTTTTTTTTK.", ".KTBBTTNNTK.", ".KTBBTTNNTK.", ".KTTTTTNNTK.", ".KTTTTTNNTK.", ".KKKKKKKKKK.",
        ],
        ["hourglass"] =
        [
            "KKKKKKKKKK", ".KTTTTTTK.", ".KyyyyyyK.", "..KyyyyK..", "...KyyK...", "....KK....",
            "...KTTK...", "..KTyyTK..", ".KTyyyyTK.", ".KyyyyyyK.", "KKKKKKKKKK",
        ],
        ["clock"] =
        [
            "...KKKKKK...", "..KwwwwwwK..", ".KwwwKwwwwK.", "KwwwwKwwwwwK", "KwwwwKwwwwwK", "KwwwwKKKKwwK",
            "KwwwwwwwwwwK", "KwwwwwwwwwwK", ".KwwwwwwwwK.", "..KwwwwwwK..", "...KKKKKK...",
        ],
        ["star"] =
        [
            ".....KK.....", ".....KyK....", "....KyYK....", "KKKKKyYKKKKK", "KyyyyYYYYYYK", ".KyYYYYYYYK.",
            "..KYYYYYYK..", "..KYYYYYYK..", ".KYYYKKYYYK.", ".KYYK..KYYK.", "KYKK....KKYK", "KK........KK",
        ],
        ["piggy"] =
        [
            "......KKK...", "....KKYKK...", "..KKFFFFFKK.", ".KFFFFFFFFFK", "KFFKFFFFFFFK", "KFFFFFFFFFFK",
            "KFFFFFFFFFKK", ".KFFFFFFFFK.", "..KFKKKKFK..", "..KK....KK..",
        ],
        ["check"] =
        [
            "..........KK", ".........KGK", "........KGGK", ".......KGGK.", "KK....KGGK..", "KGK..KGGK...",
            "KGGKKGGK....", ".KGGGGK.....", "..KGGK......", "...KK.......",
        ],
        ["plus"] =
        [
            "....KKKK....", "....KGGK....", "....KGGK....", "KKKKKGGKKKKK", "KGGGGGGGGGGK", "KGGGGGGGGGGK",
            "KKKKKGGKKKKK", "....KGGK....", "....KGGK....", "....KKKK....",
        ],
        ["bolt"] =
        [
            "......KKKK..", ".....KyyK...", "....KyyK....", "...KyyK.....", "..KyyyKKKK..", ".KyyyyyyyK..",
            "..KKKKyyK...", ".....KyK....", "....KyK.....", "...KyK......", "...KK.......",
        ],
        ["gem"] =
        [
            "..KKKKKKKK..", ".KCCwCCCCCK.", "KCwCCCCCCBBK", "KKKKKKKKKKKK", ".KCCCCCCBBK.", "..KCCCCBBK..",
            "...KCCBBK...", "....KCBK....", ".....KK.....",
        ],
        ["leaf"] =
        [
            "........KKKK", "......KKGGGK", "....KKGGGGGK", "...KGGGGGGDK", "..KGGGGGGDK.", ".KGGGGGDDK..",
            ".KGGGDDKK...", "KGGDDKK.....", "KDKKK.......", "KK..........",
        ],
        ["fire"] =
        [
            ".....K......", "....KRK.....", "...KRRK..K..", "..KRROK.KRK.", ".KRROOKKRRK.", ".KROOyORRRK.",
            "KRROyyyOORK.", "KROyyyyyORK.", "KROyyyyyORK.", ".KROyyyORK..", "..KKKKKKK...",
        ],
        ["tag"] =
        [
            "......KKKKKK", ".....KOOOOOK", "....KOOOOKOK", "...KOOOOOOOK", "..KOOOOOOOK.", ".KOOOOOOOK..",
            "KOOOOOOOK...", ".KOOOOOK....", "..KOOOK.....", "...KKK......",
        ],
        ["shield"] =
        [
            "KKKKKKKKKKKK", "KBBBBBBwwwwK", "KBBBBBBwwwwK", "KBBBBBBwwwwK", "KBBBBBBwwwwK", "KwwwwwwBBBBK",
            ".KwwwwwBBBK.", ".KwwwwwBBBK.", "..KwwwwBBK..", "...KwwwBK...", "....KKKK....",
        ],
        ["rocket"] =
        [
            ".........KKK", "........KwwK", ".......KwRwK", "......KwwwK.", "..KK.KwwwK..", ".KRRKwwwK...",
            "..KRwwwK....", "...KwwKRK...", "..KOKKRRK...", ".KyOK.KK....", "KyOK........", "KKK.........",
        ],
        ["people"] =
        [
            "...KKK......", "..KSSSK.KKK.", "..KSSSKKSSSK", "...KKK.KSSSK", "..KBBBK.KKK.", ".KBBBBBKGGGK",
            ".KBBBBBKGGGK", ".KBBBBBKGGGK", ".KBBBBBKGGGK", "..KKKKK.KKK.",
        ],
        ["cat"] =
        [
            ".K......K...", "KOK....KOK..", "KOOKKKKOOK..", "KOOOOOOOOK..", "KOKOOOOKOK..", "KOOOOFOOOK..",
            "KOOOOOOOOK.K", ".KOOOOOOK.KOK", ".KOOOOOOKKOK.", ".KOOOOOOOOK..", "..KOKKKKOK...", "..KK....KK...",
        ],
        ["chart"] =
        [
            "K...........", "K.........G.", "K........GG.", "K....G..GK..", "K...GGKGK...", "K..GK.GK....",
            "K.GK........", "KGK.........", "KK..........", "KKKKKKKKKKKK",
        ],
        ["unicorn"] =
        [
            "........y...", ".......Ky...", "......KwK...", "..KKKKwwK...", ".KwwwwwwwK..", "KPwwwwKwwK..",
            "KPPwwwwwwwK.", ".KPwwwwwwwwK", "..KwwwwwKKK.", "..KwwwwwK...", "..KwwKwwK...", "..KK.KKK....",
        ],
        ["crown"] =
        [
            "............", "K....K....K.", "KK..KYK..KK.", "KYK.KYK.KYK.", "KYYKYYYKYYK.", "KYYYYRYYYYK.",
            "KYYYYYYYYYK.", "KYYYYYYYYYK.", "KKKKKKKKKKK.",
        ],
        ["skull"] =
        [
            "..KKKKKKKK..", ".KwwwwwwwwK.", "KwwwwwwwwwwK", "KwKKKwwKKKwK", "KwKKKwwKKKwK", "KwwwwKKwwwwK",
            ".KwwwwwwwwK.", "..KwKwKwKK..", "..KwwwwwwK..", "...KKKKKK...",
        ],
        ["mail"] =
        [
            "............", "KKKKKKKKKKKK", "KwKwwwwwwKwK", "KwwKwwwwKwwK", "KwwwKwwKwwwK", "KwwwwKKwwwwK",
            "KwwwwwwwwwwK", "KwwwwwwwwwwK", "KKKKKKKKKKKK",
        ],
        ["trophy"] =
        [
            "KKKKKKKKKKKK", "KYKYYYYYYKYK", "KYKYYYYYYKYK", ".KKYYYYYYKK.", "...KYYYYK...", "....KYYK....",
            ".....KK.....", "....KYYK....", "...KNNNNK...", "...KKKKKK...",
        ],
        ["heart"] =
        [
            ".KKK...KKK..", "KRRRK.KRRRK.", "KRwRRKRRRRK.", "KRRRRRRRRRK.", "KRRRRRRRRRK.", ".KRRRRRRRK..",
            "..KRRRRRK...", "...KRRRK....", "....KRK.....", ".....K......",
        ],
        ["megaphone"] =
        [
            "........KK..", "......KKwK..", "....KKwwwK..", "KKKKwwwwwK..", "KRRKwwwwwKw.", "KRRKwwwwwK.w",
            "KKKKwwwwwKw.", "..KKKKwwwK..", "..KK..KKwK..", "..KK....KK..",
        ],
        ["dice"] =
        [
            "KKKKKKKKKK", "KwwwwwwwwK", "KwKwwwwwwK", "KwwwwwwwwK", "KwwwwKwwwK", "KwwwwwwwwK",
            "KwwwwwwKwK", "KwwwwwwwwK", "KKKKKKKKKK",
        ],
        ["suit"] =
        [
            "...KKKKK....", "..KSSSSSK...", "..KSSSSSK...", "...KSSSK....", ".KKKwRwKKK..", "KkkkwRwkkkK.",
            "KkkkkRkkkkK.", "KkkkkRkkkkK.", "KkkkkkkkkkK.", "KKKKKKKKKKK.",
        ],
        ["lock"] =
        [
            "...KKKKK....", "..KgKKKgK...", "..KgK.KgK...", "..KgK.KgK...", "KKKKKKKKKKK.", "KYYYYYYYYYK.",
            "KYYYYKYYYYK.", "KYYYYKYYYYK.", "KYYYYYYYYYK.", "KKKKKKKKKKK.",
        ],
        ["calendar"] =
        [
            "..K....K....", "KKKKKKKKKKK.", "KRRRRRRRRRK.", "KKKKKKKKKKK.", "KwwwwwwwwwK.", "KwKwKwKwKwK.",
            "KwwwwwwwwwK.", "KwKwKwKRKwK.", "KwwwwwwwwwK.", "KKKKKKKKKKK.",
        ],
        ["bill"] =
        [
            "KKKKKKKKK...", "KwwwwwwwKK..", "KwKKKKwwwK..", "KwwwwwwwwK..", "KwKKKKKKwK..", "KwwwwwwwwK..",
            "KwKKKKKKwK..", "KwwwwwwwwK..", "KwwwwKRRwK..", "KKKKKKKKKK..",
        ],
        ["gold"] =
        [
            "............", "............", "...KKKKKKK..", "..KyyyyyyYK.", ".KyyyyyyYYK.", "KKKKKKKKYYK.",
            "KYYYYYYKYK..", "KYYYYYYKK...", "KKKKKKKK....",
        ],
        ["warning"] =
        [
            ".....KK.....", "....KYYK....", "....KYYK....", "...KYKKYK...", "...KYKKYK...", "..KYYKKYYK..",
            "..KYYYYYYK..", ".KYYYKKYYYK.", "KYYYYYYYYYYK", "KKKKKKKKKKKK",
        ],
        ["briefcase"] =
        [
            "....KKKK....", "....K..K....", "KKKKKKKKKKKK", "KNNNNNNNNNNK", "KNNNNNNNNNNK", "KKKKKYYKKKKK",
            "KNNNNYYNNNNK", "KNNNNNNNNNNK", "KKKKKKKKKKKK",
        ],
        ["computer"] =
        [
            "KKKKKKKKKKKK", "KbbbbbbbbbbK", "KbCCbbbbbbbK", "KbbbCCCbbbbK", "KbbbbbbbbbbK", "KbCCCCbbbbbK",
            "KKKKKKKKKKKK", ".....KK.....", "...KKKKKK...",
        ],
        ["screen"] =
        [
            "KKKKK.KKKKK.", "KbbbK.KbbbK.", "KbCbK.KbCbK.", "KbbbK.KbbbK.", "KKKKK.KKKKK.", "..K.....K...",
            ".KKK...KKK..",
        ],
        ["chair"] =
        [
            "..KKKKKK....", "..KRRRRK....", "..KRRRRK....", "..KRRRRK....", "..KRRRRK....", ".KKKKKKKK...",
            ".KkkkkkkK...", "....KK......", "....KK......", "..KKKKKK....",
        ],
        ["wifi"] =
        [
            "..KKKKKKKK..", ".K........K.", "K..KKKKKK..K", "..K......K..", "....KKKK....", "...K....K...",
            ".....KK.....", ".....KK.....",
        ],
        ["building"] =
        [
            "..KKKKKKKK..", "..KggggggK..", "..KgCgCgCK..", "..KggggggK..", "..KgCgCgCK..", "..KggggggK..",
            "..KgCgCgCK..", "..KggggggK..", "..KggKKggK..", "KKKKKKKKKKKK",
        ],
        ["user"] =
        [
            "...KKKK.....", "..KSSSSK....", "..KSSSSK....", "...KSSK.....", ".KKKKKKKK...", "KBBBBBBBBK..",
            "KBBBBBBBBK..", "KBBBBBBBBK..",
        ],
    };

    private static readonly Dictionary<string, WriteableBitmap> Cache = new();

    /// <summary>L'icône demandée, ou une étoile si la clé est inconnue.</summary>
    /// <summary>Les noms d'icônes du jeu → icônes 16 px du pack (HUD « Papier »), teintées à l'encre ; les autres restent dessinées ici.</summary>
    private static readonly Dictionary<string, (string Id, string Tint)> PackIcons = new()
    {
        ["bill"] = ("icon_invoice", "#2b2128"), ["bolt"] = ("icon_energy", "#2b2128"), ["brief"] = ("icon_project", "#2b2128"),
        ["briefcase"] = ("icon_project", "#2b2128"), ["building"] = ("icon_office", "#2b2128"), ["calendar"] = ("icon_calendar", "#2b2128"),
        ["chair"] = ("icon_furnish", "#2b2128"), ["chart"] = ("icon_chart_line", "#2b2128"), ["check"] = ("icon_success", "#437d40"),
        ["clock"] = ("icon_time", "#2b2128"), ["coin"] = ("icon_cash", "#2b2128"), ["computer"] = ("icon_pc", "#2b2128"),
        ["crown"] = ("icon_trophy", "#2b2128"), ["dice"] = ("icon_comp_game_menu", "#2b2128"), ["fire"] = ("icon_risk", "#c05843"),
        ["gem"] = ("icon_quality", "#2b2128"), ["gold"] = ("icon_cash", "#c9922e"), ["hand"] = ("icon_assignment", "#2b2128"),
        ["heart"] = ("icon_retention", "#c05843"), ["home"] = ("icon_home", "#2b2128"), ["hourglass"] = ("icon_time", "#2b2128"),
        ["leaf"] = ("icon_idea", "#437d40"), ["link"] = ("icon_link", "#2b2128"), ["lock"] = ("icon_lock", "#6f5f5d"),
        ["mail"] = ("icon_mail", "#2b2128"), ["megaphone"] = ("icon_marketing", "#2b2128"), ["money"] = ("icon_cash", "#2b2128"),
        ["people"] = ("icon_users", "#2b2128"), ["piggy"] = ("icon_subscription", "#2b2128"), ["plus"] = ("icon_plus", "#357c82"),
        ["rocket"] = ("icon_deploy", "#2b2128"), ["screen"] = ("icon_monitor", "#2b2128"), ["shield"] = ("icon_security", "#2b2128"),
        ["skull"] = ("icon_error", "#a23a34"), ["star"] = ("icon_star", "#c9922e"), ["suit"] = ("icon_user", "#2b2128"),
        ["tag"] = ("icon_price", "#2b2128"), ["trophy"] = ("icon_trophy", "#c9922e"), ["user"] = ("icon_user", "#2b2128"),
        ["warning"] = ("icon_warning", "#a23a34"), ["wifi"] = ("icon_network", "#2b2128"),
    };

    public static Avalonia.Media.Imaging.Bitmap Get(string key)
    {
        if (PackIcons.TryGetValue(key, out var icon))
            return UI.Iso.PackLibrary.Icon(icon.Id, icon.Tint);
        if (Cache.TryGetValue(key, out var bitmap))
            return bitmap;

        bitmap = PixelArt.FromAscii(Art.TryGetValue(key, out var rows) ? rows : Art["star"]);
        Cache[key] = bitmap;
        return bitmap;
    }
}
