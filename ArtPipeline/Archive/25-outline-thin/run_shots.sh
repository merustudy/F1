#!/bin/bash
# Round 25: the game's own screenshots with the thinner outlines, without touching the working tree. No API call.
#
#   ArtPipeline/Archive/25-outline-thin/run_shots.sh <scratch dir>
#
# The project (Assets, Packages, ProjectSettings, Library) is cloned into <scratch dir>/F1-outline with APFS clones
# (cp -c: instant, no copy until a file changes), the art of a variant (thin_outline.py's output) is put over the clone's
# Assets/@Art, and the screenshot fixture (as Tools/screenshots.sh runs it) renders into <scratch dir>/shots-<variant>.
# The Addressables play mode is the asset database (AddressablesSetup), so the swapped files are what the game loads.
set -u
SRC="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../.." && pwd)"
S="${1:?scratch directory}"
CLONE="$S/F1-outline"
VERSION="$(sed -n 's/^m_EditorVersion: //p' "$SRC/ProjectSettings/ProjectVersion.txt")"
UNITY="${UNITY_PATH:-/Applications/Unity/Hub/Editor/$VERSION/Unity.app/Contents/MacOS/Unity}"
rm -rf "$CLONE"
mkdir -p "$CLONE"
for d in Assets Packages ProjectSettings Library; do cp -cR "$SRC/$d" "$CLONE/$d" || exit 1; done
for v in 2-3 1-2; do
  V="$SRC/ArtPipeline/output/outline/$v"
  cp "$V"/Unit/Job/*.png "$CLONE/Assets/@Art/Unit/Job/" && cp "$V"/Unit/Enemy/*.png "$CLONE/Assets/@Art/Unit/Enemy/" \
    && cp "$V"/Pose/Job/*.png "$CLONE/Assets/@Art/Pose/Job/" || exit 1
  OUT="$S/shots-$v"
  rm -rf "$OUT"
  mkdir -p "$OUT"
  F1_SCREENSHOT_DIR="$OUT" "$UNITY" -batchmode -projectPath "$CLONE" -runTests -testPlatform PlayMode \
    -testFilter "F1.Tests.UiScreenshotTests" -testResults "$OUT/result.xml" -logFile "$OUT/unity.log"
  echo "$v: exit $?, $(ls "$OUT"/*.png 2>/dev/null | wc -l | xargs) PNG files in $OUT"
done
