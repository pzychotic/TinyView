using NUnit.Framework.Legacy;
using System.Reflection;

namespace TinyView.Tests
{
    [TestFixture]
    public class ColorMapsTests
    {
        // Helper to enumerate maps
        private static IEnumerable<(string Name, byte[,] Map)> GetMaps()
        {
            var type = typeof(Models.ColorMaps);
            var propertyType = typeof(byte[,]);

            var fields = type
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.FieldType == propertyType)
                .Select(f => (Name: f.Name, Map: (byte[,])f.GetValue(null)!));

            var props = type
                .GetProperties(BindingFlags.Public | BindingFlags.Static)
                .Where(p => p.PropertyType == propertyType)
                .Select(p => (Name: p.Name, Map: (byte[,])p.GetValue(null)!));

            return fields.Concat(props);
        }

        [Test]
        public void ColorMaps_ExposesEightMapsWithExpectedNamesAndType()
        {
            var maps = GetMaps().ToArray();

            var memberNames = maps.Select(m => m.Name).ToArray();

            // There should be 8 maps (Mako, Turbo, Viridis, Cividis, Inferno, Plasma, Magma, Rocket)
            Assert.That(memberNames.Length, Is.EqualTo(8), "Expected exactly 8 public static byte[,] members on ColorMaps.");

            var expected = new[] { "Mako", "Turbo", "Viridis", "Cividis", "Inferno", "Plasma", "Magma", "Rocket" };

            CollectionAssert.AreEquivalent(expected, memberNames);

            // Additionally verify each map has dimensions [256,3]
            foreach (var (name, map) in maps)
            {
                Assert.That(map, Is.Not.Null, $"{name} should not be null.");
                Assert.That(map.GetLength(0), Is.EqualTo(256), $"{name} should have 256 rows (colors).");
                Assert.That(map.GetLength(1), Is.EqualTo(3), $"{name} should have 3 columns (r,g,b per color).");
            }
        }

        [Test]
        public void ColorMaps_ValuesAreWithinByteRange()
        {
            foreach (var (name, map) in GetMaps())
            {
                for (int r = 0; r < map.GetLength(0); r++)
                {
                    for (int c = 0; c < map.GetLength(1); c++)
                    {
                        Assert.That(map[r, c], Is.InRange((byte)0, (byte)255), $"{name}[{r},{c}] out of range");
                    }
                }
            }
        }

        [Test]
        public void ColorMaps_MapsAreDistinctInstances()
        {
            var maps = GetMaps().ToArray();

            for (int i = 0; i < maps.Length; i++)
            {
                for (int j = i + 1; j < maps.Length; j++)
                {
                    Assert.That(maps[i].Map, Is.Not.SameAs(maps[j].Map), $"{maps[i].Name} and {maps[j].Name} reference the same array");
                }
            }
        }

        [Test]
        public void ColorMaps_FirstAndLastRowsMatchExpected()
        {
            var expected = new Dictionary<string, (byte[] First, byte[] Last)>
            {
                { "Mako",    (new byte[] { 11, 4, 5 },     new byte[] { 222, 245, 229 }) },
                { "Turbo",   (new byte[] { 48, 18, 59 },   new byte[] { 122, 4, 3 }) },
                { "Viridis", (new byte[] { 68, 1, 84 },    new byte[] { 253, 231, 37 }) },
                { "Cividis", (new byte[] { 0, 32, 77 },    new byte[] { 255, 234, 70 }) },
                { "Inferno", (new byte[] { 0, 0, 4 },      new byte[] { 252, 255, 164 }) },
                { "Plasma",  (new byte[] { 13, 8, 135 },   new byte[] { 240, 249, 33 }) },
                { "Magma",   (new byte[] { 0, 0, 4 },      new byte[] { 252, 253, 191 }) },
                { "Rocket",  (new byte[] { 3, 5, 26 },     new byte[] { 250, 235, 221 }) },
            };

            foreach (var (name, map) in GetMaps())
            {
                Assert.That(expected.ContainsKey(name), Is.True, $"No expected values provided for map '{name}'");

                var (firstExp, lastExp) = expected[name];

                var first = new byte[] { map[0, 0], map[0, 1], map[0, 2] };
                var last = new byte[] { map[255, 0], map[255, 1], map[255, 2] };

                CollectionAssert.AreEqual(firstExp, first, $"First row for '{name}' does not match expected");
                CollectionAssert.AreEqual(lastExp, last, $"Last row for '{name}' does not match expected");
            }
        }
    }
}
