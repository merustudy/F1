#!/bin/zsh
# Round 105 female giant and small, second try (2026-10-24, user "여자 전사 거인 및 왜소 다시 생성(더 여성성 부각)" with "전사를 갑옷 입고있는 것에
# 한정시키지 말것. 자세를 꼭 동일하게 한정하지 말것."). One call per entry, cap 2. Run from the repository root:
#   zsh ArtPipeline/Archive/105-warrior-builds/generate-f2.sh [--dry-run]
set -e
cd "$(dirname "$0")/../../.."
PY=.venv/bin/python; GEN=ArtPipeline/tools/gen_image.py; OUT=ArtPipeline/output; HERE=ArtPipeline/Archive/105-warrior-builds
log() { echo "[$(date +%H:%M:%S)] $*"; }
calls=0
for build in giant small; do
  name=knightf_${build}_v2
  if [[ -f $OUT/character/$name.png ]]; then log "skip $name (exists)"; continue; fi
  if [[ $1 != --dry-run ]] && (( calls >= 2 )); then log "cap reached before $name"; break; fi
  [[ $1 != --dry-run ]] && calls=$((calls + 1))
  log "character $name ($calls/2)"
  $PY $GEN --type character --key warrior --name $name --roster $HERE/roster-f-$build-v2.csv $1 || log "FAILED $name"
done
log "done: $calls calls"
