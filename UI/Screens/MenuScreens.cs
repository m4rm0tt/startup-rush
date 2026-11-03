using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Core.campaign;
using Core.persistence;
using UI.Audio;
using UI.Hud;
using UI.Pixel;

namespace UI.Screens;

/// <summary>Fond commun des écrans de menu : titre, et la petite équipe de Pixel Office qui attend.</summary>
public abstract class MenuScreen : UserControl, IScreen
{
    protected MainWindow Window { get; }

    protected MenuScreen(MainWindow window)
    {
        Window = window;
        Background = Palette.Brush("#efe4cc");
    }

    private static readonly Core.team.Appearance[] TitleFaces =
    [
        new() { Base = "player_f", Hair = "hair_bob", HairRamp = "hair_black", Skin = "skin_medium" },
        new() { Base = "employee_m_02", Hair = "hair_curly_top", HairRamp = "hair_brown", Skin = "skin_deep" },
        new() { Base = "player_m", Hair = "hair_short_crop", HairRamp = "hair_brown", Skin = "skin_light" },
        new() { Base = "employee_f_03", Hair = "hair_bun", HairRamp = "hair_auburn", Skin = "skin_light" },
        new() { Base = "employee_m_05", Hair = "hair_side_part", HairRamp = "hair_gray", Skin = "skin_medium" },
    ];

    protected Control Frame(Control content, double width = 560)
    {
        var title = new TextBlock { Text = "STARTUP RUSH", FontSize = 44, Foreground = Palette.Gold, HorizontalAlignment = HorizontalAlignment.Center };
        title.Classes.Add("title");
        // Quelques visages de l'équipe à venir : les personnages en couches du pack
        var people = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12, HorizontalAlignment = HorizontalAlignment.Center };
        foreach (var look in TitleFaces)
            people.Children.Add(Ui.Image(UI.Iso.Portraits.Of(look), 64, 64));

        var panel = new PixelPanel { Padding = new Thickness(24, 18), Child = content, Width = width };
        var column = Ui.Column(18,
            title,
            Ui.Text(Loc.T("title.subtitle"), 18, Palette.Cloud),
            people,
            panel);
        column.HorizontalAlignment = HorizontalAlignment.Center;
        column.VerticalAlignment = VerticalAlignment.Center;
        ((TextBlock)column.Children[1]).HorizontalAlignment = HorizontalAlignment.Center;
        return new LayoutTransformControl
        {
            Child = column,
            LayoutTransform = new ScaleTransform(MenuScale(), MenuScale()),
        };
    }

    private double MenuScale()
    {
        double h = Window.Bounds.Height > 0 ? Window.Bounds.Height : Window.Height;
        // Les menus ne défilent pas : ils rétrécissent dans une petite fenêtre, et grandissent sur un écran 4K (2 au lieu de 1,4)
        return Window.Settings.UiScale > 0 ? Window.Settings.UiScale : UI.Pixel.PixelText.CrispScale(Math.Clamp(h / 900, 0.7, 2.5), Window.RenderScaling);
    }

    public virtual void Tick(double deltaTime)
    {
    }

    public virtual bool HandleKey(Key key, KeyModifiers modifiers) => false;

    public virtual void Leave()
    {
    }
}

public class TitleScreen : MenuScreen
{
    public TitleScreen(MainWindow window, string? message) : base(window)
    {
        var latest = window.Store.Latest();
        var buttons = Ui.Column(10);
        if (latest?.Summary is { } summary)
        {
            var resume = Ui.Button(Loc.T("title.continue", summary.Company, GameTime.FormatDate(summary.Minute)), () => Continue(latest), "gold big", icon: Ui.IconFor("Continuer"));
            resume.HorizontalAlignment = HorizontalAlignment.Stretch;
            buttons.Children.Add(resume);
        }
        foreach (var (key, action) in new (string, Action)[]
                 {
                     ("title.load", window.ShowLoad),
                     ("title.new", window.ShowNewCampaign),
                     ("title.settings", () => window.ShowSettings(() => window.ShowTitle())),
                     ("title.quit", window.Close),
                 })
        {
            var button = Ui.Button(Loc.T(key), action, "big", icon: Ui.IconFor(Loc.French(key)));
            button.HorizontalAlignment = HorizontalAlignment.Stretch;
            buttons.Children.Add(button);
        }
        if (message != null)
            buttons.Children.Insert(0, Ui.Card(Ui.Text(message, 15, Palette.Lemon)));
        if (LegacyProfile.Exists(LegacyProfile.DefaultPath))
            buttons.Children.Add(Ui.Muted(Loc.T("title.legacy")));
        Content = Frame(buttons, 520);
    }

