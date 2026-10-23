"""Round 98, version 02 (frozen 2026-10-23): the intro storyboard of "검은 백합" (Design/33 §9). No API call.

Reads its own copies of the scenes (scenes/<key>.jpg, from output/background/<key>_v02.png or <key>.png) and writes,
next to this script:
  frames/fNN.jpg     each scene as the 16:9 screen would show it (1280x720), subtitles laid over the bottom
  storyboard_N.jpg   the scenes, seven to a sheet, with the scene number, time and short-cut mark

Run: .venv/bin/python ArtPipeline/Archive/98-intro-storyboard/v02/make_storyboard.py
"""
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[3]
# Version 02 keeps its own copies of the scenes (JPG) so it can be rebuilt without the ignored output folder.
SCENES_DIR = HERE / "scenes"
FONT = ROOT / "Assets" / "@Fonts" / "Source" / "Pretendard" / "Pretendard-Medium.ttf"

# (number, time, image key, lines, short cut). A line in quotes is someone else's voice (printed, pale gold);
# the rest is the captain's own (white). The lobby frame uses the town background the game already has.
SCENES = [
    ("1", "0:00–0:08", "b01_mural", ["구덩이의 괴물이 마을을 하나씩 먹던 시절.", "젊은 왕자가 괴물을 막았다. 모두가 그를 영웅이라 불렀다."], "남긴다"),
    ("2", "0:08–0:16", "b02_brooch", ["우리는 백합단. 백 명이었다.", "그리고 마리안이 있었다. 돌아오면 혼인하기로 했다."], "남긴다"),
    ("3", "0:16–0:24", "b03_throne", ["왕이 우리를 불렀다. “구덩이를 쳐라. 내 병사들을 붙여 주마.”", "그날 왕의 눈이 누구를 보고 있었는지, 나는 몰랐다."], "뺀다"),
    ("4", "0:24–0:36", "b04_massacre", ["구덩이 앞에서, 함께 간 병사들의 얼굴이 바뀌었다.", "백 명이 쓰러졌다.", "괴물이 내 오른손을 물어뜯었을 때 알았다. 왕이 우리를 바쳤다."], "남긴다"),
    ("4-2", "0:36–0:46", "b04b_kings_voice", ["“약속대로 백 명입니다.”", "“스무 해 뒤 해가 먹히는 날, 왕국을 통째로 드리지요.”", "그날을, 나는 들었다."], "남긴다"),
    ("5", "0:46–0:54", "b05_trial", ["“병사들을 모두 죽게 한 죄.”", "나는 살아남은 죄로 갇혔다."], "둘째 줄만"),
    ("6", "0:54–1:06", "b06_prison", ["나는 하루도 빠짐없이 금을 그었다. 그날까지 남은 날을.", "벽 너머 늙은 기사가 나를 버티게 했고, 숨긴 보물의 자리를 알려 주었다.", "열여덟 해. 아흔아홉 주가 남았을 때 그가 죽었다."], "셋째 줄만"),
    ("7", "1:06–1:14", "b07_sack", ["그가 죽던 밤, 나는 그의 시체 자루에 들어갔다.", "감옥은 시체를 강에 버린다."], "남긴다"),
    ("7-2", "1:14–1:22", "b07b_treasure", ["강을 따라 내려가, 늙은 기사가 알려 준 곳을 찾았다.", "무너진 사냥 별장, 마른 우물 밑. 그가 말한 그대로였다."], "첫 줄만"),
    ("8", "1:22–1:32", "b08_market", ["보물로 맨 먼저 산 것은 사람이었다.", "왕의 공물 장부를 쓰던 아이, 리넷."], "남긴다"),
    ("9", "1:32–1:42", "b09_ledger", ["장부의 마지막 장에도 그날이 적혀 있었다.", "일식의 날. 바칠 것: 왕국 전부."], "뺀다"),
    ("10", "1:42–1:50", "b10_wrist", ["물린 자리에 구덩이가 남았다.", "가까이 가면, 나는 그것들이 된다."], "남긴다"),
    ("11", "1:50–1:58", "b11_queen", ["마리안은 왕비가 되어 있었다."], "남긴다"),
    ("12", "1:58–2:08", "b12_banner", ["백합단은 죽었다.", "이제 검은 백합이다. 나 대신 갈 칼을 산다."], "남긴다"),
    ("13", "2:08–", "lobby_town", [], "남긴다"),
]

FRAME = (1280, 720)
WHITE = (238, 232, 220)
GOLD = (214, 190, 130)
SHADE = (14, 12, 12)


