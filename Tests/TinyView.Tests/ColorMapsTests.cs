using System.Reflection;
using NUnit.Framework.Legacy;

namespace TinyView.Tests
{
    [TestFixture]
    public class ColorMapsTests
    {
        [Test]
        public void ColorMaps_ExposesEightMapsWithExpectedNames()
        {
            var type = typeof(TinyView.ColorMaps);

            var propertyType = typeof(byte[,]);

            var fieldNames = type
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.FieldType == propertyType)
                .Select(f => f.Name);

            var propertyNames = type
                .GetProperties(BindingFlags.Public | BindingFlags.Static)
                .Where(p => p.PropertyType == propertyType)
                .Select(p => p.Name);

            var memberNames = fieldNames.Concat(propertyNames).ToArray();

            // There should be 8 maps (Mako, Turbo, Viridis, Cividis, Inferno, Plasma, Magma, Rocket)
            Assert.That(memberNames.Length, Is.EqualTo(8), "Expected exactly 8 public static byte[,] members on ColorMaps.");

            var expected = new[] { "Mako", "Turbo", "Viridis", "Cividis", "Inferno", "Plasma", "Magma", "Rocket" };

            CollectionAssert.AreEquivalent(expected, memberNames);
        }
    }
}
