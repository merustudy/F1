"""The references the goblin raider's attack and hit poses are drawn after (round 26), no API call.
  .venv/bin/python ArtPipeline/Archive/26-monster-motion/make_references.py

Round 23's rule (../23-motion-six/make_references.py) turned for a creature that faces left: the approved raw is laid on
the canvas the pose is drawn on (1536x1024), smaller, with its soles three quarters down and open floor under them, so
that a weapon can come down below the feet. An enemy's attack lunges to the left, so its reference stands in the right
third with room on the left; a hit is knocked back to the right, so its reference stands in the middle. The pose is
scaled back to the figure's size afterwards (fit_monster_poses.py).
The approved raw of an enemy is the one with its pupils taken off (round 17), the one the game's figure is fitted from.
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
    "goblin_raider": ROOT / "ArtPipeline/Archive/17-outline-pupils/approved/enemy/goblin_raider.raw.png",
}
CANVAS = (1536, 1024)
HEIGHT = 700            # the figure's height on the canvas, as round 23's
SOLES = 768             # three quarters down, with a quarter of open floor under them
RIGHT = 152             # an attack's figure ends near the right third: the lunge reaches to the left
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
        for pose, left in (("attack", CANVAS[0] - RIGHT - subject.width), ("hit", (CANVAS[0] - subject.width) // 2)):
            canvas = Image.new("RGBA", CANVAS, (0, 0, 0, 0))
            canvas.paste(subject, (left, top))
            canvas.save(out / f"{pose}-{key}.png")
        print(f"{key}: the approved raw x{scale:.4f}, {subject.width}x{subject.height}, soles at {SOLES} of {CANVAS[1]}")


if __name__ == "__main__":
    main()
