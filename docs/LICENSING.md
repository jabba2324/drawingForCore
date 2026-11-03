# Licensing

A plain-language summary. [NOTICE](../NOTICE) is the authoritative list of components, sources and copyright holders.
This isn't legal advice.

## Can I use drawingForCore in a commercial product?

Yes. Everything in the packages is under the MIT License.

| Part | Where | Licence | Commercial use |
| --- | --- | --- | --- |
| System.Drawing.Common 6.0 (Microsoft's code) | `src/drawingForCore.System.Drawing.Common` | MIT (© .NET Foundation) | ✅ Yes |
| The GDI+ engine on SkiaSharp | `src/drawingForCore.System.Drawing.Common/SkiaGdiPlus` | MIT (© drawingForCore contributors) | ✅ Yes |
| System.Drawing (type forwards, from WebFormsForCore) | `src/drawingForCore.System.Drawing` | MIT (© Simon Jakob Egli) | ✅ Yes |
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

Public signing is the SDK's (`SignAssembly` and `PublicSign` in the projects), as dotnet/runtime's own open-source
builds do it: `keys/System.Drawing.Common.snk` and `keys/System.Drawing.snk` hold only Microsoft's public keys, so the
assemblies carry public key tokens cc7b13ffcd2ddd51 and b03f5f7f11d50a3a, which the package smoke test checks. No
SHA-1 signing happens, so no OpenSSL setting is needed to build (webForCore, which rewrote the key after building with
WebFormsForCore's FakeStrongName task, needs `OPENSSL_ENABLE_SHA1_SIGNATURES=1` on Fedora and RHEL).

## What isn't in the repository

The fonts Microsoft's tests use (Code New Roman) aren't committed or shipped: their licence text both grants the SIL
Open Font License 1.1 and forbids redistribution. `tools/ci/fetch-test-fonts.sh` downloads them for test runs.
