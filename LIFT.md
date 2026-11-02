# LIFT.md

Record of everything lifted into or changed in drawingForCore, with each decision and its result.

**Target framework: net11.0 only.**

## Sources

| Repo | Commit | Lifted by | Into |
| --- | --- | --- | --- |
| dotnet/runtime, release/6.0 | `76b96800a40eb88d9ee163af68076d7fcc8216be` | `tools/lift/lift_drawing.py`, `lift_drawing_tests.py` | `src/drawingForCore.System.Drawing.Common`; `test/drawingForCore.System.Drawing.Common.Tests`, `test/drawingForCore.TestUtilities` |
| dotnet/arcade, release/6.0 | `565a800599c7f87a0aaa2fac740c1779ebb74344` | `lift_drawing_tests.py` | `test/drawingForCore.Microsoft.DotNet.XUnitExtensions`, `.RemoteExecutor` |
| dotnet/runtime-assets, release/6.0 | `848e27e0f5c1c2cb9bf8584d13370e273c18ee57` | `lift_drawing_tests.py` | the tests' `TestData` |
| webformsforcore/WebFormsForCore | `52f4172dbc09b3ad0b849c97642720fa7a49aa22` | `lift_system_drawing.py` | `src/drawingForCore.System.Drawing` |

`tools/lift/fetch-sources.sh <folder>` fetches exactly these (shallow, sparse clones of the directories the scripts
read) and `tools/lift/lift.sh <folder>` runs the three scripts. Full clones in the same folder names work too.

## History

drawingForCore was built inside [webForCore](https://github.com/jabba2324/webForCore), which needed System.Drawing on
Linux for System.Web, Web Pages' WebImage and DataVisualization's Chart. These sections are from webForCore's LIFT.md,
where the work happened; paths are given as they were then (`webForCore.*` projects).

### System.Drawing on libgdiplus: tried and rejected (30 Oct 2026, in webForCore)

System.Drawing.Common 6.0 (the last version with a Unix implementation) over libgdiplus 6.2 (Fedora) on .NET 11, with
`System.Drawing.EnableUnixSupport`. Drawing, text, PNG/JPEG/GIF and resizing work, and 79 of the 137 WebImage and Chart
tests that ran passed. But **libgdiplus corrupts the heap and aborts the process** (`double free or corruption`) when a
24bpp or 32bppRgb bitmap that has been drawn on is rotated by 90 or 270 degrees. A JPEG loads as 24bpp, so WebImage's
resize-then-rotate takes down the whole server (2 tests). It reproduces in a 6-line program. A native crash can't be
contained by webForCore's PseudoAppDomain, and the library parses untrusted images. Not adopted. The other failures were webForCore's
DataVisualization resources (19), Windows paths in the tests (9), missing Windows fonts (6) and save-path errors.
Next: a SkiaSharp-based System.Drawing, measured by the same 136 tests (below).

### System.Drawing on SkiaSharp (30 Oct 2026, in webForCore)

`src/webForCore.System.Drawing.Common` (now `src/drawingForCore.System.Drawing.Common`) replaced the System.Drawing.Common
package (Windows only since .NET 7) for the whole of webForCore (System.Web, System.Drawing, DataVisualization and
everything built on them). It is
built by `tools/lift/lift_drawing.py`:
- **Microsoft's code, unchanged except as listed:** System.Drawing.Common 6.0 (dotnet/runtime release/6.0), the
  csproj's own file list for a Unix build (199 files). That keeps the public API, argument validation, exceptions and
  .NET Framework-compatible behaviour of `Bitmap`, `Graphics`, `Image`, `Font`, `Icon`, ... The assembly keeps
  System.Drawing.Common's name and public key token, so references (and the shared framework's System.Drawing facade)
  resolve to it.
- **GDI+ in managed code on SkiaSharp** instead of libgdiplus: every `[DllImport("libgdiplus")]` (556 GDI+ flat API
  functions) becomes a call to `SkiaGdiPlus` (`tools/lift/additions/System.Drawing.Common/`), which implements 516 of
  them; the rest return GDI+'s NotImplemented status (generated stubs).
