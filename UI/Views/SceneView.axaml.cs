using System;
using System.Collections.Generic;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Core.progression;
using UI.Pixel;

namespace UI.Views;

/// <summary>Tout ce qui change le décor. La pièce n'est reconstruite que si ça change.</summary>
public record SceneLook(
    int Computer,
    int Screens,
    int Chair,
    int Coffee,
    int Housing,
    CareerStage Stage,
    int Employees,
    bool HasCat);

/// <summary>L'ambiance du moment, animée à chaque image.</summary>
/// <param name="DayPhase">Moment du mois : 0 = matin, 0.5 = soir, 1 = nuit.</param>
public record SceneMood(bool Working, double DayPhase, bool Crisis, bool InDebt);

/// <summary>
/// Le personnage vu de dos à son bureau, rendu en vrai pixel art : la pièce est dessinée
/// hors écran en formes simples, puis convertie en basse résolution dans la palette du jeu.
/// Les textes flottants et les particules sont dessinés par-dessus, nets.
/// </summary>
public partial class SceneView : UserControl
{
    private const double SceneWidth = 560;
    private const double SceneHeight = 340;
    private const double CenterX = 280;
    private const double DeskY = 200;

    private readonly Canvas _stage = new() { Width = SceneWidth, Height = SceneHeight, Background = Brushes.Black };
    private readonly PixelRenderer _renderer;
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly List<Floater> _floaters = new();
    private readonly List<Particle> _particles = new();
    private readonly List<(Rectangle Line, double BaseWidth, double Phase)> _codeLines = new();
    private readonly List<(Shape Led, double Phase)> _leds = new();
    private readonly List<Line> _rain = new();
    private readonly Random _random = new();
    private SceneLook? _look;
    private Line? _leftArm;
    private Line? _rightArm;
    private Rectangle? _nightShade;
    private Rectangle? _alarmTint;
    private Ellipse? _alarmLight;
    private Ellipse? _sweat;
    private Path? _catTail;
    private Rect _window;
    private double _lastTime;
    private bool _renderThisFrame = true;

    private sealed record Floater(TextBlock Text, double StartY, double BornAt);

    private sealed class Particle
    {
        public required Rectangle Shape { get; init; }
        public double X, Y, VelocityX, VelocityY, BornAt;
    }

    // Les fonctions de dessin ajoutent leurs formes sur la scène hors écran
    private Canvas Stage => _stage;

    /// <summary>Le joueur a cliqué sur la scène (position en unités de scène).</summary>
    public event Action<Point>? Clicked;

    public SceneView()
    {
        InitializeComponent();
        RenderOptions.SetEdgeMode(_stage, EdgeMode.Aliased);
        _renderer = new PixelRenderer(_stage, new Size(SceneWidth, SceneHeight), 2);
        Screen.Source = _renderer.Output;
        PointerPressed += (_, e) => Clicked?.Invoke(e.GetPosition(Overlay));
    }

    /// <summary>Remplit toute la zone, quitte à rogner les bords (fond d'écran du menu).</summary>
    public void FillArea() => Frame.Stretch = Stretch.UniformToFill;

    public void Render(SceneLook look)
    {
        if (look == _look)
            return;

        _look = look;
        _stage.Children.Clear();
        _codeLines.Clear();
        _leds.Clear();
        _rain.Clear();
        _catTail = null;

        DrawRoom(look.Housing);
        DrawStageDecor(look.Stage);
        DrawEmployees(look.Employees);
        DrawDesk(look.Computer);
        DrawScreens(look.Screens);
        DrawComputer(look.Computer);
        DrawCoffee(look.Coffee);
        if (look.HasCat)
            DrawCat();
        DrawCharacter(look.Chair, look.Stage);
        DrawMoodLayers();
    }

