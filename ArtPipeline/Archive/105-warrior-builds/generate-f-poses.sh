#!/bin/zsh
# Round 105 female warriors' poses (2026-10-24, user "여자 전사 3이미지 확정. 공격 및 피격모션 이미지 생성. 각성 및 피격은 기본 이미지로 대체"): the attack and
# hit poses of normal v3, giant v3 and small v4, after their confirmed raws (gen_image.pose_reference, given by --reference). Cap 6.
#   zsh ArtPipeline/Archive/105-warrior-builds/generate-f-poses.sh [--dry-run]
set -e
cd "$(dirname "$0")/../../.."
PY=.venv/bin/python; GEN=ArtPipeline/tools/gen_image.py; OUT=ArtPipeline/output; HERE=ArtPipeline/Archive/105-warrior-builds
log() { echo "[$(date +%H:%M:%S)] $*"; }
calls=0
for kind in attack hit; do
  for name in knightf_normal_v3 knightf_giant_v3 knightf_small_v4; do
    if [[ -f $OUT/$kind/$name.raw.png ]]; then log "skip $kind $name (exists)"; continue; fi
    if [[ $1 != --dry-run ]] && (( calls >= 6 )); then log "cap reached before $kind $name"; break 2; fi
    [[ $1 != --dry-run ]] && calls=$((calls + 1))
    log "$kind $name ($calls/6)"
    $PY $GEN --type $kind --key warrior --name $name --roster $HERE/roster-$name-$kind.csv --reference $OUT/$kind/$name.reference.png $1 || log "FAILED $kind $name"
  done
done
log "done: $calls calls"
