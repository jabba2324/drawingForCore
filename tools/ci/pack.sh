#!/usr/bin/env bash
# Packs drawingForCore's NuGet packages after a Release build of drawingForCore.slnx.
# Usage: tools/ci/pack.sh <version> <output folder>
set -euo pipefail
cd "$(dirname "$0")/../.."
version=$1; out=$(realpath -m "$2")
mkdir -p "$out"
for project in src/*/*.csproj; do
  dotnet pack "$project" -c Release --no-build -nodeReuse:false -p:Version="$version" -o "$out" -v q -nologo
done
echo "$(ls "$out"/*.nupkg | wc -l) packages in $out"
