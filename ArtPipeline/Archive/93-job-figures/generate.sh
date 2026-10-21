#!/bin/zsh
# Round 93 (2026-10-21): the four stage-47 jobs' standing figures (character) and the two new base weapons' icons (item).
# One call per entry, cap 6; an entry whose output exists is skipped. Run from the repository root:  zsh ArtPipeline/Archive/93-job-figures/generate.sh
set -e
cd "$(dirname "$0")/../../.."
PY=.venv/bin/python; GEN=ArtPipeline/tools/gen_image.py; OUT=ArtPipeline/output
log() { echo "[$(date +%H:%M:%S)] $*"; }
calls=0
gen() {
  local kind=$1 key=$2; shift 2
  if [[ -f $OUT/$kind/$key.png ]]; then log "skip $kind $key (exists)"; return; fi
  if (( calls >= 6 )); then log "cap reached before $kind $key"; return; fi
  calls=$((calls + 1))
  log "$kind $key ($calls/6)"
  $PY $GEN --type $kind --key $key "$@" || log "FAILED $kind $key"
}
for key in rogue archer occultist minstrel; do gen character $key; done
gen item cursed_tome
gen item lute
log "done: $calls calls"