    /// <summary>Anime la scène, puis la convertit en pixel art (une image sur deux, ça suffit à l'œil).</summary>
    public void Animate(SceneMood mood)
    {
        double time = _clock.Elapsed.TotalSeconds;
        double delta = Math.Min(0.1, time - _lastTime);
        _lastTime = time;

        AnimateCharacter(time, mood.Working);
        AnimateMood(time, mood);

        foreach (var (line, baseWidth, phase) in _codeLines)
            line.Width = mood.Working ? baseWidth * (0.55 + 0.45 * Math.Abs(Math.Sin(time * 1.5 + phase))) : baseWidth * 0.7;

        foreach (var (led, phase) in _leds)
            led.Opacity = 0.35 + 0.65 * Math.Abs(Math.Sin(time * 3 + phase));

        _renderThisFrame = !_renderThisFrame;
        if (_renderThisFrame)
        {
            _renderer.Render();
            Screen.InvalidateVisual();
        }

        AnimateFloaters(time);
        AnimateParticles(time, delta);
    }

    /// <summary>Affiche un texte qui monte et disparaît (gain, travail fourni...).</summary>
    public void ShowFloater(string text, IBrush brush, Point? at = null, double size = 16)
    {
        var position = at ?? new Point(CenterX - 30 + _random.Next(-40, 40), 150);
        var block = new TextBlock
        {
            Text = text,
            Foreground = brush,
            FontSize = size,
            FontWeight = FontWeight.Bold,
            IsHitTestVisible = false,
        };
        Canvas.SetLeft(block, position.X);
        Canvas.SetTop(block, position.Y);
        Overlay.Children.Add(block);
        _floaters.Add(new Floater(block, position.Y, _lastTime));
    }

    /// <summary>Une gerbe de pièces d'or qui jaillit (contrat livré, gros gain).</summary>
    public void BurstCoins(int count, Point? from = null)
    {
        var origin = from ?? new Point(CenterX, 130);
        for (int i = 0; i < count && _particles.Count < 120; i++)
        {
            var shape = new Rectangle
            {
                Width = 5,
                Height = 5,
                Fill = i % 3 == 0 ? Palette.Lemon : Palette.Gold,
                IsHitTestVisible = false,
            };
            Overlay.Children.Add(shape);
            _particles.Add(new Particle
            {
                Shape = shape,
                X = origin.X,
                Y = origin.Y,
                VelocityX = _random.NextDouble() * 220 - 110,
                VelocityY = -120 - _random.NextDouble() * 160,
                BornAt = _lastTime,
            });
        }
    }

    private void AnimateCharacter(double time, bool working)
    {
        if (_leftArm == null || _rightArm == null)
            return;

        double speed = working ? 18 : 2;
        double amplitude = working ? 3 : 0.6;
        _leftArm.EndPoint = new Point(258 + Math.Sin(time * speed) * amplitude, 196 - Math.Abs(Math.Cos(time * speed)) * amplitude);
        _rightArm.EndPoint = new Point(302 + Math.Sin(time * speed + 1.7) * amplitude, 196 - Math.Abs(Math.Cos(time * speed + 1.7)) * amplitude);
    }

    private void AnimateMood(double time, SceneMood mood)
    {
        // Jour, soir, nuit : la fenêtre s'assombrit au fil du mois
        if (_nightShade != null)
            _nightShade.Opacity = Math.Clamp((mood.DayPhase - 0.45) * 1.8, 0, 0.75);

        for (int i = 0; i < _rain.Count; i++)
        {
            double y = _window.Y + (time * 160 + i * 37) % _window.Height;
            double x = _window.X + (i * 23 + time * 20) % _window.Width;
            _rain[i].StartPoint = new Point(x, y);
            _rain[i].EndPoint = new Point(x - 3, Math.Min(y + 9, _window.Bottom));
        }

        if (_alarmTint != null && _alarmLight != null)
        {
            double pulse = Math.Abs(Math.Sin(time * 4));
            _alarmTint.Opacity = mood.Crisis ? 0.12 + 0.12 * pulse : 0;
            _alarmLight.Opacity = mood.Crisis ? 0.4 + 0.6 * pulse : 0;
        }

        if (_sweat != null)
        {
            _sweat.Opacity = mood.InDebt ? 1 : 0;
            Canvas.SetTop(_sweat, 168 + time * 30 % 20);
        }

        if (_catTail != null)
            _catTail.RenderTransform = new RotateTransform(Math.Sin(time * 3) * 12);
    }

