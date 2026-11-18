using TinyView.Models;

namespace TinyView.Tests.Models;

[TestFixture]
public class RawImageDataTests
{
    [Test]
    public void Constructor_SetsProperties_And_GeneratesIndexedData_ForInt()
    {
        int width = 2, height = 2;
        // fill using 1D indexing as expected by RawImageData
        var data = new int[width * height];
        data[0 * width + 0] = 0;   // x=0,y=0
        data[0 * width + 1] = 255; // x=1,y=0
        data[1 * width + 0] = 128; // x=0,y=1
        data[1 * width + 1] = 64;  // x=1,y=1

        var provider = new RawImageData<int>(width, height, data, "INT_FMT");

        Assert.That(provider.Width, Is.EqualTo(width));
        Assert.That(provider.Height, Is.EqualTo(height));
        Assert.That(provider.Range.Min, Is.EqualTo(0.0));
        Assert.That(provider.Range.Max, Is.EqualTo(255.0));
        Assert.That(provider.DataFormat, Is.EqualTo("INT_FMT"));

        // expected bytes at index = y * width + x
        var expected = new byte[width * height];
        expected[0 * width + 0] = 0;
        expected[0 * width + 1] = 255;
        expected[1 * width + 0] = 128;
        expected[1 * width + 1] = 64;

        Assert.That(provider.IndexedData, Is.EqualTo(expected));

        // GetValueString() should return the ToString() of the raw value
        Assert.That(provider.GetValueString(1, 0), Is.EqualTo("255"));
        Assert.That(provider.GetValueString(1, 1), Is.EqualTo("64"));
    }

    [Test]
    public void Constructor_MinEqualsMax_AllIndexedValuesZero_ForFloat()
    {
        int width = 2, height = 2;
        // all entries the same -> Min == Max branch taken
        var data = new float[] { 5.0f, 5.0f, 5.0f, 5.0f };

        var provider = new RawImageData<float>(width, height, data, "FLT_FMT");

        Assert.That(provider.Width, Is.EqualTo(width));
        Assert.That(provider.Height, Is.EqualTo(height));
        Assert.That(provider.Range.Min, Is.EqualTo(5.0));
        Assert.That(provider.Range.Max, Is.EqualTo(5.0));
        Assert.That(provider.DataFormat, Is.EqualTo("FLT_FMT"));

        // when Min == Max the implementation sets scale = 1f,
        // (value - Min) will be 0 so all indexed bytes should be zero
        Assert.That(provider.IndexedData, Is.All.EqualTo((byte)0));

        // GetValueString() should return the ToString() of the raw value
        Assert.That(provider.GetValueString(0, 0), Is.EqualTo("5"));
        Assert.That(provider.GetValueString(1, 1), Is.EqualTo("5"));
    }

    [Test]
    public void RawImageData_WorksWithExtremeValues()
    {
        int width = 3, height = 1;
        var data = new float[] { float.MinValue, 0.0f, float.MaxValue };

        var provider = new RawImageData<float>(width, height, data, "FLT_FMT");

        var expected = new byte[] { (byte)0, (byte)127, (byte)255 };

        Assert.That(provider.Range.Min, Is.EqualTo(float.MinValue));
        Assert.That(provider.Range.Max, Is.EqualTo(float.MaxValue));
        Assert.That(provider.IndexedData, Is.EqualTo(expected));

        // GetValueString() should return the ToString() of the raw value
        Assert.That(provider.GetValueString(0, 0), Is.EqualTo(data[0 * width + 0].ToString()));
        Assert.That(provider.GetValueString(2, 0), Is.EqualTo(data[0 * width + 2].ToString()));
    }

    [Test]
    public void RawImageData_WorksWithHalfType()
    {
        int width = 3, height = 1;
        var data = new Half[] { Half.MinValue, (Half)0.0f, Half.MaxValue };

        var provider = new RawImageData<Half>(width, height, data, "HALF_FMT");

        var expected = new byte[] { (byte)0, (byte)127, (byte)255 };

        Assert.That(provider.Range.Min, Is.EqualTo((float)Half.MinValue));
        Assert.That(provider.Range.Max, Is.EqualTo((float)Half.MaxValue));
        Assert.That(provider.IndexedData, Is.EqualTo(expected));

        // GetValueString() should return the ToString() of the raw value
        // correctly display Half values, see: https://github.com/dotnet/runtime/issues/75204
        Assert.That(provider.GetValueString(0, 0), Is.EqualTo(((float)data[0 * width + 0]).ToString()));
        Assert.That(provider.GetValueString(2, 0), Is.EqualTo(((float)data[0 * width + 2]).ToString()));
    }

