#!/bin/bash
# Round 31: why the mip-mapped art is stored uncompressed, and what each way out costs. No API call, the working tree is
# not touched: the project is cloned into <scratch dir>/F1-probe with APFS clones (as ../29-plates/run_clean_shots.sh does).
#
#   ArtPipeline/Archive/31-texture-compression/run_probe.sh <scratch dir>
#
# 1. probe/TextureProbe.cs (batch, no graphics): the art as imported, and the valkyrie's figure imported eleven ways
#    (mip maps, sizes, quality, explicit BC7/DXT5, type, alpha). The formats are in <scratch dir>/probe.log.
# 2. probe/TextureCompare.cs (with Metal): the figure, the attack pose and an icon in each variant (as now, 2048x1024,
#    crunch, without mip maps, uncompressed controls) drawn by the GPU at the sizes the game draws them, read back into
#    <scratch dir>/readback. compare.py measures them and draws compare-sheet.png.
# Run before the poses were wired at 2048x1024 (round 31) it shows the problem; after, the game's poses are already POT.
set -u
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SRC="$(cd "$HERE/../../.." && pwd)"
S="${1:?scratch directory}"
C="$S/F1-probe"
VERSION="$(sed -n 's/^m_EditorVersion: //p' "$SRC/ProjectSettings/ProjectVersion.txt")"
UNITY="${UNITY_PATH:-/Applications/Unity/Hub/Editor/$VERSION/Unity.app/Contents/MacOS/Unity}"
rm -rf "$C"; mkdir -p "$C"
for d in Assets Packages ProjectSettings Library; do cp -cR "$SRC/$d" "$C/$d" || exit 1; done
mkdir -p "$C/Assets/ProbeTmp" "$C/Assets/@Scripts/Editor/Probe"
"$SRC/.venv/bin/python" - "$C" <<'PY' || exit 1
import sys
from PIL import Image
c = sys.argv[1]
src = Image.open(c + "/Assets/@Art/Unit/Job/valkyrie.png").convert("RGBA")
for w, h in [(768, 1024), (1024, 1024), (672, 896)]:
    canvas = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    canvas.paste(src, ((w - src.width) // 2, h - src.height))
    canvas.save(f"{c}/Assets/ProbeTmp/pad_{w}x{h}.png")
PY
cp "$HERE/probe/TextureProbe.cs" "$C/Assets/@Scripts/Editor/Probe/"
"$UNITY" -batchmode -quit -nographics -projectPath "$C" -executeMethod F1.Editor.Probe.TextureProbe.Run -logFile "$S/probe.log"
sed -n '/PROBE_BEGIN/,/PROBE_END/p' "$S/probe.log"

rm -rf "$C/Assets/ProbeTmp"/* "$C/Assets/@Scripts/Editor/Probe/TextureProbe.cs"*
"$SRC/.venv/bin/python" - "$C" <<'PY' || exit 1
import sys, shutil
from PIL import Image
c = sys.argv[1]; t = c + "/Assets/ProbeTmp/"
fig, pose, icon = (c + "/Assets/@Art/" + p for p in ("Unit/Job/valkyrie.png", "Pose/Job/valkyrie_attack.png", "Item/longbow.png"))
pose_canvas = Image.open(pose).convert("RGBA").resize((2016, 1008), Image.LANCZOS)   # the working canvas, the size the file had before round 31
pose_canvas.save(t + "pose_orig.png"); pose_canvas.save(t + "pose_crunch.png"); pose_canvas.save(t + "pose_nomips.png")
pose_canvas.resize((2048, 1024), Image.LANCZOS).save(t + "pose_pot.png"); shutil.copy(t + "pose_pot.png", t + "pose_potraw.png")
for name in ("fig_orig", "fig_crunch", "fig_crunchn", "fig_nomips"): shutil.copy(fig, t + name + ".png")
for name in ("icon_orig", "icon_crunch", "icon_nomips", "icon_nomipsraw"): shutil.copy(icon, t + name + ".png")
PY
cp "$HERE/probe/TextureCompare.cs" "$C/Assets/@Scripts/Editor/Probe/"
rm -rf "$S/readback"
PROBE_OUT="$S/readback" "$UNITY" -batchmode -quit -projectPath "$C" -executeMethod F1.Editor.Probe.TextureCompare.Run -logFile "$S/compare.log"
sed -n '/COMPARE_BEGIN/,/COMPARE_END/p' "$S/compare.log"
"$SRC/.venv/bin/python" "$HERE/compare.py" "$S/readback"