- **Lift fixups:** LibraryResolver no longer requires `System.Drawing.EnableUnixSupport` (it still loads libcups for
  printing); `ImageFormatConverter` throws NotSupportedException for an unknown name, as .NET Framework did (.NET Core
  3.0+ throws FormatException; Microsoft's System.Web.Helpers relies on the 4.8 behaviour).

The engine:
- **Images:** pixels as premultiplied BGRA (what Skia draws into), with the GDI+ pixel format kept apart and applied at
  the edges (LockBits, GetPixel, saving), so drawing never depends on it. GDI+'s reported formats (24bpp for JPEG,
  8bpp indexed for GIF, 32bppRgb for 32-bit BMP...), palettes, resolution, EXIF property items (read and written),
  GIF frames, delays and loop count, multi-page TIFF.
- **Codecs:** Skia decodes BMP, GIF, ICO, JPEG and PNG and encodes JPEG and PNG; drawingForCore's own code does what Skia
  cannot: BMP, GIF (halftone palette, LZW) and TIFF encoders, a baseline TIFF reader (none/LZW/PackBits; grey, palette,
  RGB, RGBA), EXIF and the GDI+ codec list. Formats GDI+ cannot read (WebP...) are rejected as on Windows.
- **Graphics:** an SKCanvas with GDI+'s state: world, page (unit and scale) and container transforms, device-space
  clipping, Save/Restore, smoothing, compositing and interpolation modes, and GDI+'s pixel-centre convention
  (PixelOffsetMode None puts pixel centres on integer coordinates; Skia's are at +0.5). DrawImage at physical size
  (resolution), source units, parallelograms, ImageAttributes.
- **Brushes, pens, paths, regions:** solid, hatch (8x8 patterns, the common styles exact), linear gradients
  (blends, preset colours, sigma/linear shapes, wrap modes, transforms), path gradients (drawn as GDI+ defines them,
  centre to boundary colours, as a vertex mesh), textures; pen widths, units, dash styles and caps, joins, anchor caps;
  GDI+ path data (arcs, ellipses and cardinal curves as GDI+'s Béziers), flatten, widen, warp, iterators; regions on
  Skia path operations.
- **Text:** families resolve through fontconfig; the Windows core fonts map to installed metric-compatible ones
  (Arial, Times New Roman, Courier New to Liberation; Calibri, Cambria to Carlito, Caladea) and keep their names, as
  libgdiplus did; GDI+-style layout: 1/6 em padding (not for GenericTypographic), word wrap, alignment, trimming,
  hotkey prefixes, character ranges, AddString.
- **ImageAttributes:** remap tables, colour keys, colour matrices (with grey modes), threshold, gamma, output channel,
  per adjust type, applied exactly on the CPU.

Not implemented: metafiles (EMF/WMF), X11/macOS surfaces,
SaveAdd (multi-frame saving), the encoder parameter list, pen compound arrays and alignment, path-gradient blends and
focus scales, ICC profiles, JPEG-compressed or tiled TIFF. Text: no font fallback for characters a font lacks, no
bidirectional or vertical layout, tabs as four spaces.

In webForCore, System.Web.Helpers.Test's 136 WebImage and Chart tests then ran (they had been skipped): **418/418**
(was 279 passed, 136 skipped). With libgdiplus, 79 of 137 had passed and the process crashed. Those tests stay in
webForCore, which tests its own use of System.Drawing.

### System.Drawing.Common's own tests (30 Oct 2026, in webForCore)

`tools/lift/lift_drawing_tests.py` lifts Microsoft's System.Drawing.Common 6.0 test suite (90 files) with what it needs:
TestUtilities (PlatformDetection, AssertExtensions) from dotnet/runtime, Microsoft.DotNet.XUnitExtensions and
RemoteExecutor from dotnet/arcade release/6.0 (built from source instead of the dotnet-eng feed), and the test data
from dotnet/runtime-assets release/6.0. Test files are unchanged. The suite's "is GDI+ available" checks probe for
libgdiplus and screen out old libgdiplus builds (by version, and on RedHat/Fedora/openSUSE); those four checks answer
for SkiaGdiPlus instead (fixups in the script). Selection is upstream's (`category!=failing` on Linux).

Result (tools/lift/test-results, round 11): **3,716 of 3,883 pass** in Microsoft's Linux selection; of the 151
failures, 141 are metafiles, 8 are deliberate differences from libgdiplus or .NET Core (listed there), 1 compares
against Windows' pixels and 1 is premultiplied-alpha rounding. Of the 1,008 tests Microsoft disabled on Unix because
libgdiplus failed them, 560 pass. The first run had 563 failures in the Linux selection.

Engine changes the suite drove, each following Windows GDI+ as the tests record it:
- **Lift:** `static extern unsafe` imports were not rewritten (GdipGetMatrixElements(float*) still called libgdiplus).
  Every Graphics function now returns ObjectBusy while the Graphics' HDC is out (generated in the lift).
- **Pixels:** the GDI+-to-Skia shift is 0.5 - 1/32, not 0.5: Skia's aliased rasterizer rounds an edge on a pixel
  centre the other way from GDI+, which put fills, clips and even-width pens one pixel off. Aliased 1 px lines cover
  both end pixels.
- **Matrix:** IsIdentity within 0.0001; finite overflow saturates at float.MaxValue (infinities stay); non-finite
  matrices are not invertible; invalid MatrixOrder is rejected (on brushes and pens Multiply treats it as Append and
  rejects non-invertible matrices); world transforms stay invertible.
- **LinearGradientBrush:** GDI+'s model, a horizontal gradient across the rectangle mapped by the brush transform; the
  constructor's angle is in Transform (rotation about the centre, scaled to span the corners).
- **Graphics:** BeginContainer resets the settings and clip (the outer clip becomes the container's), folds the page
  transform into the container and rejects World/Display units; DrawImage rejects World/Display source units and
  overflows past GDI+'s 28.4 fixed-point range.
- **Handles:** HDC, HICON, HBITMAP and HRGN are handles (libgdiplus's convention, which System.Drawing's Unix code
  assumes); region data is written and read in GDI+'s format (one node per region; any node tree is read).
- **Images:** ImageFlags as GDI+ (memory bitmaps HasAlpha only; HasRealDpi only when the file states a resolution;
  JPEG's colour space), icons decode the image nearest 16x16, PNG/TIFF write alpha unless the format has none
  (32bppRgb keeps it), default palette flags, missing files are ArgumentException.
- **Validation and details:** custom line caps, pen caps and dash patterns, path reverse markers and warp, region
  normalisation and half-open containment, MeasureCharacterRanges ranges and negative layouts, ImageAttributes flags,
  path-gradient bounds, surround colours and 256/511-point sigma blends, ToLogFont's charset, codec masks.


### Windows (1 Nov 2026, in webForCore #46)

Microsoft's tests choose their expectations by OS (`PlatformDetection.IsWindows`, `[PlatformSpecific]`), meaning
"GDI+ is Windows' own". Here it is SkiaGdiPlus on both, so `lift_drawing_tests.py` makes Windows answer as Linux (its
fixups apply to arcade's files too). Windows runs the 3,882 tests Linux runs, with the same 151 known failures.
`FontConverterTests` keeps its real OS check, because "Times" matching "Times New Roman" is about Windows' fonts.

## Moved to drawingForCore (2 Nov 2026)

System.Drawing moved out of webForCore into this repository
([webForCore#53](https://github.com/jabba2324/webForCore/issues/53)): it is useful to any .NET Framework code that
draws, it rarely changes, and its 151 known failures are a project of their own. webForCore now references the
packages.

What moved, and what changed in moving:
- **Projects:** `webForCore.System.Drawing.Common` → `drawingForCore.System.Drawing.Common` (package
  `webForCore.Drawing.Common` → `drawingForCore.Common`); `WebFormsForCore.Drawing` → `drawingForCore.System.Drawing`
  (package `webForCore.Drawing` → `drawingForCore`); the four test projects `webForCore.*` → `drawingForCore.*`.
  Assembly names, versions (System.Drawing.Common 11.0.0.0, System.Drawing 13.5.0.0) and public key tokens are
  unchanged.
- **The lifted files are the same.** The lift output was compared with webForCore's copies: identical except the
  comment marker (`// webForCore:` → `// drawingForCore:`) and the issue number `(#46)`, which referred to webForCore's
  issues, removed from the test fixups' comments.
- **System.Drawing is lifted too.** In webForCore, `src/WebFormsForCore.Drawing` was part of the fork base, unchanged
  from WebFormsForCore except its project file. Here `tools/lift/lift_system_drawing.py` copies it from WebFormsForCore
  at the fork commit and generates the project file.
- **System.Drawing forwards three types it used to duplicate.** WebFormsForCore's System.Drawing defined its own
  `FontConverter` (from dotnet/runtime), `ToolboxBitmapAttribute` (a stub that ignored its arguments) and
  `BitmapSuffixInSatelliteAssemblyAttribute`, with their forwards commented out in `AssemblyInfo.cs`.
  System.Drawing.Common 6.0 defines all three, so an application referencing both (as the `drawingForCore` package
  does, and webForCore's packages did) and naming one didn't compile: "error CS0433: The type 'FontConverter' exists in
  both 'System.Drawing.Common, Version=11.0.0.0' and 'System.Drawing, Version=13.5.0.0'". The new package smoke test
  hit it. Those three files aren't lifted; the three forwards are restored (fixups in the script), so only
  `AssemblyInfo.cs` is lifted. Nested types (`FontConverter.FontNameConverter`, `FontUnitConverter`) are forwarded with
  their containing type; their own commented-out forwards stay as they are. The smoke test names `FontConverter`, and
  checks that `System.Drawing` forwards it and its nested types.
- **Public signing instead of FakeStrongName.** webForCore built these assemblies with its own key and rewrote the
  public key afterwards (WebFormsForCore's FakeStrongName task, which needs Mono.Cecil and prebuilt build tools). Here
  the SDK's public signing does it: `keys/System.Drawing.Common.snk` and `keys/System.Drawing.snk` hold only
  Microsoft's public keys, and the projects set `SignAssembly` and `PublicSign`, as dotnet/runtime's own
  open-source builds do. The assemblies have the same identities (token cc7b13ffcd2ddd51 and b03f5f7f11d50a3a), checked
  by the package smoke test. No SHA-1 signing happens, so Fedora's OpenSSL policy no longer matters
  (`OPENSSL_ENABLE_SHA1_SIGNATURES` isn't needed).
- **Full commits in the manifests.** The scripts recorded abbreviated commits (`git rev-parse --short`), whose length
  depends on the clone: CI's shallow clone and a full clone gave `565a800` and `565a8005`. The manifests now record the
  full commit; project-file comments keep the first seven characters.
- **The lift is checked in CI.** `build-and-test` fetches the sources, lifts, regenerates `docs/lifted-changes.md`, and
  fails if anything differs from what is committed. webForCore didn't check this.
- **The package smoke test** (`tools/ci/package-smoke`) is new: an application that references only the `drawingForCore`
  package draws, saves and reloads PNG, JPEG and GIF, checks pixels and formats, uses `FontConverter` through
  `TypeDescriptor`, and checks the assemblies' identities.
- **The NuGet feed:** a static NuGet v3 feed on GitHub Pages, generated by Sleet 7.2.0 from the latest releases' packages
  after each release (`.github/workflows/feed.yml`). No account, token or server.

Result (Linux x64, Release, lifted from fresh shallow clones): **3,883 tests, 3,716 pass, 16 skipped, 151 fail, all
known**, the same as webForCore's last local run. In CI, on both platforms: 3,882 tests, 3,705 pass, 26 skipped, 151
known failures, the same as webForCore's CI (ten printing tests need an installed printer, which the runners lack). Packing gives `drawingForCore` and `drawingForCore.Common` with symbol
packages. The package smoke test passes. A feed made by Sleet from those packages, served over HTTP, restores and runs
the smoke test (NuGet downloads both packages from the feed's flat container).

## Open items

- **Metafiles** (EMF/WMF): 141 of the 151 known failures. Not implemented.
- **The 1,008 tests Microsoft disabled for libgdiplus:** 560 pass. Bringing those that pass into the selection would
  widen what CI checks.
- **macOS** isn't tested. SkiaSharp ships its native assets.