    [Test]
    public void IndexedData_Calculation_MatchesManualComputation_ForMixedValues()
    {
        int width = 3, height = 1;
        // use values that test normalization and rounding behavior
        var data = new double[] { -10.0, 0.0, 10.0 };

        var provider = new RawImageData<double>(width, height, data, "DBL_FMT");

        // manual computation of min/max and expected indices
        double min = double.CreateTruncating(data.Min());
        double max = double.CreateTruncating(data.Max());
        double scale = min == max ? 1.0 : 255.0 / (max - min);

        var expected = new byte[width * height];
        for (int y = 0; y < height; ++y)
        {
            for (int x = 0; x < width; ++x)
            {
                double norm = (double.CreateTruncating(data[y * width + x]) - min) * scale;
                byte index = (byte)Math.Clamp(norm, 0, 255);
                expected[y * width + x] = index;
            }
        }

        Assert.That(provider.Range.Min, Is.EqualTo(min));
        Assert.That(provider.Range.Max, Is.EqualTo(max));
        Assert.That(provider.IndexedData, Is.EqualTo(expected));
        Assert.That(provider.DataFormat, Is.EqualTo("DBL_FMT"));

        // GetValueString() should return the ToString() of the raw value
        Assert.That(provider.GetValueString(0, 0), Is.EqualTo(data[0 * width + 0].ToString()));
        Assert.That(provider.GetValueString(2, 0), Is.EqualTo(data[0 * width + 2].ToString()));
    }

    [Test]
    public void RegenerateIndexedData_UsesCustomDisplayRange()
    {
        int width = 3, height = 1;
        var data = new float[] { 0f, 50f, 100f };

        var provider = new RawImageData<float>(width, height, data, "FLT_FMT");

        // original range: 0..100 → indexed: 0, 127/128, 255
        Assert.That(provider.Range.Min, Is.EqualTo(0.0));
        Assert.That(provider.Range.Max, Is.EqualTo(100.0));

        // narrow the display range to 25..75
        provider.RegenerateIndexedData(new ValueRange(25.0, 75.0));

        // 0 is below 25 → clamped to 0
        // 50 is mid-range: (50-25)*255/50 = 127.5 → 127
        // 100 is above 75 → clamped to 255
        Assert.That(provider.IndexedData[0], Is.EqualTo((byte)0));
        Assert.That(provider.IndexedData[1], Is.EqualTo((byte)127));
        Assert.That(provider.IndexedData[2], Is.EqualTo((byte)255));

        // Min and Max properties remain unchanged (they reflect the original data)
        Assert.That(provider.Range.Min, Is.EqualTo(0.0));
        Assert.That(provider.Range.Max, Is.EqualTo(100.0));
    }

    [Test]
    public void RegenerateIndexedData_EqualMinMax_AllZero()
    {
        int width = 2, height = 1;
        var data = new float[] { 10f, 20f };

        var provider = new RawImageData<float>(width, height, data, "FLT_FMT");

        // when displayMin == displayMax, scale = 1, so (value - min) * 1
        // both values become 0 and 10 respectively, clamped to byte
        provider.RegenerateIndexedData(new ValueRange(15.0, 15.0));

        // (10 - 15) * 1 = -5 → clamped to 0
        // (20 - 15) * 1 = 5 → clamped to 5
        Assert.That(provider.IndexedData[0], Is.EqualTo((byte)0));
        Assert.That(provider.IndexedData[1], Is.EqualTo((byte)5));
    }

    [Test]
    public void RegenerateIndexedData_RevertToOriginalRange_RestoresInitialValues()
    {
        int width = 3, height = 1;
        var data = new int[] { 0, 128, 255 };

        var provider = new RawImageData<int>(width, height, data, "INT_FMT");

        var originalIndexed = (byte[])provider.IndexedData.Clone();

        // apply a custom range
        provider.RegenerateIndexedData(new ValueRange(50.0, 200.0));
        Assert.That(provider.IndexedData, Is.Not.EqualTo(originalIndexed));

        // revert to the original range
        provider.RegenerateIndexedData(provider.Range);
        Assert.That(provider.IndexedData, Is.EqualTo(originalIndexed));
    }

    [Test]
    public void GetRegionRange_FullImage_ReturnsGlobalMinMax()
    {
        int width = 3, height = 3;
        var data = new float[] { 10, 20, 30, 40, 50, 60, 70, 80, 90 };
        var provider = new RawImageData<float>(width, height, data, "FLT");

        var (min, max) = provider.GetRegionRange(0, 0, width, height);

        Assert.That(min, Is.EqualTo(10.0));
        Assert.That(max, Is.EqualTo(90.0));
    }

