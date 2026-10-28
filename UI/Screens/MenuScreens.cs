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
            Ui.Text("Simulation d'entreprise tech — de ta chambre à tes propres bureaux", 18, Palette.Cloud),
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
        return Window.Settings.UiScale > 0 ? Window.Settings.UiScale : Math.Clamp(h / 900, 0.7, 1.4);
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
            var resume = Ui.Button($"Continuer — {summary.Company}, {GameTime.FormatDate(summary.Minute)}", () => Continue(latest), "gold big");
            resume.HorizontalAlignment = HorizontalAlignment.Stretch;
            buttons.Children.Add(resume);
        }
        foreach (var (label, action) in new (string, Action)[]
                 {
                     ("Charger une partie", window.ShowLoad),
                     ("Nouvelle partie", window.ShowNewCampaign),
                     ("Réglages", () => window.ShowSettings(() => window.ShowTitle())),
                     ("Quitter", window.Close),
                 })
        {
            var button = Ui.Button(label, action, "big");
            button.HorizontalAlignment = HorizontalAlignment.Stretch;
            buttons.Children.Add(button);
        }
        if (message != null)
            buttons.Children.Insert(0, Ui.Card(Ui.Text(message, 15, Palette.Lemon)));
        if (LegacyProfile.Exists(LegacyProfile.DefaultPath))
            buttons.Children.Add(Ui.Muted("Ton ancien profil du roguelike (profile.json) est conservé tel quel ; ses lingots et son Héritage n'existent pas dans cette simulation."));
        Content = Frame(buttons, 520);
    }

    private void Continue(SaveEntry entry)
    {
        var (campaign, message) = Window.Store.LoadWithRecovery(entry.Path);
        if (campaign == null)
        {
            Window.ShowTitle(message);
            return;
        }
        Window.StartGame(campaign, entry.Path, message);
    }

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

    public NewCampaignScreen(MainWindow window) : base(window)
    {
        Build();
    }

    private void Build()
    {
        var profiles = new (FounderProfile, string)[]
        {
            (FounderProfile.Developer, "Développeur·se"), (FounderProfile.Designer, "Designer"), (FounderProfile.Generalist, "Polyvalent·e"),
        };
        string profileText = _profile switch
        {
            FounderProfile.Developer => "Code 62, design 30, tests 40, support 35. Rapide sur les intégrations et les outils.",
            FounderProfile.Designer => "Design 64, code 38, tests 34, support 40. Brille sur les sites vitrines et les refontes.",
            _ => "Tout autour de 45. Personne n'est bloqué, rien n'est excellent.",
        };
        var form = Ui.Column(10,
            Ui.Title("Nouvelle campagne"),
            Ui.Muted("Une nouvelle campagne ne remplace aucune autre : chacune a ses propres sauvegardes."),
            Ui.Text("Ton nom", 14, Palette.Fog),
            Ui.Input(_founder, v => { _founder = v; _touched = true; }),
            Ui.Text("Nom de l'entreprise", 14, Palette.Fog),
            Ui.Input(_company, v => { _company = v; _touched = true; }, "par ex. Studio Léman"),
            Ui.Text("Ton profil", 14, Palette.Fog),
            Ui.Choice(profiles, _profile, p => { _profile = p; _touched = true; Build(); }),
            Ui.Muted(profileText),
            Ui.Text("Ton apparence (sans effet sur le jeu)", 14, Palette.Fog),
            Ui.Row(12, Ui.Portrait(UI.Iso.Portraits.Of(_look)), Ui.Column(4,
                Ui.Choice(new[] { ("player_m", "Présentation masculine"), ("player_f", "Présentation féminine") }, _look.Base, b => { _look.Base = b; Build(); }),
                Ui.Choice(Core.team.Appearance.Hairs.Select(h => (h, HairName(h))), _look.Hair, h => { _look.Hair = h; Build(); }),
                // Couleurs des cheveux et de la peau : cases de couleur du pack, nommées en infobulle
                Swatches(Core.team.Appearance.HairRamps, _look.HairRamp, RampName, r => { _look.HairRamp = r; Build(); }),
                Swatches(Core.team.Appearance.Skins, _look.Skin, k => $"Peau {Array.IndexOf(Core.team.Appearance.Skins, k) + 1}", k => { _look.Skin = k; Build(); }))),
            Ui.Text("Nom de la campagne (facultatif)", 14, Palette.Fog),
            Ui.Input(_campaign, v => { _campaign = v; _touched = true; }),
            Ui.Text("Graine (facultatif, pour rejouer exactement la même partie)", 14, Palette.Fog),
            Ui.Input(_seed, v => { _seed = v; _touched = true; }, "aléatoire"),
            Ui.Row(Ui.Button("Commencer", Start, "gold"), Ui.Button("Retour", Back)));
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
        "hair_short_crop" => "Court", "hair_side_part" => "Raie", "hair_curly_top" => "Bouclé", "hair_long_straight" => "Long",
        "hair_bun" => "Chignon", _ => "Carré"
    };

    private static string RampName(string id) => id switch
    {
        "hair_black" => "Noirs", "hair_brown" => "Bruns", "hair_auburn" => "Auburn", "hair_blond" => "Blonds", _ => "Gris"
    };

    private void Start()
    {
        string company = string.IsNullOrWhiteSpace(_company) ? "Studio sans nom" : _company.Trim();
        string founder = string.IsNullOrWhiteSpace(_founder) ? "Alex" : _founder.Trim();
        ulong seed = ulong.TryParse(_seed, out ulong parsed) ? parsed : (ulong)Random.Shared.NextInt64(1, long.MaxValue);
        string name = string.IsNullOrWhiteSpace(_campaign) ? company : _campaign.Trim();
        var campaign = Campaign.New(name, founder, company, _profile, seed, _look);
        Window.Store.Save(campaign, SaveKind.Manual, "Début");
        Window.StartGame(campaign, null, "Partie créée et sauvegardée (« Début »).");
    }

    private void Back()
    {
        if (_touched && !_confirming)
        {
            _confirming = true;
            Content = Frame(Ui.Column(12, Ui.Title("Abandonner la saisie ?"), Ui.Muted("Les informations tapées seront perdues."),
                Ui.Row(Ui.Button("Abandonner", () => Window.ShowTitle(), "red"), Ui.Button("Continuer la saisie", () => { _confirming = false; Build(); }))), 480);
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
            list.Children.Add(Ui.Muted("Aucune sauvegarde pour l'instant."));
        foreach (var group in entries.GroupBy(e => e.Summary?.CampaignId ?? System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(e.Path))))
        {
            var first = group.FirstOrDefault(e => e.Summary != null)?.Summary;
            list.Children.Add(Ui.Text(first != null ? $"{first.CampaignName} — {first.Company}" : "Campagne illisible", 17, Palette.Gold, bold: true));
            foreach (var entry in group)
            {
                Control line;
                if (entry.Corrupt || entry.Summary == null)
                {
                    line = Ui.Column(2, Ui.Text($"{entry.Label} : fichier abîmé", 15, Palette.Blood), Ui.Muted(entry.Error ?? ""),
                        Ui.Button("Essayer de récupérer", () => Load(entry)));
                }
                else
                {
                    var s = entry.Summary;
                    var info = Ui.Column(2,
                        Ui.Text($"{entry.Label} · {GameTime.Format(s.Minute)}", 15, Palette.Snow),
                        Ui.Muted($"{s.Company} · {s.Situation} · {s.Cash:N0} CHF · {s.People} pers. · écrite le {s.WrittenUtc.ToLocalTime():dd.MM.yyyy HH:mm}"));
                    var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
                    grid.Children.Add(info);
                    var button = Ui.Button("Charger", () => Load(entry), "gold");
                    Grid.SetColumn(button, 1);
                    grid.Children.Add(button);
                    line = grid;
                }
                list.Children.Add(Ui.ListRow(line));
            }
        }
        var scroll = Ui.Scroll(list);
        scroll.MaxHeight = 460;
        Content = Frame(Ui.Column(12, Ui.Title("Charger une partie"), scroll, Ui.Button("Retour", () => Window.ShowTitle())), 680);
    }

    private void Load(SaveEntry entry)
    {
        var (campaign, message) = Window.Store.LoadWithRecovery(entry.Path);
        if (campaign == null)
        {
            Sfx.Play(Sound.Error);
            Content = Frame(Ui.Column(12, Ui.Title("Chargement impossible"), Ui.Text(message, 15, Palette.Lemon), Ui.Button("Retour à la liste", Build)), 560);
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
        (double, string)[] scales = [(0, "Auto"), (0.8, "80 %"), (1, "100 %"), (1.2, "120 %"), (1.4, "140 %")];
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
        static string Percent(double v) => v <= 0 ? "Coupé" : $"{v:P0}";
        var scaleRow = new WrapPanel();
        foreach (var (value, label) in scales)
        {
            var radio = Ui.Radio(label, Math.Abs(s.UiScale - value) < 0.01, () => Change(() => s.UiScale = value));
            radio.Margin = new Thickness(0, 0, 14, 4);
            scaleRow.Children.Add(radio);
        }
        var column = Ui.Column(8,
            Ui.Title("Réglages"),
            Ui.Section("Son",
                Ui.Text("Musique", 14, Palette.Fog),
                Ui.Slider(s.MusicVolume, 0, 1, 0.1, v => Volume(() => s.MusicVolume = v), Percent),
                Ui.Text("Bruitages", 14, Palette.Fog),
                Ui.Slider(s.SoundVolume, 0, 1, 0.1, v => Volume(() => s.SoundVolume = v), Percent)),
            Ui.Section("Affichage",
                Ui.Text("Taille de l'interface", 14, Palette.Fog),
                scaleRow),
            Ui.Section("Temps",
                Ui.Switch("Les grands menus (produits, finances) mettent le temps en pause", s.PauseInLargeMenus, v => Change(() => s.PauseInLargeMenus = v)),
                Ui.Switch("Nuits et week-ends sans personne au bureau accélérés ×8", s.FastNights, v => Change(() => s.FastNights = v))),
            Ui.Section("Accessibilité",
                Ui.CheckBox("L'entreprise attend pendant la saisie au terminal", s.PauseDuringTerminal, v => Change(() => s.PauseDuringTerminal = v)),
                Ui.Muted("Le rendement d'un mot ne dépend jamais du temps qui passe : ce réglage change seulement ce qui arrive pendant que tu tapes.", 13),
                Ui.CheckBox("Réduire les effets (curseur fixe, moins d'animations)", s.ReduceEffects, v => Change(() => s.ReduceEffects = v)),
                Ui.Text("Curseur du terminal", 14, Palette.Fog),
                CursorChoice(s.TerminalCursor, v => Change(() => s.TerminalCursor = v)),
                Ui.CheckBox("Saisie simplifiée au terminal", s.SimplifiedTyping, v => Change(() => s.SimplifiedTyping = v)),
                Ui.Muted("Saisie simplifiée : le mot s'écrit tout seul et Entrée le valide. Chaque mot rapporte comme une frappe lente (×0,85) : " +
                         "moins qu'une frappe rapide, assez pour mener la campagne.", 13)));
        if (back != null)
            column.Children.Add(Ui.Button("Retour", back));
        return column;
    }

    /// <summary>Forme du curseur du terminal : boutons radio, chacun montré avec son sprite du pack.</summary>
    private static Control CursorChoice(string current, Action<string> pick)
    {
        var row = new WrapPanel();
        foreach (var (id, label) in new[] { ("block", "Bloc"), ("bar", "Barre"), ("underline", "Souligné") })
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
