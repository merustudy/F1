#!/bin/zsh
# Round 94, 판정 1차 (2026-10-22): the rogue's hit and resolute poses again with the dagger in her near (foreground) hand, after the
# 5th figure that is now her approved original (output/character/rogue.raw.png). Two calls. Run from the repository root:
#   zsh ArtPipeline/Archive/94-job-poses/regen-rogue.sh
set -e
cd "$(dirname "$0")/../../.."
PY=.venv/bin/python; GEN=ArtPipeline/tools/gen_image.py
STYLE=ArtPipeline/Archive/38-breakdown-fx/STYLE_RUNTIME-statepose.md
TAIL="Draw this one character, full body. Keep the character exactly recognizable: the same face with the same eyes, nose, mouth, chin and hair, the same head size and body proportions as the attached image, the same outfit and the same weapon."
EXTRA="The dagger stays in the hand nearest the viewer, the foreground arm in front of her body: the very same hand that holds it in the attached image. The far hand, behind her body, holds nothing."
log() { echo "[$(date +%H:%M:%S)] $*"; }
log "hit rogue (1/2)"
$PY $GEN --type hit --key rogue --name rogue --extra "$EXTRA"
log "hit rogue_resolute (2/2)"
$PY $GEN --type hit --key rogue --name rogue_resolute --style $STYLE --roster ArtPipeline/Archive/94-job-poses/resolute.csv --tail "$TAIL" --extra "$EXTRA"
log "done: 2 calls"
