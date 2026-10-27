using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Core.campaign;
using Core.space;
using Core.team;
using UI.Audio;
using UI.Game;
using UI.Pixel;

namespace UI.Scene;

/// <summary>
/// La scène du local, vue de 3/4 dessus : le sol est une grille de cases de 16 pixels vue de dessus,
/// les objets sont dessinés de face avec leur dessus visible et posés sur le bas de leur emprise.
/// Tout est trié par la ligne de base (le bas de l'emprise) : un personnage assis derrière son bureau
/// est caché par le bureau, celui qui passe devant le recouvre.
/// </summary>
public class SceneCanvas : Control
{
    public const int Cell = 16;
    private const int WallHeight = 2 * Cell;

    private readonly GameSession _session;
    private double _camX, _camY;
    private int _zoom = 3;
    private bool _cameraPlaced;
    private Point? _panFrom;
    private Cell? _hover;
    private double _clock;

    /// <summary>Message après une action dans la scène (placement refusé...) : texte et réussite.</summary>
    public event Action<string, bool>? Feedback;

    public SceneCanvas(GameSession session)
    {
        _session = session;
        ClipToBounds = true;
        Focusable = true;
        Cursor = new Cursor(StandardCursorType.Arrow);
    }

    private Campaign Campaign => _session.Campaign;
    private Premises Premises => Campaign.Premises;

    public int Zoom => _zoom;

    /// <summary>Place le survol sur une case (utilisé par le parcours de captures, sans souris).</summary>
    public void SetHover(Cell? cell)
    {
        _hover = cell;
        _hoverPinned = cell != null;
    }

    private bool _hoverPinned;

    public void Tick(double deltaTime)
    {
        _clock += deltaTime;
        InvalidateVisual();
    }

    // ------------------------------------------------------------------ Caméra

    private double Scale => _zoom / (TopLevel.GetTopLevel(this)?.RenderScaling ?? 1);

    /// <summary>Recentre la vue sur le local, au plus grand zoom entier où il tient (appelé après un déménagement).</summary>
    public void ResetCamera()
    {
        _camX = Premises.Width * Cell / 2.0;
        _camY = (Premises.Height * Cell - WallHeight) / 2.0;
        double scaling = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1;
        double fitW = Bounds.Width * scaling / (Premises.Width * Cell + 2 * Cell);
        double fitH = Bounds.Height * scaling * 0.9 / (Premises.Height * Cell + WallHeight + Cell);
        _zoom = Math.Clamp((int)Math.Floor(Math.Min(fitW, fitH)), 1, 6);
        _cameraPlaced = Bounds.Width > 0;
    }

    public void Pan(double dx, double dy)
    {
        _camX = Math.Clamp(_camX + dx / Scale, -Cell * 2, Premises.Width * Cell + Cell * 2);
        _camY = Math.Clamp(_camY + dy / Scale, -WallHeight - Cell * 2, Premises.Height * Cell + Cell * 2);
    }

    public void ZoomBy(int delta, Point? around = null)
    {
        var anchor = around ?? new Point(Bounds.Width / 2, Bounds.Height / 2);
        var before = ToWorld(anchor);
        _zoom = Math.Clamp(_zoom + delta, 1, 8);
        var after = ToWorld(anchor);
        _camX += before.X - after.X;
        _camY += before.Y - after.Y;
    }

    private Point Origin()
    {
        // Translation arrondie au pixel physique : les sprites restent nets
        double scaling = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1;
        double x = Math.Round((Bounds.Width / 2 - _camX * Scale) * scaling) / scaling;
        double y = Math.Round((Bounds.Height / 2 - _camY * Scale) * scaling) / scaling;
        return new Point(x, y);
    }

    private Point ToWorld(Point screen)
    {
        var origin = Origin();
        return new Point((screen.X - origin.X) / Scale, (screen.Y - origin.Y) / Scale);
    }

    private Rect ToScreen(double x, double y, double w, double h)
    {
        var origin = Origin();
        return new Rect(origin.X + x * Scale, origin.Y + y * Scale, w * Scale, h * Scale);
    }

    private Cell CellAt(Point screen)
    {
        var world = ToWorld(screen);
        return new Cell((int)Math.Floor(world.X / Cell), (int)Math.Floor(world.Y / Cell));
    }

    // ------------------------------------------------------------------ Rendu

    private record Drawable(double Key, Action<DrawingContext> Draw, Rect Bounds, SelectionKind Kind, int Id);

