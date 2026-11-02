# Contributing to drawingForCore

drawingForCore runs Microsoft's System.Drawing.Common on .NET 11 on Linux as well as Windows, with GDI+ written in
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

## Pull requests

- One problem per commit, with `Fixes #<issue>` in the message where there is one.
- CI (`.github/workflows/ci.yml`) checks the lift, builds, runs the tests, packs the packages and runs the package
  smoke test, on Linux and on Windows; both must pass.

## Releases

1. Set `<Version>` in `Directory.Build.props` and write `docs/release-notes/<version>.md`.
2. Update the test numbers in README.md and docs/TESTING.md.
3. Tag the merged commit `v<version>`. CI builds and tests the tag on both platforms, creates the GitHub release with
   the packages, and rebuilds the NuGet feed.

Licences: drawingForCore's own code is MIT, and Microsoft's code keeps its MIT licence
([`docs/LICENSING.md`](docs/LICENSING.md)). Contributions are accepted under the MIT License.
