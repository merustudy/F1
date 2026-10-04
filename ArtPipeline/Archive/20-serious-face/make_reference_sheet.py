"""The style reference sheet again, from the approved figures with the serious face (round 20), no API call.
  .venv/bin/python ArtPipeline/Archive/20-serious-face/make_reference_sheet.py

Every type attaches References/Character/style_ref_roster.png, the sheet of the three approved figures (round 12:
the valkyrie, the knight and the archmage on white). When the approved figures change, the sheet changes with them
(Architecture/13: an approved picture of ours becomes the reference). The old sheet (before/style_ref_roster.png) is
read for where and how large each figure stood, and the new raw of the same figure is put there at the same scale,
on the same bottom line and centre.
"""
from pathlib import Path
from PIL import Image

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
OLD_SHEET = HERE / "before/style_ref_roster.png"
OLD_RAW = ROOT / "ArtPipeline/Archive/12-roar-style/approved/character"
NEW_RAW = HERE / "approved/character"
OUT = ROOT / "ArtPipeline/References/Character/style_ref_roster.png"
ORDER = ["valkyrie", "knight", "archmage"]


def subject(image):
    return image.getchannel("A").point(lambda v: 255 if v > 8 else 0).getbbox()


def figures(sheet):
    """The boxes of the figures on the white sheet: runs of columns holding non-white pixels, left to right."""
    grey = sheet.convert("L").point(lambda v: 255 if v < 245 else 0)
    columns = [any(grey.getpixel((x, y)) for y in range(0, grey.height, 2)) for x in range(grey.width)]
    runs, start = [], None
    for x, on in enumerate(columns + [False]):
        if on and start is None:
            start = x
        elif not on and start is not None:
            if x - start > 40:
                runs.append((start, x))
            start = None
    boxes = []
    for x0, x1 in runs:
        box = grey.crop((x0, 0, x1, grey.height)).getbbox()
        boxes.append((x0 + box[0], box[1], x0 + box[2], box[3]))
    return boxes


def main():
    old = Image.open(OLD_SHEET).convert("RGB")
    boxes = figures(old)
    assert len(boxes) == 3, boxes
    sheet = Image.new("RGB", old.size, (255, 255, 255))
    for key, box in zip(ORDER, boxes):
        old_raw = Image.open(OLD_RAW / f"{key}.raw.png").convert("RGBA")
        scale = (box[3] - box[1]) / (subject(old_raw)[3] - subject(old_raw)[1])
        new_raw = Image.open(NEW_RAW / f"{key}.raw.png").convert("RGBA")
        cut = new_raw.crop(subject(new_raw))
        cut = cut.resize((round(cut.width * scale), round(cut.height * scale)), Image.LANCZOS)
        x = round((box[0] + box[2]) / 2 - cut.width / 2)
        y = box[3] - cut.height
        sheet.paste(cut, (x, y), cut)
        print(f"{key}: scale {scale:.4f}, at ({x}, {y}) {cut.width}x{cut.height} (old box {box})")
    sheet.save(OUT)
    print("기준 그림:", OUT, sheet.size)


if __name__ == "__main__":
    main()