    private readonly List<Drawable> _drawn = new();

    public override void Render(DrawingContext context)
    {
        if (!_cameraPlaced && Bounds.Width > 0)
            ResetCamera();
        context.FillRectangle(new SolidColorBrush(Color.Parse("#193c3e")), new Rect(Bounds.Size));
        using var _ = context.PushRenderOptions(new RenderOptions { BitmapInterpolationMode = BitmapInterpolationMode.None, EdgeMode = EdgeMode.Aliased });

        DrawFloor(context);
        DrawWalls(context);

        _drawn.Clear();
        CollectItems();
        CollectPeople();
        foreach (var drawable in _drawn.OrderBy(d => d.Key))
            drawable.Draw(context);

        DrawSouthWall(context);
        if (_session.BuildMode)
            DrawBuildOverlay(context);
        DrawSelection(context);
        DrawNight(context);
    }

    private static readonly IBrush WoodA = Palette.Brush("#c28569");
    private static readonly IBrush WoodB = Palette.Brush("#b86f50");
    private static readonly IBrush WoodLine = Palette.Brush("#733e39");
    private static readonly IBrush TileA = Palette.Brush("#ead4aa");
    private static readonly IBrush TileB = Palette.Brush("#e4a672");
    private static readonly IBrush CarpetA = Palette.Brush("#5a6988");
    private static readonly IBrush CarpetB = Palette.Brush("#4f5d7c");
    private static readonly IBrush CarpetLine = Palette.Brush("#3a4466");
    private static readonly IBrush WallFace = Palette.Brush("#c0cbdc");
    private static readonly IBrush WallTrim = Palette.Brush("#8b9bb4");
    private static readonly IBrush WallTop = Palette.Brush("#3a4466");
    private static readonly IBrush WallDark = Palette.Brush("#262b44");

    private void DrawFloor(DrawingContext context)
    {
        bool apartment = Premises.ListingId == RealEstate.Apartment;
        for (int y = 0; y < Premises.Height; y++)
        for (int x = 0; x < Premises.Width; x++)
        {
            var rect = ToScreen(x * Cell, y * Cell, Cell, Cell);
            if (Premises.InLivingZone(new Cell(x, y)))
            {
                context.FillRectangle((x + y) % 2 == 0 ? TileA : TileB, rect);
            }
            else if (apartment)
            {
                // Parquet : lames de deux cases, joints décalés d'une rangée à l'autre
                context.FillRectangle(y % 2 == 0 ? WoodA : WoodB, rect);
                context.FillRectangle(WoodLine, ToScreen(x * Cell, y * Cell + Cell - 1, Cell, 1));
                if ((x + y) % 3 == 0)
                    context.FillRectangle(WoodLine, ToScreen(x * Cell, y * Cell, 1, Cell));
            }
            else
            {
                context.FillRectangle((x / 2 + y / 2) % 2 == 0 ? CarpetA : CarpetB, rect);
                if (x % 4 == 0)
                    context.FillRectangle(CarpetLine, ToScreen(x * Cell, y * Cell, 1, Cell));
            }
        }
        // Séparation entre la partie privée et la partie travail
        if (Premises.LivingWidth > 0)
            context.FillRectangle(WallTrim, ToScreen(Premises.LivingWidth * Cell - 1, 0, 2, Premises.Height * Cell));
    }

    private void DrawWalls(DrawingContext context)
    {
        int width = Premises.Width * Cell;
        // Mur du fond, vu de face, avec fenêtres
        context.FillRectangle(WallFace, ToScreen(0, -WallHeight, width, WallHeight));
        context.FillRectangle(WallTrim, ToScreen(0, -4, width, 4));
        context.FillRectangle(WallTop, ToScreen(-4, -WallHeight - 4, width + 8, 4));
        var window = Atlas.Sprite("window");
        if (window != null)
            for (int x = 3; x + 2 < Premises.Width; x += 5)
                DrawSprite(context, window, x * Cell + 3, -WallHeight + 4);
        // Murs latéraux, vus de dessus
        context.FillRectangle(WallTop, ToScreen(-4, -WallHeight, 4, WallHeight + Premises.Height * Cell + 4));
        context.FillRectangle(WallTop, ToScreen(width, -WallHeight, 4, WallHeight + Premises.Height * Cell + 4));
    }

