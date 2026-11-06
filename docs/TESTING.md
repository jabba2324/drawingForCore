# Tests

drawingForCore is tested with **Microsoft's own test suite** for System.Drawing.Common 6.0, from the dotnet/runtime
submodule with the code ([MODIFICATIONS.md](MODIFICATIONS.md)). Its tests record what Windows GDI+ does, so they measure how
close the engine is to it.

**Latest run (0.2.0, .NET 10, CI, 3 Nov 2026, Linux x64 and Windows x64, Release): 3,882 tests in Microsoft's Linux selection,
the same on both. 3,705 pass, 26 are skipped by the suite, and 151 fail, all of them known (141 are metafiles, which
aren't implemented).** The numbers are the same as 0.1.0's on .NET 11, and as webForCore's CI numbers for the same suite.

**A second run (#12, CI, 4 Nov 2026) takes the tests Microsoft disabled on Unix for libgdiplus
([below](#the-unix-disabled-tests)): on Linux 1,006 tests, 527 pass, 19 skipped and 460 fail; on Windows 1,019 tests,
537 pass, 19 skipped and 463 fail. All the failures are known.** With drawingForCore's own tests (below), CI runs
4,982 tests on Linux and 4,995 on Windows.

Ten printing tests run only where a printer is installed (`PrinterSettingsTests`, `PrintControllerTests`,
`PreviewPrintControllerTests`): CI's runners have none, so they are skipped there. On a machine with a printer they run
and pass: 3,883 tests, 3,716 pass, 16 skipped, 151 known failures (Fedora 44, with CUPS; .NET 10.0.14 and 11.0 RC 3 alike).

## Running the tests

```bash
tools/ci/fetch-test-fonts.sh     # the test fonts (not redistributable, so not committed); before the build
dotnet build drawingForCore.slnx -c Release
tools/ci/run-tests.sh            # both runs; .trx and summary.md in ./TestResults; fails on any failure not known
```

On Windows: Git for Windows and the SDK in `global.json`; then the same commands in Git Bash. The test tools
(`tools/ci/*.cs`) are C# file-based apps that `run-tests.sh` runs with `dotnet run`. On macOS the same commands, in
Terminal (the SDK in `global.json`; nothing else to install: SkiaSharp's package has the macOS native library, and
fonts come from the system) ([macOS](#macos)).

## drawingForCore's own tests

Where Microsoft's suite doesn't cover a behaviour, drawingForCore adds its own tests (CONTRIBUTING.md, principle 2)
in the test project's own folders, which its `**/*.cs` item picks up: `Engine/NativeBoundaryTests.cs` (100 tests, #3)
records what the flat API does with the buffers Microsoft's code passes it: LockBits in every format with either stride
sign, UnlockBits with another BitmapData, a bitmap disposed while locked, Scan0, the codec lists, the BMP and GIF
encoders' bytes, JPEG quality and the integer overloads. They run in the same selection, so from #3 the counts above
are 100 higher: 3,982 tests, 3,805 pass, 26 skipped, 151 known failures in CI. `Engine/MacOSCrashTests.cs` (6 tests,
#13) records what crashed the test host on macOS: a Graphics whose image is disposed before it (flushing, its
bounds, drawing), and the scans of regions taller than 4,096 rows.

## The selection

Microsoft's tests mark what doesn't apply: `[ActiveIssue]`, `[PlatformSpecific]` and similar attributes add xunit traits
(`category=failing`, `nonlinuxtests`, ...), and dotnet/runtime's Linux runs filter them out. `tools/ci/run-tests.sh`
uses the same filter:

```text
category!=failing&category!=nonlinuxtests&category!=nonnetcoreapptests&category!=OuterLoop&category!=IgnoreForCI&category!=nonunixtests
```

Windows and macOS run the same selection. The engine is drawingForCore's on all three, not Windows' own GDI+, so the
suite's Windows expectations don't apply ([MODIFICATIONS.md](MODIFICATIONS.md)).

### The Unix-disabled tests

Outside the selection, Microsoft disabled tests on Unix because libgdiplus failed them (`category=failing` from
`[ActiveIssue(..., TestPlatforms.AnyUnix)]`, and the `nonlinuxtests`/`nonunixtests` categories). The engine isn't
libgdiplus, so `run-tests.sh` runs them too, as a second run with its own known failures
([#12](https://github.com/xforcore/drawingForCore/issues/12)):

```text
(category=failing|category=nonlinuxtests|category=nonunixtests)&category!=nonnetcoreapptests&category!=OuterLoop&category!=IgnoreForCI
```

Until #13, seven theories were left out of it by name: `RegionTests.Exclude_Rectangle_Success`,
`Exclude_RectangleF_Success`, `Exclude_GraphicsPath_Success`, `Xor_Rectangle_Success`, `Xor_RectangleF_Success`,
`Xor_GraphicsPath_Success` and `Xor_Region_Success`. All 66 of their cases passed, but in one case of each the engine
took 80 seconds for `GetRegionScans` of an infinite region less a rectangle (Skia rasterised the
8,388,608-pixel-high region one row at a time). The engine now rasterises a region taller than 4,096 rows in bands,
one row for each band that only vertical edges cross, so the cases take milliseconds and the theories run: the run
has 66 more tests, and the main selection's `Complement_InfiniteRegion_Success` no longer takes two minutes.

The run's size differs by platform: Windows enumerates the cases of theories whose data come from Windows (window
handles, device contexts, system fonts), which fail as one test elsewhere, and the printing tests depend on an
installed printer. On Fedora 44 with a printer: 1,007 tests, 537 pass, 2 skipped, 468 fail.

## Known failures

`tools/ci/known-failures/System.Drawing.Common.Tests.txt` lists the 151 tests the engine fails, and
`tools/ci/check_failures.cs` fails the run on any other failure. It also reports known failures that now pass, so the
list only shrinks: remove a test from the list when it passes.

| Failures | Why |
| ---: | --- |
| 141 | Metafiles (EMF/WMF) aren't implemented: the calls throw `NotImplementedException` |
| 6 | `IconCodecTest.*_Palette_Entries_Unix`: libgdiplus keeps an icon's palette; Windows GDI+ returns none, as the engine does (the `_Windows` variants pass) |
| 1 | `ImageFormatConverterTest.ConvertFrom_ThrowsFormatExceptionOnInvalidFormatString`: .NET Framework's `NotSupportedException` is kept ([MODIFICATIONS.md](MODIFICATIONS.md)) |
| 1 | `BitmapTests.UnixSupportDisabledThrows`: there is no libgdiplus opt-in switch |
| 1 | `DrawBezier_Point`: compares an MD5 of Windows GDI+'s pixels; antialiased output differs |
| 1 | `IconTests.CorrectColorDepthExtracted`: pixels are stored premultiplied, so a translucent 32bpp pixel reads back rounded (0x879FE531 for 0x879EE532) |

### Known failures of the Unix-disabled tests

`tools/ci/known-failures/System.Drawing.Common.Tests.unix-disabled.txt` lists the second run's failures: 454 entries
(the union of Linux, Windows, macOS and a Linux machine with a printer). Since #3, a missing `UserInputBuffer` is
InvalidParameter, and the three `LockBits_InvalidLockMode_ThrowsArgumentException` cases pass: the CI counts here
and at the top are from before that, three failures higher. Some cases fail on one platform only, and are
reported as passing on the others: font metrics, a long path and a UNC path pass on Windows, the two
`FontTests.GetHeight_Dpi_ReturnsExpected` cases pass on macOS (its fonts are the real ones), and
`PrintDocumentTests.Ctor_Default_Success` passes where a printer is installed. An entry that ends in `*` covers every
case of a theory whose names change from run to run or machine to machine (window handles, absolute paths, system
fonts), and infinities are listed both as `∞` and `Infinity`, since the name depends on the machine's ICU data. Most of
the failures are GDI+ features the engine doesn't have yet. By test class, in Linux CI (460 failing cases):

| Failures | Test class | Mostly |
| ---: | --- | --- |
| 82 | `GraphicsTests` | HDC and HWND interop (`FromHdc`, `FromHwnd`, `GetHdc`), `CopyFromScreen` (needs a screen: X11 or `user32.dll`), compositing quality and mode pixels |
| 52 | `BitmapTests` | Argument checks GDI+ makes and the engine doesn't (invalid pixel formats, rectangles and lock modes in `Clone` and `LockBits`), `LockBits` in unusual formats, custom pixel formats, `GetHbitmap` of grayscale |
| 45 | `IconTests` | An icon at a requested size, from a stream, a file or another icon; invalid icon data |
| 41 | `ImageTests` | EXIF property items (`Property cannot be found`), invalid streams |
| 39 | `TextureBrushTests` | The brush's image comes back as 32bppArgb, not in the source's pixel format |
| 28 | `ImageAttributesTests` | Output channels and colour profiles (not implemented), colour keys |
| 28 | `FontTests` | `LOGFONT` interop (`FromLogFont`, `ToLogFont`), `FromHdc`, argument checks |
| 24 | `PrinterSettingsTests` | Printer queries not implemented |
| 23 | `ToolboxBitmapAttributeTests` | Bitmaps from resources and files (`NullReferenceException`) |
| 16 | `RegionTests` | Combining a region with itself, infinite and empty regions from paths, rectangles of infinite size |
| 15 | `PropertyItemTests` | EXIF property items |
| 12 | `GraphicsPathTests` | `Widen`, `Warp`, arcs, closed curves, strings and `IsVisible` |
| 55 | 18 other classes | 9 or fewer each: buffered graphics, private font families, metafiles, line caps, Bézier and line pixels, printing, ... |

## macOS

Since #13, CI runs both runs on macOS 15 on Apple silicon (arm64) with the same selections and known-failure lists as
on Linux and Windows, and fails on the same terms. In its first runs (6 Nov 2026): **3,951 tests in the main
selection, 3,687 pass, 113 skipped and 151 fail, the same 151 known failures as on Linux and Windows; the
Unix-disabled run has 1,073 tests, 603 pass, 24 skipped and 446 fail, all known.**

What differs from Linux x64:

- **Skips on arm64.** Microsoft's suite skips tests on arm64 processes for libgdiplus problems
  (dotnet/runtime#28859, and float precision in #35744): 64 `MatrixTests` cases (`Multiply`, `Scale`, `Shear`,
  `Translate`), and tests and theories conditioned on `PlatformDetection.IsNotArm64Process` (`LockBits_Invoke_Success`,
  `Rotate1bit4bit` and the codec `...Data` tests; a skipped theory counts as one test, not one per case). So the main
  selection has 37 fewer results and 87 more skipped than on Linux. These are the suite's arm64 conditions, not
  macOS's: the engine has no libgdiplus, but running them needs a change to Microsoft's tests, which isn't made.
- **Fonts.** Families resolve through CoreText, SkiaSharp's font manager on macOS, and Arial, Times New Roman and
  Courier New are the real fonts, so `FontTests.GetHeight_Dpi_ReturnsExpected(dpi: ±1)`, known failures on Linux,
  pass (reported as passing, as font metrics are on Windows). No other test that compares text metrics or pixels
  differs from Linux.
- **The crashes before #13.** The test host crashed part-way through every macOS run, at a different test each time,
  because of two engine bugs that Linux didn't reach:
  - On macOS `Graphics.Dispose` flushes first (Microsoft's Unix code: `Gdip.UseX11Drawable` is false there), and
    Microsoft's `BufferedGraphics.Dispose` disposes its bitmap before its Graphics. The engine's `GdipFlush` then
    called into the disposed image's freed SKBitmap. A Graphics now keeps its own size and leaves a disposed image
    alone (the same crash happened on Linux when an application flushed after disposing the image).
  - `GetRegionScans` of a region as tall as the infinite one (`RegionTests.Complement_InfiniteRegion_Success`)
    rasterised 8,388,608 rows: two minutes on Linux, a crash in `sk_region_set_path` on macOS. Tall regions are now
    rasterised in bands.

  The crash dump read in #17 stopped in CoreText (`CTFontGetBoundingBox`), on another thread than the one these bugs
  crash; no CoreText crash has happened since they were fixed (CI's macOS runs, and the diagnostic runs on the pull
  request).

## CI

CI (`.github/workflows/ci.yml`) runs on every push and pull request, in three jobs that run in parallel:

- `build-and-test`, Ubuntu 24.04: the build (with the `external/` submodules checked out), the tests, packing and the
  package smoke test.
- `build-windows`, Windows Server 2025 under Git Bash: the build, the tests, packing and the package smoke test.
- `build-macos`, macOS 15 on Apple silicon (arm64, the `macos-15` runner), #13: the build, the tests, packing and
  the package smoke test, as on Linux ([macOS](#macos)). A release waits for it as for the other two.

Each run is checked by `tools/ci/check_failures.cs`, which also fails a run whose test host crashed (the tests it
didn't reach have no results to fail), and `--blame` names the tests that were running when it did.

Packing builds version `<Version>-ci.<run number>` (Directory.Build.props). The package smoke test
(`tools/ci/package-smoke.sh`) builds an application that references only the `drawingForCore` package from that
folder, then draws, saves and reloads PNG, JPEG and GIF images, checks pixels, formats and `FontConverter`, and checks
that the assemblies have Microsoft's names, versions and public key tokens.

The default branch, `main`, is protected by the repository ruleset `default-branch` (#11), the
same in every xforcore repository: changes land only through a pull request (no approvals required), which merges
only once `build-and-test` and `build-windows` have passed on it with the branch up to date with `main`
(`gh pr update-branch <n>` when it is behind). Force pushes and deleting the branch are blocked, and nobody can bypass the rules, administrators included.
Version bumps therefore go through a pull request before the `v<Version>` tag is pushed; tags aren't covered.

A `v<Version>` tag also builds the release version and, once the Linux, Windows and macOS jobs pass, attaches the packages
to a GitHub release (with `docs/release-notes/<version>.md` as its notes) and rebuilds the NuGet feed on GitHub Pages from the latest
releases (`.github/workflows/feed.yml`, which can also be run by hand).
