# Autodesk.Forge.Core

![SDK](https://img.shields.io/badge/SDK-1.0.0-lightgree.svg)
![.NET](https://img.shields.io/badge/.NET%20Standard-2.0-blue.svg)

## Overview

### Requirements

- .NET Standard 2.0 or later

### Dependencies

- [Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json) 12.0.1 or later
- [Polly](https://github.com/App-vNext/Polly) 6.1.2 or later

### Changelog

The change log for the SDK can be found in the [changelog file](CHANGELOG.md).

### Contributions

Contributions are welcome! Please open a Pull Request.

## Support

Please ask questions on [StackOverflow](https://stackoverflow.com/questions/ask?tags=autodesk-forge,csharp) with tag `autodesk-designautomation` tag. If it turns out that you may have found a bug, please open an issue.

## Getting Started

This package is intended to be used by other packages, such as `Autodesk.Forge.DesignAutomation`.

## Versioning

Using [Semantic Version](https://semver.org/) scheme following the pattern of `x.y.z.`:

- `x`: MAJOR version when you make incompatible changes,
- `y`: MINOR version when you add functionality in a backwards-compatible manner, and
- `z`: PATCH version when you make backwards-compatible bug fixes.


## Source-code

Generated with [swagger-codegen](https://github.com/swagger-api/swagger-codegen).

#### Build
```
dotnet build Autodesk.Forge.Core.sln
```

#### Test
```
dotnet test Autodesk.Forge.Core.sln
```