    [Test]
    public void GetRegionRange_SubRegion_ReturnsCorrectValues()
    {
        // Layout (3x3):
        //  10  20  30
        //  40  50  60
        //  70  80  90
        int width = 3, height = 3;
        var data = new float[] { 10, 20, 30, 40, 50, 60, 70, 80, 90 };
        var provider = new RawImageData<float>(width, height, data, "FLT");

        // select the center 2x2 region at (1,1)
        var (min, max) = provider.GetRegionRange(1, 1, 2, 2);

        Assert.That(min, Is.EqualTo(50.0));
        Assert.That(max, Is.EqualTo(90.0));
    }

    [Test]
    public void GetRegionRange_SinglePixel_ReturnsSameMinMax()
    {
        int width = 3, height = 3;
        var data = new float[] { 10, 20, 30, 40, 50, 60, 70, 80, 90 };
        var provider = new RawImageData<float>(width, height, data, "FLT");

        // single pixel at (1,1) = 50
        var (min, max) = provider.GetRegionRange(1, 1, 1, 1);

        Assert.That(min, Is.EqualTo(50.0));
        Assert.That(max, Is.EqualTo(50.0));
    }

    [Test]
    public void GetRegionRange_ClampsToImageBounds()
    {
        int width = 3, height = 3;
        var data = new float[] { 10, 20, 30, 40, 50, 60, 70, 80, 90 };
        var provider = new RawImageData<float>(width, height, data, "FLT");

        // region extends past the bottom-right corner
        var (min, max) = provider.GetRegionRange(2, 2, 5, 5);

        // only pixel at (2,2) = 90 is within bounds
        Assert.That(min, Is.EqualTo(90.0));
        Assert.That(max, Is.EqualTo(90.0));
    }

    [Test]
    public void GetRegionRange_NegativeOrigin_ClampsToZero()
    {
        int width = 3, height = 3;
        var data = new float[] { 10, 20, 30, 40, 50, 60, 70, 80, 90 };
        var provider = new RawImageData<float>(width, height, data, "FLT");

        // region starts at (-1,-1) with size 2x2 → effective region is (0,0) to (1,1)
        var (min, max) = provider.GetRegionRange(-1, -1, 2, 2);

        // only pixel at (0,0) = 10 is within the effective region
        Assert.That(min, Is.EqualTo(10.0));
        Assert.That(max, Is.EqualTo(10.0));
    }

    [Test]
    public void GetRegionRange_EmptyRegion_FallsBackToGlobalMinMax()
    {
        int width = 2, height = 2;
        var data = new float[] { 5, 15, 25, 35 };
        var provider = new RawImageData<float>(width, height, data, "FLT");

        // zero-size region
        var (min, max) = provider.GetRegionRange(0, 0, 0, 0);

        Assert.That(min, Is.EqualTo(provider.Range.Min));
        Assert.That(max, Is.EqualTo(provider.Range.Max));
    }

    [Test]
    public void GetRegionRange_FirstRow_ReturnsRowMinMax()
    {
        // Layout (4x2):
        //  100  200  300  400
        //    5   10   15   20
        int width = 4, height = 2;
        var data = new float[] { 100, 200, 300, 400, 5, 10, 15, 20 };
        var provider = new RawImageData<float>(width, height, data, "FLT");

        // select first row only
        var (min, max) = provider.GetRegionRange(0, 0, width, 1);

        Assert.That(min, Is.EqualTo(100.0));
        Assert.That(max, Is.EqualTo(400.0));
    }

    [Test]
    public void GetRegionRange_WorksWithIntData()
    {
        int width = 2, height = 2;
        var data = new int[] { -50, 0, 100, 200 };
        var provider = new RawImageData<int>(width, height, data, "INT");

        var (min, max) = provider.GetRegionRange(0, 0, 1, 2);

        // column 0: -50, 100
        Assert.That(min, Is.EqualTo(-50.0));
        Assert.That(max, Is.EqualTo(100.0));
    }

    [Test]
    public void Range_IgnoresInfinity()
    {
        var data = new float[] { float.NegativeInfinity, 1f, 3f, float.PositiveInfinity };
        var provider = new RawImageData<float>(4, 1, data, "FLT");

        Assert.That(provider.Range, Is.EqualTo(new ValueRange(1, 3)));
    }

