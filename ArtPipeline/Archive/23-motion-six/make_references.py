"""The references the six mercenaries' attack and hit poses are drawn after (round 23), no API call.
  .venv/bin/python ArtPipeline/Archive/23-motion-six/make_references.py

The rule of round 19's second attack (../19-motion-test/make_reference.py): the approved raw is laid on the canvas the
pose is drawn on (1536x1024), smaller, with its soles three quarters down and open floor under them, so that a weapon
can come down below the feet. An attack lunges to the right, so its reference stands in the left third with room on the
right; a hit is knocked back to the left, so its reference stands in the middle. The pose is scaled back to the figure's
size afterwards (fit_poses.py).
Writes references/attack-<key>.png and references/hit-<key>.png.
"""
import sys
from pathlib import Path
from PIL import Image

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
sys.path.insert(0, str(ROOT / "ArtPipeline" / "tools"))
from gen_image import subject_box  # noqa: E402

APPROVED = {
    "knight": ROOT / "ArtPipeline/Archive/20-serious-face/approved/character/knight.raw.png",
    "valkyrie": ROOT / "ArtPipeline/Archive/20-serious-face/approved/character/valkyrie.raw.png",
    "bishop": ROOT / "ArtPipeline/Archive/20-serious-face/approved/character/bishop.raw.png",
    "paladin": ROOT / "ArtPipeline/Archive/22-paladin-head/approved/character/paladin.raw.png",
    "archmage": ROOT / "ArtPipeline/Archive/20-serious-face/approved/character/archmage.raw.png",
    "spellblade": ROOT / "ArtPipeline/Archive/20-serious-face/approved/character/spellblade.raw.png",
}
CANVAS = (1536, 1024)
HEIGHT = 700            # the figure's height on the canvas, as round 19's (its raw's 918 times about 0.76)
SOLES = 768             # three quarters down, with a quarter of open floor under them
LEFT = 152              # an attack's figure starts near the left third: the lunge reaches to the right
MAX_WIDTH = 1100        # a wide figure is made smaller so that the room it needs is left


def main():
    out = HERE / "references"
    out.mkdir(exist_ok=True)
    for key, path in APPROVED.items():
        approved = Image.open(path).convert("RGBA")
        subject = approved.crop(subject_box(approved))
        scale = min(HEIGHT / subject.height, MAX_WIDTH / subject.width)
        subject = subject.resize((round(subject.width * scale), round(subject.height * scale)), Image.LANCZOS)
        top = SOLES - subject.height
        for pose, left in (("attack", LEFT), ("hit", (CANVAS[0] - subject.width) // 2)):
            canvas = Image.new("RGBA", CANVAS, (0, 0, 0, 0))
            canvas.paste(subject, (left, top))
            canvas.save(out / f"{pose}-{key}.png")
        print(f"{key}: the approved raw x{scale:.4f}, {subject.width}x{subject.height}, soles at {SOLES} of {CANVAS[1]}")


if __name__ == "__main__":
    main()
