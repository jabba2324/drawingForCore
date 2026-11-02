# Tests

drawingForCore is tested with **Microsoft's own test suite** for System.Drawing.Common 6.0, lifted from dotnet/runtime
with the code ([MODIFICATIONS.md](MODIFICATIONS.md)). Its tests record what Windows GDI+ does, so they measure how
close the engine is to it.

**Latest run (0.1.0, CI, 2 Nov 2026, Linux x64 and Windows x64, Release): 3,882 tests in Microsoft's Linux selection,
the same on both. 3,705 pass, 26 are skipped by the suite, and 151 fail, all of them known (141 are metafiles, which
aren't implemented).** The numbers are webForCore's CI numbers for the same suite.

Ten printing tests run only where a printer is installed (`PrinterSettingsTests`, `PrintControllerTests`,
`PreviewPrintControllerTests`): CI's runners have none, so they are skipped there. On a machine with a printer they run
and pass: 3,883 tests, 3,716 pass, 16 skipped, 151 known failures (Fedora 44, with CUPS).

## Running the tests

```bash
tools/ci/fetch-test-fonts.sh     # the test fonts (not redistributable, so not committed); before the build
dotnet build drawingForCore.slnx -c Release
tools/ci/run-tests.sh            # .trx and summary.md in ./TestResults; fails on any failure not known
```

On Windows: Git for Windows, Python 3 (as `python3`) and the SDK in `global.json`; then the same commands in Git Bash.

## The selection

Microsoft's tests mark what doesn't apply: `[ActiveIssue]`, `[PlatformSpecific]` and similar attributes add xunit traits
(`category=failing`, `nonlinuxtests`, ...), and dotnet/runtime's Linux runs filter them out. `tools/ci/run-tests.sh`
uses the same filter:

```text
category!=failing&category!=nonlinuxtests&category!=nonnetcoreapptests&category!=OuterLoop&category!=IgnoreForCI&category!=nonunixtests
```

Windows runs the same selection. The engine is drawingForCore's on both, not Windows' own GDI+, so the suite's Windows
expectations don't apply ([MODIFICATIONS.md](MODIFICATIONS.md)).

Outside the selection, Microsoft disabled 1,008 tests on Unix because libgdiplus failed them. 560 of them pass here
(LIFT.md). They aren't run in CI.

## Known failures

`tools/ci/known-failures/System.Drawing.Common.Tests.txt` lists the 151 tests the engine fails, and
`tools/ci/check_failures.py` fails the run on any other failure. It also reports known failures that now pass, so the
list only shrinks: remove a test from the list when it passes.

| Failures | Why |
| ---: | --- |
| 141 | Metafiles (EMF/WMF) aren't implemented: the calls throw `NotImplementedException` |
| 6 | `IconCodecTest.*_Palette_Entries_Unix`: libgdiplus keeps an icon's palette; Windows GDI+ returns none, as the engine does (the `_Windows` variants pass) |
| 1 | `ImageFormatConverterTest.ConvertFrom_ThrowsFormatExceptionOnInvalidFormatString`: .NET Framework's `NotSupportedException` is kept ([MODIFICATIONS.md](MODIFICATIONS.md)) |
| 1 | `BitmapTests.UnixSupportDisabledThrows`: there is no libgdiplus opt-in switch |
| 1 | `DrawBezier_Point`: compares an MD5 of Windows GDI+'s pixels; antialiased output differs |
| 1 | `IconTests.CorrectColorDepthExtracted`: pixels are stored premultiplied, so a translucent 32bpp pixel reads back rounded (0x879FE531 for 0x879EE532) |

## CI

CI (`.github/workflows/ci.yml`) runs on every push and pull request, in two jobs that run in parallel:

- `build-and-test`, Ubuntu 24.04: the lift check (the source is regenerated from upstream with
  `tools/lift/fetch-sources.sh` and `tools/lift/lift.sh`, and `docs/lifted-changes.md` with
  `tools/lift/changes_report.py`, and any difference from what is committed fails the job), then the build, the tests,
  packing and the package smoke test.
- `build-windows`, Windows Server 2025 under Git Bash: the build, the tests, packing and the package smoke test.

Packing builds version `<Version>-ci.<run number>` (Directory.Build.props). The package smoke test
(`tools/ci/package-smoke.sh`) builds an application that references only the `drawingForCore` package from that
folder, then draws, saves and reloads PNG, JPEG and GIF images, checks pixels, formats and `FontConverter`, and checks
that the assemblies have Microsoft's names, versions and public key tokens.

A `v<Version>` tag also builds the release version and, once both jobs pass, attaches the packages to a GitHub release
(with `docs/release-notes/<version>.md` as its notes) and rebuilds the NuGet feed on GitHub Pages from the latest
releases (`.github/workflows/feed.yml`, which can also be run by hand).
