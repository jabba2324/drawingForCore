# drawingForCore

**System.Drawing on .NET 11, on Linux as well as Windows: Microsoft's System.Drawing.Common with GDI+ written in
managed code on SkiaSharp.**

Microsoft made System.Drawing.Common Windows-only in .NET 7. Version 6.0 was the last to run on Linux and macOS, and it
did so on libgdiplus, a native library that is no longer maintained and can crash the process. A .NET Framework
application that draws, resizes or converts images therefore has nothing to call when it moves to .NET on Linux.

drawingForCore keeps Microsoft's System.Drawing.Common 6.0 source (the public API, argument validation, exceptions and
.NET Framework behaviour of `Bitmap`, `Graphics`, `Image`, `Font`, `Icon`...) and replaces libgdiplus with a GDI+
written in C# on [SkiaSharp](https://github.com/mono/SkiaSharp). The assemblies have Microsoft's names, versions and
public key tokens, so code compiled against System.Drawing binds to them unchanged.

It started inside [webForCore](https://github.com/jabba2324/webForCore), which runs ASP.NET 4.x applications on .NET 11,
and moved to its own repository because it is useful to any .NET Framework code that draws.

## Status

| | |
| --- | --- |
| Status | Pre-release (0.1.0) |
| Target | .NET 11 (`net11.0`). Linux x64 and Windows x64, both built and tested in CI. SkiaSharp also ships macOS assets; macOS isn't tested |
| Tests | Microsoft's System.Drawing.Common 6.0 test suite: 3,883 tests, 3,716 pass, 16 skipped, 151 known failures (141 of them metafiles, which aren't implemented). CI fails on any other failure |
| Packages | `drawingForCore` (System.Drawing) and `drawingForCore.Common` (System.Drawing.Common), on the feed below and on the [GitHub releases](https://github.com/jabba2324/drawingForCore/releases). Not on nuget.org |

## Using it

Add the feed to your `nuget.config`:

```xml
<configuration>
  <packageSources>
    <add key="drawingForCore" value="https://jabba2324.github.io/drawingForCore/index.json" />
  </packageSources>
</configuration>
```

Then reference the package, instead of Microsoft's `System.Drawing.Common`:

```xml
<PackageReference Include="drawingForCore" Version="0.1.0" />
```

`drawingForCore` is the `System.Drawing` assembly (13.5.0.0, which replaces the shared framework's facade). It depends
on `drawingForCore.Common`, which is System.Drawing.Common (11.0.0.0). Code that only uses System.Drawing.Common can
reference `drawingForCore.Common` alone.

Text needs fonts installed. On Linux the Windows core fonts are matched to metric-compatible ones when they are
installed: Liberation for Arial, Times New Roman and Courier New; Carlito and Caladea for Calibri and Cambria.

The feed keeps the latest few versions. Every version's packages are also attached to its
[release](https://github.com/jabba2324/drawingForCore/releases).

## What works

- **Images:** BMP, GIF, ICO, JPEG, PNG and TIFF, read and written (no ICO writing), with GDI+'s pixel formats,
  palettes, resolution, EXIF properties, GIF frames and multi-page TIFF. `LockBits`, `GetPixel` and `SetPixel`.
- **Graphics:** world, page and container transforms, clipping, `Save`/`Restore`, smoothing, compositing and
  interpolation modes, GDI+'s pixel positions, `DrawImage` in all its forms, `ImageAttributes` (colour matrices, remap
  tables, colour keys, gamma, thresholds).
- **Brushes, pens, paths and regions:** solid, hatch, linear and path gradients, textures; widths, dash styles, caps
  and joins; GDI+'s path model (flatten, widen, warp, iterators); regions.
- **Text:** fonts by family name, GDI+'s text layout (padding, wrapping, alignment, trimming, hotkey prefixes,
  character ranges), `GraphicsPath.AddString`.

Not implemented: metafiles (EMF/WMF), saving several frames to one file (`SaveAdd`), encoder parameter lists, pen
compound arrays and alignment, path-gradient blends and focus scales, ICC profiles, JPEG-compressed or tiled TIFF,
font fallback for characters a font lacks, and bidirectional and vertical text. Printing isn't tested. Calls into what isn't
implemented throw `NotImplementedException`, as GDI+'s "not implemented" status does.

## How it's made

Microsoft's code is never edited by hand. The lift scripts in `tools/lift/` copy it byte for byte from upstream at a
fixed commit and apply a short list of recorded changes. CI regenerates the source on every run and fails if it
differs from what is committed. The GDI+ engine is drawingForCore's own code, in `tools/lift/additions/`.

| Project | Assembly | From |
| --- | --- | --- |
| `src/drawingForCore.System.Drawing.Common` | System.Drawing.Common 11.0.0.0 | [dotnet/runtime](https://github.com/dotnet/runtime) release/6.0, with the engine (`SkiaGdiPlus/`) |
| `src/drawingForCore.System.Drawing` | System.Drawing 13.5.0.0 | [WebFormsForCore](https://github.com/webformsforcore/WebFormsForCore) |
| `test/drawingForCore.System.Drawing.Common.Tests` | Microsoft's tests | dotnet/runtime release/6.0, with test data from [dotnet/runtime-assets](https://github.com/dotnet/runtime-assets) |
| `test/drawingForCore.TestUtilities`, `.Microsoft.DotNet.XUnitExtensions`, `.Microsoft.DotNet.RemoteExecutor` | Microsoft's test infrastructure | dotnet/runtime and [dotnet/arcade](https://github.com/dotnet/arcade) release/6.0 |

- [LIFT.md](LIFT.md): the record of how it was built, with every decision and result.
- [docs/MODIFICATIONS.md](docs/MODIFICATIONS.md): every change from Microsoft's source and why.
- [docs/lifted-changes.md](docs/lifted-changes.md): every lifted file that differs from upstream (generated).
- [docs/TESTING.md](docs/TESTING.md): running the tests, the selection, and the known failures.
- [CONTRIBUTING.md](CONTRIBUTING.md): how to make a change.

## Building

The .NET SDK in `global.json` (11.0.100-rc.3). Python 3 and Bash (Git Bash on Windows) run the scripts.

```bash
tools/ci/fetch-test-fonts.sh                     # fonts for the tests (not redistributable, so not committed)
dotnet build drawingForCore.slnx -c Release
tools/ci/run-tests.sh                            # the tests, checked against the known failures
tools/ci/pack.sh 0.1.0-local packages            # the NuGet packages
```

## Repository layout

```text
src/                                  # the two lifted projects (generated; do not edit)
test/                                 # Microsoft's tests and test infrastructure (generated)
keys/                                 # Microsoft's public keys, for public signing
tools/lift/                           # the lift scripts, the GDI+ engine (additions/) and the manifests
tools/ci/                             # tests, known failures, packing and the package smoke test
docs/                                 # modifications, lifted changes, testing, licensing, release notes
```

## Licence

MIT. Microsoft's code is MIT-licensed by the .NET Foundation, and drawingForCore's own code is MIT. See
[LICENSE](LICENSE), [NOTICE](NOTICE) and [docs/LICENSING.md](docs/LICENSING.md). drawingForCore isn't affiliated with or
endorsed by Microsoft. Its assemblies carry Microsoft's public keys so that references resolve, but they are not
signed by Microsoft.