    private void DrawSouthWall(DrawingContext context)
    {
        int y = Premises.Height * Cell;
        int door = Premises.DoorX * Cell;
        context.FillRectangle(WallTop, ToScreen(-4, y, door + 4, 4));
        context.FillRectangle(WallTop, ToScreen(door + Cell, y, Premises.Width * Cell - door - Cell + 4, 4));
        context.FillRectangle(WallDark, ToScreen(door, y + 3, Cell, 1));
    }

    private void DrawSprite(DrawingContext context, AtlasSprite sprite, double x, double y, double opacity = 1)
    {
        var target = ToScreen(x, y, sprite.Rect.Width, sprite.Rect.Height);
        if (opacity < 1)
        {
            using (context.PushOpacity(opacity))
                context.DrawImage(Atlas.Image, sprite.Rect.ToRect(1), target);
        }
        else
            context.DrawImage(Atlas.Image, sprite.Rect.ToRect(1), target);
    }

    /// <summary>Position du sprite d'un meuble : centré sur son emprise, posé sur sa ligne du bas.</summary>
    private static (double X, double Y) SpritePosition(FurnitureDef def, AtlasSprite sprite, int x, int y)
    {
        double px = x * Cell + (def.Width * Cell - sprite.Rect.Width) / 2.0;
        double py = (y + def.Height) * Cell - sprite.Rect.Height;
        return (Math.Round(px), py);
    }

    private void CollectItems()
    {
        foreach (var item in Premises.Items)
        {
            var def = item.Def;
            var sprite = Atlas.Sprite(def.Id);
            if (sprite == null)
                continue;
            double key;
            double x, y;
            if (def.OnDesk)
            {
                // Sur le bureau : à gauche du plan de travail, posé sur le dessus
                var desk = Premises.Find(item.HostId ?? -1);
                if (desk == null)
                    continue;
                x = desk.X * Cell + 2;
                y = desk.Y * Cell + 9 - sprite.Rect.Height;
                key = (desk.Y + 1) * Cell + 0.2;
            }
            else
            {
                (x, y) = SpritePosition(def, sprite, item.X, item.Y);
                key = (item.Y + def.Height) * Cell + (def.Kind == FurnitureKind.Chair ? -0.5 : 0) + (def.Kind == FurnitureKind.Decor ? -100 : 0);
            }
            var bounds = new Rect(x, y, sprite.Rect.Width, sprite.Rect.Height);
            _drawn.Add(new Drawable(key, c => DrawSprite(c, sprite, x, y, _session.Tool?.MovingItemId == item.Id ? 0.35 : 1), bounds, SelectionKind.Item, item.Id));
        }
    }

    private (double X, double Y) PersonPosition(Person person)
    {
        if (person.Activity == Activity.Walking && person.Path.Count > 1 && person.PathIndex + 1 < person.Path.Count)
        {
            var a = person.Path[person.PathIndex];
            var b = person.Path[person.PathIndex + 1];
            double t = _session.Running ? _session.SubMinute : 0;
            return (a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);
        }
        return (person.Position.X, person.Position.Y);
    }

    private void CollectPeople()
    {
        foreach (var person in Campaign.People)
        {
            if (person.Activity is Activity.Away or Activity.Training)
                continue;
            var sprite = Atlas.Sprite($"person{person.Look % 5}");
            if (sprite == null)
                continue;
            var (cx, cy) = PersonPosition(person);
            bool seated = person.Activity == Activity.Working;
            double x = Math.Round(cx * Cell + Cell / 2.0 - sprite.Rect.Width / 2.0);
            double bottom = seated ? (cy + 1) * Cell + 6 : (cy + 1) * Cell - 2;
            if (person.Activity == Activity.Walking && (int)(_clock * 6 + person.Id) % 2 == 0)
                bottom -= 1;
            double y = Math.Round(bottom - sprite.Rect.Height);
            double key = (cy + 1) * Cell + 0.1;
            var bounds = new Rect(x, y, sprite.Rect.Width, sprite.Rect.Height);
            _drawn.Add(new Drawable(key, c =>
            {
                DrawSprite(c, sprite, x, y);
                DrawBubble(c, person, x + sprite.Rect.Width / 2.0, y - 2);
            }, bounds, SelectionKind.Person, person.Id));
        }
    }

