#!/usr/bin/env bash
# Fetches the upstream sources drawingForCore is lifted from, at the commits it was lifted from, into a folder: only
# the directories the lift scripts read (shallow, sparse and blobless clones). CI uses it to check that the committed
# source is what the lift scripts produce; locally, full clones in the same folder names work too.
# Usage: tools/lift/fetch-sources.sh <folder>
set -euo pipefail
out=$(realpath -m "$1")
mkdir -p "$out"

fetch() {  # name, GitHub repository, commit, paths...
  local name=$1 repo=$2 commit=$3; shift 3
  local dir="$out/$name"
  if [ "$(git -C "$dir" rev-parse HEAD 2>/dev/null)" = "$commit" ]; then echo "$name: already at ${commit:0:7}"; return; fi
  rm -rf "$dir"
  git init -q "$dir"
  git -C "$dir" remote add origin "https://github.com/$repo.git"
  git -C "$dir" sparse-checkout set --no-cone "$@"
  git -C "$dir" fetch -q --depth 1 --filter=blob:none origin "$commit"
  git -C "$dir" -c advice.detachedHead=false checkout -q FETCH_HEAD
  echo "$name: $repo ${commit:0:7}"
}

fetch dotnet-runtime-6.0 dotnet/runtime 76b96800a40eb88d9ee163af68076d7fcc8216be \
  /src/libraries/System.Drawing.Common/ /src/libraries/Common/
fetch arcade-6.0 dotnet/arcade 565a800599c7f87a0aaa2fac740c1779ebb74344 \
  /src/Microsoft.DotNet.XUnitExtensions/ /src/Microsoft.DotNet.RemoteExecutor/
fetch runtime-assets dotnet/runtime-assets 848e27e0f5c1c2cb9bf8584d13370e273c18ee57 \
  /src/System.Drawing.Common.TestData/ /src/System.ComponentModel.TypeConverter.TestData/ \
  /src/System.Windows.Extensions.TestData/
fetch WebFormsForCore webformsforcore/WebFormsForCore 52f4172dbc09b3ad0b849c97642720fa7a49aa22 \
  /src/WebFormsForCore.Drawing/
