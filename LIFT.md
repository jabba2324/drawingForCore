# LIFT.md

Record of everything lifted into or changed in drawingForCore, with each decision and its result.

**Target framework: net10.0 only** (.NET 10 LTS, from 0.2.0; 0.1.0 was net11.0).

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

drawingForCore was built inside [webForCore](https://github.com/xforcore/webForCore), which needed System.Drawing on
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
([webForCore#53](https://github.com/xforcore/webForCore/issues/53)): it is useful to any .NET Framework code that
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

## .NET 10 LTS (3 Nov 2026, 0.2.0)

webForCore moved from .NET 11 RC to .NET 10 LTS (webForCore #55, #58), and a `net11.0` package can't be restored by a
`net10.0` application, so drawingForCore moves with it.

- The lift scripts (`lift_drawing.py`, `lift_drawing_tests.py`, `lift_system_drawing.py`) generate `net10.0` projects,
  and the package smoke test is `net10.0`. Lifting again changed only the six project files' target framework and
  description; no source file changed, and `docs/lifted-changes.md` is unchanged.
- `global.json` pins SDK 10.0.100 with `latestFeature`; CI and the feed workflow install `10.0.x`.
- The assemblies keep their identities: System.Drawing.Common 11.0.0.0 and System.Drawing 13.5.0.0, as before.
- Nothing else changed: no package version moved (SkiaSharp 3.119.0, xunit), and the code is the same as 0.1.0.

Result (Linux x64, Release, SDK 10.0.403, runtime 10.0.14): **3,883 tests, 3,716 pass, 16 skipped, 151 fail, all
known**, the same as on .NET 11. Packing gives both packages with `lib/net10.0`, and the package smoke test passes.

## The code rules (3 Nov 2026)

drawingForCore takes webForCore's rules for its own code (CONTRIBUTING.md, 8–10): no reflection into internals, no
unsafe code or GC tricks outside the native boundary, slow and stable over fast. webForCore's list of code that broke
them (webForCore #57) had the engine's on it, "moving to drawingForCore"; what the move left is fixed here or listed
in #3.

- **Handles:** a GDI+ handle was a strong `GCHandle` to the engine's object. It is now a number in the engine's handle
  table (a dictionary under a lock), removed by the matching `Gdip*Delete`/`Dispose` function. An unknown or freed
  handle is InvalidParameter instead of a crash. Objects are freed exactly as before: the wrappers' `Dispose` or
  finalisers call the delete functions. (Checked before the change with 0.2.0: 6,000 undisposed 512×512 bitmaps and
  20,000 `ToolboxBitmapAttribute` lookups, whose constructor loads a bitmap as on Windows, ended at 75 MB.)
- **JPEG quality:** read from GDI+'s `EncoderParameters` block, in the layout Microsoft's
  `EncoderParameters.ConvertToMemory` writes, instead of reflecting on `EncoderParameter._parameterValue`.
- **`GdipGetNearestColor`:** answered with `Unsafe.SkipInit`, on the idea that the colour came in through the out
  variable. Microsoft's Unix `Graphics.GetNearestColor` passes none (dotnet/runtime#22221; its test is disabled on
  Unix), so the answer is 0, as it was.
- **`Opaque`** used `MemoryMarshal` to write through a read-only span; SkiaSharp 3's `SKBitmap.GetPixelSpan()` is
  writable.
- **Left (#3):** pointers past the boundary. The integer overloads convert their points to `stackalloc`/`fixed`
  floats and call the pointer-taking float functions, and the codecs read pixels through `GpImage.ReadPixels(byte*)`.
  They are to take spans, with the flat API functions as thin pointer wrappers.

Result (Linux x64, Release, .NET 10.0.14): **3,883 tests, 3,716 pass, 16 skipped, 151 fail, all known**, the same as
0.2.0.

## Pointers at the edge (#3, 3 Nov 2026)

What #3 listed, re-inventoried first: 14 `unsafe` modifiers, 9 `fixed`, 3 `stackalloc`, 39 lines of `Marshal` calls and
48 flat API functions that take typed pointers. No `GC`, `GCHandle`, `MemoryMarshal`, `Unsafe`, reflection or P/Invoke
of the engine's own. Fixed here:

- **Pixel buffers:** `GpImage.ReadPixels`/`WritePixels` take a span, the offset of the first row and a stride that can
  be negative, and read and write 16-bit channels with `BinaryPrimitives` (little-endian, as GDI+'s formats are).
  `GdipBitmapLockBits`, `GdipBitmapUnlockBits` and `GdipCreateBitmapFromScan0` make the span from the pointer; the BMP
  and GIF encoders pass their arrays. Before, the encoders pinned their arrays (`Codecs.cs` 505, 531) and the three
  flat functions passed `byte*` on (`Images.cs` 488, 575, 596).
- **Flat functions calling flat functions with pointers:** the integer overloads (`GdipTransformPointsI`,
  `GdipDrawRectanglesI`, `GdipFillRectanglesI`, `GdipDrawImagePointsI`, `GdipDrawImagePointsRectI`,
  `GdipCreatePathGradientI`, `GdipCreateMatrix3I`), `GdipPathIterEnumerate` and `GdipGetMatrixElements(float*)`. Each
  now reads its own pointers and calls a managed core.
- **`EncoderParameters`:** read in `GdipSaveImageToFile` and `GdipSaveImageToDelegate_linux`, which receive the block,
  instead of in `Codecs.Encode`.
- The `unsafe` modifiers on `Bmp.Write`, `Gif.Write`, `Tiff.Write` and the stubs' class, which use no pointers.

`Images.cs` 35 and 120, listed in #3, were already at the edge: they pin a buffer for Microsoft's `byte*` stream
delegates (`StreamGetBytesDelegate`, `StreamPutBytesDelegate`) in the function that receives them.

Left, waiting on a decision about what counts as the edge: private helpers of the flat API class that turn a pointer
into managed data at once (`Graphics.cs` `Pts`/`PtsF`, 20 callers; `Images.cs` `CodecListSize`/`CodecList`), and
`GpImage` keeping the LockBits buffer as an `IntPtr` that its `Dispose` frees.

drawingForCore's own tests (`test/.../Engine/NativeBoundaryTests.cs`, 94) were written and recorded on the engine
before the change, for what Microsoft's suite doesn't cover: LockBits in every format with GDI+'s buffer and the
caller's (negative and wider strides), Scan0 with a negative stride, the BMP and GIF encoders' bytes, JPEG quality from
`EncoderParameters`, and the integer overloads' pixels against the float ones. They pass unchanged after it.

Result (Linux x64, Release, .NET 10.0.14, with a printer): **3,977 tests (3,883 and the 94 new), 3,810 pass, 16
skipped, 151 fail, all known**, before and after the change.

## Submodules instead of the lift (xforcore/webForCore#61, 3 Nov 2026)

Microsoft's code comes from three submodules in `external/`, each a shallow checkout of a cleared-down fork branch:

| Submodule | Branch | Upstream | Files | Change commits |
| --- | --- | --- | --- | --- |
| `external/runtime` | xforcore/runtime `drawingforcore` | `76b9680` (release/6.0) | 325 | 10 |
| `external/arcade` | xforcore/arcade `drawingforcore` | `565a800` | 46 | 1 |
| `external/runtime-assets` | xforcore/runtime-assets `drawingforcore` | `848e27e` | 59 | 0, and one that removes the fonts |

How the projects use them:
- They compile the submodules' files through `Link` items at their old paths, next to drawingForCore's own files: the
  engine in `SkiaGdiPlus/`, the generated stubs and `SR.g.cs`, and System.Drawing's `AssemblyInfo.cs`, which came from
  WebFormsForCore and stays here.
- The tests' data is listed file by file, copied to the output as the `TestData/**` glob did. The five files that are
  also embedded resources are copied too, as before.
- The Code New Roman fonts aren't redistributable, so the runtime-assets branch removes them, and
  `tools/ci/fetch-test-fonts.sh` still downloads them.

Removed: `tools/lift/` (the scripts, the manifests and `additions/`, whose 11 engine files had identical copies in
`SkiaGdiPlus/`), `docs/lifted-changes.md`, and CI's lift check, which was the last Python in the repository. CI checks
out submodules on both platforms.

Result (Linux x64, Release, .NET 10.0.14): **3,883 tests, 3,716 pass, 16 skipped, 151 fail, all known**, the same as
0.2.0.

## Open items

- **Metafiles** (EMF/WMF): 141 of the 151 known failures. Not implemented.
- **The 1,008 tests Microsoft disabled for libgdiplus:** 560 pass. Bringing those that pass into the selection would
  widen what CI checks.
- **macOS** isn't tested. SkiaSharp ships its native assets.
