using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using UI.Iso;
using UI.Pixel;

namespace UI.Scene;

/// <summary>
/// La scène du local en vue isométrique 2:1 (cases de 64 × 32 px, X = (U − V)·32, Y = (U + V)·16), composée par
/// RoomRenderer comme le compositeur de référence du pack. L'image de la pièce n'est recomposée que si le local ou les
/// personnes assises changent ; à chaque image, on la dessine au zoom entier puis on ajoute les bulles, la sélection,
/// le survol et, en aménagement, le fantôme de l'objet à poser. Caméra sans rotation ; les objets tournent (touche R).
/// </summary>
public class SceneCanvas : Control
{
    private readonly GameSession _session;
    private double _camX, _camY;
    private int _zoom = 2;
    private bool _cameraPlaced;
    private bool _userMoved;
    private Point? _panFrom;
    private Cell? _hover;
    private bool _hoverPinned;
    private double _clock;

    private RoomImage? _room;
    private Bitmap? _bitmap;
    private string _signature = "";
    private readonly Dictionary<string, (Bitmap Image, int AnchorX, int AnchorY)> _ghosts = new();

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

    public void Tick(double deltaTime)
    {
        _clock += deltaTime;
        InvalidateVisual();
    }

    // ------------------------------------------------------------------ Image de la pièce

    /// <summary>Qui est assis où en ce moment (chaise → personne), pour dessiner les personnages à leur poste.</summary>
    private Dictionary<int, Seated> SeatedPeople()
    {
        var seated = new Dictionary<int, Seated>();
        foreach (var person in Campaign.People.Where(p => p.Activity == Activity.Working))
            if (Premises.SeatOf(person.Id) is { } post && Premises.ChairOf(post) is { } chair)
                seated[chair.Id] = new Seated(person.Id, person.Appearance);
        return seated;
    }

    private string Signature(Dictionary<int, Seated> seated)
    {
        var sb = new StringBuilder(Premises.PlanId);
        foreach (var i in Premises.Items)
            sb.Append('|').Append(i.Id).Append(i.DefId).Append(i.X).Append(',').Append(i.Y).Append(i.Facing).Append(i.HostId).Append(i.Socket)
                .Append(string.Join(",", i.Swatches.Select(kv => kv.Key + kv.Value)));
        foreach (var (chair, who) in seated.OrderBy(k => k.Key))
            sb.Append('#').Append(chair).Append(':').Append(who.PersonId).Append(who.Look.Base).Append(who.Look.Hair).Append(who.Look.HairRamp).Append(who.Look.Skin);
        return sb.ToString();
    }

    private void Refresh()
    {
        var seated = SeatedPeople();
        string signature = Signature(seated);
        if (signature == _signature && _bitmap != null)
            return;
        _signature = signature;
        _room = RoomRenderer.Render(Premises, seated);
        _bitmap = _room.Image.ToBitmap();
    }

    // ------------------------------------------------------------------ Caméra

    private double Scale => _zoom / (TopLevel.GetTopLevel(this)?.RenderScaling ?? 1);

    /// <summary>Recentre la vue sur le local, au plus grand zoom entier où il tient (appelé après un déménagement).</summary>
    public void ResetCamera()
    {
        Refresh();
        if (_room == null)
            return;
        _camX = _room.Image.W / 2.0;
        _camY = _room.Image.H / 2.0;
        double scaling = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1;
        // L'image a des marges : on accepte qu'elles débordent un peu pour garder le plus grand zoom entier
        double fitW = Bounds.Width * scaling / (_room.Image.W - 60);
        double fitH = Bounds.Height * scaling * 0.92 / (_room.Image.H - 60);
        _zoom = Math.Clamp((int)Math.Floor(Math.Min(fitW, fitH)), 1, 4);
        _cameraPlaced = Bounds.Width > 0;
        _userMoved = false;
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        if (!_userMoved)
            ResetCamera();
    }

    public void Pan(double dx, double dy)
    {
        _userMoved = true;
        if (_room == null)
            return;
        _camX = Math.Clamp(_camX + dx / Scale, 0, _room.Image.W);
        _camY = Math.Clamp(_camY + dy / Scale, 0, _room.Image.H);
    }

    public void ZoomBy(int delta, Point? around = null)
    {
        _userMoved = true;
        var anchor = around ?? new Point(Bounds.Width / 2, Bounds.Height / 2);
        var before = ToWorld(anchor);
        _zoom = Math.Clamp(_zoom + delta, 1, 4);
        var after = ToWorld(anchor);
        _camX += before.X - after.X;
        _camY += before.Y - after.Y;
    }