    private void Continue(SaveEntry entry) => Window.Busy(() =>
    {
        var (campaign, message) = Window.Store.LoadWithRecovery(entry.Path);
        if (campaign == null)
        {
            Window.ShowTitle(message);
            return;
        }
        Window.StartGame(campaign, entry.Path, message);
    });

    public override bool HandleKey(Key key, KeyModifiers modifiers)
    {
        if (key == Key.Enter && Window.Store.Latest() is { } latest)
        {
            Continue(latest);
            return true;
        }
        return false;
    }
}

public class NewCampaignScreen : MenuScreen
{
    private string _founder = "Alex";
    private string _company = "";
    private string _campaign = "";
    private FounderProfile _profile = FounderProfile.Developer;
    private readonly Core.team.Appearance _look = new() { Base = "player_m", Hair = "hair_short_crop", HairRamp = "hair_brown", Skin = "skin_light" };
    private string _seed = "";
    private bool _touched;
    private bool _tutorial = true;

    /// <summary>Pourquoi le nom d'entreprise est refusé (affiché sous le champ après « Commencer »), ou null.</summary>
    private string? _companyError;

    public NewCampaignScreen(MainWindow window) : base(window)
    {
        Build();
    }

    private void Build()
    {
        var profiles = new (FounderProfile, string)[]
        {
            (FounderProfile.Developer, Loc.T("new.profile.developer")), (FounderProfile.Designer, Loc.T("new.profile.designer")), (FounderProfile.Generalist, Loc.T("new.profile.generalist")),
        };
        string profileText = _profile switch
        {
            FounderProfile.Developer => Loc.T("new.profileText.developer"),
            FounderProfile.Designer => Loc.T("new.profileText.designer"),
            _ => Loc.T("new.profileText.generalist"),
        };
        var form = Ui.Column(10,
            Ui.Title(Loc.T("new.title")),
            Ui.Muted(Loc.T("new.intro")),
            Ui.Text(Loc.T("new.founderName"), 14, Palette.Fog),
            Ui.Input(_founder, v => { _founder = v; _touched = true; }),
            Ui.Text(Loc.T("new.company"), 14, Palette.Fog),
            Ui.Input(_company, v => { _company = v; _touched = true; }, Loc.T("new.companyPlaceholder")),
            _companyError != null ? Ui.Row(6, Ui.Icon("warning", 18), Ui.Text(_companyError, 14, Palette.Blood)) : null,
            Ui.Text(Loc.T("new.profile"), 14, Palette.Fog),
            Ui.Choice(profiles, _profile, p => { _profile = p; _touched = true; Build(); }),
            Ui.Muted(profileText),
            Ui.Text(Loc.T("new.look"), 14, Palette.Fog),
            Ui.Row(12, Ui.Portrait(UI.Iso.Portraits.Of(_look)), Ui.Column(4,
                Ui.Choice(new[] { ("player_m", Loc.T("new.look.male")), ("player_f", Loc.T("new.look.female")) }, _look.Base, b => { _look.Base = b; Build(); }),
                Ui.Choice(Core.team.Appearance.Hairs.Select(h => (h, HairName(h))), _look.Hair, h => { _look.Hair = h; Build(); }),
                // Couleurs des cheveux et de la peau : cases de couleur du pack, nommées en infobulle
                Swatches(Core.team.Appearance.HairRamps, _look.HairRamp, RampName, r => { _look.HairRamp = r; Build(); }),
                Swatches(Core.team.Appearance.Skins, _look.Skin, k => Loc.T("new.skin", Array.IndexOf(Core.team.Appearance.Skins, k) + 1), k => { _look.Skin = k; Build(); }),
                // Tenue : teintes du pack pour les canaux de la tenue (recoloration par masque, sans nouvelle image)
                Swatches(Core.team.Appearance.Outfits, _look.Outfit ?? "", r => Loc.T("new.outfit", OutfitName(r)), r => { _look.Outfit = r; Build(); }),
                Swatches(Core.team.Appearance.Trims, _look.OutfitTrim ?? "", r => Loc.T("new.trim", OutfitName(r)), r => { _look.OutfitTrim = r; Build(); }))),
            Ui.Text(Loc.T("new.campaignName"), 14, Palette.Fog),
            Ui.Input(_campaign, v => { _campaign = v; _touched = true; }),
            Ui.Text(Loc.T("new.seed"), 14, Palette.Fog),
            Ui.Input(_seed, v => { _seed = v; _touched = true; }, Loc.T("new.seedPlaceholder")),
            Ui.CheckBox(Loc.T("new.tutorial"), _tutorial, v => { _tutorial = v; Build(); }),
            Ui.Row(Ui.Button(Loc.T("new.start"), Start, "gold", icon: Ui.IconFor(Loc.French("new.start"))), Ui.Button(Loc.T("new.back"), Back, "", icon: Ui.IconFor(Loc.French("new.back")))));
        Content = Frame(form, 560);
    }

