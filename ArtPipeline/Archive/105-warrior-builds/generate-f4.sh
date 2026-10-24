#!/bin/zsh
# Round 105 female warriors, fourth try (2026-10-24, user "재생성시 롱소드 이미지 최대한 기존 전사 롱소드 이미지 유지"): the small one's younger face and
# the normal one's other face, now holding the knight's longsword — the round's style document STYLE_RUNTIME-longsword.md (§3 says what the second
# image is for) with the knight's raw attached second (--extra-reference). One call per entry, cap 2. Run from the repository root:
#   zsh ArtPipeline/Archive/105-warrior-builds/generate-f4.sh [--dry-run]
set -e
cd "$(dirname "$0")/../../.."
PY=.venv/bin/python; GEN=ArtPipeline/tools/gen_image.py; OUT=ArtPipeline/output; HERE=ArtPipeline/Archive/105-warrior-builds
log() { echo "[$(date +%H:%M:%S)] $*"; }
calls=0
for pair in "knightf_small_v4:f-small-v4" "knightf_normal_v3:f-normal-v3"; do
  name=${pair%%:*}; roster=${pair##*:}
  if [[ -f $OUT/character/$name.png ]]; then log "skip $name (exists)"; continue; fi
  if [[ $1 != --dry-run ]] && (( calls >= 2 )); then log "cap reached before $name"; break; fi
  [[ $1 != --dry-run ]] && calls=$((calls + 1))
  log "character $name ($calls/2)"
  $PY $GEN --type character --key warrior --name $name --roster $HERE/roster-$roster.csv --style $HERE/STYLE_RUNTIME-longsword.md \
    --extra-reference $OUT/character/knight.raw.png $1 || log "FAILED $name"
done
log "done: $calls calls"
