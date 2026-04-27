namespace TinyView.ViewModels;

/// <summary>
/// Strongly-typed pixel rectangle used for region-select commands/behaviors.
/// </summary>
public readonly record struct PixelRect(int X, int Y, int Width, int Height);
