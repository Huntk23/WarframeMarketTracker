using Avalonia;
using Avalonia.Controls;
using WarframeMarketTracker.Services;

namespace WarframeMarketTracker.Controls;

public class CachedImage
{
    private static IThumbnailCache? _cache;

    public static readonly AttachedProperty<string?> ThumbPathProperty =
        AvaloniaProperty.RegisterAttached<CachedImage, Image, string?>("ThumbPath");

    static CachedImage()
    {
        ThumbPathProperty.Changed.AddClassHandler<Image>(OnThumbPathChanged);
    }

    public static void Initialize(IThumbnailCache cache) => _cache = cache;

    public static string? GetThumbPath(Image obj) => obj.GetValue(ThumbPathProperty);
    public static void SetThumbPath(Image obj, string? value) => obj.SetValue(ThumbPathProperty, value);

    private CachedImage() { }

    private static async void OnThumbPathChanged(Image image, AvaloniaPropertyChangedEventArgs e)
    {
        // Clear immediately so a recycled container doesn't briefly show the previous item's thumb
        image.Source = null;

        var path = e.GetNewValue<string?>();
        if (string.IsNullOrEmpty(path) || _cache == null) return;

        var bitmap = await _cache.GetAsync(path);
        if (bitmap == null) return;

        // Container may have been recycled to a different item while loading
        if (GetThumbPath(image) != path) return;
        image.Source = bitmap;
    }
}