    private void AnimateFloaters(double time)
    {
        for (int i = _floaters.Count - 1; i >= 0; i--)
        {
            var floater = _floaters[i];
            double age = time - floater.BornAt;
            if (age > 1.2)
            {
                Overlay.Children.Remove(floater.Text);
                _floaters.RemoveAt(i);
                continue;
            }

            Canvas.SetTop(floater.Text, floater.StartY - age * 45);
            floater.Text.Opacity = 1 - age / 1.2;
        }
    }

    private void AnimateParticles(double time, double delta)
    {
        for (int i = _particles.Count - 1; i >= 0; i--)
        {
            var p = _particles[i];
            p.VelocityY += 520 * delta; // gravité
            p.X += p.VelocityX * delta;
            p.Y += p.VelocityY * delta;

            if (time - p.BornAt > 1.6 || p.Y > SceneHeight)
            {
                Overlay.Children.Remove(p.Shape);
                _particles.RemoveAt(i);
                continue;
            }

            // Positions arrondies à la grille des pixels de la scène
            Canvas.SetLeft(p.Shape, Math.Round(p.X / 2) * 2);
            Canvas.SetTop(p.Shape, Math.Round(p.Y / 2) * 2);
        }
    }

    // Couches d'ambiance posées au-dessus du décor : alarme de crise, sueur
    private void DrawMoodLayers()
    {
        _alarmTint = Box(0, 0, SceneWidth, SceneHeight, "#e43b44");
        _alarmTint.Opacity = 0;
        Box(520, 8, 16, 6, "#3a4466");
        _alarmLight = Oval(518, 12, 20, 14, "#ff0044");
        _alarmLight.Opacity = 0;
        _sweat = Oval(304, 170, 6, 9, "#2ce8f5");
        _sweat.Opacity = 0;
    }

    private void DrawCat()
    {
        // Un chat orange roulé en boule sur le bureau, la queue qui bat
        Oval(392, DeskY - 22, 40, 24, "#f77622");
        Oval(420, DeskY - 32, 20, 18, "#f77622");
        Stroke($"M 423,{DeskY - 30} L 425,{DeskY - 38} L 429,{DeskY - 31} M 433,{DeskY - 31} L 437,{DeskY - 38} L 438,{DeskY - 29}",
            "#f77622", 2, "#f77622");
        Oval(426, DeskY - 25, 3, 3, "#181425");
        Oval(433, DeskY - 25, 3, 3, "#181425");
        for (int i = 0; i < 3; i++)
            Box(398 + i * 9, DeskY - 18, 3, 12, "#d77643");

        _catTail = new Path
        {
            Data = Geometry.Parse("M 0,0 Q -14,-6 -18,-18"),
            Stroke = Palette.Ember,
            StrokeThickness = 5,
            StrokeLineCap = PenLineCap.Round,
            RenderTransformOrigin = RelativePoint.TopLeft,
        };
        Canvas.SetLeft(_catTail, 394);
        Canvas.SetTop(_catTail, DeskY - 8);
        _stage.Children.Add(_catTail);
    }

    // ---------- Pièce ----------

    private record RoomStyle(string Wall, string Floor, string SkyTop, string SkyBottom, double WindowWidth);

    private static readonly RoomStyle[] Rooms =
    [
        new("#5A5047", "#3A3632", "#7E8A93", "#A3ABB0", 110), // Studio miteux
        new("#6F8392", "#6B4F3A", "#2E3F66", "#E39A6E", 150), // Appartement
        new("#8D4E3C", "#55585C", "#0F1A33", "#2A3B66", 200), // Loft
        new("#9DB894", "#9C7A54", "#69A8E0", "#CDE7F7", 200), // Maison avec jardin
        new("#EFE6D2", "#D7D1C6", "#3FA0E8", "#BFE6FF", 250), // Villa avec piscine
        new("#2B2D42", "#1B1C2B", "#0A0F2A", "#3B2A5C", 330), // Penthouse
        new("#C9A66B", "#E6D3A3", "#FF8A5B", "#FFD27F", 330), // Île privée
    ];

