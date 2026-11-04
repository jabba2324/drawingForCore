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
(`tools/ci/*.cs`) are C# file-based apps that `run-tests.sh` runs with `dotnet run`. On macOS the same commands
build and the package smoke test passes, but the test host crashes part-way through the suite
([#13](https://github.com/xforcore/drawingForCore/issues/13), [CI](#ci)).

## drawingForCore's own tests

Where Microsoft's suite doesn't cover a behaviour, drawingForCore adds its own tests (CONTRIBUTING.md, principle 2)
in the test project's own folders, which its `**/*.cs` item picks up: `Engine/NativeBoundaryTests.cs` (94 tests, #3)
records what the flat API does with the buffers Microsoft's code passes it: LockBits in every format with either stride
sign, Scan0, the BMP and GIF encoders' bytes, JPEG quality and the integer overloads. They run in the same selection,
so from #3 the counts above are 94 higher: 3,976 tests, 3,799 pass, 26 skipped, 151 known failures in CI.

## The selection

Microsoft's tests mark what doesn't apply: `[ActiveIssue]`, `[PlatformSpecific]` and similar attributes add xunit traits
(`category=failing`, `nonlinuxtests`, ...), and dotnet/runtime's Linux runs filter them out. `tools/ci/run-tests.sh`
uses the same filter:

```text
category!=failing&category!=nonlinuxtests&category!=nonnetcoreapptests&category!=OuterLoop&category!=IgnoreForCI&category!=nonunixtests
```

Windows runs the same selection. The engine is drawingForCore's on both, not Windows' own GDI+, so the suite's Windows
expectations don't apply ([MODIFICATIONS.md](MODIFICATIONS.md)).

### The Unix-disabled tests

Outside the selection, Microsoft disabled tests on Unix because libgdiplus failed them (`category=failing` from
`[ActiveIssue(..., TestPlatforms.AnyUnix)]`, and the `nonlinuxtests`/`nonunixtests` categories). The engine isn't
libgdiplus, so `run-tests.sh` runs them too, as a second run with its own known failures
([#12](https://github.com/xforcore/drawingForCore/issues/12)):

```text
(category=failing|category=nonlinuxtests|category=nonunixtests)&category!=nonnetcoreapptests&category!=OuterLoop&category!=IgnoreForCI
```

Seven theories are left out of it by name (`SLOW` in `run-tests.sh`): `RegionTests.Exclude_Rectangle_Success`,
`Exclude_RectangleF_Success`, `Exclude_GraphicsPath_Success`, `Xor_Rectangle_Success`, `Xor_RectangleF_Success`,
`Xor_GraphicsPath_Success` and `Xor_Region_Success`. All 66 of their cases pass, but in one case of each the engine
takes 80 seconds for `GetRegionScans` of an infinite region less a rectangle (Skia rasterises the
8,388,608-pixel-high region one row at a time), which would add 9 minutes to every run. Their data aren't
serialisable, so xunit can't filter the slow case alone. With them the run has 1,073 tests on Fedora 44 and 603 pass.

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

`tools/ci/known-failures/System.Drawing.Common.Tests.unix-disabled.txt` lists the second run's failures: 457 entries
(the union of Linux, Windows and a Linux machine with a printer). Some cases fail on one platform only, and are
reported as passing on the others: font metrics, a long path and a UNC path pass on Windows, and
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

## CI

CI (`.github/workflows/ci.yml`) runs on every push and pull request, in three jobs that run in parallel:

- `build-and-test`, Ubuntu 24.04: the build (with the `external/` submodules checked out), the tests, packing and the
  package smoke test.
- `build-windows`, Windows Server 2025 under Git Bash: the build, the tests, packing and the package smoke test.
- `build-macos`, macOS on Apple silicon (`macos-latest`), #13: the build, packing and the package smoke test, which
  must pass, and the tests, for information only (the step's failure doesn't fail the job; the results are uploaded
  as `test-results-macos`). The test host crashes part-way through the suite on macOS: in the run analysed, in
  CoreText (`CTFontGetBoundingBox`, called by SkiaSharp for font metrics), after 30 to 1,900 tests depending on the
  run. It isn't a required check, and a release doesn't wait for it.

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

A `v<Version>` tag also builds the release version and, once the Linux and Windows jobs pass, attaches the packages
to a GitHub release (with `docs/release-notes/<version>.md` as its notes) and rebuilds the NuGet feed on GitHub Pages from the latest
releases (`.github/workflows/feed.yml`, which can also be run by hand).