    private static Control Swatches(string[] ramps, string current, Func<string, string> name, Action<string> pick)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal };
        foreach (string ramp in ramps)
            row.Children.Add(Ui.Swatch(UI.Iso.PackLibrary.RampColor(ramp), ramp == current, () => pick(ramp), name(ramp)));
        return row;
    }

    private static string HairName(string id) => id switch
    {
        "hair_short_crop" => Loc.T("new.hair.short"), "hair_side_part" => Loc.T("new.hair.side"), "hair_curly_top" => Loc.T("new.hair.curly"),
        "hair_long_straight" => Loc.T("new.hair.long"), "hair_bun" => Loc.T("new.hair.bun"), _ => Loc.T("new.hair.bob")
    };

    public static string OutfitName(string id) => id switch
    {
        "teal" => "sarcelle", "coral" => "corail", "sun" => "soleil", "sky" => "ciel", "sage" => "sauge", "navy" => "marine",
        "rose" => "rose", "graphite" => "graphite", "plastic" => "blanc cassé", "steel" => "acier",
        "oak" => "chêne", "walnut" => "noyer", "birch" => "bouleau", "brass" => "laiton", "copper" => "cuivre", "cream" => "crème",
        "sand" => "sable", "concrete" => "béton", "leaf" => "feuille", "red" => "rouge", _ => id
    };

    private static string RampName(string id) => id switch
    {
        "hair_black" => Loc.T("new.ramp.black"), "hair_brown" => Loc.T("new.ramp.brown"), "hair_auburn" => Loc.T("new.ramp.auburn"),
        "hair_blond" => Loc.T("new.ramp.blond"), _ => Loc.T("new.ramp.gray")
    };

    private void Start()
    {
        // Le nom de l'entreprise est obligatoire : il s'affiche partout (barre du haut, sauvegardes, journal)
        _companyError = Loc.CompanyNameError(_company);
        if (_companyError != null)
        {
            Sfx.Play(Sound.Error);
            Build();
            return;
        }
        string company = _company.Trim();
        string founder = string.IsNullOrWhiteSpace(_founder) ? "Alex" : _founder.Trim();
        ulong seed = ulong.TryParse(_seed, out ulong parsed) ? parsed : (ulong)Random.Shared.NextInt64(1, long.MaxValue);
        string name = string.IsNullOrWhiteSpace(_campaign) ? company : _campaign.Trim();
        var campaign = Campaign.New(name, founder, company, _profile, seed, _look);
        campaign.TutorialOn = _tutorial;
        Window.Store.Save(campaign, SaveKind.Manual, "Début");
        Window.StartGame(campaign, null, Loc.T("new.created"));
    }

    private void Back()
    {
        if (_touched && !_confirming)
        {
            _confirming = true;
            Content = Frame(Ui.Column(12, Ui.Title(Loc.T("new.abandon.title")), Ui.Muted(Loc.T("new.abandon.text")),
                Ui.Row(Ui.Button(Loc.T("new.abandon.confirm"), () => Window.ShowTitle(), "red", icon: Ui.IconFor(Loc.French("new.abandon.confirm"))), Ui.Button(Loc.T("new.abandon.keep"), () => { _confirming = false; Build(); }, "", icon: Ui.IconFor(Loc.French("new.abandon.keep"))))), 480);
            return;
        }
        Window.ShowTitle();
    }

    private bool _confirming;

    public override bool HandleKey(Key key, KeyModifiers modifiers)
    {
        if (key == Key.Escape)
        {
            Back();
            return true;
        }
        return false;
    }
}

