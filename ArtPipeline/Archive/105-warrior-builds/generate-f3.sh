#!/bin/zsh
# Round 105 female warriors, third try (2026-10-24, user "여자 전사 왜소 캐릭터 이미지 재생성(얼굴 더 어리게), 보통 캐릭터 이미지 재생성(다른 얼굴로)").
# One call per entry, cap 2. Run from the repository root:  zsh ArtPipeline/Archive/105-warrior-builds/generate-f3.sh [--dry-run]
set -e
cd "$(dirname "$0")/../../.."
PY=.venv/bin/python; GEN=ArtPipeline/tools/gen_image.py; OUT=ArtPipeline/output; HERE=ArtPipeline/Archive/105-warrior-builds
log() { echo "[$(date +%H:%M:%S)] $*"; }
calls=0
for pair in "knightf_small_v3:f-small-v3" "knightf_normal_v2:f-normal-v2"; do
  name=${pair%%:*}; roster=${pair##*:}
  if [[ -f $OUT/character/$name.png ]]; then log "skip $name (exists)"; continue; fi
  if [[ $1 != --dry-run ]] && (( calls >= 2 )); then log "cap reached before $name"; break; fi
  [[ $1 != --dry-run ]] && calls=$((calls + 1))
  log "character $name ($calls/2)"
  $PY $GEN --type character --key warrior --name $name --roster $HERE/roster-$roster.csv $1 || log "FAILED $name"
done
log "done: $calls calls"
