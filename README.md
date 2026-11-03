# drawingForCore

**System.Drawing on .NET 10, on Linux as well as Windows: Microsoft's System.Drawing.Common with GDI+ written in
managed code on SkiaSharp.**

Microsoft made System.Drawing.Common Windows-only in .NET 7. Version 6.0 was the last to run on Linux and macOS, and it
did so on libgdiplus, a native library that is no longer maintained and can crash the process. A .NET Framework
application that draws, resizes or converts images therefore has nothing to call when it moves to .NET on Linux.

drawingForCore keeps Microsoft's System.Drawing.Common 6.0 source (the public API, argument validation, exceptions and
.NET Framework behaviour of `Bitmap`, `Graphics`, `Image`, `Font`, `Icon`...) and replaces libgdiplus with a GDI+
written in C# on [SkiaSharp](https://github.com/mono/SkiaSharp). The assemblies have Microsoft's names, versions and
public key tokens, so code compiled against System.Drawing binds to them unchanged.

It started inside [webForCore](https://github.com/xforcore/webForCore), which runs ASP.NET 4.x applications on .NET 10,
and moved to its own repository because it is useful to any .NET Framework code that draws.

## Status

| | |
| --- | --- |
| Status | Pre-release (0.2.0) |
| Target | .NET 10 LTS (`net10.0`), from 0.2.0 (0.1.0 was `net11.0`). Linux x64 and Windows x64, both built and tested in CI. SkiaSharp also ships macOS assets; macOS isn't tested |
| Tests | Microsoft's System.Drawing.Common 6.0 test suite, in CI on Linux and on Windows: 3,882 tests, 3,705 pass, 26 skipped, 151 known failures (141 of them metafiles, which aren't implemented). CI fails on any other failure |
| Packages | `drawingForCore` (System.Drawing) and `drawingForCore.Common` (System.Drawing.Common), on the feed below and on the [GitHub releases](https://github.com/xforcore/drawingForCore/releases). Not on nuget.org |

## Using it

Add the feed to your `nuget.config`:

```xml
<configuration>
  <packageSources>
    <add key="drawingForCore" value="https://xforcore.github.io/drawingForCore/index.json" />
  </packageSources>
</configuration>
```

Then reference the package, instead of Microsoft's `System.Drawing.Common`:

```xml
<PackageReference Include="drawingForCore" Version="0.2.0" />
```

`drawingForCore` is the `System.Drawing` assembly (13.5.0.0, which replaces the shared framework's facade). It depends
on `drawingForCore.Common`, which is System.Drawing.Common (11.0.0.0). Code that only uses System.Drawing.Common can
reference `drawingForCore.Common` alone.

Text needs fonts installed. On Linux the Windows core fonts are matched to metric-compatible ones when they are
installed: Liberation for Arial, Times New Roman and Courier New; Carlito and Caladea for Calibri and Cambria.

The feed keeps the latest few versions. Every version's packages are also attached to its
[release](https://github.com/xforcore/drawingForCore/releases).

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

Microsoft's code comes from submodules in `external/`: forks of dotnet/runtime, dotnet/arcade and
dotnet/runtime-assets under [xforcore](https://github.com/xforcore), cleared down to the files drawingForCore uses at
a fixed commit, with each of drawingForCore's changes as a commit on the `drawingforcore` branch. The GDI+ engine is
drawingForCore's own code, in `src/drawingForCore.System.Drawing.Common/SkiaGdiPlus/`.

| Project | Assembly | From |
| --- | --- | --- |
| `src/drawingForCore.System.Drawing.Common` | System.Drawing.Common 11.0.0.0 | [dotnet/runtime](https://github.com/dotnet/runtime) release/6.0, with the engine (`SkiaGdiPlus/`) |
| `src/drawingForCore.System.Drawing` | System.Drawing 13.5.0.0 (type forwards to System.Drawing.Common) | [WebFormsForCore](https://github.com/webformsforcore/WebFormsForCore) |
| `test/drawingForCore.System.Drawing.Common.Tests` | Microsoft's tests | dotnet/runtime release/6.0, with test data from [dotnet/runtime-assets](https://github.com/dotnet/runtime-assets) |
| `test/drawingForCore.TestUtilities`, `.Microsoft.DotNet.XUnitExtensions`, `.Microsoft.DotNet.RemoteExecutor` | Microsoft's test infrastructure | dotnet/runtime and [dotnet/arcade](https://github.com/dotnet/arcade) release/6.0 |

- [docs/MODIFICATIONS.md](docs/MODIFICATIONS.md): every change from Microsoft's source and why.
- [docs/TESTING.md](docs/TESTING.md): running the tests, the selection, and the known failures.
- [CONTRIBUTING.md](CONTRIBUTING.md): how to make a change.

### The engine

drawingForCore's GDI+ is managed code on SkiaSharp. Every `libgdiplus` import in System.Drawing.Common (556 GDI+ flat
API functions) calls `SkiaGdiPlus` instead, which implements 516 of them; the rest return GDI+'s NotImplemented status.

- **Why not libgdiplus.** System.Drawing.Common 6.0 over libgdiplus 6.2 drew, but libgdiplus corrupted the heap and
  aborted the process (`double free or corruption`) when a 24bpp or 32bppRgb bitmap that had been drawn on was rotated
  by 90 or 270 degrees. A JPEG loads as 24bpp, so Web Pages' resize-then-rotate took the whole server down. A native
  crash can't be contained, and the library parses untrusted images.
- **Pixels** are premultiplied BGRA, what Skia draws into. The GDI+ pixel format is kept apart and applied at the
  edges (`LockBits`, `GetPixel`, saving), so drawing never depends on it.
- **Codecs:** Skia decodes BMP, GIF, ICO, JPEG and PNG and encodes JPEG and PNG. drawingForCore's own code does the
  rest: the BMP, GIF (halftone palette, LZW) and TIFF encoders, a baseline TIFF reader, EXIF and GDI+'s codec list.
  Formats GDI+ can't read (WebP, ...) are rejected, as on Windows.
- **Pixel positions:** with `PixelOffsetMode.None` GDI+ puts pixel centres on integer coordinates; Skia's are at +0.5.
  The shift is 0.5 − 1/32, not 0.5, because Skia's aliased rasterizer rounds an edge on a pixel centre the other way
  from GDI+, which put fills, clips and even-width pens one pixel off.
- **Handles:** HDC, HICON, HBITMAP and HRGN are handles, as System.Drawing's Unix code assumes (libgdiplus's
  convention). A GDI+ object handle is a number in the engine's handle table, freed by the matching `Gdip*Delete`; an
  unknown or freed handle is InvalidParameter, not a crash.
- **Text:** families resolve through fontconfig, and the Windows core fonts map to installed metric-compatible ones
  that keep the Windows names. Layout is GDI+'s (1/6 em padding except for `GenericTypographic`, wrapping, alignment,
  trimming).
- **Pointers stay at the edge** (CONTRIBUTING.md, principle 9): a flat API function that receives a pointer or a
  memory block, for example LockBits' buffer, `Scan0` or `EncoderParameters`, turns it into a span or an object, then
  calls managed code. One flat function never passes a pointer to another, and the codecs and `GpImage` work on arrays
  and spans. The open questions are in #3.

Where Microsoft's tests record how Windows GDI+ behaves (matrices, gradients, containers, image flags, region data),
the engine follows them, even where libgdiplus differed.

## Building

The .NET SDK in `global.json` (10.0.100, or a later 10.0 feature band), and Bash (Git Bash on Windows) for the
scripts. Clone with `git clone --recurse-submodules`.

```bash
tools/ci/fetch-test-fonts.sh                     # fonts for the tests (not redistributable, so not committed)
dotnet build drawingForCore.slnx -c Release
tools/ci/run-tests.sh                            # the tests, checked against the known failures
tools/ci/pack.sh 0.2.0-local packages            # the NuGet packages
```

## Repository layout

```text
external/                             # Microsoft's code: submodules of the xforcore forks' drawingforcore branches
src/                                  # the two projects, and the GDI+ engine (SkiaGdiPlus/)
test/                                 # Microsoft's tests and test infrastructure
keys/                                 # Microsoft's public keys, for public signing
tools/ci/                             # tests, known failures, packing and the package smoke test
docs/                                 # modifications, testing, licensing, release notes
```

## Licence

MIT. Microsoft's code is MIT-licensed by the .NET Foundation, and drawingForCore's own code is MIT. See
[LICENSE](LICENSE), [NOTICE](NOTICE) and [docs/LICENSING.md](docs/LICENSING.md). drawingForCore isn't affiliated with or
endorsed by Microsoft. Its assemblies carry Microsoft's public keys so that references resolve, but they are not
signed by Microsoft.