    private void DrawRoom(int housing)
    {
        var style = Rooms[Math.Clamp(housing, 0, Rooms.Length - 1)];

        Box(0, 0, SceneWidth, 250, style.Wall);
        Box(0, 250, SceneWidth, 90, style.Floor);
        Box(0, 248, SceneWidth, 4, Darker(style.Floor));

        // Fenêtre et vue
        double windowX = 24;
        double windowWidth = style.WindowWidth;
        Box(windowX - 5, 20, windowWidth + 10, 125, "#2B2B2B", 3);
        var sky = new Rectangle
        {
            Width = windowWidth,
            Height = 115,
            Fill = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
                GradientStops = { new GradientStop(Color.Parse(style.SkyTop), 0), new GradientStop(Color.Parse(style.SkyBottom), 1) }
            }
        };
        Place(sky, windowX, 25);
        DrawView(housing, windowX, windowWidth);
        _window = new Rect(windowX, 25, windowWidth, 115);
        _nightShade = Box(windowX, 25, windowWidth, 115, "#0b0a1a");
        _nightShade.Opacity = 0;
        if (housing <= 1)
        {
            for (int i = 0; i < 14; i++)
            {
                var drop = new Line { Stroke = Palette.Fog, StrokeThickness = 1.5 };
                _rain.Add(drop);
                _stage.Children.Add(drop);
            }
        }
        Box(windowX + windowWidth / 2 - 2, 25, 4, 115, "#2B2B2B");

