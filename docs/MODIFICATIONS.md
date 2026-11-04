# What drawingForCore changed in Microsoft's code and tests

drawingForCore runs Microsoft's own System.Drawing.Common rather than a reimplementation of it. Microsoft's source
comes from **submodules** in `external/`: forks of the upstream repositories, cleared down to the files drawingForCore
uses at a pinned commit, built for .NET 10 and tested with Microsoft's own tests. (Until xforcore/webForCore#61 the
files were copied in by lift scripts; git history has that record.) This page explains the rules for that code and
what had to change.

- **Every change is a commit** on the fork's `drawingforcore` branch, with its reason in the message:

  | Submodule | Fork branch | Upstream commit | Changes |
  | --- | --- | --- | --- |
  | `external/runtime` | [xforcore/runtime `drawingforcore`](https://github.com/xforcore/runtime/commits/drawingforcore) | `76b9680` (release/6.0) | 11, including the GDI+ import rewrite |
  | `external/arcade` | [xforcore/arcade `drawingforcore`](https://github.com/xforcore/arcade/commits/drawingforcore) | `565a800` | 1 |
  | `external/runtime-assets` | [xforcore/runtime-assets `drawingforcore`](https://github.com/xforcore/runtime-assets/commits/drawingforcore) | `848e27e` | the fonts removed (not redistributable) |

  Each branch starts at the upstream commit, then one commit keeps only the files drawingForCore uses (and the
  licences), then the changes, one file per commit. `git log` on a submodule lists them; `git revert` undoes one.
- Licences and attributions are in [`NOTICE`](../NOTICE) and [`LICENSE`](../LICENSE).

## Rules for Microsoft's code

1. **Don't rewrite.** Each file is upstream's, with its copyright header, at the pinned commit.
2. **Every change is a commit on the fork,** one file per commit, each with a `// drawingForCore:` comment that gives
   the reason (CONTRIBUTING.md principle 4).
3. **drawingForCore's own code stays in the projects:** the GDI+ engine in `SkiaGdiPlus/`, and the generated stubs
   and resource class, next to the `Link` items that compile the submodules' files at their old paths.
4. **Every file is traceable:** the submodule pins the fork's commit, and the fork's history goes back to upstream's.
5. **Identity is kept.** The assemblies keep Microsoft's names, versions and public keys (`keys/`, public signed), so
   existing references resolve to them. They aren't signed by Microsoft.
6. **Project files are drawingForCore's,** edited by hand: they name the submodule files they compile.
7. **.NET Framework is the reference.** Where System.Drawing.Common on .NET behaves differently from System.Drawing on
   .NET Framework 4.8, a change restores 4.8's behaviour, and Windows GDI+'s output is what the engine aims for.

## Components from submodules

| Component | Upstream (pinned commit) | Licence | Files used | Files changed |
| --- | --- | --- | ---: | ---: |
| System.Drawing.Common 6.0 | [dotnet/runtime@76b9680](https://github.com/dotnet/runtime/tree/76b96800a40eb88d9ee163af68076d7fcc8216be) (release/6.0) | MIT | 199 | 4 |
| System.Drawing (type forwards) | [webformsforcore/WebFormsForCore@52f4172](https://github.com/webformsforcore/WebFormsForCore/tree/52f4172dbc09b3ad0b849c97642720fa7a49aa22) | MIT | 1 | 1 |
| System.Drawing.Common 6.0's tests, TestUtilities | dotnet/runtime@76b9680, [dotnet/arcade@565a800](https://github.com/dotnet/arcade/tree/565a800599c7f87a0aaa2fac740c1779ebb74344) (release/6.0), [dotnet/runtime-assets@848e27e](https://github.com/dotnet/runtime-assets/tree/848e27e0f5c1c2cb9bf8584d13370e273c18ee57) (test data) | MIT | 233 | 7 |

## Library changes, by kind

| Kind | Change | Where |
| --- | --- | --- |
| **GDI+ on SkiaSharp** | Every `[DllImport(LibraryName)]` of the GDI+ flat API (556 functions) becomes a method with the same signature that calls `SkiaGdiPlus`, the managed engine. It implements 516; the others are generated stubs that return GDI+'s NotImplemented status, which Microsoft's code turns into `NotImplementedException`. Calls on a `Graphics` whose HDC is out return ObjectBusy, as GDI+ does | `GdiplusNative.cs`, `GdiplusNative.Unix.cs`: generated once, committed on the fork's `drawingforcore` branch and maintained by hand since xforcore/webForCore#61 |
| **No native GDI+ to load** | `LibraryResolver` no longer requires the `System.Drawing.EnableUnixSupport` switch (it still loads libcups for printing), and the GDI+ startup doesn't resolve libgdiplus | `LibraryResolver.cs`, `GdiplusNative.Unix.cs` |
| **.NET Framework behaviour** | `ImageFormatConverter` passes an unknown format name to `TypeConverter.ConvertFrom` (`NotSupportedException`), as .NET Framework did; .NET Core 3.0+ throws `FormatException`. Microsoft's System.Web.Helpers relies on the 4.8 behaviour | `ImageFormatConverter.cs` |
| **One definition of each type** | WebFormsForCore's System.Drawing defined its own `FontConverter`, `ToolboxBitmapAttribute` (a stub) and `BitmapSuffixInSatelliteAssemblyAttribute`, with their forwards commented out, and System.Drawing.Common defines them too: code that named one didn't compile (CS0433). Those three files aren't lifted, and the forwards are restored | System.Drawing's `AssemblyInfo.cs` |
| **Generated resources** | `SR.g.cs` is generated from `Strings.resx` (dotnet/runtime generates it at build time). It was generated once and is committed output, maintained by hand; the generator is in git history before xforcore/webForCore#61 | `src/drawingForCore.System.Drawing.Common/SR.g.cs` |

## Test changes, by kind

Tests change only to fit the platform. A failing test is never edited just to make it pass: either the engine is
fixed, or the failure is listed as known with its reason (`tools/ci/known-failures/`).

| Kind | Change |
| --- | --- |
| **GDI+ availability** | The suite's four "is GDI+ available" checks probe for libgdiplus (libgdiplus.dylib on macOS) and screen out old libgdiplus builds (by version, and on RedHat, Fedora and openSUSE). They answer for the managed engine instead, on Linux, Windows and macOS |
| **Windows runs as Linux** | The suite's Windows checks (`PlatformDetection.IsWindows`, `[PlatformSpecific]`, two direct OS checks) mean "GDI+ is Windows' own", which it never is here. Windows answers as Linux and runs the same tests with the same expectations. The font-name test keeps its real OS check, since it depends on the fonts Windows installs |
| **Selection** | Microsoft's own categories: `category!=failing` and the non-Linux categories, as dotnet/runtime's Linux runs; then, in a second run with its own known failures, the tests those categories leave out ([TESTING.md](TESTING.md#the-unix-disabled-tests)) |
| **Test infrastructure** | arcade's Microsoft.DotNet.XUnitExtensions and RemoteExecutor are built from source (dotnet/runtime takes them from the dotnet-eng feed). xunit 2.4.1 and xunit.runner.visualstudio 2.4.5, the versions of the suite's time ([TESTING.md](TESTING.md)) |
