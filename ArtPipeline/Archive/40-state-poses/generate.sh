#!/bin/zsh
# Round 40: the five mercenaries' broken and resolute poses, one paid call each (10 calls), by the road of round 38
# (Docs/Architecture/13_ART_PIPELINE.md "붕괴·각성의 자세"). The tail sentence adds the user's ask of this round: the same face and
# head size as the approved figure. Run from anywhere: zsh ArtPipeline/Archive/40-state-poses/generate.sh [job ...]
PY=/Users/funitup/Projects/F1/.venv/bin/python
GEN=/Users/funitup/Projects/F1/ArtPipeline/tools/gen_image.py
STYLE=/Users/funitup/Projects/F1/ArtPipeline/Archive/38-breakdown-fx/STYLE_RUNTIME-statepose.md
HERE=${0:A:h}
TAIL="Draw this one character, full body. Keep the character exactly recognizable: the same face with the same eyes, nose, mouth, chin and hair, the same head size and body proportions as the attached image, the same outfit and the same weapon."
if (( $# )); then JOBS=("$@"); else JOBS=(knight bishop paladin archmage spellblade); fi
for job in $JOBS; do
  for state in broken resolute; do
    echo "=== $job $state $(date +%T) ==="
    $PY $GEN --type hit --key $job --name ${job}_${state} --style $STYLE --roster $HERE/$state.csv --tail "$TAIL" 2>&1
  done
done
echo "=== done $(date +%T) ==="
