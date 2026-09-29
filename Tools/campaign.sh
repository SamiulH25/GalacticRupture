#!/usr/bin/env bash
# Scripted campaign opening, fullscreen screenshots with UI.
# Usage: Tools/rts.sh <output-dir> [--no-build]
set -euo pipefail
cd "$(dirname "$0")/.."
out="${1:?output dir}"
mkdir -p "$out"
if [[ "${2:-}" != "--no-build" ]]; then
  unity --no-banner run . --editor-version 6000.6.3f1 --timeout 1500 -- \
    -executeMethod GalacticRupture.EditorTools.PlayerBuild.BuildAllAndLinux -logFile "$out/build.log" >/dev/null 2>&1 || {
    grep -E 'error CS|\[build\]|Shader error' "$out/build.log" | sort -u | head -20; exit 1; }
  grep -E '\[build\]' "$out/build.log" || true
  grep -E 'Shader error' "$out/build.log" | sort -u | sed 's/^/shader errors: /' || true
fi
rm -f "$out"/camp_*.png
timeout 300 ./Builds/Linux/GalacticRupture.x86_64 -screen-width 1920 -screen-height 1080 -screen-fullscreen 1 \
  -grCaptureCampaign "$out" -logFile "$out/player.log"
grep -E '\[campaign\]|Exception' "$out/player.log" | sort -u
