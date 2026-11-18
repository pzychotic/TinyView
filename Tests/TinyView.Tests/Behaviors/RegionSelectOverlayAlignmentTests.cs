using Microsoft.Xaml.Behaviors;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TinyView.Behaviors;

namespace TinyView.Tests.Behaviors;

/// <summary>
/// Renders the selection overlay on top of a zoomed image, mirroring the MainWindow
/// layout (inherited layout rounding + a zoom LayoutTransform on the image container),
/// and checks that the lime border lands on the same device pixels as the image pixel
/// it is placed on.
/// </summary>
/// <remarks>
/// WPF layout rounding uses the process' system DPI (VisualTreeHelper.SetRootDpi does
/// not influence it), so the misalignment this guards against only shows up when the
/// tests run at a non-100% display scale (e.g. 125% or 150%). At 100% the test passes
/// regardless.
/// </remarks>
[TestFixture]
public class RegionSelectOverlayAlignmentTests
{
    private const int ImageSize = 12;

    private static BitmapSource CreateCheckerboard()
    {
        var pixels = new byte[ImageSize * ImageSize];
        for (int y = 0; y < ImageSize; y++)
            for (int x = 0; x < ImageSize; x++)
                pixels[y * ImageSize + x] = (byte)(((x + y) & 1) == 0 ? 0x00 : 0xFF);
        return BitmapSource.Create(ImageSize, ImageSize, 96, 96, PixelFormats.Gray8, null, pixels, ImageSize);
    }

    private static (byte[] Pixels, int Width, double DpiScale) Render(double zoom, PixelRectSpec sel)
    {
        var image = new Image { Source = CreateCheckerboard(), Stretch = Stretch.Fill };
        RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);

        var container = new Grid
        {
            Width = ImageSize,
            Height = ImageSize,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            LayoutTransform = new ScaleTransform(zoom, zoom),
        };
        container.Children.Add(image);

        var root = new Grid { UseLayoutRounding = true, SnapsToDevicePixels = true, Background = Brushes.Black };
        root.Children.Add(container);

        var behavior = new RegionSelectBehavior();
        Interaction.GetBehaviors(image).Add(behavior);

        double size = Math.Ceiling(ImageSize * zoom) + 2;
        void Layout()
        {
            root.Measure(new Size(size, size));
            root.Arrange(new Rect(0, 0, size, size));
            root.UpdateLayout();
        }

        Layout();

        // Drive the real overlay code path (the mouse handlers end up here).
        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
        typeof(RegionSelectBehavior).GetMethod("EnsureOverlay", flags)!.Invoke(behavior, null);
        typeof(RegionSelectBehavior).GetMethod("UpdateSelectionRect", flags)!
            .Invoke(behavior, [new Point(sel.X, sel.Y), new Point(sel.X + sel.W, sel.Y + sel.H)]);

        Layout();

        double dpiScale = VisualTreeHelper.GetDpi(root).DpiScaleX;
        int px = (int)Math.Ceiling(size * dpiScale);
        var rtb = new RenderTargetBitmap(px, px, 96 * dpiScale, 96 * dpiScale, PixelFormats.Pbgra32);
        rtb.Render(root);
        var data = new byte[px * px * 4];
        rtb.CopyPixels(data, px * 4, 0);
        return (data, px, dpiScale);
    }

    private readonly record struct PixelRectSpec(int X, int Y, int W, int H);

    private static bool IsLime(byte[] p, int w, int x, int y)
    {
        int i = (y * w + x) * 4;
        return p[i + 1] > 128 && p[i + 2] < 64 && p[i] < 64;
    }

    private static byte Gray(byte[] p, int w, int x, int y) => p[(y * w + x) * 4];

    /// <summary>Device columns [Start, End) of image column <paramref name="ix"/>, read off a row without overlay.</summary>
    private static (int Start, int End) ImageColumnSpan(byte[] p, int w, int row, int ix)
    {
        var bounds = new List<int> { 0 };
        for (int x = 1; x < w; x++)
            if (Gray(p, w, x, row) != Gray(p, w, x - 1, row))
                bounds.Add(x);
        return (bounds[ix], bounds[ix + 1]);
    }

    private static (int Start, int End) LimeSpan(byte[] p, int w, int row, int toX)
    {
        int start = -1, end = -1;
        for (int x = 0; x < toX; x++)
        {
            if (!IsLime(p, w, x, row))
                continue;
            if (start < 0) start = x;
            end = x + 1;
        }
        return (start, end);
    }

    // Integer zoom factors map image pixels to whole device pixels at common DPI
    // scales, so the border must match exactly. Fractional (mouse-wheel) factors get
    // one device pixel of slack for stroke rasterization; the layout-rounding bug
    // shifted the border by about 1/3 image pixel, which is several device pixels here.
    [TestCase(4.0, 0)]
    [TestCase(6.0, 0)]
    [TestCase(10.0, 0)]
    [TestCase(3.1384283767, 1)]  // 1.1^12
    [TestCase(6.7274999493, 1)]  // 1.1^20
    [TestCase(10.834705943, 1)]  // 1.1^25
    public void SelectionLeftBorder_IsAlignedWithImagePixel(double zoom, int toleranceDevicePixels)
    {
        var failures = new List<string>();
        double dpi = 1;
        for (int selX = 1; selX <= 6; selX++)
        {
            var (p, w, dpiScale) = Render(zoom, new PixelRectSpec(selX, 2, 4, 6));
            dpi = dpiScale;
            double scale = zoom * dpiScale;

            var img = ImageColumnSpan(p, w, (int)(0.5 * scale), selX);           // image row 0: no overlay
            var lime = LimeSpan(p, w, (int)(4.5 * scale), (int)((selX + 2) * scale)); // image row 4: left border

            if (Math.Abs(img.Start - lime.Start) > toleranceDevicePixels || Math.Abs(img.End - lime.End) > toleranceDevicePixels)
                failures.Add($"selX={selX}: image cols [{img.Start},{img.End}) vs border cols [{lime.Start},{lime.End})");
        }

        Assert.That(failures, Is.Empty, $"zoom={zoom}, dpiScale={dpi}:\n" + string.Join("\n", failures));
    }
}
