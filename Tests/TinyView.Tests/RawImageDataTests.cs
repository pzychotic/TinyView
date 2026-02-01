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
            // fill using [x,y] indexing as expected by RawImageData
            var data = new int[width, height];
            data[0, 0] = 0;   // x=0,y=0
            data[1, 0] = 255; // x=1,y=0
            data[0, 1] = 128; // x=0,y=1
            data[1, 1] = 64;  // x=1,y=1

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
            var data = new float[width, height];

            // all entries the same -> Min == Max branch taken
            for (int x = 0; x < width; ++x)
            {
                for (int y = 0; y < height; ++y)
                {
                    data[x, y] = 5.0f;
                }
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
            var data = new double[width, height];
            // use values that test normalization and rounding behavior
            data[0, 0] = -10.0;
            data[1, 0] = 0.0;
            data[2, 0] = 10.0;

            var provider = new RawImageData<double>(width, height, data, "DBL_FMT");

            // manual computation of min/max and expected indices
            float min = Convert.ToSingle(data.Cast<double>().Min());
            float max = Convert.ToSingle(data.Cast<double>().Max());
            float scale = min == max ? 1f : 255f / (max - min);

            var expected = new byte[width * height];
            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    float norm = (Convert.ToSingle(data[x, y]) - min) * scale;
                    byte idx = (byte)Math.Clamp(norm, 0, 255);
                    expected[y * width + x] = idx;
                }
            }

            Assert.That(provider.Min, Is.EqualTo(min));
            Assert.That(provider.Max, Is.EqualTo(max));
            Assert.That(provider.IndexedData, Is.EqualTo(expected));
            Assert.That(provider.DataFormat, Is.EqualTo("DBL_FMT"));

            // GetValueString() should return the ToString() of the raw value
            Assert.That(provider.GetValueString(0, 0), Is.EqualTo(data[0, 0].ToString()));
            Assert.That(provider.GetValueString(2, 0), Is.EqualTo(data[2, 0].ToString()));
        }
    }
}
