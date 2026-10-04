"""The reference for a pose that reaches below the feet (round 19, attack 2), no API call.
  .venv/bin/python ArtPipeline/Archive/19-motion-test/make_reference.py

The first attack (valkyrie_attack) was drawn after the approved raw as it is: the boots stand at the very bottom of its
canvas, so the model kept the axe above them and bent the axe head along the bottom edge (user's verdict 2026-10-04).
Here the approved raw is laid on the canvas the pose is drawn on (1536x1024), smaller, with open floor under the boots
and room to the right for the lunge, so the model draws the pose at that size and place and the axe can come down
below the soles. The pose is scaled back to the figure's size afterwards (fit_motion.py), so drawing it smaller only
costs resolution it has to spare: the game shows the figure about 616 px high on its canvas.
Writes reference-floor.png next to this file.
"""
import sys
from pathlib import Path
from PIL import Image

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
sys.path.insert(0, str(ROOT / "ArtPipeline" / "tools"))
from gen_image import subject_box  # noqa: E402

APPROVED = ROOT / "ArtPipeline/Archive/12-roar-style/approved/character/valkyrie.raw.png"
CANVAS = (1536, 1024)
HEIGHT = 700            # the figure's height on the canvas: the approved raw's 918 times about 0.76
TOP = 68                # so the soles stand at 768, three quarters down, with a quarter of open floor under them
LEFT = 152              # the back foot near the left third: the lunge and the axe reach to the right


def main():
    approved = Image.open(APPROVED).convert("RGBA")
    box = subject_box(approved)
    subject = approved.crop(box)
    scale = HEIGHT / subject.height
    subject = subject.resize((round(subject.width * scale), HEIGHT), Image.LANCZOS)
    canvas = Image.new("RGBA", CANVAS, (0, 0, 0, 0))
    canvas.paste(subject, (LEFT, TOP))
    canvas.save(HERE / "reference-floor.png")
    print(f"reference-floor.png: the approved raw x{scale:.4f} at ({LEFT}, {TOP}), {subject.width}x{subject.height}, "
          f"soles at {TOP + HEIGHT} of {CANVAS[1]}")


if __name__ == "__main__":
    main()
