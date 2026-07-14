using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace TinyView.Behaviors;

/// <summary>
/// Shared display-to-pixel coordinate conversion for behaviors attached to an
/// <see cref="Image"/> that displays a <see cref="BitmapSource"/>.
/// </summary>
internal static class ImagePixelHelper
{
    /// <summary>
    /// Converts the mouse position to continuous (unclamped) bitmap pixel
    /// coordinates, or null when the image has no bitmap source or no layout size yet.
    /// Callers decide how to round and how to treat positions outside the bitmap.
    /// </summary>
    public static (Point Position, BitmapSource Bitmap)? GetPixelPosition(Image image, MouseEventArgs e)
    {
        if (image.Source is not BitmapSource bmp)
            return null;

        double displayWidth = image.ActualWidth;
        double displayHeight = image.ActualHeight;
        if (displayWidth <= 0 || displayHeight <= 0)
            return null;

        var pos = e.GetPosition(image);
        return (new Point(pos.X * bmp.PixelWidth / displayWidth, pos.Y * bmp.PixelHeight / displayHeight), bmp);
    }
}
