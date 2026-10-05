#!/bin/bash
# Round 29, proposal 2 with names B and the stage 48 lower: every screen of the Korean lap (EveryScreen_Korean) rendered by
# the game with the plates hidden and the stages where the proposal puts them, so that the marks and the board headers are
# drawn where the units stand. No API call, the working tree is not touched.
#
#   ArtPipeline/Archive/29-plates/run_party_shots.sh <scratch dir> [seed]     (default 7)
#
# The project is cloned (APFS clones). In the clone:
# - the battle: BattleUnitView hides its plate and BattleFieldTop is 48 lower (the figures and the background);
# - the party side of the node map and the reward screen: the plate is hidden and the move buttons stand under the marks
#   that replace it (the bar at the floor + 8, 22 high, and the state line that names the job, to the floor + 52): the plate's
#   92 becomes 46, and as the column stands on the panel, the figures go 46 lower;
# - every new run gets the seed (a new run's seed comes from the OS, so the lobby's mercenaries and the first battle's enemies
#   otherwise differ from run to run).
# PLATES=1 leaves the screens as they are (only the seed), for the screens as the game shows them now.
# The clone's setup builds the screens' prefabs again and EveryScreen_Korean renders into <scratch dir>/shots-party[-plates].
set -u
SRC="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../.." && pwd)"
S="${1:?scratch directory}"
SEED="${2:-7}"
TAG="party${PLATES:+-plates}"
CLONE="$S/F1-$TAG"
VERSION="$(sed -n 's/^m_EditorVersion: //p' "$SRC/ProjectSettings/ProjectVersion.txt")"
UNITY="${UNITY_PATH:-/Applications/Unity/Hub/Editor/$VERSION/Unity.app/Contents/MacOS/Unity}"
rm -rf "$CLONE"
mkdir -p "$CLONE"
for d in Assets Packages ProjectSettings Library; do cp -cR "$SRC/$d" "$CLONE/$d" || exit 1; done
/usr/bin/python3 - "$CLONE" "$SEED" "${PLATES:-}" <<'PY' || exit 1
import sys
clone, seed, plates = sys.argv[1:4]
def patch(path, old, new):
    text = open(path, encoding="utf-8").read()
    assert text.count(old) == 1, (path, old)
    open(path, "w", encoding="utf-8").write(text.replace(old, new))
patch(clone + "/Assets/@Scripts/Core/Bootstrap/AppRoot.cs", "            var bytes = new byte[8];",
      f"            if (true) return {seed}UL;\n            var bytes = new byte[8];")
if not plates:
    patch(clone + "/Assets/@Scripts/UI/Views/BattleUnitView.cs", "            _hitPose = hitPose;\n        }",
          "            _hitPose = hitPose;\n            _plate.gameObject.SetActive(false);\n        }")
    patch(clone + "/Assets/@Scripts/Editor/Setup/Ui/UiPrefabSetup.Battle.cs", "        const float BattleFieldTop = 206f;",
          "        const float BattleFieldTop = 206f + 48f;")
    party = clone + "/Assets/@Scripts/Editor/Setup/Ui/UiPrefabSetup.PartySide.cs"
    patch(party, "            UiBuild.Line(plate.Plate, 0f, PlateHeight);\n",
          "            UiBuild.Line(plate.Plate, 0f, PlateHeight);\n            plate.Plate.gameObject.SetActive(false);\n")
    patch(party, "        const float PartyMoveTop = PartyPlateTop + PlateHeight + PlateGap;",
          "        const float PartyMoveTop = PartyPlateTop + 46f + PlateGap;")
PY
OUT="$S/shots-$TAG"
rm -rf "$OUT"
mkdir -p "$OUT"
"$UNITY" -batchmode -quit -nographics -projectPath "$CLONE" -executeMethod F1.Editor.Setup.ProjectSetup.ApplyMenu -logFile "$OUT/setup.log"
grep -q "F1_PROJECT_SETUP_DONE" "$OUT/setup.log" || { echo "setup failed: $OUT/setup.log"; exit 1; }
F1_SCREENSHOT_DIR="$OUT" "$UNITY" -batchmode -projectPath "$CLONE" -runTests -testPlatform PlayMode \
  -testFilter "F1.Tests.UiScreenshotTests.EveryScreen_Korean" -testResults "$OUT/result.xml" -logFile "$OUT/unity.log"
echo "$TAG (seed $SEED): exit $?, $(ls "$OUT"/*.png 2>/dev/null | wc -l | xargs) PNG files in $OUT"
