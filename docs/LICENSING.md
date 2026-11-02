# Licensing

A plain-language summary. [NOTICE](../NOTICE) is the authoritative list of components, sources and copyright holders.
This isn't legal advice.

## Can I use drawingForCore in a commercial product?

Yes. Everything in the packages is under the MIT License.

| Part | Where | Licence | Commercial use |
| --- | --- | --- | --- |
| System.Drawing.Common 6.0 (Microsoft's code) | `src/drawingForCore.System.Drawing.Common` | MIT (© .NET Foundation) | ✅ Yes |
| The GDI+ engine on SkiaSharp | `tools/lift/additions/System.Drawing.Common` (copied to `SkiaGdiPlus/`) | MIT (© drawingForCore contributors) | ✅ Yes |
| System.Drawing (from WebFormsForCore; FontConverter from .NET) | `src/drawingForCore.System.Drawing` | MIT (© Simon Jakob Egli; © .NET Foundation) | ✅ Yes |
| SkiaSharp and Skia (from NuGet) | restored at build time | MIT (© Microsoft, Xamarin); Skia BSD 3-Clause (© Google) | ✅ Yes |
| Tests and test infrastructure from dotnet/runtime, arcade and runtime-assets | `test/` | MIT (© .NET Foundation) | ✅ Yes (not in the packages) |

### What the licences ask of you

| Licence | When you distribute the code or binaries (including inside a commercial product) |
| --- | --- |
| MIT | Include the copyright notice and the licence text |
| BSD 3-Clause (Skia, inside SkiaSharp's native library) | The same as MIT, and don't use the authors' names to endorse your product |

## Names and signatures

The assemblies keep Microsoft's names (System.Drawing.Common, System.Drawing) and public keys, so that code compiled
against them binds without changes. They are public signed with those public keys, not signed by Microsoft, and
drawingForCore isn't affiliated with or endorsed by Microsoft.

## What isn't in the repository

The fonts Microsoft's tests use (Code New Roman) aren't committed or shipped: their licence text both grants the SIL
Open Font License 1.1 and forbids redistribution. `tools/ci/fetch-test-fonts.sh` downloads them for test runs.
