#!/usr/bin/env bash
# Renders every screen to PNG files for a visual check.
#
#   Tools/screenshots.sh [output directory]
#
# It runs the PlayMode fixture F1.Tests.UiScreenshotTests with a graphics device (the chain runs
# without one). The files go outside the repository unless a directory is given.
set -u

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
VERSION="$(sed -n 's/^m_EditorVersion: //p' "$ROOT/ProjectSettings/ProjectVersion.txt")"
UNITY="${UNITY_PATH:-/Applications/Unity/Hub/Editor/$VERSION/Unity.app/Contents/MacOS/Unity}"
OUT="${1:-$HOME/Library/Caches/F1/screenshots/$(date +%Y%m%d-%H%M%S)}"

if [ ! -x "$UNITY" ]; then
  echo "Unity $VERSION was not found at: $UNITY"
  exit 2
fi

for pid in $(pgrep -f "^/Applications/Unity/Hub/Editor/[^ ]*/Unity.app/Contents/MacOS/Unity( |$)"); do
  if ps -o command= -p "$pid" | grep -qiF -- "$ROOT"; then
    echo "A Unity Editor has this project open (pid $pid). Close it and try again."
    exit 2
  fi
done

mkdir -p "$OUT"
F1_SCREENSHOT_DIR="$OUT" "$UNITY" -batchmode -projectPath "$ROOT" -runTests -testPlatform PlayMode \
  -testFilter "F1.Tests.UiScreenshotTests" -testResults "$OUT/result.xml" -logFile "$OUT/unity.log"

if [ ! -f "$OUT/result.xml" ]; then
  echo "No result (log: $OUT/unity.log)"
  grep -E "error CS[0-9]+|Exception" "$OUT/unity.log" | sort -u | head -20
  exit 1
fi

python3 - "$OUT/result.xml" <<'PY'
import sys
import xml.etree.ElementTree as ET

root = ET.parse(sys.argv[1]).getroot()
print(f"  screenshots: total={root.get('total')} passed={root.get('passed')} failed={root.get('failed')}")
for case in root.iter("test-case"):
    if case.get("result") == "Failed":
        print(f"    FAILED {case.get('fullname')}")
        for line in case.findtext("failure/message", default="").strip().splitlines()[:6]:
            print(f"      {line}")
PY

echo "PNG files: $OUT"
ls "$OUT"/*.png 2>/dev/null | wc -l | xargs echo "  count:"

# A run with a graphics device can touch render pipeline assets. They must not be committed.
if ! git -C "$ROOT" diff --quiet -- ProjectSettings Assets/Settings Assets/DefaultVolumeProfile.asset Assets/UniversalRenderPipelineGlobalSettings.asset; then
  echo "  NOTE: the run changed project or render settings. Review with 'git status' and restore them."
fi
