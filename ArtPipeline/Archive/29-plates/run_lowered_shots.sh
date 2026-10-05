#!/bin/bash
# Round 29, proposal 2: the game's own screenshots with the stage lowered (the figures and the background together) and the
# plates hidden, so that the chosen marks (Slay the Spire's way) are drawn where the lowered units stand. No API call, the
# working tree is not touched.
#
#   ArtPipeline/Archive/29-plates/run_lowered_shots.sh <scratch dir> [pixels] [seed]     (default 48, the OS random seed)
#
# The project is cloned (APFS clones), the clone's UiPrefabSetup.Battle lowers BattleFieldTop by the pixels (the background is
# placed from it, so it follows), the clone's BattleUnitView hides its plate, the clone's setup builds the screens' prefabs
# again, and AfterAnAdvance_Korean and DeathsDoor_Korean render into <scratch dir>/shots-lowered-<pixels>. A new run's seed
# comes from the OS, so the first battle's enemies differ from run to run: with a seed the clone's AppRoot gives every new run
# that seed, and two renders (0 and 48) show the same battle. PLATES=1 keeps the plates (to read the units' names, HP and
# states for the mockup) and renders into <scratch dir>/shots-lowered-<pixels>-plates.
set -u
SRC="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../.." && pwd)"
S="${1:?scratch directory}"
DOWN="${2:-48}"
SEED="${3:-}"
TAG="$DOWN${PLATES:+-plates}"
CLONE="$S/F1-lowered-$TAG"
VERSION="$(sed -n 's/^m_EditorVersion: //p' "$SRC/ProjectSettings/ProjectVersion.txt")"
UNITY="${UNITY_PATH:-/Applications/Unity/Hub/Editor/$VERSION/Unity.app/Contents/MacOS/Unity}"
rm -rf "$CLONE"
mkdir -p "$CLONE"
for d in Assets Packages ProjectSettings Library; do cp -cR "$SRC/$d" "$CLONE/$d" || exit 1; done
/usr/bin/python3 - "$CLONE" "$DOWN" "$SEED" "${PLATES:-}" <<'PY' || exit 1
import sys
clone, down, seed, plates = sys.argv[1:5]
def patch(path, old, new):
    text = open(path, encoding="utf-8").read()
    assert text.count(old) == 1, (path, old)
    open(path, "w", encoding="utf-8").write(text.replace(old, new))
if not plates:
    patch(clone + "/Assets/@Scripts/UI/Views/BattleUnitView.cs", "            _hitPose = hitPose;\n        }",
          "            _hitPose = hitPose;\n            _plate.gameObject.SetActive(false);\n        }")
patch(clone + "/Assets/@Scripts/Editor/Setup/Ui/UiPrefabSetup.Battle.cs", "        const float BattleFieldTop = 206f;",
      f"        const float BattleFieldTop = 206f + {down}f;")
if seed:
    patch(clone + "/Assets/@Scripts/Core/Bootstrap/AppRoot.cs", "            var bytes = new byte[8];",
          f"            if (true) return {seed}UL;\n            var bytes = new byte[8];")
PY
OUT="$S/shots-lowered-$TAG"
rm -rf "$OUT"
mkdir -p "$OUT"
"$UNITY" -batchmode -quit -nographics -projectPath "$CLONE" -executeMethod F1.Editor.Setup.ProjectSetup.ApplyMenu -logFile "$OUT/setup.log"
grep -q "F1_PROJECT_SETUP_DONE" "$OUT/setup.log" || { echo "setup failed: $OUT/setup.log"; exit 1; }
F1_SCREENSHOT_DIR="$OUT" "$UNITY" -batchmode -projectPath "$CLONE" -runTests -testPlatform PlayMode \
  -testFilter "F1.Tests.UiScreenshotTests.AfterAnAdvance_Korean;F1.Tests.UiScreenshotTests.DeathsDoor_Korean" \
  -testResults "$OUT/result.xml" -logFile "$OUT/unity.log"
echo "lowered by $DOWN (seed ${SEED:-random}${PLATES:+, plates kept}): exit $?, $(ls "$OUT"/*.png 2>/dev/null | wc -l | xargs) PNG files in $OUT"
