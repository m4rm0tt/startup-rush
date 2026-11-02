using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;

namespace UI.Hud;

/// <summary>
/// Une image du pack qui suit l'état du bouton qui la contient (normal, survol, appui). L'état se lit sur le bouton :
/// aucun gestionnaire n'écrit l'image, si bien qu'une mise à jour du menu (UiPatch) ne la remet jamais à « normal » sous la souris.
/// </summary>
public class HoverImage : Image
{
    public static readonly StyledProperty<IImage?> NormalProperty = AvaloniaProperty.Register<HoverImage, IImage?>(nameof(Normal));
    public static readonly StyledProperty<IImage?> HoverProperty = AvaloniaProperty.Register<HoverImage, IImage?>(nameof(Hover));
    public static readonly StyledProperty<IImage?> PressedProperty = AvaloniaProperty.Register<HoverImage, IImage?>(nameof(Pressed));

    private Button? _owner;

    public HoverImage()
    {
        Stretch = Stretch.Uniform;
        RenderOptions.SetBitmapInterpolationMode(this, BitmapInterpolationMode.None);
    }

    public IImage? Normal { get => GetValue(NormalProperty); set => SetValue(NormalProperty, value); }
    public IImage? Hover { get => GetValue(HoverProperty); set => SetValue(HoverProperty, value); }
    public IImage? Pressed { get => GetValue(PressedProperty); set => SetValue(PressedProperty, value); }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _owner = this.FindAncestorOfType<Button>();
        if (_owner != null)
            _owner.PropertyChanged += OwnerChanged;
        Update();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (_owner != null)
            _owner.PropertyChanged -= OwnerChanged;
        _owner = null;
    }

    private void OwnerChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == IsPointerOverProperty || e.Property == Button.IsPressedProperty || e.Property == IsEnabledProperty)
            Update();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == NormalProperty || change.Property == HoverProperty || change.Property == PressedProperty)
            Update();
    }

    private void Update()
    {
        var owner = _owner;
        var source = owner is { IsEnabled: true, IsPressed: true } && Pressed != null ? Pressed
            : owner is { IsEnabled: true, IsPointerOver: true } && Hover != null ? Hover
            : Normal;
        SetCurrentValue(SourceProperty, source);
    }
}
