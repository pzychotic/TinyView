---
layout: default
title: TinyView
---

# TinyView

[![GitHub License](https://img.shields.io/github/license/pzychotic/TinyView?color=blue)](https://github.com/pzychotic/TinyView/blob/main/LICENSE)
[![GitHub Release](https://img.shields.io/github/v/release/pzychotic/TinyView)](https://github.com/pzychotic/TinyView/releases/latest)
[![CI](https://github.com/pzychotic/TinyView/actions/workflows/ci.yml/badge.svg)](https://github.com/pzychotic/TinyView/actions/workflows/ci.yml)

A specialized viewer for 16/32-bit single channel images.

- **DDS:** R16F, R32F
- **PNG:** 16-bit grayscale
- **TIFF:** 16/32-bit uint and float

If you ever found yourself wondering why your images only show up in black or white in other image viewers, you've come to the right place.

The pixel value range of the image, from minimum to maximum, will be scaled to the 0-255 range for screen display.
This allows data encoded in images (e.g. heightmaps) to be visualized, which normal image viewers usually can't display.
In addition to the grayscale display, there are different color maps available that also work for common types of color blindness.
Hovering over an area will display the raw pixel value under the cursor in the status bar.

## Features

- Open files by drag & drop
- Zoom using keyboard shortcuts and mouse wheel
- Pan around by holding the right mouse button
- Flip the image horizontally and vertically
- Automatic dynamic range scaling adjusts pixel values for screen display
- Dynamic range scaling based on a rectangular selection
- Manually define the display min/max range
- Multiple color maps, including colorblind-friendly palettes
- Pixel inspection shows raw pixel values in the status bar on mouse over

## Dependencies

- [Pfim](https://github.com/nickbabcock/Pfim) for DDS file loading
- [Magick.Net](https://github.com/dlemstra/Magick.NET) for PNG file loading
- [LibTiff.Net](https://github.com/BitMiracle/libtiff.net) for TIFF file loading
- [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet)
- [Microsoft.Xaml.Behaviors.Wpf](https://github.com/microsoft/XamlBehaviorsWpf)

## References

- ColorMaps created from [viridisLite](https://github.com/sjmgarnier/viridisLite)
- Icons created from [Fluent System Icons](https://github.com/microsoft/fluentui-system-icons)

---

[Screenshots](Screenshots) · [Changelog](Changelog) · [GitHub Repository](https://github.com/pzychotic/TinyView) · [Releases](https://github.com/pzychotic/TinyView/releases)
