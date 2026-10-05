#!/bin/bash
# Round 29, second proposal: the game's own screenshots of the same moments without the units' plates, so that the plates
# proposed in their place are drawn over the stage as it really is. No API call, the working tree is not touched.
#
#   ArtPipeline/Archive/29-plates/run_clean_shots.sh <scratch dir>
#
# The project is cloned into <scratch dir>/F1-noplate with APFS clones (as ../25-outline-thin/run_shots.sh does), the clone's
# BattleUnitView hides its plate (the plate and everything on it: badge, name, HP, states) when it is bound, and two screenshot
# tests render: AfterAnAdvance_Korean (ko_15) and DeathsDoor_Korean (ko_18), into <scratch dir>/shots-noplate.
set -u
SRC="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../.." && pwd)"
S="${1:?scratch directory}"
CLONE="$S/F1-noplate"
VERSION="$(sed -n 's/^m_EditorVersion: //p' "$SRC/ProjectSettings/ProjectVersion.txt")"
UNITY="${UNITY_PATH:-/Applications/Unity/Hub/Editor/$VERSION/Unity.app/Contents/MacOS/Unity}"
rm -rf "$CLONE"
mkdir -p "$CLONE"
for d in Assets Packages ProjectSettings Library; do cp -cR "$SRC/$d" "$CLONE/$d" || exit 1; done
VIEW="$CLONE/Assets/@Scripts/UI/Views/BattleUnitView.cs"
/usr/bin/python3 - "$VIEW" <<'PY' || exit 1
import sys
path = sys.argv[1]
text = open(path, encoding="utf-8").read()
old = "            _hitPose = hitPose;\n        }"
assert text.count(old) == 1
open(path, "w", encoding="utf-8").write(text.replace(old, "            _hitPose = hitPose;\n            _plate.gameObject.SetActive(false);\n        }"))
PY
OUT="$S/shots-noplate"
rm -rf "$OUT"
mkdir -p "$OUT"
F1_SCREENSHOT_DIR="$OUT" "$UNITY" -batchmode -projectPath "$CLONE" -runTests -testPlatform PlayMode \
  -testFilter "F1.Tests.UiScreenshotTests.AfterAnAdvance_Korean;F1.Tests.UiScreenshotTests.DeathsDoor_Korean" \
  -testResults "$OUT/result.xml" -logFile "$OUT/unity.log"
echo "exit $?, $(ls "$OUT"/*.png 2>/dev/null | wc -l | xargs) PNG files in $OUT"
