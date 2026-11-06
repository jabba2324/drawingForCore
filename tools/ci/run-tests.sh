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
UNIX_DISABLED="(category=failing|category=nonlinuxtests|category=nonunixtests)&$NOT_RUN"

status=0
for run in "$s:$SELECTION" "$s.unix-disabled:$UNIX_DISABLED"; do
  name=${run%%:*}
  # --blame: if the test host crashes, the log names the test that was running.
  dotnet test "test/drawingForCore.$s/drawingForCore.$s.csproj" -c Release --no-build --blame --filter "${run#*:}" \
    --logger "trx;LogFileName=$name.trx" --results-directory "$OUT"
  dotnet run tools/ci/check_failures.cs -- "$OUT/$name.trx" "tools/ci/known-failures/$name.txt" || status=1
done
dotnet run tools/ci/trx_summary.cs -- "$OUT/$s.trx" "$OUT/$s.unix-disabled.trx" > "$OUT/summary.md" || true
exit $status