public class LoadScreen : MenuScreen
{
    public LoadScreen(MainWindow window) : base(window)
    {
        Build();
    }

    private void Build()
    {
        var list = Ui.Column(8);
        var entries = Window.Store.List();
        if (entries.Count == 0)
            list.Children.Add(Ui.Muted(Loc.T("load.none")));
        foreach (var group in entries.GroupBy(e => e.Summary?.CampaignId ?? System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(e.Path))))
        {
            var first = group.FirstOrDefault(e => e.Summary != null)?.Summary;
            string title = first != null ? $"{first.CampaignName} — {first.Company}" : Loc.T("load.unreadableCampaign");
            string campaignId = group.Key;
            var header = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
            var name = Ui.Text(title, 17, Palette.Gold, bold: true);
            name.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center;
            header.Children.Add(name);
            var delete = Ui.Button(Loc.T("load.delete"), () => ConfirmDelete(campaignId, title, group.Count()), "", icon: Ui.IconFor(Loc.French("load.delete")));
            Grid.SetColumn(delete, 1);
            header.Children.Add(delete);
            list.Children.Add(header);
            foreach (var entry in group)
            {
                Control line;
                if (entry.Corrupt || entry.Summary == null)
                {
                    line = Ui.Column(2, Ui.Text(Loc.T("load.damaged", Label(entry)), 15, Palette.Blood), Ui.Muted(entry.Error ?? ""),
                        Ui.Button(Loc.T("load.recover"), () => Load(entry), "", icon: Ui.IconFor(Loc.French("load.recover"))));
                }
                else
                {
                    var s = entry.Summary;
                    var info = Ui.Column(2,
                        Ui.Text($"{Label(entry)} · {GameTime.Format(s.Minute)}", 15, Palette.Snow),
                        Ui.Muted(Loc.T("load.summary", s.Company, s.Situation, s.Cash, s.People, s.WrittenUtc.ToLocalTime())));
                    var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
                    grid.Children.Add(info);
                    var button = Ui.Button(Loc.T("load.load"), () => Load(entry), "gold", icon: Ui.IconFor(Loc.French("load.load")));
                    Grid.SetColumn(button, 1);
                    grid.Children.Add(button);
                    line = grid;
                }
                list.Children.Add(Ui.ListRow(line));
            }
        }
        var scroll = Ui.Scroll(list);
        scroll.MaxHeight = 460;
        Content = Frame(Ui.Column(12, Ui.Title(Loc.T("load.title")), scroll, Ui.Button(Loc.T("load.back"), () => Window.ShowTitle(), "", icon: Ui.IconFor(Loc.French("load.back")))), 680);
    }

    /// <summary>
    /// Le nom d'une sauvegarde dans la langue choisie : manuelle = le nom donné par le joueur ; automatique, de sortie et
    /// d'urgence = d'après le nom du fichier (SaveStore donne ces libellés en français).
    /// </summary>
    private static string Label(SaveEntry entry)
    {
        string name = System.IO.Path.GetFileNameWithoutExtension(entry.Path);
        return entry.Kind switch
        {
            SaveKind.Manual => entry.Label,
            SaveKind.Quick => Loc.T(name == "crash" ? "load.crash" : "load.exit"),
            _ => Loc.T("load.auto", name.Length > 0 ? name[^1].ToString() : ""),
        };
    }

    /// <summary>Confirmation avant de retirer une partie : son dossier part dans « deleted », rien n'est effacé du disque.</summary>
    private void ConfirmDelete(string campaignId, string title, int saves)
    {
        Content = Frame(Ui.Column(12,
            Ui.Title(Loc.T("load.deleteTitle")),
            Ui.Text(Loc.T("load.deleteText", title, saves), 15, Palette.Snow),
            Ui.Muted(Loc.T("load.deleteWhere", Window.Store.TrashFolder)),
            Ui.Row(10,
                Ui.Button(Loc.T("load.deleteConfirm"), () =>
                {
                    try
                    {
                        Window.Store.DeleteCampaign(campaignId);
                        Sfx.Play(Sound.Select);
                    }
                    catch (Exception e) when (e is System.IO.IOException or UnauthorizedAccessException)
                    {
                        Sfx.Play(Sound.Error);
                        Content = Frame(Ui.Column(12, Ui.Title(Loc.T("load.deleteFailed")), Ui.Text(e.Message, 15, Palette.Lemon), Ui.Button(Loc.T("load.backToList"), Build, "", icon: Ui.IconFor(Loc.French("load.backToList")))), 560);
                        return;
                    }
                    Build();
                }, "red", icon: Ui.IconFor(Loc.French("load.deleteConfirm"))),
                Ui.Button(Loc.T("load.cancel"), Build, "", icon: Ui.IconFor(Loc.French("load.cancel"))))), 560);
    }

    private void Load(SaveEntry entry)
    {
        var (campaign, message) = Window.Store.LoadWithRecovery(entry.Path);
        if (campaign == null)
        {
            Sfx.Play(Sound.Error);
            Content = Frame(Ui.Column(12, Ui.Title(Loc.T("load.failed")), Ui.Text(message, 15, Palette.Lemon), Ui.Button(Loc.T("load.backToList"), Build, "", icon: Ui.IconFor(Loc.French("load.backToList")))), 560);
            return;
        }
        Window.StartGame(campaign, entry.Path, string.IsNullOrEmpty(message) ? "Partie chargée." : message);
    }

    public override bool HandleKey(Key key, KeyModifiers modifiers)
    {
        if (key == Key.Escape)
        {
            Window.ShowTitle();
            return true;
        }
        return false;
    }
}

