#!/usr/bin/env bash
# Runs System.Drawing.Common's test suite in Release after a build of drawingForCore.slnx (Release), as CI does, and
# fails only on failures that are not in tools/ci/known-failures/ (docs/TESTING.md). Two runs:
# - Microsoft's selection for Linux, on every platform: the GDI+ engine is drawingForCore's own everywhere, not
#   Windows' or libgdiplus.
# - The tests Microsoft disabled on Unix because libgdiplus failed them ([ActiveIssue] and [PlatformSpecific] traits),
#   with their own known failures (#12).
# Usage: tools/ci/run-tests.sh [results-directory]
set -uo pipefail
cd "$(dirname "$0")/../.."
# The absolute path (realpath -m is GNU only, and macOS has BSD tools).
mkdir -p "${1:-TestResults}"
OUT=$(cd "${1:-TestResults}" && pwd)
# Test names have non-ASCII characters, and Windows' Python writes and reads its code page otherwise.
export PYTHONUTF8=1

s=System.Drawing.Common.Tests
NOT_RUN="category!=nonnetcoreapptests&category!=OuterLoop&category!=IgnoreForCI"
SELECTION="category!=failing&category!=nonlinuxtests&category!=nonunixtests&$NOT_RUN"
# Left out of the Unix-disabled run: seven Exclude/Xor theories pass, but one case of each asks for the scans of an
# infinite region less a rectangle, which takes the engine 80 seconds (Skia rasterises the 8,388,608-pixel-high
# region a row at a time). The theories' data aren't serialisable, so the case can't be filtered out on its own.
SLOW="FullyQualifiedName!=System.Drawing.Tests.RegionTests.Exclude_Rectangle_Success&FullyQualifiedName!=System.Drawing.Tests.RegionTests.Exclude_RectangleF_Success&FullyQualifiedName!=System.Drawing.Tests.RegionTests.Exclude_GraphicsPath_Success&FullyQualifiedName!=System.Drawing.Tests.RegionTests.Xor_Rectangle_Success&FullyQualifiedName!=System.Drawing.Tests.RegionTests.Xor_RectangleF_Success&FullyQualifiedName!=System.Drawing.Tests.RegionTests.Xor_GraphicsPath_Success&FullyQualifiedName!=System.Drawing.Tests.RegionTests.Xor_Region_Success"
UNIX_DISABLED="(category=failing|category=nonlinuxtests|category=nonunixtests)&$NOT_RUN&$SLOW"

status=0
for run in "$s:$SELECTION" "$s.unix-disabled:$UNIX_DISABLED"; do
  name=${run%%:*}
  dotnet test "test/drawingForCore.$s/drawingForCore.$s.csproj" -c Release --no-build --filter "${run#*:}" \
    --logger "trx;LogFileName=$name.trx" --results-directory "$OUT"
  dotnet run tools/ci/check_failures.cs -- "$OUT/$name.trx" "tools/ci/known-failures/$name.txt" || status=1
done
dotnet run tools/ci/trx_summary.cs -- "$OUT/$s.trx" "$OUT/$s.unix-disabled.trx" > "$OUT/summary.md" || true
exit $status
