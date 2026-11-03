# Contributing to drawingForCore

drawingForCore runs Microsoft's System.Drawing.Common on .NET 10 on Linux as well as Windows, with GDI+ written in
managed code on SkiaSharp. A change is welcome when it brings the engine closer to what Windows GDI+ does, or
System.Drawing closer to .NET Framework, and when it can prove it with a test.

## The principles

1. **Microsoft's tests are the measure.** The engine aims for Windows GDI+'s behaviour as Microsoft's own tests record
   it. A fix that makes a known failure pass removes it from `tools/ci/known-failures/System.Drawing.Common.Tests.txt`
   in the same change; CI reports known failures that pass.

2. **Every change comes with a test.** Where Microsoft's suite covers the behaviour, that is the test: a known failure
   that now passes, or one of the 1,008 tests Microsoft disabled for libgdiplus (outside the selection) that now
   passes. Otherwise add drawingForCore's own test, and say in the pull request that it failed before the change.

3. **Avoid changing Microsoft's code.** Microsoft's System.Drawing.Common is lifted unchanged except for a short list
   of changes ([`docs/MODIFICATIONS.md`](docs/MODIFICATIONS.md)). Its value is that it *is* Microsoft's code: its API,
   argument checks and exceptions. Fix the engine (`tools/lift/additions/System.Drawing.Common/`) instead; almost every
   difference so far was the engine's.

4. **When Microsoft's code must change, keep the record.** Keep the change as small as it can be and:
   - make it a fixup in the lift script (`tools/lift/lift_drawing.py` or `lift_drawing_tests.py`), never an edit of
     the lifted file, with a `// drawingForCore:` comment that says why;
   - run the lift again (`tools/lift/fetch-sources.sh <folder>`, then `tools/lift/lift.sh <folder>`);
   - regenerate [`docs/lifted-changes.md`](docs/lifted-changes.md) (`python3 tools/lift/changes_report.py <folder>`);
   - add the reason to [`LIFT.md`](LIFT.md) and the kind of change to [`docs/MODIFICATIONS.md`](docs/MODIFICATIONS.md).

5. **Generated files are output.** `src/` and `test/` are made by the lift scripts. Edit the scripts or
   `tools/lift/additions/`, then lift again. CI regenerates them and fails if they differ from what is committed.

6. **Report numbers.** A change to the engine says what the suite gave before and after: tests run, passed, failed and
   skipped.

7. **Every change names what needs it.** Say which application hit the problem, what it did and what its user saw
   (the [webForCore smoke test](https://github.com/jabba2324/webForCore-smoke-test)'s applications are a good source),
   or which of Microsoft's tests records the behaviour. Synthetic edge cases are fine as extra tests, but the reason
   for a change is an application or GDI+'s recorded behaviour.

## How drawingForCore's own code is written

These are webForCore's rules for its own code (its CONTRIBUTING.md, 6–9), applied here to the engine
(`tools/lift/additions/`), the lift scripts and the tools. They don't apply to Microsoft's lifted code (principle 3
keeps that as it is). The aim is the same: code a reviewer can see is correct, that keeps working when .NET or
SkiaSharp is patched, and that fails safely.

8. **No reflection into internals.** Don't read, write or call non-public members of the runtime, of SkiaSharp, or of
   Microsoft's System.Drawing.Common types, and don't depend on their private names or layout. Use public APIs, or the
   data Microsoft's code passes through the GDI+ flat API. If a behaviour needs something that isn't exposed, document
   the difference with a test instead.

9. **No unsafe code and no GC tricks, except at the native boundary.** The engine stands in for `gdiplus.dll`, so its
   flat API functions take what Microsoft's code passes to GDI+: pointers and memory blocks in GDI+'s layouts
   (`LockBits` buffers, point and rectangle arrays, palettes, `EncoderParameters`). Reading and writing those, with
   pointers or `Marshal`, in the function that receives them is the native contract, and is allowed there only:
   convert to managed types (arrays, spans, objects) at that edge, and keep the rest of the engine free of pointers.
   Nothing else: no `Unsafe`/`MemoryMarshal` reinterpretation, no P/Invoke of the engine's own (SkiaSharp's are
   SkiaSharp's), and nothing that steers the garbage collector: no `GC.Collect`, `GC.WaitForPendingFinalizers` or
   `GC.AddMemoryPressure`, no `GCHandle`s, no polling weak references. A GDI+ handle is an entry in the engine's
   handle table, removed by the matching `Gdip*Delete`/`Dispose` function, which the wrapper's `Dispose` or finaliser
   calls.

10. **Slow, secure and stable beats fast.** Don't optimise before an application shows a problem with a measurement.
    Choose the simplest design that is clearly correct over a faster one that is harder to reason about. When
    performance does need work, the pull request includes the measurement.

11. **Programs and tools are .NET.** Everything this repository builds, and every tool it uses (CI helpers, checks),
    is written in C# as a project or as a file-based app run with `dotnet run tool.cs`. No Python, Node.js, Ruby, Perl
    or other general-purpose language. Shell scripts (bash, PowerShell) and CI YAML are fine as glue that calls
    dotnet, git, gh and curl. Anything that parses data or makes decisions belongs in C#. Front-end code
    (JavaScript, TypeScript, CSS, HTML and anything else that runs in the browser) is fine too.

Code written before rules 8–10 that breaks them is listed in #3, and Python written before rule 11 in #5. Each item is
removed or replaced when it is next changed. New code doesn't add to either list.

## Pull requests

- One problem per commit, with `Fixes #<issue>` in the message where there is one.
- The pull request lists, for each change: the problem, what needs it (principle 7), the fix, the test and the suite's
  numbers (principle 6), and whether Microsoft's code changed (and where that is documented).
- CI (`.github/workflows/ci.yml`) checks the lift, builds, runs the tests, packs the packages and runs the package
  smoke test, on Linux and on Windows; both must pass.

## Releases

1. Set `<Version>` in `Directory.Build.props` and write `docs/release-notes/<version>.md`.
2. Update the test numbers in README.md and docs/TESTING.md.
3. Tag the merged commit `v<version>`. CI builds and tests the tag on both platforms, creates the GitHub release with
   the packages, and rebuilds the NuGet feed.

Licences: drawingForCore's own code is MIT, and Microsoft's code keeps its MIT licence
([`docs/LICENSING.md`](docs/LICENSING.md)). Contributions are accepted under the MIT License.