    /// <summary>Petite icône au-dessus de la tête : ce que la personne fait vraiment en ce moment.</summary>
    private void DrawBubble(DrawingContext context, Person person, double centerX, double top)
    {
        string? icon = person.Activity switch
        {
            Activity.Break => "coffee",
            Activity.Idle => "warning",
            Activity.Working when person.Status.StartsWith("Bloqué") || person.Status.StartsWith("Sans") || person.Status.StartsWith("Rien") || person.Status.StartsWith("Aucun") => "hourglass",
            Activity.Working => person.Assignment.Kind switch
            {
                AssignmentKind.Support => "mail",
                AssignmentKind.Maintenance => "bolt",
                _ => CurrentDiscipline(person) switch
                {
                    Discipline.Design => "gem",
                    Discipline.Test => "check",
                    _ => "computer"
                }
            },
            _ => null
        };
        if (icon == null)
            return;
        var bitmap = Icons.Get(icon);
        double size = 10;
        var rect = ToScreen(centerX - size / 2, top - size - 1, size, size);
        context.FillRectangle(Palette.Ink, rect.Inflate(Scale));
        context.FillRectangle(Palette.Snow, rect.Inflate(Scale * 0.5));
        context.DrawImage(bitmap, new Rect(0, 0, bitmap.PixelSize.Width, bitmap.PixelSize.Height), rect);
    }

    private Discipline? CurrentDiscipline(Person person) =>
        person.CurrentWorkItemId is int id ? Campaign.FindWork(id)?.Discipline : null;

    private static readonly IBrush ValidFill = new SolidColorBrush(Color.Parse("#6063c74d"));
    private static readonly IBrush InvalidFill = new SolidColorBrush(Color.Parse("#70e43b44"));
    private static readonly IBrush LivingShade = new SolidColorBrush(Color.Parse("#50181425"));
    private static readonly IPen GridPen = new Pen(new SolidColorBrush(Color.Parse("#30ffffff")), 1);

    private void DrawBuildOverlay(DrawingContext context)
    {
        // Grille légère, partie privée assombrie
        for (int x = 0; x <= Premises.Width; x++)
        {
            var a = ToScreen(x * Cell, 0, 0, 0);
            var b = ToScreen(x * Cell, Premises.Height * Cell, 0, 0);
            context.DrawLine(GridPen, a.TopLeft, b.TopLeft);
        }
        for (int y = 0; y <= Premises.Height; y++)
        {
            var a = ToScreen(0, y * Cell, 0, 0);
            var b = ToScreen(Premises.Width * Cell, y * Cell, 0, 0);
            context.DrawLine(GridPen, a.TopLeft, b.TopLeft);
        }
        if (Premises.LivingWidth > 0)
            context.FillRectangle(LivingShade, ToScreen(0, 0, Premises.LivingWidth * Cell, Premises.Height * Cell));
        context.FillRectangle(ValidFill, ToScreen(Premises.Entrance.X * Cell, Premises.Entrance.Y * Cell, Cell, Cell));

        // État de chaque poste
        foreach (var desk in Premises.Desks)
        {
            var issue = Layout.Check(Premises, desk);
            var icon = Icons.Get(issue == WorkstationIssue.None ? "check" : "warning");
            context.DrawImage(icon, new Rect(0, 0, icon.PixelSize.Width, icon.PixelSize.Height), ToScreen(desk.X * Cell + 10, desk.Y * Cell - 30, 12, 12));
        }

        // Fantôme de l'objet à poser
        if (_session.Tool is { } tool && _hover is { } cell)
        {
            var def = FurnitureCatalog.Get(tool.DefId);
            var check = Layout.CanPlace(Premises, def, cell.X, cell.Y, tool.MovingItemId);
            if (def.OnDesk)
            {
                context.FillRectangle(check.Ok ? ValidFill : InvalidFill, ToScreen(cell.X * Cell, cell.Y * Cell, Cell, Cell));
            }
            else
            {
                foreach (var c in Premises.Footprint(def, cell.X, cell.Y))
                    context.FillRectangle(check.Ok ? ValidFill : InvalidFill, ToScreen(c.X * Cell, c.Y * Cell, Cell, Cell));
                if (def.Kind == FurnitureKind.Desk)
                    context.DrawRectangle(new Pen(Palette.Lemon, 1), ToScreen(cell.X * Cell + 2, (cell.Y - 1) * Cell + 2, Cell - 4, Cell - 4));
                var sprite = Atlas.Sprite(def.Id);
                if (sprite != null)
                {
                    var (x, y) = SpritePosition(def, sprite, cell.X, cell.Y);
                    DrawSprite(context, sprite, x, y, 0.75);
                }
            }
        }
    }

