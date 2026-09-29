#!/usr/bin/env bash
# Unit-count benchmark (IDEAS.md §8.8). Builds the player unless --no-build, runs -grBench.
# Usage: Tools/bench.sh <output-dir> [--no-build]   -> <output-dir>/bench.csv, bench_<n>.png
set -euo pipefail
cd "$(dirname "$0")/.."
out="${1:?output dir}"
mkdir -p "$out"
if [[ "${2:-}" != "--no-build" ]]; then
  unity --no-banner run . --editor-version 6000.6.3f1 --timeout 1500 -- \
    -executeMethod GalacticRupture.EditorTools.PlayerBuild.BuildAllAndLinux -logFile "$out/build.log" >/dev/null 2>&1 || {
    grep -E 'error CS|\[build\]|Shader error' "$out/build.log" | sort -u | head -20; exit 1; }
fi
timeout 600 ./Builds/Linux/GalacticRupture.x86_64 -screen-width 1920 -screen-height 1080 -screen-fullscreen 1 \
  -grBench "$out" -logFile "$out/player.log"
cat "$out/bench.csv"
