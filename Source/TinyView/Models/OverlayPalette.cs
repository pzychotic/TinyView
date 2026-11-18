using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TinyView.Models;

/// <summary>
/// Palette of the Indexed8 overlay layer drawn on top of the palette-mapped image.
/// Index 0 is fully transparent, so only marked pixels cover the image below.
/// Unused entries are transparent and reserved for future overlay kinds (e.g. clipping).
/// </summary>
public static class OverlayPalette
{
    /// <summary>Index for pixels that show the image unchanged.</summary>
    public const byte TransparentIndex = 0;

    /// <summary>Index for NaN and ±Infinity pixels.</summary>
    public const byte NonFiniteIndex = 1;

    /// <summary>
    /// Highlight color for non-finite pixels. Magenta is far from every entry of the
    /// built-in palettes and distinct from the lime selection border.
    /// </summary>
    public static readonly Color NonFiniteColor = Color.FromRgb(255, 0, 255);

    public static readonly BitmapPalette Palette = CreatePalette();

    private static BitmapPalette CreatePalette()
    {
        var colors = Enumerable.Repeat(Colors.Transparent, 256).ToArray();
        colors[NonFiniteIndex] = NonFiniteColor;
        return new BitmapPalette(colors);
    }
}
