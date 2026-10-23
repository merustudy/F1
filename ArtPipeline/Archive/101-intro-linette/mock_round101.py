"""Round 101 mockups: the intro and Linette's dialogue. No calls.

Reads the draft data in data/ (the same columns the game would read) so the mockup shows the data-driven plan:
the scenes, their order, time, lines, voices, motion and transitions all come from data/StorySceneData.csv,
and the dialogue lines, speakers and expressions from data/Dialogue*.csv and data/NpcData.csv.

    .venv/bin/python ArtPipeline/Archive/101-intro-linette/mock_round101.py stills
    .venv/bin/python ArtPipeline/Archive/101-intro-linette/mock_round101.py video     # MP4s (gitignored)
"""
import csv
import subprocess
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
DATA = HERE / "data"
SCENES_DIR = ROOT / "ArtPipeline" / "Archive" / "98-intro-storyboard" / "v02" / "scenes"
LINETTE = ROOT / "ArtPipeline" / "Archive" / "99-linette" / "approved"
LINETTE_FIT = ROOT / "ArtPipeline" / "output" / "character" / "linette_ingame_dhair.png"
SHOTS = Path.home() / "Library" / "Caches" / "F1" / "screenshots" / "20261023-r100"
FONT = ROOT / "Assets" / "@Fonts" / "Source" / "Pretendard" / "Pretendard-Medium.ttf"

WHITE = (238, 232, 220)
GOLD = (214, 190, 130)
BONE = (196, 188, 170)
BRASS = (176, 146, 88)
DIM = (150, 144, 132)

# Mockup-only stand-ins for things the game takes from elsewhere.
COUNTDOWN = {"ko-KR": "일식까지 99주", "en-US": "99 weeks to the eclipse"}  # UI String Table (Story.WeeksLeft) + TotalDays
SKIP = {"ko-KR": "건너뛰기  Esc", "en-US": "Skip  Esc"}
HINT = {"ko-KR": "클릭하면 다음 장면", "en-US": "Click for the next scene"}
NEXT = {"ko-KR": "클릭: 다음  ·  Esc: 건너뛰기", "en-US": "Click: next  ·  Esc: skip"}

# Presentation constants (code, not story): line reveal, fades, motion size.
FIRST_LINE_MS, LINE_GAP_MS, LINE_FADE_MS = 600, 2200, 500
CROSS_MS, BLACK_MS, OPEN_MS = 800, 600, 1000
ZOOM = 1.10

# The image file each logical address stands for in this mockup (02판 storyboard copies).
def image_file(address):
    if address == "background/lobby/town":
        return ROOT / "Assets" / "@Art" / "Background" / "Lobby" / "town.png"
    key = address.split("/")[-1]
    names = {
        "mural": "b01_mural", "brooch": "b02_brooch", "throne": "b03_throne", "massacre": "b04_massacre",
        "kings-voice": "b04b_kings_voice", "trial": "b05_trial", "prison": "b06_prison", "sack": "b07_sack",
        "treasure": "b07b_treasure", "market": "b08_market", "ledger": "b09_ledger", "wrist": "b10_wrist",
        "queen": "b11_queen", "banner": "b12_banner",
    }
    return SCENES_DIR / f"{names[key]}.jpg"


def font(size):
    return ImageFont.truetype(str(FONT), size)


def rows(name):
    with open(DATA / name, encoding="utf-8") as f:
        return list(csv.DictReader(f))


def scenes(sequence="intro"):
    result = [r for r in rows("StorySceneData.csv") if r["Sequence"] == sequence]
    return sorted(result, key=lambda r: int(r["Order"]))


def lines_of(scene, locale, short):
    found = []
    for n in (1, 2, 3):
        text = scene[f"Line{n}.{locale}"]
        if text:
            found.append((n, text, scene[f"Line{n}Voice"]))
    if not short or scene["ShortLines"] == "all":
        return found
    keep = {int(x) for x in scene["ShortLines"].split("+")}
    return [line for line in found if line[0] in keep]


def duration(scene, short):
    return int(scene["ShortDurationMs"] if short else scene["DurationMs"])


# ---------- the intro frame ----------

