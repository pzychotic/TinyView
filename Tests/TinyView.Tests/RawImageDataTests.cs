using TinyView.Models;

namespace TinyView.Tests
{
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
            Assert.That(provider.Min, Is.EqualTo(0f));
            Assert.That(provider.Max, Is.EqualTo(255f));
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
            var data = new float[width * height];

            // all entries the same -> Min == Max branch taken
            for (int i = 0; i < data.Length; ++i)
            {
                data[i] = 5.0f;
            }

            var provider = new RawImageData<float>(width, height, data, "FLT_FMT");

            Assert.That(provider.Width, Is.EqualTo(width));
            Assert.That(provider.Height, Is.EqualTo(height));
            Assert.That(provider.Min, Is.EqualTo(5f));
            Assert.That(provider.Max, Is.EqualTo(5f));
            Assert.That(provider.DataFormat, Is.EqualTo("FLT_FMT"));

            // when Min == Max the implementation sets scale = 1f,
            // (value - Min) will be 0 so all indexed bytes should be zero
            Assert.That(provider.IndexedData, Is.All.EqualTo((byte)0));

            // GetValueString() should return the ToString() of the raw value
            Assert.That(provider.GetValueString(0, 0), Is.EqualTo("5"));
            Assert.That(provider.GetValueString(1, 1), Is.EqualTo("5"));
        }

        [Test]
        public void IndexedData_Calculation_MatchesManualComputation_ForMixedValues()
        {
            int width = 3, height = 1;
            var data = new double[width * height];
            // use values that test normalization and rounding behavior
            data[0 * width + 0] = -10.0;
            data[0 * width + 1] = 0.0;
            data[0 * width + 2] = 10.0;

            var provider = new RawImageData<double>(width, height, data, "DBL_FMT");

            // manual computation of min/max and expected indices
            float min = Convert.ToSingle(data.Min());
            float max = Convert.ToSingle(data.Max());
            float scale = min == max ? 1f : 255f / (max - min);

            var expected = new byte[width * height];
            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    float norm = (Convert.ToSingle(data[y * width + x]) - min) * scale;
                    byte idx = (byte)Math.Clamp(norm, 0, 255);
                    expected[y * width + x] = idx;
                }
            }

            Assert.That(provider.Min, Is.EqualTo(min));
            Assert.That(provider.Max, Is.EqualTo(max));
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
            Assert.That(provider.Min, Is.EqualTo(0f));
            Assert.That(provider.Max, Is.EqualTo(100f));

            // narrow the display range to 25..75
            provider.RegenerateIndexedData(25f, 75f);

            // 0 is below 25 → clamped to 0
            // 50 is mid-range: (50-25)*255/50 = 127.5 → 127
            // 100 is above 75 → clamped to 255
            Assert.That(provider.IndexedData[0], Is.EqualTo((byte)0));
            Assert.That(provider.IndexedData[1], Is.EqualTo((byte)127));
            Assert.That(provider.IndexedData[2], Is.EqualTo((byte)255));

            // Min and Max properties remain unchanged (they reflect the original data)
            Assert.That(provider.Min, Is.EqualTo(0f));
            Assert.That(provider.Max, Is.EqualTo(100f));
        }

        [Test]
        public void RegenerateIndexedData_EqualMinMax_AllZero()
        {
            int width = 2, height = 1;
            var data = new float[] { 10f, 20f };

            var provider = new RawImageData<float>(width, height, data, "FLT_FMT");

            // when displayMin == displayMax, scale = 1, so (value - min) * 1
            // both values become 0 and 10 respectively, clamped to byte
            provider.RegenerateIndexedData(15f, 15f);

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
            provider.RegenerateIndexedData(50f, 200f);
            Assert.That(provider.IndexedData, Is.Not.EqualTo(originalIndexed));

            // revert to the original range
            provider.RegenerateIndexedData(provider.Min, provider.Max);
            Assert.That(provider.IndexedData, Is.EqualTo(originalIndexed));
        }
    }
}