def font(size):
    return ImageFont.truetype(str(FONT), size)


def screen_crop(image):
    """The 3:2 scene seen through the 16:9 screen: the middle band, then scaled to the frame."""
    width, height = image.size
    band = round(width * 9 / 16)
    top = (height - band) // 2
    return image.crop((0, top, width, top + band)).resize(FRAME, Image.LANCZOS)


def subtitle(frame, lines):
    draw = ImageDraw.Draw(frame, "RGBA")
    if not lines:
        return
    size = 30
    gap = 12
    face = font(size)
    total = len(lines) * size + (len(lines) - 1) * gap
    bottom = FRAME[1] - 46
    top = bottom - total
    draw.rectangle((0, top - 34, FRAME[0], FRAME[1]), fill=(*SHADE, 150))
    y = top
    for line in lines:
        color = GOLD if line.startswith("“") else WHITE
        width = draw.textlength(line, font=face)
        x = (FRAME[0] - width) / 2
        draw.text((x + 2, y + 2), line, font=face, fill=(0, 0, 0, 200))
        draw.text((x, y), line, font=face, fill=color)
        y += size + gap


def lobby(frame):
    """The first lobby after the intro: the countdown and the guide line (Design/33 §7·§9)."""
    draw = ImageDraw.Draw(frame, "RGBA")
    draw.rectangle((0, 0, FRAME[0], 64), fill=(*SHADE, 190))
    head = font(30)
    text = "일식까지 99주"
    draw.text(((FRAME[0] - draw.textlength(text, font=head)) / 2, 14), text, font=head, fill=GOLD)
    guide = font(26)
    text = "첫 마을 — 광산 마을"
    width = draw.textlength(text, font=guide)
    draw.rectangle(((FRAME[0] - width) / 2 - 24, FRAME[1] - 92, (FRAME[0] + width) / 2 + 24, FRAME[1] - 40), fill=(*SHADE, 190))
    draw.text(((FRAME[0] - width) / 2, FRAME[1] - 84), text, font=guide, fill=WHITE)


def make_frame(key, lines):
    path = SCENES_DIR / f"{key}.jpg"
    if not path.exists():
        frame = Image.new("RGB", FRAME, (40, 36, 34))
        draw = ImageDraw.Draw(frame)
        draw.text((40, 40), f"{key}: 그림 없음", font=font(32), fill=WHITE)
    else:
        frame = screen_crop(Image.open(path).convert("RGB"))
    if key == "lobby_town":
        lobby(frame)
    else:
        subtitle(frame, lines)
    return frame


def sheet(out, scenes, name, first):
    cols, thumb, pad, label = 2, (800, 450), 28, 46
    rows = (len(scenes) + cols - 1) // cols
    head = 90
    width = cols * thumb[0] + (cols + 1) * pad
    height = head + rows * (thumb[1] + label + pad) + pad
    board = Image.new("RGB", (width, height), (24, 22, 22))
    draw = ImageDraw.Draw(board)
    draw.text((pad, 26), "Round 98 · 인트로 콘티 「검은 백합」 (Design/33 §9)", font=font(34), fill=WHITE)
    for index, (number, time, key, lines, cut) in enumerate(scenes):
        col, row = index % cols, index // cols
        x = pad + col * (thumb[0] + pad)
        y = head + row * (thumb[1] + label + pad)
        draw.text((x, y + 6), f"#{number}  {time}", font=font(26), fill=GOLD)
        mark = f"짧은 판: {cut}"
        draw.text((x + thumb[0] - draw.textlength(mark, font=font(22)), y + 10), mark, font=font(22),
                  fill=(150, 140, 130) if cut == "뺀다" else WHITE)
        frame = Image.open(out / "frames" / f"f{first + index + 1:02d}.jpg").resize(thumb, Image.LANCZOS)
        board.paste(frame, (x, y + label))
    board.save(out / name, quality=88)
    print(f"{name}: {board.size[0]}x{board.size[1]}")


def main():
    out = HERE
    (out / "frames").mkdir(parents=True, exist_ok=True)
    for index, (number, time, key, lines, cut) in enumerate(SCENES):
        frame = make_frame(key, lines)
        frame.save(out / "frames" / f"f{index + 1:02d}.jpg", quality=88)
    for first in range(0, len(SCENES), 7):
        sheet(out, SCENES[first:first + 7], f"storyboard_{first // 7 + 1}.jpg", first)


if __name__ == "__main__":
    main()
