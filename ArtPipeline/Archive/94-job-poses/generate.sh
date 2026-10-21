#!/bin/zsh
# Round 94 (2026-10-21): the four stage-47 jobs' attack, hit and resolute poses (the broken pose is the hit pose by the user's decision).
# One call per entry, cap 12; an entry whose raw exists is skipped. Run from the repository root:  zsh ArtPipeline/Archive/94-job-poses/generate.sh
set -e
cd "$(dirname "$0")/../../.."
PY=.venv/bin/python; GEN=ArtPipeline/tools/gen_image.py; OUT=ArtPipeline/output
STYLE=ArtPipeline/Archive/38-breakdown-fx/STYLE_RUNTIME-statepose.md
TAIL="Draw this one character, full body. Keep the character exactly recognizable: the same face with the same eyes, nose, mouth, chin and hair, the same head size and body proportions as the attached image, the same outfit and the same weapon."
log() { echo "[$(date +%H:%M:%S)] $*"; }
calls=0
gen() {
  local kind=$1 key=$2 name=$3; shift 3
  if [[ -f $OUT/$kind/$name.raw.png ]]; then log "skip $kind $name (exists)"; return; fi
  if (( calls >= 12 )); then log "cap reached before $kind $name"; return; fi
  calls=$((calls + 1))
  log "$kind $name ($calls/12)"
  $PY $GEN --type $kind --key $key --name $name "$@" || log "FAILED $kind $name"
}
for key in rogue archer occultist minstrel; do gen attack $key $key; done
for key in rogue archer occultist minstrel; do gen hit $key $key; done
for key in rogue archer occultist minstrel; do gen hit $key ${key}_resolute --style $STYLE --roster ArtPipeline/Archive/94-job-poses/resolute.csv --tail "$TAIL"; done
log "done: $calls calls"
