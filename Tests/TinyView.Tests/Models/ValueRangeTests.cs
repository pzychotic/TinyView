using TinyView.Models;

namespace TinyView.Tests.Models;

[TestFixture]
public class ValueRangeTests
{
    [Test]
    public void Length_IsMaxMinusMin()
    {
        Assert.That(new ValueRange(-2, 8).Length, Is.EqualTo(10));
        Assert.That(new ValueRange(8, -2).Length, Is.EqualTo(-10));
    }

    [TestCase(0.0, 1.0, false)]
    [TestCase(5.0, 5.0, true)]
    [TestCase(6.0, 5.0, true)]
    [TestCase(double.NaN, 1.0, true)]
    public void IsEmpty_ForZeroWidthInvertedOrNaN(double min, double max, bool expected)
    {
        Assert.That(new ValueRange(min, max).IsEmpty, Is.EqualTo(expected));
    }

    [TestCase(-3.0, 0.0)]
    [TestCase(2.0, 0.5)]
    [TestCase(7.0, 1.0)]
    [TestCase(12.0, 1.5)]
    public void Normalize_MapsLinearly(double value, double expected)
    {
        Assert.That(new ValueRange(-3, 7).Normalize(value), Is.EqualTo(expected).Within(1e-12));
    }

    [Test]
    public void Normalize_EmptyRange_ReturnsCenter()
    {
        Assert.That(new ValueRange(5, 5).Normalize(42), Is.EqualTo(0.5));
        Assert.That(new ValueRange(5, 1).Normalize(3), Is.EqualTo(0.5));
    }

    [Test]
    public void Lerp_IsInverseOfNormalize()
    {
        var range = new ValueRange(-3, 7.5);
        foreach (double value in new[] { -3.0, 0.0, 1.25, 7.5, 10.0 })
            Assert.That(range.Lerp(range.Normalize(value)), Is.EqualTo(value).Within(1e-9));
    }

    [TestCase(-5.0, 0.0)]
    [TestCase(3.0, 3.0)]
    [TestCase(15.0, 10.0)]
    public void Clamp_KeepsValueInRange(double value, double expected)
    {
        Assert.That(new ValueRange(0, 10).Clamp(value), Is.EqualTo(expected));
    }

    [Test]
    public void Clamp_InvertedRange_DoesNotThrowAndMinWins()
    {
        Assert.That(new ValueRange(4, 2).Clamp(3), Is.EqualTo(4));
    }

    [Test]
    public void Equality_IsByValue()
    {
        Assert.That(new ValueRange(1, 2), Is.EqualTo(new ValueRange(1, 2)));
        Assert.That(new ValueRange(1, 2) != new ValueRange(1, 3), Is.True);
    }

    [Test]
    public void Deconstructs_IntoMinAndMax()
    {
        var (min, max) = new ValueRange(1, 2);
        Assert.That((min, max), Is.EqualTo((1.0, 2.0)));
    }

    [Test]
    public void ToString_FormatsWithTwoDecimals()
    {
        Assert.That(new ValueRange(0.126, 255).ToString(), Is.EqualTo(FormattableString.CurrentCulture($"{0.13:0.##}..255")));
    }
}
