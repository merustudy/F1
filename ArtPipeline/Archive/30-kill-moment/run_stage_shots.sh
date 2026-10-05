#!/bin/bash
# Round 30: the stage of the game as it is now (the marks of round 29, the stage 48 lower), for the kill-moment mockups.
# No API call, the working tree is not touched.
#
#   ArtPipeline/Archive/30-kill-moment/run_stage_shots.sh <scratch dir>
#
# The project is cloned (APFS clones) and the clone's UiScreenshotTests gets one more test: the boss battle at its start,
# paused (UiTestUtil.EnterTheBossBattle), captured as the game shows it (ko_30_stage_full: to check the mockup's figures
# against), then again with every unit hidden (a CanvasGroup at alpha 0 on each BattleUnitView: the figure, its shadow,
# its marks): the empty stage the mockup draws its units on (ko_30_stage_empty: the background, the candle's light, the
# vignette). Both are copied into game/.
set -u
SRC="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../.." && pwd)"
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
S="${1:?scratch directory}"
CLONE="$S/F1-killstage"
VERSION="$(sed -n 's/^m_EditorVersion: //p' "$SRC/ProjectSettings/ProjectVersion.txt")"
UNITY="${UNITY_PATH:-/Applications/Unity/Hub/Editor/$VERSION/Unity.app/Contents/MacOS/Unity}"
rm -rf "$CLONE"
mkdir -p "$CLONE"
for d in Assets Packages ProjectSettings Library; do cp -cR "$SRC/$d" "$CLONE/$d" || exit 1; done
/usr/bin/python3 - "$CLONE" <<'PY' || exit 1
import sys
path = sys.argv[1] + "/Assets/@Tests/PlayMode/UI/UiScreenshotTests.cs"
text = open(path, encoding="utf-8").read()
anchor = "        IEnumerator CaptureLap(string localeCode, string prefix)"
assert text.count(anchor) == 1
test = '''        [UnityTest]
        public IEnumerator KillStage_Korean()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            yield return UiTestUtil.EnterTheBossBattle();
            yield return UiTestUtil.WaitForRedraw();
            yield return Capture("ko_30_stage_full");
            foreach (BattleUnitView view in UiTestUtil.Views<BattleUnitView>(UiTestUtil.Screen<BattleScreen>()))
            {
                view.gameObject.AddComponent<UnityEngine.CanvasGroup>().alpha = 0f;
            }

            yield return UiTestUtil.WaitForRedraw();
            yield return Capture("ko_30_stage_empty");
        }

'''
open(path, "w", encoding="utf-8").write(text.replace(anchor, test + anchor))
PY
OUT="$S/shots-killstage"
rm -rf "$OUT"
mkdir -p "$OUT"
"$UNITY" -batchmode -quit -nographics -projectPath "$CLONE" -executeMethod F1.Editor.Setup.ProjectSetup.ApplyMenu -logFile "$OUT/setup.log"
grep -q "F1_PROJECT_SETUP_DONE" "$OUT/setup.log" || { echo "setup failed: $OUT/setup.log"; exit 1; }
F1_SCREENSHOT_DIR="$OUT" "$UNITY" -batchmode -projectPath "$CLONE" -runTests -testPlatform PlayMode \
  -testFilter "F1.Tests.UiScreenshotTests.KillStage_Korean" -testResults "$OUT/result.xml" -logFile "$OUT/unity.log"
echo "exit $?"
cp "$OUT/ko_30_stage_full.png" "$OUT/ko_30_stage_empty.png" "$HERE/game/" && echo "copied into $HERE/game"
