# TinyView
A specialized viewer for 16+32bit single channel images.
- PNG: 16-bit grayscale
- DDS: R16F, R32F

The value range between min and max from the image will be scaled to 0-255 range for screen display while the mouse over will display the raw value in the status bar.

## Build
Visual Studio 2022

## Dependencies
- [Magick.Net](https://github.com/dlemstra/Magick.NET) for PNG file loading
- [Pfim](https://github.com/nickbabcock/Pfim) for DDS file loading
