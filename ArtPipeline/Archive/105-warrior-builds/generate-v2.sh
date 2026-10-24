#!/bin/zsh
# Round 105 v2 (2026-10-24, verdict "A수행"): the giant in a narrow upright stance at Height 95, the small one with the knight's head size at Height 62.
# One call per entry, cap 2; an entry whose output exists is skipped. Run from the repository root:  zsh ArtPipeline/Archive/105-warrior-builds/generate-v2.sh
set -e
cd "$(dirname "$0")/../../.."
PY=.venv/bin/python; GEN=ArtPipeline/tools/gen_image.py; OUT=ArtPipeline/output; HERE=ArtPipeline/Archive/105-warrior-builds
log() { echo "[$(date +%H:%M:%S)] $*"; }
calls=0
for build in giant small; do
  name=knight_${build}_v2
  if [[ -f $OUT/character/$name.png ]]; then log "skip $name (exists)"; continue; fi
  if (( calls >= 2 )); then log "cap reached before $name"; break; fi
  calls=$((calls + 1))
  log "character $name ($calls/2)"
  $PY $GEN --type character --key warrior --name $name --roster $HERE/roster-$build-v2.csv || log "FAILED $name"
done
log "done: $calls calls"
