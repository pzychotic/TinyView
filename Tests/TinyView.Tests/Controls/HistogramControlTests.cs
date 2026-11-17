using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TinyView.Controls;
using TinyView.Models;
using TinyView.Views;

namespace TinyView.Tests.Controls;

[TestFixture]
public class HistogramControlTests
{
    [Test]
    public void Render_WithHistogramAndPalette_DoesNotThrow()
    {
        var provider = new RawImageData<float>(4, 1, [0f, 0.25f, 0.5f, 1f], "R32F");
        var control = new HistogramControl
        {
            Histogram = provider.ComputeHistogram(64),
            RangeMinimum = provider.Range.Min,
            RangeMaximum = provider.Range.Max,
            BlackPoint = 0.2,
            WhitePoint = 0.8,
            Palette = ColorPalettes.Palettes[0].Palette,
        };

        control.Measure(new Size(200, 100));
        control.Arrange(new Rect(0, 0, 200, 100));

        var bitmap = new RenderTargetBitmap(200, 100, 96, 96, PixelFormats.Pbgra32);
        Assert.DoesNotThrow(() => bitmap.Render(control));
    }

    [Test]
    public void Render_WithoutHistogram_DoesNotThrow()
    {
        var control = new HistogramControl();
        control.Measure(new Size(100, 50));
        control.Arrange(new Rect(0, 0, 100, 50));

        var bitmap = new RenderTargetBitmap(100, 50, 96, 96, PixelFormats.Pbgra32);
        Assert.DoesNotThrow(() => bitmap.Render(control));
    }

    [Test]
    public void HistogramWindow_LoadsXaml()
    {
        HistogramWindow? window = null;
        Assert.DoesNotThrow(() => window = new HistogramWindow());
        window?.CloseForShutdown();
    }
}
