using System.Collections;
using System.Reflection;

namespace TinyView.Tests
{
    [TestFixture]
    public class ColorPalettesTests
    {
        private static IEnumerable<(string Name, object Palette)> GetPaletteEntries()
        {
            var asm = typeof(Models.ColorMaps).Assembly;
            var palettesType = asm.GetType("TinyView.Models.ColorPalettes", throwOnError: true)!;
            var palettesField = palettesType.GetField("Palettes", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;
            var list = palettesField.GetValue(null) as IEnumerable ?? throw new InvalidOperationException("Palettes field is not enumerable");

            foreach (var entry in list)
            {
                var entryType = entry.GetType();
                var name = (string)entryType.GetProperty("Name", BindingFlags.Instance | BindingFlags.Public)!.GetValue(entry)!;
                var palette = entryType.GetProperty("Palette", BindingFlags.Instance | BindingFlags.Public)!.GetValue(entry)!;
                yield return (name, palette!);
            }
        }

        [Test]
        public void Palettes_ContainsNineEntriesAndGrayFirst()
        {
            var entries = GetPaletteEntries().ToArray();
            Assert.That(entries.Length, Is.EqualTo(9), "Expected 9 palettes (Gray + 8 color maps)");
            Assert.That(entries[0].Name, Is.EqualTo("Gray"));
        }

        [Test]
        public void Palettes_AllPalettesHave256Colors()
        {
            foreach (var (name, paletteObj) in GetPaletteEntries())
            {
                var palette = (System.Windows.Media.Imaging.BitmapPalette)paletteObj;
                var colors = palette.Colors;
                Assert.That(colors.Count, Is.EqualTo(256), $"Palette '{name}' should contain 256 colors");
            }
        }

        [Test]
        public void Palettes_MapPalettesMatchColorMaps()
        {
            // Expected mapping between palette names and ColorMaps fields
            var expectedMapNames = new[] { "Magma", "Inferno", "Plasma", "Viridis", "Cividis", "Rocket", "Mako", "Turbo" };

            // Build a dictionary of palette entries for lookup
            var paletteDict = GetPaletteEntries().ToDictionary(e => e.Name, e => e.Palette);

            foreach (var name in expectedMapNames)
            {
                Assert.That(paletteDict.ContainsKey(name), Is.True, $"Palette '{name}' not found");

                var paletteObj = paletteDict[name];
                var palette = (System.Windows.Media.Imaging.BitmapPalette)paletteObj;
                var colors = palette.Colors;

                var map = (byte[,])typeof(Models.ColorMaps)
                    .GetField(name, BindingFlags.Public | BindingFlags.Static)!
                    .GetValue(null)!;

                for (int i = 0; i < 256; i++)
                {
                    var colorObj = colors[i];
                    var clrType = colorObj.GetType();
                    var r = (byte)clrType.GetProperty("R")!.GetValue(colorObj)!;
                    var g = (byte)clrType.GetProperty("G")!.GetValue(colorObj)!;
                    var b = (byte)clrType.GetProperty("B")!.GetValue(colorObj)!;

                    Assert.That(r, Is.EqualTo(map[i, 0]), $"Palette '{name}' color[{i}].R mismatch");
                    Assert.That(g, Is.EqualTo(map[i, 1]), $"Palette '{name}' color[{i}].G mismatch");
                    Assert.That(b, Is.EqualTo(map[i, 2]), $"Palette '{name}' color[{i}].B mismatch");
                }
            }
        }
    }
}
