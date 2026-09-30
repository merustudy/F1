#!/usr/bin/env bash
# F1 validation chain. Rules: Docs/Architecture/10_TESTING_VALIDATION.md
#
#   Tools/chain.sh                 run every step
#   Tools/chain.sh editmode        run only the named steps (setup, editmode, playmode)
#
# Test results are judged from the result XML, never from Unity's exit code.
# Logs and result XML go outside the repository.
set -u

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
VERSION="$(sed -n 's/^m_EditorVersion: //p' "$ROOT/ProjectSettings/ProjectVersion.txt")"
UNITY="${UNITY_PATH:-/Applications/Unity/Hub/Editor/$VERSION/Unity.app/Contents/MacOS/Unity}"
OUT="${F1_CHAIN_OUT:-$HOME/Library/Caches/F1/chain}/$(date +%Y%m%d-%H%M%S)"

SETUP_METHOD="F1.Editor.Setup.ProjectSetup.ApplyMenu"
SETUP_TOKEN="F1_PROJECT_SETUP_DONE"

ALL_STEPS=(setup editmode playmode)
if [ $# -gt 0 ]; then STEPS=("$@"); else STEPS=("${ALL_STEPS[@]}"); fi

require_unity() {
  if [ ! -x "$UNITY" ]; then
    echo "Unity $VERSION was not found at: $UNITY"
    exit 2
  fi

  # A batch run fails while an Editor has the project open. Anchor on the Editor binary so that
  # Unity Hub and this shell are not matched.
  local pid
  for pid in $(pgrep -f "^/Applications/Unity/Hub/Editor/[^ ]*/Unity.app/Contents/MacOS/Unity( |$)"); do
    if ps -o command= -p "$pid" | grep -qiF -- "$ROOT"; then
      echo "A Unity Editor has this project open (pid $pid). Close it and run the chain again."
      exit 2
    fi
  done
}

show_errors() {
  grep -E "error CS[0-9]+|Exception|executeMethod|Aborting batchmode" "$1" | sort -u | head -40 | sed 's/^/    /'
}

run_setup() {
  local log="$OUT/setup.log"
  "$UNITY" -batchmode -quit -nographics -projectPath "$ROOT" -executeMethod "$SETUP_METHOD" -logFile "$log"
  local code=$?
  if [ $code -eq 0 ] && grep -q "$SETUP_TOKEN" "$log"; then
    echo "  setup: OK"
    return 0
  fi

  echo "  setup: FAILED (exit $code, log $log)"
  show_errors "$log"
  return 1
}

run_tests() {
  local platform="$1"
  local name
  name="$(echo "$platform" | tr '[:upper:]' '[:lower:]')"
  local xml="$OUT/$name.xml"
  local log="$OUT/$name.log"

  "$UNITY" -batchmode -nographics -projectPath "$ROOT" -runTests -testPlatform "$platform" \
    -testResults "$xml" -logFile "$log"
  local code=$?

  if [ ! -f "$xml" ]; then
    echo "  $name: NO RESULT (exit $code, log $log) - compile error or crash"
    show_errors "$log"
    return 1
  fi

  python3 - "$xml" "$name" <<'PY'
import sys
import xml.etree.ElementTree as ET

path, name = sys.argv[1], sys.argv[2]
root = ET.parse(path).getroot()
total = int(root.get("total", "0"))
passed = int(root.get("passed", "0"))
failed = int(root.get("failed", "0"))
skipped = int(root.get("skipped", "0")) + int(root.get("inconclusive", "0"))
print(f"  {name}: total={total} passed={passed} failed={failed} skipped={skipped}")

for case in root.iter("test-case"):
    if case.get("result") == "Failed":
        message = case.findtext("failure/message", default="").strip().splitlines()
        print(f"    FAILED {case.get('fullname')}")
        for line in message[:6]:
            print(f"      {line}")

ok = total > 0 and failed == 0 and passed + skipped == total
if total == 0:
    print("    no tests ran")
sys.exit(0 if ok else 1)
PY
}

require_unity
mkdir -p "$OUT"
echo "F1 chain: Unity $VERSION, results in $OUT"

FAILED=()
for step in "${STEPS[@]}"; do
  case "$step" in
    setup)    run_setup || FAILED+=("$step") ;;
    editmode) run_tests EditMode || FAILED+=("$step") ;;
    playmode) run_tests PlayMode || FAILED+=("$step") ;;
    *)        echo "  unknown step: $step"; FAILED+=("$step") ;;
  esac
done

# An interrupted test run leaves its temporary scene behind.
if ls "$ROOT"/Assets/InitTestScene* >/dev/null 2>&1; then
  echo "  leftover: Assets/InitTestScene* exists - delete it"
  FAILED+=("leftover")
fi

if [ ${#FAILED[@]} -gt 0 ]; then
  echo "CHAIN FAILED: ${FAILED[*]}"
  exit 1
fi

echo "CHAIN OK: ${STEPS[*]}"
