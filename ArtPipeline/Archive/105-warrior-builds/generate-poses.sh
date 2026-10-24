#!/bin/zsh
# Round 105 poses (2026-10-24, user "공격모션, 피격 모션만 생성"): the attack and hit poses of the v1 giant and small warriors.
# The reference is gen_image.pose_reference of the v1 raw (output/<type>/knight_<build>.reference.png), given by --reference
# because the roster's Key must be a job id (warrior). One call per entry, cap 4; an entry whose raw exists is skipped.
# Run from the repository root:  zsh ArtPipeline/Archive/105-warrior-builds/generate-poses.sh [--dry-run]
set -e
cd "$(dirname "$0")/../../.."
PY=.venv/bin/python; GEN=ArtPipeline/tools/gen_image.py; OUT=ArtPipeline/output; HERE=ArtPipeline/Archive/105-warrior-builds
log() { echo "[$(date +%H:%M:%S)] $*"; }
calls=0
for kind in attack hit; do
  for build in giant small; do
    name=knight_$build
    if [[ -f $OUT/$kind/$name.raw.png ]]; then log "skip $kind $name (exists)"; continue; fi
    if [[ $1 != --dry-run ]] && (( calls >= 4 )); then log "cap reached before $kind $name"; break 2; fi
    [[ $1 != --dry-run ]] && calls=$((calls + 1))
    log "$kind $name ($calls/4)"
    $PY $GEN --type $kind --key warrior --name $name --roster $HERE/roster-$build-$kind.csv --reference $OUT/$kind/$name.reference.png $1 || log "FAILED $kind $name"
  done
done
log "done: $calls calls"
