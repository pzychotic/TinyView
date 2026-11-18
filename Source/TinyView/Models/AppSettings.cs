namespace TinyView.Models;

/// <summary>
/// State remembered between application runs (window placement, selected palette).
/// Null properties mean "never saved" and leave the built-in defaults in effect.
/// </summary>
public sealed class AppSettings
{
    public double? WindowLeft { get; set; }
    public double? WindowTop { get; set; }
    public double? WindowWidth { get; set; }
    public double? WindowHeight { get; set; }
    public bool WindowMaximized { get; set; }

    /// <summary>Name of the selected color palette.</summary>
    public string? SelectedPaletteName { get; set; }

    /// <summary>Whether the histogram window was open.</summary>
    public bool HistogramVisible { get; set; }

    /// <summary>Whether the histogram uses a logarithmic count axis (null = default).</summary>
    public bool? HistogramLogScale { get; set; }

    public double? HistogramLeft { get; set; }
    public double? HistogramTop { get; set; }
    public double? HistogramWidth { get; set; }
    public double? HistogramHeight { get; set; }

    /// <summary>Whether NaN and ±Infinity pixels are highlighted (null = default, on).</summary>
    public bool? HighlightNonFinite { get; set; }
}
