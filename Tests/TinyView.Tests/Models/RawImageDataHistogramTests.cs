using TinyView.Models;

namespace TinyView.Tests.Models;

[TestFixture]
public class RawImageDataHistogramTests
{
    [Test]
    public void ComputeHistogram_IntegerSmallRange_UsesOneBinPerValue()
    {
        var provider = new RawImageData<int>(4, 1, [0, 1, 1, 3], "INT_FMT");

        var histogram = provider.ComputeHistogram(512);

        Assert.That(histogram.Counts, Is.EqualTo([1, 2, 0, 1]));
        // bins are centered on the integer values
        Assert.That(histogram.Min, Is.EqualTo(-0.5));
        Assert.That(histogram.Max, Is.EqualTo(3.5));
        Assert.That(histogram.BinWidth, Is.EqualTo(1.0));
        Assert.That(histogram.MaxCount, Is.EqualTo(2));
    }

    [Test]
    public void ComputeHistogram_UShortSmallRange_UsesOneBinPerValue()
    {
        var provider = new RawImageData<ushort>(3, 1, [10, 11, 12], "U16");

        var histogram = provider.ComputeHistogram(512);

        Assert.That(histogram.Counts, Is.EqualTo([1, 1, 1]));
    }

    [Test]
    public void ComputeHistogram_IntegerLargeRange_UsesRequestedBinCount()
    {
        var provider = new RawImageData<int>(3, 1, [0, 500, 1000], "INT_FMT");

        var histogram = provider.ComputeHistogram(10);

        Assert.That(histogram.Counts, Has.Length.EqualTo(10));
        Assert.That(histogram.Counts[0], Is.EqualTo(1));
        Assert.That(histogram.Counts[5], Is.EqualTo(1));
        Assert.That(histogram.Counts[9], Is.EqualTo(1), "the maximum value belongs to the last bin");
    }

    [Test]
    public void ComputeHistogram_Float_PlacesEdgeValuesInFirstAndLastBin()
    {
        var provider = new RawImageData<float>(3, 1, [0f, 0.5f, 1f], "R32F");

        var histogram = provider.ComputeHistogram(4);

        Assert.That(histogram.Min, Is.EqualTo(0.0));
        Assert.That(histogram.Max, Is.EqualTo(1.0));
        Assert.That(histogram.Counts, Is.EqualTo([1, 0, 1, 1]));
    }

    [Test]
    public void ComputeHistogram_ConstantImage_ProducesSingleBin()
    {
        var provider = new RawImageData<float>(2, 2, [2f, 2f, 2f, 2f], "R32F");

        var histogram = provider.ComputeHistogram(512);

        Assert.That(histogram.Counts, Is.EqualTo([4]));
        Assert.That(histogram.Min, Is.LessThan(2.0));
        Assert.That(histogram.Max, Is.GreaterThan(2.0));
    }

    [Test]
    public void ComputeHistogram_SkipsNaN()
    {
        var provider = new RawImageData<float>(3, 1, [0f, float.NaN, 1f], "R32F");

        var histogram = provider.ComputeHistogram(8);

        Assert.That(histogram.Counts.Sum(), Is.EqualTo(2));
    }

    [Test]
    public void ComputeHistogram_CountsSumToPixelCount()
    {
        var random = new Random(42);
        var data = Enumerable.Range(0, 1000).Select(_ => (float)random.NextDouble() * 100f).ToArray();
        var provider = new RawImageData<float>(100, 10, data, "R32F");

        var histogram = provider.ComputeHistogram(Histogram.DefaultBinCount);

        Assert.That(histogram.Counts, Has.Length.EqualTo(Histogram.DefaultBinCount));
        Assert.That(histogram.Counts.Sum(), Is.EqualTo(1000));
    }

    [Test]
    public void ComputeHistogram_ThrowsForNonPositiveBinCount()
    {
        var provider = new RawImageData<int>(1, 1, [0], "INT_FMT");

        Assert.Throws<ArgumentOutOfRangeException>(() => provider.ComputeHistogram(0));
    }
}
