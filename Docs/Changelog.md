# Changelog

All notable changes to TinyView are documented in this file.

## v1.1.0 - 2026-02-27

### User-Visible Changes

- Added support for TIFF files through LibTiff.Net
- Show a wait cursor while loading an image
- Disabled zoom and palette selection when no image is loaded
- Updated README

### Internal Changes

- Refactored project to use CommunityToolkit.Mvvm library
- Refactored project to use Microsoft.Xaml.Behaviors.Wpf library
- Refactored image loaders for better performance
- Added more tests

### Dependency Updates

- Added BitMiracle.LibTiff.NET 2.4.660
- Added CommunityToolkit.Mvvm 8.4.0
- Added Microsoft.Xaml.Behaviors.Wpf 1.1.141
- Bumped Magick.NET-Q16-AnyCPU from 14.10.2 to 14.10.3
- Bumped Microsoft.NET.Test.Sdk from 18.0.1 to 18.3.0
- Bumped NUnit from 4.4.0 to 4.5.0

## v1.0.0 - 2026-02-13

First official release.
