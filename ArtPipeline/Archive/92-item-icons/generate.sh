#!/bin/zsh
# Round 92 (2026-10-21): the icons of stage 46's catalog — 29 items (item), 2 bags (bag), 2 potions (ui_icon).
# One call per entry, cap 33; an entry whose output exists is skipped, so a rerun spends calls only on what is missing.
# Run from the repository root:  zsh ArtPipeline/Archive/92-item-icons/generate.sh
set -e
cd "$(dirname "$0")/../../.."
PY=.venv/bin/python; GEN=ArtPipeline/tools/gen_image.py; OUT=ArtPipeline/output
log() { echo "[$(date +%H:%M:%S)] $*"; }
calls=0
gen() {
  local kind=$1 key=$2; shift 2
  if [[ -f $OUT/$kind/$key.png ]]; then log "skip $kind $key (exists)"; return; fi
  if (( calls >= 33 )); then log "cap reached before $kind $key"; return; fi
  calls=$((calls + 1))
  log "$kind $key ($calls/33)"
  $PY $GEN --type $kind --key $key "$@" || log "FAILED $kind $key"
}
for key in serrated_dagger war_hammer pickaxe berserker_axe crossbow frost_staff shadow_staff chant_staff \
           frost_flask ash_pouch caltrops hex_bone censer holy_water war_drum war_horn bloodlust_charm \
           spiked_shield studded_armor leather_armor robe cloth_hood goblin_sawblade bristle_hide \
           bloodgale windcutter cinder dawnlight; do
  gen item $key
done
# The banner is the only upright piece (1x3): the near-square composition would lay it diagonally, so the extra asks for a standing pole.
gen item banner --extra "Draw the banner upright: the pole stands vertical from the bottom edge to the top edge of the picture and the flag hangs down along it, so that the whole picture is tall and narrow, not diagonal."
gen bag knapsack
gen bag big_pack
gen ui_icon potion_cleanse
gen ui_icon potion_regen
log "done: $calls calls"
