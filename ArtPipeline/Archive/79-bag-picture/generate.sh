#!/bin/zsh
# Round 79: three looks of a bag as a thing. The leather pouch (3x1) in all three, the belt pouch (2x1) and the pack (3x3) in looks 2 and 3.
# 7 calls, cap 10. Run from the repository root:  zsh ArtPipeline/Archive/79-bag-picture/generate.sh
set -e
cd "$(dirname "$0")/../../.."
PY=.venv/bin/python; GEN=ArtPipeline/tools/gen_image.py; R=ArtPipeline/Archive/79-bag-picture; OUT=ArtPipeline/output/bag
log() { echo "[$(date +%H:%M:%S)] $*"; }
for spec in "leather_pouch 1" "leather_pouch 2" "leather_pouch 3" "belt_pouch 2" "belt_pouch 3" "leather_pack 2" "leather_pack 3"; do
  set -- ${=spec}
  key=$1; look=$2
  if [[ ! -f $OUT/${key}_look${look}.png ]]; then
    log "bag $key look$look"
    $PY $GEN --type bag --key $key --name ${key}_look${look} --roster $R/look${look}.csv
  fi
done
log "done"