public class SettingsScreen : MenuScreen
{
    private readonly Action _back;

    public SettingsScreen(MainWindow window, Action back) : base(window)
    {
        _back = back;
        Build();
    }

    private void Build()
    {
        Content = Frame(SettingsForm(Window, Build, _back), 560);
    }

    /// <summary>Le formulaire des réglages, réutilisé dans le menu de pause.</summary>
    public static Control SettingsForm(MainWindow window, Action rebuild, Action? back)
    {
        var s = window.Settings;
        // Auto et les tailles entières gardent la police et les sprites nets ; 150 % est proposé pour les grands écrans (moins net)
        (double, string)[] scales = [(0, Loc.T("settings.scaleAuto")), (1, "100 %"), (1.5, "150 %"), (2, "200 %")];
        void Change(Action apply)
        {
            apply();
            window.SaveSettings();
            rebuild();
        }
        // Glissières : la valeur s'applique tout de suite, sans reconstruire le formulaire
        void Volume(Action apply)
        {
            apply();
            window.SaveSettings();
        }
        static string Percent(double v) => v <= 0 ? Loc.T("settings.muted") : $"{v:P0}";
        var scaleRow = new WrapPanel();
        foreach (var (value, label) in scales)
        {
            var radio = Ui.Radio(label, Math.Abs(s.UiScale - value) < 0.01, () => Change(() => s.UiScale = value));
            radio.Margin = new Thickness(0, 0, 14, 4);
            scaleRow.Children.Add(radio);
        }
        var column = Ui.Column(8,
            Ui.Title(Loc.T("settings.title")),
            Ui.Section(Loc.T("settings.language"),
                Ui.Choice(Loc.Languages.Select(l => (l, Loc.Table(l).TryGetValue("language.name", out var n) ? n : l)), Loc.Language,
                    l => { window.SetLanguage(l); rebuild(); })),
            Ui.Section(Loc.T("settings.sound"),
                Ui.Text(Loc.T("settings.music"), 14, Palette.Fog),
                Ui.Slider(s.MusicVolume, 0, 1, 0.1, v => Volume(() => s.MusicVolume = v), Percent),
                Ui.Text(Loc.T("settings.sfx"), 14, Palette.Fog),
                Ui.Slider(s.SoundVolume, 0, 1, 0.1, v => Volume(() => s.SoundVolume = v), Percent)),
            Ui.Section(Loc.T("settings.display"),
                Ui.Switch(Loc.T("settings.fullscreen"), s.Fullscreen, v => { window.SetFullscreen(v); rebuild(); }),
                Ui.Switch(Loc.T("settings.largeText"), s.LargeText, v => { window.SetLargeText(v); rebuild(); }),
                Ui.Text(Loc.T("settings.uiScale"), 14, Palette.Fog),
                scaleRow),
            Ui.Section(Loc.T("settings.time"),
                Ui.Switch(Loc.T("settings.pauseMenus"), s.PauseInLargeMenus, v => Change(() => s.PauseInLargeMenus = v)),
                Ui.Switch(Loc.T("settings.fastNights"), s.FastNights, v => Change(() => s.FastNights = v))),
            Ui.Section(Loc.T("settings.accessibility"),
                Ui.CheckBox(Loc.T("settings.pauseTerminal"), s.PauseDuringTerminal, v => Change(() => s.PauseDuringTerminal = v)),
                Ui.Muted(Loc.T("settings.pauseTerminalNote"), 13),
                Ui.CheckBox(Loc.T("settings.reduceEffects"), s.ReduceEffects, v => Change(() => s.ReduceEffects = v)),
                Ui.Text(Loc.T("settings.cursor"), 14, Palette.Fog),
                CursorChoice(s.TerminalCursor, v => Change(() => s.TerminalCursor = v)),
                Ui.CheckBox(Loc.T("settings.simplified"), s.SimplifiedTyping, v => Change(() => s.SimplifiedTyping = v)),
                Ui.Muted(Loc.T("settings.simplifiedNote"), 13)));
        column.Children.Add(Ui.Section(Loc.T("settings.help"),
            Ui.Muted(Loc.T("settings.logsNote", CrashLog.Folder), 13),
            Ui.Button(Loc.T("settings.openLogs"), CrashLog.OpenFolder, "", icon: Ui.IconFor(Loc.French("settings.openLogs")))));
        if (back != null)
            column.Children.Add(Ui.Button(Loc.T("settings.back"), back, "", icon: Ui.IconFor(Loc.French("settings.back"))));
        return column;
    }

    /// <summary>Forme du curseur du terminal : boutons radio, chacun montré avec son sprite du pack.</summary>
    private static Control CursorChoice(string current, Action<string> pick)
    {
        var row = new WrapPanel();
        foreach (var (id, label) in new[] { ("block", Loc.T("settings.cursor.block")), ("bar", Loc.T("settings.cursor.bar")), ("underline", Loc.T("settings.cursor.underline")) })
        {
            var sample = Ui.Image(PackSkins.Sprite($"term_cursor_{id}"), 12, 24);
            var radio = Ui.Radio(label, current == id, () => pick(id));
            radio.Margin = new Thickness(0, 0, 4, 4);
            row.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2, Margin = new Thickness(0, 0, 12, 0),
                Children = { radio, new Border { Background = Palette.Brush("#162235"), Padding = new Thickness(4, 2), VerticalAlignment = VerticalAlignment.Center, Child = sample } } });
        }
        return row;
    }

    public override bool HandleKey(Key key, KeyModifiers modifiers)
    {
        if (key == Key.Escape)
        {
            _back();
            return true;
        }
        return false;
    }
}
