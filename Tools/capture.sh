#!/usr/bin/env bash
# Regenerates assets, builds the Linux player, runs it in capture mode.
# Usage: Tools/capture.sh <output-dir>   (screenshots land in <output-dir>/*.png)
set -euo pipefail
cd "$(dirname "$0")/.."
out="${1:?output dir}"
mkdir -p "$out"
unity --no-banner run . --editor-version 6000.6.3f1 --timeout 1500 -- \
  -executeMethod GalacticRupture.EditorTools.PlayerBuild.BuildAllAndLinux -logFile "$out/build.log" >/dev/null 2>&1 || {
  grep -E 'error CS|\[build\]|Shader error' "$out/build.log" | sort -u | head -20; exit 1; }
grep -E '\[build\]' "$out/build.log" || true
rm -f "$out"/*.png
timeout 240 ./Builds/Linux/GalacticRupture.x86_64 -screen-width 1280 -screen-height 720 -screen-fullscreen 0 \
  -grCapture "$out" -logFile "$out/player.log"
grep -E '\[capture\]|Exception' "$out/player.log" | sort -u
