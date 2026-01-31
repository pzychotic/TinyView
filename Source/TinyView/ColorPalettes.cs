using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TinyView
{
    public static class ColorPalettes
    {
        public readonly record struct PaletteEntry(string Name, BitmapPalette Palette);

        public static readonly IReadOnlyList<PaletteEntry> Palettes;

        static ColorPalettes()
        {
            var list = new List<PaletteEntry>
            {
                new("Gray", BitmapPalettes.Gray256),

                // color maps
                CreateEntry("Magma", ColorMaps.Magma),
                CreateEntry("Inferno", ColorMaps.Inferno),
                CreateEntry("Plasma", ColorMaps.Plasma),
                CreateEntry("Viridis", ColorMaps.Viridis),
                CreateEntry("Cividis", ColorMaps.Cividis),
                CreateEntry("Rocket", ColorMaps.Rocket),
                CreateEntry("Mako", ColorMaps.Mako),
                CreateEntry("Turbo", ColorMaps.Turbo)
            };

            Palettes = list.AsReadOnly();
        }

        static PaletteEntry CreateEntry(string name, byte[,] colors)
        {
            Color[] paletteColors = new Color[256];
            for (int i = 0; i < 256; ++i)
            {
                paletteColors[i] = Color.FromRgb(colors[i, 0], colors[i, 1], colors[i, 2]);
            }
            var palette = new BitmapPalette(paletteColors);

            return new PaletteEntry(name, palette);
        }
    }
}