class Plate:
    """A scene's picture, pre-scaled once, with the Ken Burns window for a moment of the scene."""

    def __init__(self, scene, size):
        self.scene = scene
        self.size = size
        # A scene without a picture is the run's first lobby itself: its lines are a banner over the lobby screen.
        src = (Image.open(image_file(scene["Image"])) if scene["Image"] else lobby_shot()).convert("RGB")
        width = int(size[0] * ZOOM * 1.05)
        self.image = src.resize((width, int(width * src.height / src.width)), Image.LANCZOS)

    def window(self, p):
        """The crop box at progress p (0..1) of the scene's motion."""
        iw, ih = self.image.size
        aspect = self.size[1] / self.size[0]
        motion = self.scene["Motion"]
        lerp = lambda a, b: a + (b - a) * p
        if motion == "ZoomIn":
            zoom, cx, cy = lerp(1.0, ZOOM), 0.5, 0.47
        elif motion == "ZoomOut":
            zoom, cx, cy = lerp(ZOOM, 1.0), 0.5, 0.47
        elif motion == "PanRight":
            zoom, cx, cy = ZOOM, lerp(0.0, 1.0), 0.47
        elif motion == "PanLeft":
            zoom, cx, cy = ZOOM, lerp(1.0, 0.0), 0.47
        elif motion == "PanUp":
            zoom, cx, cy = 1.04, 0.5, lerp(1.0, 0.0)
        elif motion == "PanDown":
            zoom, cx, cy = 1.04, 0.5, lerp(0.0, 1.0)
        else:
            zoom, cx, cy = 1.0, 0.5, 0.5
        w = iw / zoom
        h = w * aspect
        if h > ih:
            h = ih
            w = h / aspect
        x = (iw - w) * cx
        y = (ih - h) * cy
        return (x, y, x + w, y + h)

    def at(self, p):
        return self.image.resize(self.size, Image.BILINEAR, box=self.window(p))