    private void DrawSelection(DrawingContext context)
    {
        var pen = new Pen(Palette.Gold, Math.Max(1, Scale / 1.5));
        if (_session.SelectionKind == SelectionKind.Item && Premises.Find(_session.SelectionId) is { } item)
        {
            if (item.Def.OnDesk)
                item = Premises.Find(item.HostId ?? -1) ?? item;
            foreach (var c in Premises.Footprint(item))
                context.DrawRectangle(pen, ToScreen(c.X * Cell, c.Y * Cell, Cell, Cell));
            if (item.Def.Kind == FurnitureKind.Desk)
                context.DrawRectangle(new Pen(Palette.Lemon, 1, DashStyle.Dash), ToScreen(item.X * Cell, (item.Y - 1) * Cell, Cell, Cell));
        }
        else if (_session.SelectionKind == SelectionKind.Person)
        {
            var drawn = _drawn.FirstOrDefault(d => d.Kind == SelectionKind.Person && d.Id == _session.SelectionId);
            if (drawn != null)
                context.DrawRectangle(pen, ToScreen(drawn.Bounds.X - 1, drawn.Bounds.Y - 1, drawn.Bounds.Width + 2, drawn.Bounds.Height + 2));
        }

        if (!_session.BuildMode && _hover is { } hover && Premises.InBounds(hover))
            context.DrawRectangle(new Pen(new SolidColorBrush(Color.Parse("#60ffffff")), 1), ToScreen(hover.X * Cell, hover.Y * Cell, Cell, Cell));
    }

    private void DrawNight(DrawingContext context)
    {
        int minute = GameTime.MinuteOfDay(Campaign.Minute);
        double hour = minute / 60.0;
        double darkness = hour < 6 ? 0.45 : hour < 8 ? 0.45 * (8 - hour) / 2 : hour > 21 ? 0.45 : hour > 18 ? 0.45 * (hour - 18) / 3 : 0;
        if (darkness > 0.01)
            context.FillRectangle(new SolidColorBrush(Color.FromArgb((byte)(darkness * 255), 0x12, 0x14, 0x30)), new Rect(Bounds.Size));
    }

    // ------------------------------------------------------------------ Souris

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var point = e.GetPosition(this);
        if (_panFrom is { } from)
        {
            Pan(from.X - point.X, from.Y - point.Y);
            _panFrom = point;
        }
        var cell = CellAt(point);
        _hoverPinned = false;
        _hover = Premises.InBounds(cell) ? cell : null;
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        if (!_hoverPinned)
            _hover = null;
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        ZoomBy(e.Delta.Y > 0 ? 1 : -1, e.GetPosition(this));
        e.Handled = true;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        Focus();
        var point = e.GetCurrentPoint(this);
        if (point.Properties.IsRightButtonPressed || point.Properties.IsMiddleButtonPressed)
        {
            if (point.Properties.IsRightButtonPressed && _session.Tool != null)
            {
                _session.SetTool(null);
                e.Handled = true;
                return;
            }
            _panFrom = point.Position;
            e.Handled = true;
            return;
        }
        if (!point.Properties.IsLeftButtonPressed)
            return;

        var cell = CellAt(point.Position);
        if (_session.Tool is { } tool)
        {
            Place(tool, cell);
        }
        else
        {
            var world = ToWorld(point.Position);
            var hit = _drawn.OrderByDescending(d => d.Key).FirstOrDefault(d => d.Bounds.Contains(world));
            if (hit != null)
            {
                _session.Select(hit.Kind, hit.Id);
                Sfx.Play(Sound.Click);
            }
            else
                _session.ClearSelection();
        }
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _panFrom = null;
    }

    private void Place(BuildTool tool, Cell cell)
    {
        CommandResult result;
        if (tool.MovingItemId is int moving)
            result = Campaign.Move(moving, cell.X, cell.Y);
        else if (tool.StoredId is int stored)
            result = Campaign.PlaceFromStorage(stored, cell.X, cell.Y);
        else
            result = Campaign.BuyAndPlace(tool.DefId, cell.X, cell.Y);

        Sfx.Play(result.Ok ? Sound.Place : Sound.Error);
        if (!result.Ok)
        {
            Feedback?.Invoke(result.Message, false);
            return;
        }
        // Après un déplacement ou un objet des cartons, l'outil se range ; un achat peut se répéter
        if (tool.MovingItemId != null || tool.StoredId != null)
            _session.SetTool(null);
        Feedback?.Invoke(result.Message, true);
    }
}