    /// <summary>Position écran du coin haut-gauche de l'image de la pièce, arrondie au pixel physique (sprites nets).</summary>
    private Point Origin()
    {
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

    private Point ToScreen(double x, double y)
    {
        var origin = Origin();
        return new Point(origin.X + x * Scale, origin.Y + y * Scale);
    }

    /// <summary>Coin nord d'une case, en pixels de l'image de la pièce.</summary>
    private (double X, double Y) Tile(double u, double v) =>
        (_room!.OriginX + (u - v) * 32, _room.OriginY + (u + v) * 16);

    private Cell CellAt(Point screen)
    {
        var world = ToWorld(screen);
        double dx = world.X - _room!.OriginX, dy = world.Y - _room.OriginY;
        return new Cell((int)Math.Floor((dx / 32 + dy / 16) / 2), (int)Math.Floor((dy / 16 - dx / 32) / 2));
    }

    private StreamGeometry Diamond(Cell cell)
    {
        var geometry = new StreamGeometry();
        using var ctx = geometry.Open();
        var (ax, ay) = Tile(cell.X, cell.Y);
        var (bx, by) = Tile(cell.X + 1, cell.Y);
        var (cx, cy) = Tile(cell.X + 1, cell.Y + 1);
        var (dx, dy) = Tile(cell.X, cell.Y + 1);
        ctx.BeginFigure(ToScreen(ax, ay), true);
        ctx.LineTo(ToScreen(bx, by));
        ctx.LineTo(ToScreen(cx, cy));
        ctx.LineTo(ToScreen(dx, dy));
        ctx.EndFigure(true);
        return geometry;
    }

    private StreamGeometry Polygon((double X, double Y)[] points)
    {
        var geometry = new StreamGeometry();
        using var ctx = geometry.Open();
        ctx.BeginFigure(ToScreen(points[0].X, points[0].Y), true);
        foreach (var p in points.Skip(1))
            ctx.LineTo(ToScreen(p.X, p.Y));
        ctx.EndFigure(true);
        return geometry;
    }

    // ------------------------------------------------------------------ Rendu

    private static readonly IBrush Backdrop = Palette.Brush("#2b2128");
    private static readonly IBrush ValidFill = new SolidColorBrush(Color.Parse("#7062a04b"));
    private static readonly IBrush InvalidFill = new SolidColorBrush(Color.Parse("#80c8453d"));
    private static readonly IBrush EntranceFill = new SolidColorBrush(Color.Parse("#50357c82"));
    private static readonly IPen GridPen = new Pen(new SolidColorBrush(Color.Parse("#40fbf5e6")), 1);
    private static readonly IPen HoverPen = new Pen(new SolidColorBrush(Color.Parse("#a0fbf5e6")), 1);

    public override void Render(DrawingContext context)
    {
        Refresh();
        if (!_cameraPlaced && Bounds.Width > 0)
            ResetCamera();
        context.FillRectangle(Backdrop, new Rect(Bounds.Size));
        if (_room == null || _bitmap == null)
            return;
        using var _ = context.PushRenderOptions(new RenderOptions { BitmapInterpolationMode = BitmapInterpolationMode.None, EdgeMode = EdgeMode.Aliased });

        var origin = Origin();
        context.DrawImage(_bitmap, new Rect(0, 0, _bitmap.PixelSize.Width, _bitmap.PixelSize.Height),
            new Rect(origin.X, origin.Y, _room.Image.W * Scale, _room.Image.H * Scale));

        if (_session.BuildMode)
            DrawBuildOverlay(context);
        DrawSelection(context);
        DrawBubbles(context);
        DrawNight(context);
    }

    /// <summary>
    /// Au-dessus de la tête : une icône de ce que la personne fait vraiment (terminal, design, code, tests, support,
    /// maintenance, attente) et la barre de sa tâche. Qui n'a pas de poste attend à l'entrée, avec un avertissement.
    /// </summary>
    private void DrawBubbles(DrawingContext context)
    {
        int waiting = 0;
        foreach (var person in Campaign.People)
        {
            if (person.Activity is Activity.Away or Activity.Training)
                continue;
            double x, y;
            if (person.Activity == Activity.Working && _room!.Heads.TryGetValue(person.Id, out var head))
                (x, y) = (head.X, head.Y);
            else
            {
                var (ex, ey) = Tile(Premises.Entrance.X + 0.5, Premises.Entrance.Y + 0.5);
                (x, y) = (ex + waiting * 14, ey - 8);
                waiting++;
            }
            string? icon = person.Activity switch
            {
                Activity.Idle => "icon_warning",
                Activity.Working when person.IsFounder && Campaign.Terminal != null => "icon_terminal",
                Activity.Working when person.Status.Contains("répond") => "icon_support",
                Activity.Working when person.Status.Contains("maintenance") => "icon_maintenance",
                Activity.Working when person.CurrentWorkItemId == null => "icon_time",
                Activity.Working => CurrentDiscipline(person) switch
                {
                    Discipline.Design => "icon_design",
                    Discipline.Test => "icon_test",
                    _ => "icon_code"
                },
                _ => null
            };
            if (icon == null)
                continue;
            string tint = icon == "icon_warning" ? "#c8453d" : icon == "icon_terminal" ? "#357c82" : "#2b2128";
            var bitmap = PackLibrary.Icon(icon, tint);
            var top = ToScreen(x - 9, y - 22);
            var rect = new Rect(top.X, top.Y, 18 * Scale, 18 * Scale);
            context.FillRectangle(Palette.Brush("#2b2128"), rect.Inflate(Scale));
            context.FillRectangle(Palette.Brush("#fbf5e6"), rect);
            context.DrawImage(bitmap, new Rect(0, 0, 16, 16), rect.Deflate(Scale));

            if (person.CurrentWorkItemId is int id && Campaign.FindWork(id) is { } item)
            {
                var track = new Rect(ToScreen(x - 10, y - 3), new Size(20 * Scale, 2 * Scale));
                context.FillRectangle(Palette.Brush("#2b2128"), track.Inflate(Scale * 0.5));
                context.FillRectangle(Palette.Brush("#ecb841"), new Rect(track.X, track.Y, track.Width * item.Fraction, track.Height));
            }
        }
    }

    private Discipline? CurrentDiscipline(Person person) =>
        person.CurrentWorkItemId is int id ? Campaign.FindWork(id)?.Discipline : null;

    private (Bitmap Image, int AnchorX, int AnchorY) Ghost(string defId, string facing)
    {
        string key = defId + facing;
        if (_ghosts.TryGetValue(key, out var ghost))
            return ghost;
        var o = PackLibrary.Orient(PackLibrary.Asset(defId), facing);
        var (ax, ay) = PackLibrary.Anchor(o);
        var img = PackLibrary.Layer(o.GetProperty("layers").GetProperty("base"), new Dictionary<string, string>());
        return _ghosts[key] = (img.ToBitmap(), ax, ay);
    }

    private void DrawBuildOverlay(DrawingContext context)
    {
        // Grille des cases, entrée à laisser libre
        for (int u = 0; u < Premises.Width; u++)
            for (int v = 0; v < Premises.Height; v++)
                context.DrawGeometry(null, GridPen, Diamond(new Cell(u, v)));
        context.DrawGeometry(EntranceFill, null, Diamond(Premises.Entrance));

        // État de chaque poste : coche ou avertissement au-dessus du bureau
        foreach (var post in Premises.Workstations)
        {
            var issue = Layout.Check(Premises, post);
            var seat = Premises.SeatCell(post);
            var (sx, sy) = Tile(seat.X + 0.5, seat.Y + 0.5);
            var icon = PackLibrary.Icon(issue == WorkstationIssue.None ? "icon_success" : "icon_warning", issue == WorkstationIssue.None ? "#62a04b" : "#c8453d");
            var at = ToScreen(sx - 8, sy - 70);
            context.DrawImage(icon, new Rect(0, 0, 16, 16), new Rect(at.X, at.Y, 16 * Scale, 16 * Scale));
        }

        if (_session.Tool is not { } tool || _hover is not { } cell)
            return;
        var def = FurnitureCatalog.Get(tool.DefId);
        if (def.Attached)
        {
            // Objet à accrocher : on survole le meuble qui le recevra
            var host = HostAt(cell);
            bool ok = host != null && Layout.FreeSocket(Premises, def, host, tool.MovingItemId) != null;
            if (host != null && _room!.Hits.FirstOrDefault(h => !h.Person && h.Id == host.Id) is { } hit)
                context.DrawGeometry(ok ? ValidFill : InvalidFill, null, Polygon(hit.Polygon));
            else
                context.DrawGeometry(InvalidFill, null, Diamond(cell));
            return;
        }
        string facing = tool.Facing ?? def.Pack.DefaultFacing;
        var check = Layout.CanPlace(Premises, def, cell.X, cell.Y, facing, tool.MovingItemId);
        var cells = Premises.Cells(def, cell.X, cell.Y, facing).ToList();
        foreach (var c in cells.Count > 0 ? cells : [cell])
            context.DrawGeometry(check.Ok ? ValidFill : InvalidFill, null, Diamond(c));
        foreach (var c in Premises.AccessCells(def, cell.X, cell.Y, facing))
            context.DrawGeometry(null, new Pen(Palette.Brush("#ecb841"), 1, DashStyle.Dash), Diamond(c));
        var (image, ax2, ay2) = Ghost(def.Id, facing);
        var (tx, ty) = def.Pack.Centered ? Tile(cell.X + 0.5, cell.Y + 0.5) : Tile(cell.X, cell.Y);
        var pos = ToScreen(Math.Round(tx) - ax2, Math.Round(ty) - ay2);
        using (context.PushOpacity(0.7))
            context.DrawImage(image, new Rect(0, 0, image.PixelSize.Width, image.PixelSize.Height),
                new Rect(pos.X, pos.Y, image.PixelSize.Width * Scale, image.PixelSize.Height * Scale));
    }

    /// <summary>Le meuble au sol sur cette case (pour y accrocher un objet), le plus proche d'abord.</summary>
    private PlacedItem? HostAt(Cell cell) =>
        Premises.Floor.Where(i => Premises.Cells(i).Contains(cell) || i.X == cell.X && i.Y == cell.Y)
            .OrderByDescending(i => i.Def.Pack.Facing(i.Facing).Sockets.Count).FirstOrDefault();

    private void DrawSelection(DrawingContext context)
    {
        var pen = new Pen(Palette.Brush("#ecb841"), Math.Max(1, Scale));
        if (_session.SelectionKind == SelectionKind.Item && Premises.Find(_session.SelectionId) is { } item)
        {
            var root = Premises.Root(item);
            if (_room!.Hits.FirstOrDefault(h => !h.Person && h.Id == root.Id) is { } hit)
                context.DrawGeometry(null, pen, Polygon(hit.Polygon));
        }
        else if (_session.SelectionKind == SelectionKind.Person && _room!.Hits.FirstOrDefault(h => h.Person && h.Id == _session.SelectionId) is { } person)
            context.DrawGeometry(null, pen, Polygon(person.Polygon));

        if (!_session.BuildMode && _hover is { } hover && Premises.InBounds(hover))
            context.DrawGeometry(null, HoverPen, Diamond(hover));
    }

    private void DrawNight(DrawingContext context)
    {
        double hour = GameTime.MinuteOfDay(Campaign.Minute) / 60.0;
        double darkness = hour < 6 ? 0.4 : hour < 8 ? 0.4 * (8 - hour) / 2 : hour > 21 ? 0.4 : hour > 18 ? 0.4 * (hour - 18) / 3 : 0;
        if (darkness > 0.01)
            context.FillRectangle(new SolidColorBrush(Color.FromArgb((byte)(darkness * 255), 0x16, 0x22, 0x35)), new Rect(Bounds.Size));
    }

    // ------------------------------------------------------------------ Souris

    private static bool Inside((double X, double Y)[] polygon, Point p)
    {
        bool inside = false;
        for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            if ((polygon[i].Y > p.Y) != (polygon[j].Y > p.Y)
                && p.X < (polygon[j].X - polygon[i].X) * (p.Y - polygon[i].Y) / (polygon[j].Y - polygon[i].Y) + polygon[i].X)
                inside = !inside;
        return inside;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var point = e.GetPosition(this);
        if (_panFrom is { } from)
        {
            Pan(from.X - point.X, from.Y - point.Y);
            _panFrom = point;
        }
        if (_room == null)
            return;
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
        if (!point.Properties.IsLeftButtonPressed || _room == null)
            return;

        var cell = CellAt(point.Position);
        if (_session.Tool is { } tool)
            Place(tool, cell);
        else
        {
            var world = ToWorld(point.Position);
            var hit = _room.Hits.OrderByDescending(h => h.Order).FirstOrDefault(h => Inside(h.Polygon, world));
            if (hit != null)
            {
                _session.Select(hit.Person ? SelectionKind.Person : SelectionKind.Item, hit.Id);
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
        var def = FurnitureCatalog.Get(tool.DefId);
        string? facing = tool.Facing;
        CommandResult result;
        if (def.Attached)
        {
            var host = HostAt(cell);
            result = host == null ? CommandResult.Fail("Clique un bureau ou un meuble qui peut le recevoir.")
                : tool.StoredId is int storedId ? Campaign.AttachFromStorage(storedId, host.Id)
                : Campaign.BuyAndAttach(tool.DefId, host.Id);
        }
        else if (tool.MovingItemId is int moving)
            result = Campaign.Move(moving, cell.X, cell.Y, facing);
        else if (tool.StoredId is int stored)
            result = Campaign.PlaceFromStorage(stored, cell.X, cell.Y, facing);
        else
            result = Campaign.BuyAndPlace(tool.DefId, cell.X, cell.Y, facing);

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
