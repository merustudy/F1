#!/bin/zsh
# Round 105 female warriors (2026-10-24, user "전사 여자 캐릭터 이미지도 보통, 거인, 왜소 전투 준비자세 이미지 생성"): the ready stance only.
# One call per entry, cap 3; an entry whose output exists is skipped. Run from the repository root:  zsh ArtPipeline/Archive/105-warrior-builds/generate-f.sh [--dry-run]
set -e
cd "$(dirname "$0")/../../.."
PY=.venv/bin/python; GEN=ArtPipeline/tools/gen_image.py; OUT=ArtPipeline/output; HERE=ArtPipeline/Archive/105-warrior-builds
log() { echo "[$(date +%H:%M:%S)] $*"; }
calls=0
for build in normal giant small; do
  name=knightf_$build
  if [[ -f $OUT/character/$name.png ]]; then log "skip $name (exists)"; continue; fi
  if [[ $1 != --dry-run ]] && (( calls >= 3 )); then log "cap reached before $name"; break; fi
  [[ $1 != --dry-run ]] && calls=$((calls + 1))
  log "character $name ($calls/3)"
  $PY $GEN --type character --key warrior --name $name --roster $HERE/roster-f-$build.csv $1 || log "FAILED $name"
done
log "done: $calls calls"