def text_layer(size, locale, items):
    """items: (text, voice, alpha 0..1). Captions at the bottom over a soft band; the guide voice at the upper middle."""
    s = size[0] / 1920
    layer = Image.new("RGBA", size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(layer)
    subtitles = [i for i in items if i[1] in ("Narration", "Quote")]
    if subtitles:
        band = Image.new("L", (1, 256))
        for y in range(256):
            band.putpixel((0, y), int(185 * min(1, y / 140)))
        top = int(size[1] * 0.74)
        layer.paste((10, 9, 9, 255), (0, top, size[0], size[1]), band.resize((size[0], size[1] - top)))
        f = font(int(38 * s))
        step = int(56 * s)
        y0 = size[1] - int(64 * s) - step * len(subtitles)
        for i, (text, voice, alpha) in enumerate(subtitles):
            color = GOLD if voice == "Quote" else WHITE
            w = draw.textlength(text, font=f)
            draw.text(((size[0] - w) / 2, y0 + i * step), text, font=f, fill=color + (int(255 * alpha),))
    for text, voice, alpha in items:
        if voice == "Countdown":
            f = font(int(76 * s))
            w = draw.textlength(text, font=f)
            draw.text(((size[0] - w) / 2, int(330 * s)), text, font=f, fill=GOLD + (int(255 * alpha),),
                      stroke_width=int(3 * s), stroke_fill=(12, 10, 10, int(230 * alpha)))
        elif voice == "Guide":
            f = font(int(42 * s))
            w = draw.textlength(text, font=f)
            draw.text(((size[0] - w) / 2, int(440 * s)), text, font=f, fill=WHITE + (int(255 * alpha),),
                      stroke_width=int(3 * s), stroke_fill=(12, 10, 10, int(230 * alpha)))
    return layer


def scene_frame(plate, t, locale, short, skip_ui=True, guide_dim=True):
    scene = plate.scene
    dur = duration(scene, short)
    frame = plate.at(min(1.0, t / dur)).convert("RGBA")
    shown = lines_of(scene, locale, short)
    items = []
    for i, (_, text, voice) in enumerate(shown):
        start = FIRST_LINE_MS + i * LINE_GAP_MS
        a = max(0.0, min(1.0, (t - start) / LINE_FADE_MS))
        if a > 0:
            items.append((text, voice, a))
    if scene["Countdown"] == "true":
        if guide_dim:
            shade = Image.new("RGBA", frame.size, (8, 8, 10, 0))
            sd = ImageDraw.Draw(shade)
            for y in range(int(frame.size[1] * 0.62)):
                sd.line([(0, y), (frame.size[0], y)], fill=(8, 8, 10, int(120 * (1 - y / (frame.size[1] * 0.62)))))
            frame = Image.alpha_composite(frame, shade)
        a = max(0.0, min(1.0, (t - 200) / LINE_FADE_MS))
        items.insert(0, (COUNTDOWN[locale], "Countdown", a))
    frame = Image.alpha_composite(frame, text_layer(frame.size, locale, items))
    if skip_ui and scene["Image"]:  # over the lobby a click just closes the banner
        frame = Image.alpha_composite(frame, skip_layer(frame.size, locale, t))
    return frame


def skip_layer(size, locale, t_total):
    s = size[0] / 1920
    layer = Image.new("RGBA", size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    box = (size[0] - int(250 * s), int(28 * s), size[0] - int(30 * s), int(80 * s))
    d.rounded_rectangle(box, radius=int(6 * s), fill=(14, 13, 14, 150), outline=BRASS + (150,), width=max(1, int(2 * s)))
    f = font(int(24 * s))
    text = SKIP[locale]
    w = d.textlength(text, font=f)
    d.text(((box[0] + box[2] - w) / 2, box[1] + int(12 * s)), text, font=f, fill=BONE + (220,))
    return layer


class Intro:
    def __init__(self, size, locale="ko-KR", short=False):
        self.size, self.locale, self.short = size, locale, short
        self.list = [sc for sc in scenes() if duration(sc, short) > 0]
        self.plates = [Plate(sc, size) for sc in self.list]
        self.starts = []
        t = 0
        for sc in self.list:
            self.starts.append(t)
            t += duration(sc, short)
        self.total = t

    def frame(self, t):
        i = max(k for k, s in enumerate(self.starts) if s <= t) if t < self.total else len(self.list) - 1
        local = t - self.starts[i]
        dur = duration(self.list[i], self.short)
        hint_alpha = max(0.0, min(1.0, (4000 - t) / 600)) if t < 4000 else 0
        img = scene_frame(self.plates[i], local, self.locale, self.short)
        transition = self.list[i]["Transition"]
        if i == 0 and local < OPEN_MS:
            img = Image.blend(Image.new("RGBA", self.size, (0, 0, 0, 255)), img, local / OPEN_MS)
        if i > 0 and self.list[i - 1]["Transition"] == "Black" and local < BLACK_MS:
            img = Image.blend(Image.new("RGBA", self.size, (0, 0, 0, 255)), img, local / BLACK_MS)
        remain = dur - local
        if i + 1 < len(self.list):
            if transition == "Cross" and remain < CROSS_MS:
                nxt = scene_frame(self.plates[i + 1], 0, self.locale, self.short)
                img = Image.blend(img, nxt, 1 - remain / CROSS_MS)
            elif transition == "Black" and remain < BLACK_MS:
                img = Image.blend(img, Image.new("RGBA", self.size, (0, 0, 0, 255)), 1 - remain / BLACK_MS)
        if hint_alpha > 0:
            s = self.size[0] / 1920
            layer = Image.new("RGBA", self.size, (0, 0, 0, 0))
            d = ImageDraw.Draw(layer)
            f = font(int(22 * s))
            text = HINT[self.locale]
            w = d.textlength(text, font=f)
            d.text((self.size[0] - int(140 * s) - w / 2, int(92 * s)), text, font=f, fill=DIM + (int(230 * hint_alpha),))
            img = Image.alpha_composite(img, layer)
        return img


# ---------- Linette's dialogue ----------

def portraits():
    files = {"neutral": "linette_portrait8b.png", "cold": "linette_portrait7.png", "smile": "linette_portrait8.png"}
    return {k: Image.open(LINETTE / v).convert("RGBA") for k, v in files.items()}


def dialogue_lines(dialogue):
    found = [r for r in rows("DialogueLineData.csv") if r["Dialogue"] == dialogue]
    return sorted(found, key=lambda r: int(r["Order"]))


def wrap(draw, text, f, width):
    out, line = [], ""
    for ch in text:
        if draw.textlength(line + ch, font=f) > width:
            cut = line.rfind(" ")
            if cut > 0:
                out.append(line[:cut])
                line = line[cut + 1:] + ch
            else:
                out.append(line)
                line = ch
        else:
            line += ch
    if line:
        out.append(line)
    return out


def lobby_shot():
    return Image.open(SHOTS / "ko_03_lobby_party.png").convert("RGBA")


def dialogue_frame(base, line, index, count, portrait, locale="ko-KR", appear=1.0, text_alpha=1.0, shade=0.62):
    """Plan A: the town darkened, Linette's dialogue picture on the left, the dialogue box along the bottom."""
    size = base.size
    img = base.copy()
    img = Image.alpha_composite(img, Image.new("RGBA", size, (6, 6, 8, int(255 * shade * appear))))
    # portrait: height 940, bottom at the screen bottom, x from 70 (slides in 40px)
    ph = 880
    pw = int(portrait.width * ph / portrait.height)
    p = portrait.resize((pw, ph), Image.LANCZOS)
    if appear < 1:
        alpha = p.getchannel("A").point(lambda v: int(v * appear))
        p.putalpha(alpha)
    img.alpha_composite(p, (int(40 - 40 * (1 - appear)), size[1] - ph + 10))
    layer = Image.new("RGBA", size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    a = int(255 * appear)
    box = (640, 820, 1860, 1040)
    d.rectangle(box, fill=(16, 15, 18, int(236 * appear)), outline=BRASS + (a,), width=3)
    name_box = (670, 790, 870, 846)
    d.rectangle(name_box, fill=(28, 24, 22, a), outline=BRASS + (a,), width=3)
    speaker = {r["Id"]: r for r in rows("NpcData.csv")}[line["Speaker"]][f"Name.{locale}"]
    f = font(32)
    w = d.textlength(speaker, font=f)
    d.text(((name_box[0] + name_box[2] - w) / 2, name_box[1] + 9), speaker, font=f, fill=GOLD + (a,))
    f = font(36)
    for i, ln in enumerate(wrap(d, line[f"Text.{locale}"], f, 1150)):
        d.text((690, 872 + i * 54), ln, font=f, fill=WHITE + (int(255 * text_alpha * appear),))
    f = font(22)
    hint = f"{index + 1}/{count}    {NEXT[locale]}"
    w = d.textlength(hint, font=f)
    d.text((1830 - w, 1000), hint, font=f, fill=DIM + (a,))
    return Image.alpha_composite(img, layer)


def plan_b(base):
    """Plan B: Linette stands by the gate in the town; pointing at her shows a bubble; a click opens the plan A box."""
    img = base.copy()
    fig = Image.open(LINETTE_FIT).convert("RGBA")
    h = 280
    fw = int(fig.width * h / fig.height)
    fig = fig.resize((fw, h), Image.LANCZOS)
    foot = (1585, 700)
    # a soft ground shadow
    shadow = Image.new("RGBA", img.size, (0, 0, 0, 0))
    ImageDraw.Draw(shadow).ellipse((foot[0] - 46, foot[1] - 12, foot[0] + 46, foot[1] + 8), fill=(0, 0, 0, 110))
    img = Image.alpha_composite(img, shadow.filter(ImageFilter.GaussianBlur(4)))
    img.alpha_composite(fig, (foot[0] - fw // 2, foot[1] - h + int(h * 16 / 896)))
    d = ImageDraw.Draw(img)
    plate = (foot[0] - 70, foot[1] + 14, foot[0] + 70, foot[1] + 56)
    d.rectangle(plate, fill=(14, 14, 16, 230), outline=BRASS, width=2)
    f = font(26)
    w = d.textlength("리넷", font=f)
    d.text(((plate[0] + plate[2] - w) / 2, plate[1] + 6), "리넷", font=f, fill=WHITE)
    bubble = (foot[0] - 230, foot[1] - h - 104, foot[0] + 230, foot[1] - h - 24)
    d.rounded_rectangle(bubble, radius=10, fill=(22, 20, 22, 240), outline=BRASS, width=2)
    d.polygon([(foot[0] - 14, bubble[3]), (foot[0] + 14, bubble[3]), (foot[0], bubble[3] + 18)], fill=(22, 20, 22, 240))
    f = font(26)
    for i, t in enumerate(["장부의 다음 줄: 광산 마을", "누르면 리넷의 말을 다시 듣는다"]):
        w = d.textlength(t, font=f)
        d.text(((bubble[0] + bubble[2] - w) / 2, bubble[1] + 10 + i * 32), t, font=f, fill=WHITE if i == 0 else DIM)
    return img


def plan_c():
    """Plan C: the ledger line sits in the expedition screen's panel, with a small bust of Linette."""
    img = Image.open(SHOTS / "ko_56_expedition.png").convert("RGBA")
    d = ImageDraw.Draw(img)
    area = (1346, 700, 1854, 780)
    d.rectangle((1340, 694, 1860, 786), fill=(30, 33, 42, 255))
    bust = portraits()["neutral"].crop((150, 60, 870, 780)).resize((86, 86), Image.LANCZOS)
    mask = Image.new("L", (86, 86), 0)
    ImageDraw.Draw(mask).ellipse((0, 0, 85, 85), fill=255)
    ring = Image.new("RGBA", (92, 92), (0, 0, 0, 0))
    ImageDraw.Draw(ring).ellipse((0, 0, 91, 91), outline=BRASS, width=3)
    img.paste(bust, (area[0] + 3, area[1] - 3), mask)
    img.alpha_composite(ring, (area[0], area[1] - 6))
    f = font(22)
    d.text((area[0] + 108, area[1] - 2), "리넷 — 장부의 첫 줄", font=f, fill=GOLD)
    f = font(21)
    d.text((area[0] + 108, area[1] + 30), "무너진 갱의 광부들이 구덩이에 떨어졌습니다.", font=f, fill=WHITE)
    return img


def title_replay():
    img = Image.open(SHOTS / "ko_01_title.png").convert("RGBA")
    d = ImageDraw.Draw(img)
    d.rectangle((500, 780, 1420, 880), fill=(22, 22, 28, 255))
    labels = ["언어: 한국어", "음악: 켬", "효과음: 켬", "인트로 보기"]
    x0 = (1920 - (4 * 260 + 3 * 20)) // 2
    f = font(28)
    for i, t in enumerate(labels):
        x = x0 + i * 280
        d.rectangle((x, 800, x + 260, 864), fill=(59, 64, 79, 255))
        w = d.textlength(t, font=f)
        d.text((x + (260 - w) / 2, 814), t, font=f, fill=WHITE)
    d.rectangle((x0 + 3 * 280 - 4, 796, x0 + 3 * 280 + 264, 868), outline=(232, 120, 160), width=3)
    f = font(22)
    d.text((x0 + 3 * 280 - 40, 884), "본편을 처음부터. 끝나면 타이틀로", font=f, fill=(232, 120, 160))
    return img


def label(img, text):
    d = ImageDraw.Draw(img)
    f = font(30)
    w = d.textlength(text, font=f)
    d.rectangle((0, 0, w + 40, 52), fill=(232, 120, 160, 255))
    d.text((20, 8), text, font=f, fill=(20, 16, 20))
    return img


def sheet(images, cols, title, scale=0.5):
    tw, th = int(1920 * scale), int(1080 * scale)
    pad, head = 24, 80
    rows_ = (len(images) + cols - 1) // cols
    board = Image.new("RGB", (cols * tw + (cols + 1) * pad, head + rows_ * (th + pad) + pad), (24, 22, 22))
    d = ImageDraw.Draw(board)
    d.text((pad, 22), title, font=font(34), fill=WHITE)
    for k, im in enumerate(images):
        x = pad + (k % cols) * (tw + pad)
        y = head + (k // cols) * (th + pad)
        board.paste(im.convert("RGB").resize((tw, th), Image.LANCZOS), (x, y))
    return board


def stills():
    out = HERE
    size = (1920, 1080)
    intro = Intro(size)
    by_id = {sc["Id"]: i for i, sc in enumerate(intro.list)}
    picks = [("intro_throne", 7000, "#3 혼잣말(흰색)과 남의 말(옅은 금색)"), ("intro_kings_voice", 9000, "#4-2 남의 말 둘 + 혼잣말"),
             ("intro_market", 9000, "#8 리넷"), ("intro_lobby", 3500, "#13 로비: 일식까지 99주 + 안내 줄")]
    frames = []
    for key, t, cap in picks:
        i = by_id[key]
        frame = scene_frame(intro.plates[i], t, "ko-KR", False)
        frame.convert("RGB").save(out / f"mock-intro-{key.split('_', 1)[1]}.png")
        frames.append(label(frame.copy(), cap))
    en = Intro(size, "en-US")
    i = {sc["Id"]: k for k, sc in enumerate(en.list)}["intro_kings_voice"]
    en_frame = scene_frame(en.plates[i], 9000, "en-US", False)
    en_frame.convert("RGB").save(out / "mock-intro-kings-voice-en.png")
    frames.append(label(en_frame.copy(), "#4-2 영어"))
    first = intro.frame(1500)
    frames.append(label(first.copy(), "시작 1.5초: 건너뛰기 단추와 안내(4초 뒤 사라짐)"))
    sheet(frames, 2, "Round 101 · 인트로 프레임 (02판 콘티, 데이터 초안 StorySceneData.csv)").save(out / "mock-intro-sheet.png", quality=92)

    base = lobby_shot()
    pics = portraits()
    lines = dialogue_lines("first_lobby")
    a_frames = []
    for k, line in enumerate(lines):
        img = dialogue_frame(base, line, k, len(lines), pics[line["Expression"]])
        img.convert("RGB").save(out / f"mock-dialogue-a-{k + 1}.png")
        a_frames.append(label(img.copy(), f"안 A · 첫 로비 {k + 1}/3 · 표정 {line['Expression']}"))
    ab = dialogue_lines("abyss_unlocked")
    img = dialogue_frame(base, ab[2], 2, len(ab), pics[ab[2]["Expression"]])
    img.convert("RGB").save(out / "mock-dialogue-a-abyss.png")
    a_frames.append(label(img.copy(), "안 A · 심연이 열린 뒤 3/3 · 표정 smile"))
    b = plan_b(base)
    b.convert("RGB").save(out / "mock-dialogue-b.png")
    c = plan_c()
    c.convert("RGB").save(out / "mock-dialogue-c.png")
    sheet(a_frames + [label(b.copy(), "안 B · 마을의 리넷(성문 옆, 마우스를 올림)"), label(c.copy(), "안 C · 원정 화면의 장부 줄")], 2,
          "Round 101 · 리넷의 대화 (데이터 초안 Dialogue*.csv, NpcData.csv)").save(out / "mock-dialogue-sheet.png", quality=92)
    t = title_replay()
    t.convert("RGB").save(out / "mock-title-replay.png")
    print("stills written")


def encode(path, size, frames_iter, fps=30):
    proc = subprocess.Popen(["ffmpeg", "-y", "-loglevel", "error", "-f", "rawvideo", "-pix_fmt", "rgb24", "-s", f"{size[0]}x{size[1]}",
                             "-r", str(fps), "-i", "-", "-c:v", "libx264", "-pix_fmt", "yuv420p", "-crf", "20", str(path)],
                            stdin=subprocess.PIPE)
    for f in frames_iter:
        proc.stdin.write(f.convert("RGB").tobytes())
    proc.stdin.close()
    proc.wait()


def video():
    size = (1280, 720)
    fps = 30
    lobby = lobby_shot().resize(size, Image.LANCZOS)

    def intro_frames(short, speed=1.0, only=None):
        intro = Intro(size, short=short)
        if only:
            first = intro.starts[[sc["Id"] for sc in intro.list].index(only[0])]
            last_i = [sc["Id"] for sc in intro.list].index(only[1])
            end = intro.starts[last_i] + duration(intro.list[last_i], short)
        else:
            first, end = 0, intro.total
        t = first
        while t < end:
            yield intro.frame(t)
            t += 1000 / fps * speed
        if not only:
            for _ in range(fps * 2):
                yield lobby

    encode(HERE / "intro-main.mp4", size, intro_frames(False))
    encode(HERE / "intro-short.mp4", size, intro_frames(True))

    def slow():
        for _ in range(2):
            yield from intro_frames(False, speed=0.5, only=("intro_throne", "intro_kings_voice"))
    encode(HERE / "intro-slow-throne-to-voice.mp4", size, slow())

    big = lobby_shot()
    pics = portraits()

    def dialogue():
        for name in ("first_lobby", "abyss_unlocked"):
            lines = dialogue_lines(name)
            for _ in range(int(fps * 0.6)):
                yield big.resize(size, Image.BILINEAR)
            for k, line in enumerate(lines):
                hold = int(fps * 3.2)
                for f in range(hold):
                    appear = min(1.0, f / (fps * 0.4)) if k == 0 else 1.0
                    text_alpha = min(1.0, f / (fps * 0.25))
                    pic = pics[line["Expression"]]
                    if k > 0 and lines[k - 1]["Expression"] != line["Expression"] and f < fps * 0.2:
                        pic = Image.blend(pics[lines[k - 1]["Expression"]], pic, f / (fps * 0.2))
                    yield dialogue_frame(big, line, k, len(lines), pic, appear=appear, text_alpha=text_alpha).resize(size, Image.BILINEAR)
            last = lines[-1]
            for f in range(int(fps * 0.4)):
                yield dialogue_frame(big, last, len(lines) - 1, len(lines), pics[last["Expression"]], appear=1 - f / (fps * 0.4)).resize(size, Image.BILINEAR)
    encode(HERE / "dialogue-a.mp4", size, dialogue())
    print("videos written")


if __name__ == "__main__":
    what = sys.argv[1] if len(sys.argv) > 1 else "stills"
    if what in ("stills", "all"):
        stills()
    if what in ("video", "all"):
        video()
