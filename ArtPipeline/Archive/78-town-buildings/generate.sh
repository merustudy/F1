#!/bin/zsh
# Round 78 (2026-10-17, user "11. 한번에"): the far town grounds (1 call), four buildings at three levels (12), the scaffold (1).
# Each item is one call; an item whose output exists is skipped (gen_image refuses to overwrite). Stops at the first failure.
# Cap for the round: 24 calls (README). Run from the repository root:  zsh ArtPipeline/Archive/78-town-buildings/generate.sh
set -e
cd "$(dirname "$0")/../../.."
PY=.venv/bin/python
GEN=ArtPipeline/tools/gen_image.py
R=ArtPipeline/Archive/78-town-buildings
OUT=ArtPipeline/output
log() { echo "[$(date +%H:%M:%S)] $*"; }

if [[ ! -f $OUT/background/lobby_town_far.png ]]; then
  log "background lobby_town_far"
  $PY $GEN --type background --key lobby_town --name lobby_town_far --style $R/STYLE_RUNTIME-town-far.md --roster $R/town-far.csv \
    --extra-reference ArtPipeline/Archive/71-lobby-expedition/approved/lobby_town_topdown.png
fi

for b in barracks inn coach gate; do
  if [[ ! -f $OUT/building/${b}_1.png ]]; then
    log "building ${b}_1"
    $PY $GEN --type building --key ${b}_1
  fi
  for lv in 2 3; do
    prev=$((lv - 1))
    if [[ ! -f $OUT/building/${b}_${lv}.png ]]; then
      log "building ${b}_${lv} (reference ${b}_${prev})"
      $PY $GEN --type building --key ${b}_${lv} --extra-reference $OUT/building/${b}_${prev}.png
    fi
  done
done

if [[ ! -f $OUT/building/scaffold.png ]]; then
  log "building scaffold"
  $PY $GEN --type building --key scaffold
fi
log "done"