    [Test]
    public void Range_IgnoresNaN_AtFirstIndex()
    {
        var data = new float[] { float.NaN, 1f, 3f };
        var provider = new RawImageData<float>(3, 1, data, "FLT");

        Assert.That(provider.Range, Is.EqualTo(new ValueRange(1, 3)));
    }

    [Test]
    public void Range_IgnoresNonFinite_ForHalf()
    {
        var data = new Half[] { Half.NaN, (Half)2f, Half.PositiveInfinity, (Half)(-1f), Half.NegativeInfinity };
        var provider = new RawImageData<Half>(5, 1, data, "HALF");

        Assert.That(provider.Range, Is.EqualTo(new ValueRange(-1, 2)));
    }

    [Test]
    public void Range_NoFiniteValues_IsZero()
    {
        var data = new float[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity };
        var provider = new RawImageData<float>(3, 1, data, "FLT");

        Assert.That(provider.Range, Is.EqualTo(new ValueRange(0, 0)));
    }

    [Test]
    public void IndexedData_NonFiniteValues_MapToPaletteEnds()
    {
        // Agreed behavior for now: NaN and -Inf use the first palette entry, +Inf the last.
        var data = new float[] { 1f, float.NaN, float.NegativeInfinity, float.PositiveInfinity, 3f };
        var provider = new RawImageData<float>(5, 1, data, "FLT");

        Assert.That(provider.IndexedData, Is.EqualTo(new byte[] { 0, 0, 0, 255, 255 }));
    }

    [Test]
    public void GetRegionRange_IgnoresNonFinite()
    {
        // Layout (3x2):
        //  NaN   +Inf  9
        //  2     -Inf  4
        var data = new float[] { float.NaN, float.PositiveInfinity, 9f, 2f, float.NegativeInfinity, 4f };
        var provider = new RawImageData<float>(3, 2, data, "FLT");

        var range = provider.GetRegionRange(0, 0, 2, 2);

        Assert.That(range, Is.EqualTo(new ValueRange(2, 2)));
    }

    [Test]
    public void GetRegionRange_NoFiniteValuesInRegion_IsZero()
    {
        var data = new float[] { float.NaN, float.PositiveInfinity, 5f, 7f };
        var provider = new RawImageData<float>(2, 2, data, "FLT");

        var range = provider.GetRegionRange(0, 0, 2, 1);

        Assert.That(range, Is.EqualTo(new ValueRange(0, 0)));
    }

    [Test]
    public void NonFiniteMask_MarksNaNAndInfinity()
    {
        var data = new float[] { 1f, float.NaN, float.NegativeInfinity, 2f, float.PositiveInfinity };
        var provider = new RawImageData<float>(5, 1, data, "FLT");

        const byte n = OverlayPalette.NonFiniteIndex;
        const byte t = OverlayPalette.TransparentIndex;
        Assert.That(provider.NonFiniteMask, Is.EqualTo(new byte[] { t, n, n, t, n }));
    }

    [Test]
    public void NonFiniteMask_MarksNonFiniteHalf()
    {
        var data = new Half[] { Half.NaN, (Half)1f, Half.PositiveInfinity };
        var provider = new RawImageData<Half>(3, 1, data, "HALF");

        Assert.That(provider.NonFiniteMask, Is.EqualTo(new byte[] { OverlayPalette.NonFiniteIndex, OverlayPalette.TransparentIndex, OverlayPalette.NonFiniteIndex }));
    }

    [Test]
    public void NonFiniteMask_IsNull_WhenAllValuesFinite()
    {
        var provider = new RawImageData<float>(3, 1, [0f, 1f, -2f], "FLT");

        Assert.That(provider.NonFiniteMask, Is.Null);
    }

    [Test]
    public void NonFiniteMask_IsNull_ForIntegerData()
    {
        var provider = new RawImageData<int>(2, 1, [int.MinValue, int.MaxValue], "INT_FMT");

        Assert.That(provider.NonFiniteMask, Is.Null);
    }

    [Test]
    public void GetValueString_NonFiniteHalf_MatchesFloatFormatting()
    {
        var data = new Half[] { Half.NaN, Half.PositiveInfinity, Half.NegativeInfinity };
        var provider = new RawImageData<Half>(3, 1, data, "HALF");

        Assert.That(provider.GetValueString(0, 0), Is.EqualTo(float.NaN.ToString()));
        Assert.That(provider.GetValueString(1, 0), Is.EqualTo(float.PositiveInfinity.ToString()));
        Assert.That(provider.GetValueString(2, 0), Is.EqualTo(float.NegativeInfinity.ToString()));
    }
}
