using TinyView.Controls;
using TinyView.Models;

namespace TinyView.Tests.Controls;

[TestFixture]
public class HistogramMappingTests
{
    [TestCase(0.0, 0.0)]
    [TestCase(5.0, 100.0)]
    [TestCase(10.0, 200.0)]
    [TestCase(-5.0, -100.0)]
    public void ValueToX_MapsLinearly(double value, double expectedX)
    {
        Assert.That(HistogramMapping.ValueToX(value, new ValueRange(0, 10), 200), Is.EqualTo(expectedX).Within(1e-9));
    }

    [Test]
    public void XToValue_IsInverseOfValueToX()
    {
        foreach (double value in new[] { -3.0, 0.0, 1.25, 7.5 })
        {
            double x = HistogramMapping.ValueToX(value, new ValueRange(-3, 7.5), 321);
            Assert.That(HistogramMapping.XToValue(x, new ValueRange(-3, 7.5), 321), Is.EqualTo(value).Within(1e-9));
        }
    }

    [Test]
    public void Mapping_HandlesDegenerateRanges()
    {
        Assert.That(HistogramMapping.ValueToX(5, new ValueRange(5, 5), 100), Is.EqualTo(50));
        Assert.That(HistogramMapping.XToValue(30, new ValueRange(5, 5), 100), Is.EqualTo(5));
        Assert.That(HistogramMapping.XToValue(30, new ValueRange(0, 10), 0), Is.EqualTo(0));
    }

    [Test]
    public void NormalizedHeight_Linear()
    {
        Assert.That(HistogramMapping.NormalizedHeight(50, 100, logScale: false), Is.EqualTo(0.5));
        Assert.That(HistogramMapping.NormalizedHeight(100, 100, logScale: false), Is.EqualTo(1.0));
        Assert.That(HistogramMapping.NormalizedHeight(0, 100, logScale: false), Is.EqualTo(0.0));
    }

    [Test]
    public void NormalizedHeight_Log_KeepsSmallCountsVisible()
    {
        double linear = HistogramMapping.NormalizedHeight(1, 1_000_000, logScale: false);
        double log = HistogramMapping.NormalizedHeight(1, 1_000_000, logScale: true);

        Assert.That(log, Is.GreaterThan(linear * 1000));
        Assert.That(HistogramMapping.NormalizedHeight(1_000_000, 1_000_000, logScale: true), Is.EqualTo(1.0).Within(1e-12));
        Assert.That(HistogramMapping.NormalizedHeight(0, 1_000_000, logScale: true), Is.EqualTo(0.0));
    }

    [Test]
    public void NormalizedHeight_EmptyHistogram_IsZero()
    {
        Assert.That(HistogramMapping.NormalizedHeight(0, 0, logScale: true), Is.EqualTo(0.0));
    }

    [TestCase(-5.0, 0.0)]  // below image range
    [TestCase(3.0, 3.0)]   // inside
    [TestCase(9.0, 6.0)]   // past the white point
    public void ClampBlackPoint_StaysInImageRangeAndBelowWhitePoint(double value, double expected)
    {
        Assert.That(HistogramMapping.ClampBlackPoint(value, new ValueRange(0, 10), whitePoint: 6), Is.EqualTo(expected));
    }

    [TestCase(15.0, 10.0)] // above image range
    [TestCase(7.0, 7.0)]   // inside
    [TestCase(1.0, 4.0)]   // before the black point
    public void ClampWhitePoint_StaysInImageRangeAndAboveBlackPoint(double value, double expected)
    {
        Assert.That(HistogramMapping.ClampWhitePoint(value, new ValueRange(0, 10), blackPoint: 4), Is.EqualTo(expected));
    }

    [Test]
    public void ClampBlackPoint_DoesNotThrow_WhenWhitePointBelowImageRange()
    {
        Assert.That(HistogramMapping.ClampBlackPoint(5, new ValueRange(0, 10), whitePoint: -2), Is.EqualTo(0));
    }
}
