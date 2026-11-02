#!/usr/bin/env bash
# Runs System.Drawing.Common's test suite in Release after a build of drawingForCore.slnx (Release), as CI does, and
# fails only on failures that are not in tools/ci/known-failures/ (docs/TESTING.md).
# The selection is Microsoft's for Linux, on every platform: the GDI+ engine is drawingForCore's own everywhere, not
# Windows' or libgdiplus.
# Usage: tools/ci/run-tests.sh [results-directory]
set -uo pipefail
cd "$(dirname "$0")/../.."
OUT=$(realpath -m "${1:-TestResults}")
mkdir -p "$OUT"
# Test names have non-ASCII characters, and Windows' Python writes and reads its code page otherwise.
export PYTHONUTF8=1

FILTER="category!=failing&category!=nonlinuxtests&category!=nonnetcoreapptests&category!=OuterLoop&category!=IgnoreForCI&category!=nonunixtests"
s=System.Drawing.Common.Tests
dotnet test "test/drawingForCore.$s/drawingForCore.$s.csproj" -c Release --no-build --filter "$FILTER" \
  --logger "trx;LogFileName=$s.trx" --results-directory "$OUT"
python3 tools/ci/trx_summary.py "$OUT/$s.trx" > "$OUT/summary.md" || true
python3 tools/ci/check_failures.py "$OUT/$s.trx" "tools/ci/known-failures/$s.txt"
