#!/bin/zsh
# Round 68 (2026-10-15, the user: "1. 확정 2. 75 3. 진행"): the archmage's four poses drawn after her new
# approved figure (the 6th try, output/character/archmage.raw.png), one paid call each. Attack and hit by the pose types'
# own road (Rosters/attack.csv, hit.csv), broken and resolute by round 38's (its state-pose style document, this round's
# state/*.csv). The tail is round 40's: the same face and head size as the approved figure.
#   zsh ArtPipeline/Archive/68-archmage-new-face/generate_poses.sh [attack hit broken resolute]
PY=/Users/funitup/Projects/F1/.venv/bin/python
GEN=/Users/funitup/Projects/F1/ArtPipeline/tools/gen_image.py
STATE_STYLE=/Users/funitup/Projects/F1/ArtPipeline/Archive/38-breakdown-fx/STYLE_RUNTIME-statepose.md
HERE=${0:A:h}
TAIL="Draw this one character, full body. Keep the character exactly recognizable: the same face with the same eyes, nose, mouth, chin and hair, the same head size and body proportions as the attached image, the same outfit and the same weapon."
if (( $# )); then POSES=("$@"); else POSES=(attack hit broken resolute); fi
for pose in $POSES; do
  echo "=== $pose $(date +%T) ==="
  case $pose in
    attack|hit) $PY $GEN --type $pose --key archmage --tail "$TAIL" 2>&1 ;;
    broken|resolute) $PY $GEN --type hit --key archmage --name archmage_$pose --style $STATE_STYLE --roster $HERE/state/$pose.csv --tail "$TAIL" 2>&1 ;;
  esac
done