        switch (housing)
        {
            case 0:
                // Fissure au mur et matelas par terre
                Stroke("M 470,30 L 478,55 L 470,70 L 482,100", "#3D352E", 2);
                Box(470, 270, 85, 22, "#8C8478", 4);
                Box(474, 266, 30, 10, "#B8B0A4", 4);
                break;
            case 1:
                DrawPlant(500, 205);
                break;
            case 2:
                // Briques et poster
                for (int row = 0; row < 8; row++)
                    Box(row % 2 == 0 ? 400 : 420, 20 + row * 25, 160, 1.5, "#6E3C2E");
                Box(450, 40, 70, 90, "#1E1E1E", 2);
                Box(456, 46, 58, 78, "#E53935", 1);
                DrawPlant(510, 205);
                break;
            case 3:
                Box(445, 40, 90, 65, "#6D4C41", 2);
                Box(450, 45, 80, 55, "#7CB342", 1);
                Oval(475, 55, 30, 30, "#FDD835");
                DrawPlant(505, 205);
                break;
            default:
                // Tableaux de maître et plantes pour les logements de luxe
                Box(450, 35, 90, 70, "#C9A227", 2);
                Box(456, 41, 78, 58, "#263238", 1);
                Oval(480, 55, 28, 28, "#8E24AA");
                DrawPlant(505, 200);
                DrawPlant(10, 200);
                break;
        }
    }

    private void DrawView(int housing, double x, double width)
    {
        switch (housing)
        {
            case 0:
                // Mur de briques de l'immeuble d'en face
                Box(x, 25, width, 115, "#7A4B3A");
                for (int row = 0; row < 7; row++)
                    Box(x, 40 + row * 15, width, 1.5, "#5C372A");
                break;
            case 1:
            case 2:
            case 5:
                // Ville, fenêtres allumées
                var lights = housing == 1 ? "#FFE082" : "#FFD54F";
                for (int i = 0; i < width / 28; i++)
                {
                    double h = 40 + (i * 37 % 60);
                    Box(x + i * 28, 140 - h, 24, h, housing == 1 ? "#37474F" : "#1A237E");
                    for (int w = 0; w < h / 14 - 1; w++)
                        if ((i + w) % 3 != 0)
                            Box(x + i * 28 + 5, 140 - h + 6 + w * 14, 4, 5, lights);
                }
                if (housing == 5)
                    Stroke($"M {x + width - 90},135 L {x + width - 30},135 L {x + width - 40},128 L {x + width - 80},128 Z", "#ECEFF1", 0, "#ECEFF1");
                break;
            case 3:
                Box(x, 110, width, 30, "#7CB342");
                Oval(x + 30, 60, 50, 55, "#558B2F");
                Box(x + 52, 105, 6, 15, "#5D4037");
                Oval(x + width - 50, 35, 26, 26, "#FFF59D");
                break;
            case 4:
            case 6:
                // La mer, le soleil et un palmier
                Box(x, 95, width, 45, housing == 6 ? "#26C6DA" : "#0288D1");
                Oval(x + width - 80, 45, 40, 40, housing == 6 ? "#FFEB3B" : "#FFF176");
                Box(x + 40, 70, 5, 70, "#6D4C41");
                Stroke($"M {x + 42},70 Q {x + 15},55 {x + 5},75 M {x + 42},70 Q {x + 70},50 {x + 82},72 M {x + 42},70 Q {x + 40},45 {x + 25},45",
                    "#2E7D32", 5);
                break;
        }
    }

    private void DrawPlant(double x, double y)
    {
        Box(x, y + 25, 30, 25, "#8D6E63", 3);
        Oval(x - 8, y - 10, 26, 34, "#43A047");
        Oval(x + 12, y - 18, 26, 40, "#2E7D32");
    }

    // ---------- Statut ----------

    private void DrawStageDecor(CareerStage stage)
    {
        // Une couverture de magazine encadrée pour les puissants
        if (stage >= CareerStage.Investor)
        {
            Box(395, 35, 42, 56, "#C9A227", 2);
            Box(399, 39, 34, 48, "#FAFAFA");
            Box(402, 42, 28, 6, "#D32F2F");
            Oval(408, 52, 16, 16, "#F2C29B");
            Box(406, 68, 20, 16, "#263238");
        }

        if (stage == CareerStage.KingOfTheWorld)
        {
            // Lingots d'or au pied du bureau
            for (int i = 0; i < 5; i++)
                Stroke($"M {340 + i * 18},300 L {356 + i * 18},300 L {352 + i * 18},290 L {344 + i * 18},290 Z", "#B8860B", 1, "#FFC107");
            for (int i = 0; i < 3; i++)
                Stroke($"M {349 + i * 18},290 L {365 + i * 18},290 L {361 + i * 18},280 L {353 + i * 18},280 Z", "#B8860B", 1, "#FFC107");
        }
    }

    // ---------- Employés ----------

    private void DrawEmployees(int count)
    {
        // Les employés travaillent dans les coins, vus de dos eux aussi
        var spots = new List<Point>();
        for (int row = 0; row < 2; row++)
        for (int i = 0; i < 3; i++)
        {
            spots.Add(new Point(12 + i * 58, 262 + row * 40));
            spots.Add(new Point(392 + i * 58, 262 + row * 40));
        }

        int drawn = Math.Min(count, spots.Count);
        string[] shirts = ["#1E88E5", "#43A047", "#F4511E", "#8E24AA", "#00897B", "#6D4C41"];
        for (int i = 0; i < drawn; i++)
        {
            var p = spots[i];
            Box(p.X, p.Y + 10, 46, 5, "#795548");
            Box(p.X + 10, p.Y - 8, 26, 17, "#263238", 2);
            Box(p.X + 12, p.Y - 6, 22, 13, "#4FC3F7", 1);
            Box(p.X + 12, p.Y + 18, 22, 18, shirts[i % shirts.Length], 6);
            Oval(p.X + 15, p.Y + 6, 16, 16, i % 3 == 0 ? "#4E342E" : i % 3 == 1 ? "#212121" : "#F9A825");
        }

        if (count > drawn)
            Label($"+{count - drawn}", 500, 225, "#FFFFFF", 14);
    }

    // ---------- Bureau et matériel ----------

    private void DrawDesk(int computer)
    {
        string top = computer switch
        {
            <= 1 => "#8B6B4A",
            <= 3 => "#2A2A2A",
            <= 6 => "#ECEFF1",
            _ => "#B3E5FC"
        };

        Box(110, DeskY + 10, 8, 80, Darker(top));
        Box(442, DeskY + 10, 8, 80, Darker(top));
        Box(100, DeskY, 360, 12, top, 2);

        if (computer is >= 2 and <= 3)
        {
            var strip = Box(100, DeskY + 11, 360, 2, "#E040FB");
            _leds.Add((strip, 0));
        }

        // Clavier et souris
        Box(238, DeskY - 6, 84, 7, computer >= 2 ? "#212121" : "#9E9E9E", 2);
        Oval(330, DeskY - 6, 10, 7, computer >= 2 ? "#212121" : "#9E9E9E");
    }

    private void DrawScreens(int screens)
    {
        switch (screens)
        {
            case 0:
                DrawScreen(220, 110, 120, 78, cracked: true);
                break;
            case 1:
                DrawScreen(155, 112, 120, 76);
                DrawScreen(285, 112, 120, 76);
                break;
            case 2:
                DrawScreen(110, 115, 110, 72);
                DrawScreen(225, 110, 110, 78);
                DrawScreen(340, 115, 110, 72);
                break;
            default:
                // Mur d'écrans
                for (int i = 0; i < 3; i++)
                {
                    DrawScreen(110 + i * 115, 118, 110, 70);
                    DrawScreen(110 + i * 115, 42, 110, 70, stand: false);
                }
                break;
        }
    }

    private void DrawScreen(double x, double y, double w, double h, bool cracked = false, bool stand = true)
    {
        if (stand)
        {
            Box(x + w / 2 - 4, y + h, 8, DeskY - y - h, "#37474F");
            Box(x + w / 2 - 18, DeskY - 3, 36, 3, "#37474F", 1);
        }

        Box(x, y, w, h, "#212121", 3);
        Box(x + 4, y + 4, w - 8, h - 8, "#0D1B2A", 1);

        string[] colors = ["#80CBC4", "#F48FB1", "#FFF59D", "#90CAF9", "#A5D6A7"];
        for (int i = 0; i < (h - 14) / 8; i++)
        {
            double indent = (i % 3) * 8;
            double width = 25 + (i * 31 + (int)x) % (w - 45);
            var line = Box(x + 9 + indent, y + 9 + i * 8, width, 3, colors[(i + (int)x) % colors.Length], 1);
            _codeLines.Add((line, width, i * 0.9 + x));
        }

        if (cracked)
            Stroke($"M {x + 20},{y + 6} L {x + 38},{y + 30} L {x + 30},{y + 45} L {x + 50},{y + 70}", "#ECEFF1", 1.5);
    }

    private void DrawComputer(int computer)
    {
        switch (computer)
        {
            case 0:
                // Vieux portable sur le bureau
                Box(372, DeskY - 4, 60, 4, "#757575", 1);
                Stroke($"M 378,{DeskY - 4} L 384,{DeskY - 44} L 432,{DeskY - 44} L 426,{DeskY - 4} Z", "#616161", 1, "#9E9E9E");
                break;
            case 1:
                DrawTower(468, 230, 34, 70, "#D7CCC8", null);
                break;
            case 2:
                DrawTower(468, 222, 40, 78, "#212121", "#E040FB");
                break;
            case 3:
                DrawTower(466, 210, 48, 90, "#263238", "#00E5FF");
                break;
            case 4:
                DrawTower(462, 214, 42, 86, "#ECEFF1", "#00E5FF");
                DrawTower(510, 214, 42, 86, "#ECEFF1", "#00E5FF");
                break;
            case 5:
            case 6:
                DrawRack(470, computer == 5 ? 130 : 110);
                if (computer == 6)
                    DrawRack(515, 110);
                break;
            default:
                // Ordinateur quantique : un lustre doré qui brille
                var glow = Oval(455, 120, 100, 180, "#4000E5FF");
                _leds.Add((glow, 0));
                for (int i = 0; i < 4; i++)
                    Box(470 + i * 2, 140 + i * 35, 70 - i * 4, 22, "#FFC107", 4);
                Box(503, 120, 4, 160, "#FFD54F");
                break;
        }
    }

    private void DrawTower(double x, double y, double w, double h, string color, string? glow)
    {
        Box(x, y, w, h, color, 3);
        if (glow == null)
            return;

        var strip = Box(x + 4, y + 6, 3, h - 12, glow, 1);
        _leds.Add((strip, x));
        Oval(x + w / 2 - 6, y + 12, 12, 12, glow);
    }

    private void DrawRack(double x, double top)
    {
        Box(x, top, 42, 300 - top, "#212121", 2);
        for (double y = top + 8; y < 290; y += 14)
        {
            Box(x + 4, y, 34, 10, "#37474F", 1);
            var led = Oval(x + 30, y + 3, 4, 4, (int)y % 3 == 0 ? "#76FF03" : "#00E5FF");
            _leds.Add((led, y));
        }
    }

    private void DrawCoffee(int coffee)
    {
        switch (coffee)
        {
            case 0:
                // Verre d'eau du robinet
                Box(150, DeskY - 18, 12, 18, "#80B3E5F7", 2);
                break;
            case 1:
                Box(146, DeskY - 18, 16, 18, "#ECEFF1", 3);
                Stroke($"M 162,{DeskY - 14} Q 170,{DeskY - 10} 162,{DeskY - 5}", "#ECEFF1", 2);
                break;
            case 2:
                Box(118, DeskY - 42, 40, 42, "#B0BEC5", 4);
                Box(124, DeskY - 36, 28, 10, "#263238", 2);
                Box(132, DeskY - 14, 12, 12, "#FFFFFF", 2);
                break;
            default:
                // Machine dorée de barista
                Box(114, DeskY - 50, 48, 50, "#FFC107", 5);
                Box(120, DeskY - 44, 36, 12, "#4E342E", 2);
                Box(131, DeskY - 16, 14, 14, "#FFFFFF", 3);
                Stroke("M 124,140 Q 128,130 124,120 M 140,140 Q 144,130 140,120", "#B0FFFFFF", 2);
                break;
        }
    }

    // ---------- Personnage ----------

    private void DrawCharacter(int chair, CareerStage stage)
    {
        var (outfit, hair) = stage switch
        {
            CareerStage.Broke => ("#6D6D6D", "#4E342E"),
            CareerStage.Freelancer => ("#1E88E5", "#4E342E"),
            CareerStage.Founder => ("#212121", "#3E2723"),
            CareerStage.Boss => ("#3B6EA5", "#3E2723"),
            CareerStage.Investor => ("#1F2A44", "#212121"),
            CareerStage.Tycoon => ("#111111", "#212121"),
            _ => ("#5B2A86", "#212121")
        };

        // Corps : les épaules dépassent du dossier de la chaise
        Box(228, 205, 104, 80, outfit, 22);
        if (stage == CareerStage.Broke)
            Stroke("M 250,208 Q 280,232 310,208", Darker(outfit), 6); // capuche du sweat
        if (stage >= CareerStage.Investor)
            Stroke("M 268,206 L 280,225 L 292,206", "#ECEFF1", 0, "#ECEFF1"); // col de chemise

        DrawChair(chair, stage);

        // Les bras partent des épaules, devant le dossier, jusqu'au clavier
        _leftArm = ArmLine(outfit, 236, 216, 258, 196);
        _rightArm = ArmLine(outfit, 324, 216, 302, 196);

        // Tête vue de dos : cou, oreilles, cheveux
        Box(272, 194, 16, 14, "#E0A47E", 4);
        Oval(258, 175, 8, 14, "#E0A47E");
        Oval(294, 175, 8, 14, "#E0A47E");
        Oval(261, 158, 38, 44, hair);

        if (stage == CareerStage.Broke)
            Stroke("M 263,165 L 268,150 L 274,162 L 280,147 L 286,162 L 292,151 L 297,166", hair, 3, hair); // cheveux en bataille

        if (stage is >= CareerStage.Founder and < CareerStage.Investor)
        {
            // Casque audio
            Stroke("M 258,180 Q 280,140 302,180", "#212121", 4);
            Box(253, 172, 10, 18, "#E53935", 4);
            Box(297, 172, 10, 18, "#E53935", 4);
        }

        if (stage == CareerStage.KingOfTheWorld)
        {
            // Couronne
            Stroke("M 262,160 L 262,138 L 271,150 L 280,132 L 289,150 L 298,138 L 298,160 Z", "#B8860B", 1.5, "#FFC107");
            Oval(277, 140, 6, 6, "#E53935");
            // Cape royale
            Stroke("M 236,212 L 222,300 L 338,300 L 324,212", "#B8860B", 2, "#7B1FA2");
        }
    }

    private Line ArmLine(string outfit, double x1, double y1, double x2, double y2)
    {
        var arm = new Line
        {
            StartPoint = new Point(x1, y1),
            EndPoint = new Point(x2, y2),
            Stroke = Brush(outfit),
            StrokeThickness = 13,
            StrokeLineCap = PenLineCap.Round
        };
        Stage.Children.Add(arm);
        return arm;
    }

    private void DrawChair(int chair, CareerStage stage)
    {
        switch (chair)
        {
            case 0:
                // Tabouret : pas de dossier, on voit le dos voûté
                Box(250, 285, 60, 10, "#E53935", 4);
                Box(256, 295, 5, 40, "#C62828");
                Box(299, 295, 5, 40, "#C62828");
                break;
            case 1:
                Box(246, 222, 68, 72, "#424242", 10);
                Box(276, 294, 8, 26, "#616161");
                Box(250, 318, 60, 6, "#616161", 3);
                break;
            case 2:
                // Fauteuil gamer
                Box(240, 196, 80, 104, "#212121", 14);
                Box(256, 200, 10, 96, "#E53935", 3);
                Box(294, 200, 10, 96, "#E53935", 3);
                Box(276, 300, 8, 22, "#424242");
                Box(246, 320, 68, 6, "#424242", 3);
                break;
            default:
                // Fauteuil ergonomique (ou trône pour le roi)
                bool throne = stage == CareerStage.KingOfTheWorld;
                Box(240, 200, 80, 100, throne ? "#B8860B" : "#37474F", 16);
                Box(246, 206, 68, 88, throne ? "#8E1B1B" : "#546E7A", 12);
                if (!throne)
                    for (int i = 0; i < 6; i++)
                        Box(248, 212 + i * 14, 64, 1.5, "#78909C");
                Box(276, 300, 8, 22, "#455A64");
                Box(246, 320, 68, 6, "#455A64", 3);
                break;
        }
    }

    // ---------- Petites aides de dessin ----------

    private static IBrush Brush(string color) => new SolidColorBrush(Color.Parse(color));

    private static string Darker(string color)
    {
        var c = Color.Parse(color);
        return $"#{c.A:X2}{(byte)(c.R * 0.7):X2}{(byte)(c.G * 0.7):X2}{(byte)(c.B * 0.7):X2}";
    }

    private Rectangle Box(double x, double y, double w, double h, string color, double radius = 0)
    {
        var rect = new Rectangle
        {
            Width = Math.Max(0, w),
            Height = Math.Max(0, h),
            Fill = Brush(color),
            RadiusX = radius,
            RadiusY = radius
        };
        Place(rect, x, y);
        return rect;
    }

    private Ellipse Oval(double x, double y, double w, double h, string color)
    {
        var ellipse = new Ellipse { Width = w, Height = h, Fill = Brush(color) };
        Place(ellipse, x, y);
        return ellipse;
    }

    private void Stroke(string data, string stroke, double thickness, string? fill = null)
    {
        var path = new Path
        {
            Data = Geometry.Parse(data),
            Stroke = Brush(stroke),
            StrokeThickness = thickness,
            StrokeLineCap = PenLineCap.Round,
            Fill = fill == null ? null : Brush(fill)
        };
        Stage.Children.Add(path);
    }

    private void Label(string text, double x, double y, string color, double size)
    {
        var block = new TextBlock { Text = text, Foreground = Brush(color), FontSize = size, FontWeight = FontWeight.Bold };
        Place(block, x, y);
    }

    private void Place(Control control, double x, double y)
    {
        Canvas.SetLeft(control, x);
        Canvas.SetTop(control, y);
        Stage.Children.Add(control);
    }
}
