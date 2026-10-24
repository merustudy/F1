#!/bin/zsh
# Round 105 female giant, third try (2026-10-24, user "여자 전사 거인 이미지도 얼굴을 살짝만 더 크게 만들어줘.(얼굴 및 자세 옷 등은 마음에 들어서 최대한
# 기존 유지) / 재생성시 롱소드 이미지 최대한 기존 전사 롱소드 이미지 유지"): v2 attached second (kept as she is but for a head about a tenth bigger),
# the knight attached third (the longsword), the round's style document STYLE_RUNTIME-redraw.md (§3 says what each image is for). Cap 1.
#   zsh ArtPipeline/Archive/105-warrior-builds/generate-f5.sh [--dry-run]
set -e
cd "$(dirname "$0")/../../.."
PY=.venv/bin/python; GEN=ArtPipeline/tools/gen_image.py; OUT=ArtPipeline/output; HERE=ArtPipeline/Archive/105-warrior-builds
name=knightf_giant_v3
if [[ -f $OUT/character/$name.png ]]; then echo "skip $name (exists)"; exit 0; fi
$PY $GEN --type character --key warrior --name $name --roster $HERE/roster-f-giant-v3.csv --style $HERE/STYLE_RUNTIME-redraw.md \
  --extra-reference $OUT/character/knightf_giant_v2.raw.png --extra-reference $OUT/character/knight.raw.png $1
