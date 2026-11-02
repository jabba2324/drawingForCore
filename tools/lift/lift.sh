#!/usr/bin/env bash
# Regenerates every lifted project from the upstream sources in a folder (tools/lift/fetch-sources.sh fetches them):
# src/drawingForCore.System.Drawing.Common, src/drawingForCore.System.Drawing and the four test projects, with their
# manifests in tools/lift/manifests/.
# Usage: tools/lift/lift.sh <folder with dotnet-runtime-6.0, arcade-6.0, runtime-assets and WebFormsForCore>
set -euo pipefail
cd "$(dirname "$0")/../.."
src=$(realpath "$1")
root=$(pwd)
python3 tools/lift/lift_drawing.py "$src/dotnet-runtime-6.0" "$root"
python3 tools/lift/lift_system_drawing.py "$src/WebFormsForCore" "$root"
python3 tools/lift/lift_drawing_tests.py "$src/dotnet-runtime-6.0" "$src/arcade-6.0" "$src/runtime-assets" "$root"
