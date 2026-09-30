#!/usr/bin/env python3
"""Reference-driven smoke test for the art pipeline.

Generates one slot symbol with the master style reference attached, so that a broken
key, a broken reference file or a prompt that drifts off style shows up here rather
than in the middle of a real batch.

The style rules are read from ArtPipeline/STYLE_RUNTIME.md at run time rather than
copied into this file. Editing that document changes what this sends; a rule that
lives in two places is a rule that will disagree with itself.

Exactly one API call is made per run.
"""

import argparse
import base64
import getpass
import re
import struct
import subprocess
import sys
from io import BytesIO
from pathlib import Path

from openai import OpenAI
from PIL import Image

# tools/ -> ArtPipeline/. Everything is resolved from the file, not the working
# directory, so the script can be run from anywhere.
ROOT = Path(__file__).resolve().parents[1]
STYLE_RUNTIME = ROOT / "STYLE_RUNTIME.md"

# One sheet per type, each showing that type's drawing style with nothing to copy but the
# style: the sheets carry no cabinet, no reel grid and no number display. A run attaches its
# own type's sheet and no other. An approved piece of this project's own art may be passed
# with --reference for a close variant.
ITEM_REFERENCE = ROOT / "References" / "Symbols" / "item_symbol_master.jpg"
CHARACTER_REFERENCE = ROOT / "References" / "Symbols" / "character_symbol_master.jpg"
MONSTER_REFERENCE = ROOT / "References" / "Monsters" / "monster_icon_master.png"
UI_REFERENCE = ROOT / "References" / "UI" / "panel_master.PNG"

OUTPUT_DIR = ROOT / "output"

KEYCHAIN_SERVICE = "OPENAI_API_KEY"

MODEL = "gpt-image-2"
SIZE = "1024x1024"

# STYLE_RUNTIME.md Quality Policy: every type is "medium" since the symbols took on the
# monster icon's drawing style (2026-09-15).
DEFAULT_QUALITY = "medium"

CANVAS = 1024

# How much of the canvas the subject's long edge may occupy after post-processing.
# STYLE_RUNTIME asks for 70-80% with a 10% margin; 0.78 sits inside that and leaves
# 11% of clear canvas on the long axis.
TARGET_LONG_EDGE = 0.78

# An icon is not a slot symbol: nothing places it in a reel cell, so it keeps only the margin
# a sprite needs to avoid clipping rather than the 10% a symbol leaves for its window.
ICON_LONG_EDGE = 0.88

# Alpha at or below this is treated as empty when measuring the subject.
ALPHA_FLOOR = 8

# 체리 한 쌍 슬롯 심볼. Kept short and noun-led per the guide's SUBJECT rules;
# everything else about how it looks comes from the style document.
DEFAULT_SUBJECT = "a pair of bright red cherries on one green stem"
DEFAULT_NAME = "test_cherry_pair"

# STYLE_RUNTIME.md names four generation types. Each one picks the section of that
# document that describes its composition, the reference sheet it may borrow style
# from, the quality its policy allows, and how the subject sentence ends.
#
# The three fits are not interchangeable. A slot symbol is re-framed to leave the margin
# its reel cell needs; a monster icon goes in no cell and keeps a wider allowance; a UI
# panel is only trimmed, because a nine-slice needs its own edges, not a margin.
DEFAULT_TYPE = "item_symbol"
TYPES = {
    "item_symbol": {
        "section": 6,
        "reference": MONSTER_REFERENCE,
        "quality": "medium",
        "tail": "Draw it as a single slot machine symbol.",
        "fit": "symbol",
    },
    "character_symbol": {
        "section": 7,
        "reference": MONSTER_REFERENCE,
        "quality": "medium",
        "tail": "Draw it as a single character symbol showing the head only.",
        "fit": "symbol",
    },
    "monster_icon": {
        "section": 11,
        "reference": MONSTER_REFERENCE,
        "quality": "medium",
        "tail": "Draw it as a single monster icon.",
        "fit": "icon",
    },
    "ui": {
        "section": 10,
        "reference": UI_REFERENCE,
        "quality": "medium",
        "tail": "Draw it as a single flat UI panel.",
        "fit": "trim",
    },
}

# The reference teaches how to draw, never what to draw.
REFERENCE_RULE = (
    "Use the attached image only as a style reference for line weight, shape "
    "simplification, color range, shading amount and rendering. It is a sheet of separate "
    "symbols, not a scene: do not copy any symbol shown in it, do not reproduce its grid "
    "or its white background, and do not draw a slot machine, a reel grid, a number "
    "display or a face. Draw the described subject as a new, original, standalone symbol "
    "on a transparent background."
)

# Used with --copy-reference, when the attached image is the symbol itself rather than
# a style sample. Only ever point this at art this project owns.
MATCH_REFERENCE_RULE = (
    "The attached image is the exact reference for this symbol. Reproduce its object, "
    "shapes, proportions, colors and expression as closely as you can, redrawn cleanly "
    "at high resolution in the same style. Do not redesign it and do not add new "
    "elements. Do not draw a slot machine, a grid or any background."
)

PNG_SIGNATURE = b"\x89PNG\r\n\x1a\n"


class PipelineError(Exception):
    """Something the operator can act on. Never carries the key."""


# --------------------------------------------------------------------------- key


def read_api_key() -> str:
    """Reads the key straight out of the login keychain.

    Deliberately not os.environ: an exported key leaks into every child process,
    shell history and crash dump. The value returned here is passed to the client and
    nowhere else - it is never printed, logged or put in an exception.
    """
    account = getpass.getuser()

    try:
        completed = subprocess.run(
            ["security", "find-generic-password", "-s", KEYCHAIN_SERVICE, "-a", account, "-w"],
            capture_output=True,
            text=True,
            check=False,
        )
    except FileNotFoundError as error:
        raise PipelineError("/usr/bin/security 를 찾지 못했다. macOS가 아닌 환경으로 보인다.") from error

    if completed.returncode != 0:
        # security writes its own reason to stderr; the password never reaches it.
        reason = completed.stderr.strip() or f"security 가 종료 코드 {completed.returncode} 로 끝났다."
        raise PipelineError(
            f"Keychain에서 키를 읽지 못했다.\n"
            f"  서비스: {KEYCHAIN_SERVICE}\n"
            f"  계정:   {account}\n"
            f"  원인:   {reason}\n"
            f"  등록:   security add-generic-password -s {KEYCHAIN_SERVICE} -a {account} -w"
        )

    key = completed.stdout.strip()
    if not key:
        raise PipelineError(
            f"Keychain 항목 '{KEYCHAIN_SERVICE}' / '{account}' 은 있지만 값이 비어 있다."
        )

    return key


# ------------------------------------------------------------------------ inputs


def require_file(path: Path, what: str) -> None:
    """Fails before anything is spent, naming the file that is missing."""
    if not path.exists():
        raise PipelineError(f"{what} 가 없다.\n  기대 경로: {path}")
    if not path.is_file():
        raise PipelineError(f"{what} 가 파일이 아니다.\n  경로: {path}")
    if path.stat().st_size == 0:
        raise PipelineError(f"{what} 가 비어 있다.\n  경로: {path}")


def style_sections(text: str) -> dict:
    """Splits the runtime document on its numbered '## N. Title' headings."""
    sections, current, buffer = {}, None, []

    for line in text.split("\n"):
        heading = re.match(r"^##\s+(\d+)\.\s", line)
        if heading:
            if current is not None:
                sections[current] = "\n".join(buffer)
            current, buffer = int(heading.group(1)), []
        elif current is not None:
            buffer.append(line)

    if current is not None:
        sections[current] = "\n".join(buffer)

    return sections


def fenced_block(body: str) -> str:
    """First ```text fence in a section - the blocks meant to be pasted verbatim."""
    match = re.search(r"```text\n(.*?)\n```", body, re.S)
    return match.group(1).strip() if match else ""


def bullet_rules(body: str) -> str:
    """The '- ' rules of a section, joined into one sentence run."""
    rules = [line[2:].strip() for line in body.split("\n") if line.startswith("- ")]
    return " ".join(rules)


def build_prompt(subject: str, extra: str = "", reference_rule: str = REFERENCE_RULE,
                 kind: str = DEFAULT_TYPE) -> str:
    """Assembles the prompt in the order the style document prescribes."""
    sections = style_sections(STYLE_RUNTIME.read_text(encoding="utf-8"))
    spec = TYPES[kind]

    quick_rule = fenced_block(sections.get(9, ""))
    composition = bullet_rules(sections.get(spec["section"], ""))
    forbidden = fenced_block(sections.get(8, ""))

    missing = [
        name
        for name, value in (
            ("9. Quick Shared Style Rule (```text 블록)", quick_rule),
            (f"{spec['section']}. {kind} composition (- 규칙)", composition),
            ("8. Forbidden Elements (```text 블록)", forbidden),
        )
        if not value
    ]
    if missing:
        raise PipelineError(
            "STYLE_RUNTIME.md 에서 필요한 섹션을 읽지 못했다.\n"
            + "".join(f"  누락: {name}\n" for name in missing)
            + f"  파일: {STYLE_RUNTIME}"
        )

    return "\n\n".join(
        [
            quick_rule,
            f"Subject: {subject}. {extra + ' ' if extra else ''}{spec['tail']}",
            f"Composition: {composition}",
            forbidden,
            reference_rule,
        ]
    )


def reference_upload(path: Path):
    """Packs the reference for upload, labelled by what its bytes actually are.

    The extension is not trusted. A reference named .png has been seen holding JPEG
    data, and the API validates uploads by content, so a wrong content type is
    rejected with an error that points nowhere near the real cause.
    """
    data = path.read_bytes()

    if data.startswith(PNG_SIGNATURE):
        extension, mime = "png", "image/png"
    elif data.startswith(b"\xff\xd8\xff"):
        extension, mime = "jpg", "image/jpeg"
    elif data[:4] == b"RIFF" and data[8:12] == b"WEBP":
        extension, mime = "webp", "image/webp"
    else:
        raise PipelineError(
            f"레퍼런스 이미지의 포맷을 알 수 없다 (PNG/JPEG/WEBP 아님).\n"
            f"  경로: {path}\n"
            f"  앞 8바이트: {data[:8].hex(' ')}"
        )

    return (f"{path.stem}.{extension}", data, mime), extension, len(data)


# ---------------------------------------------------------------- post-processing


def subject_box(image: Image.Image):
    """Bounding box of the drawn subject, measured on alpha alone.

    Image.getbbox() is not usable here: it treats any non-zero channel as content, and
    generated symbols carry near-black RGB under fully transparent pixels, so it
    reports the whole canvas every time.
    """
    mask = image.getchannel("A").point(lambda value: 255 if value > ALPHA_FLOOR else 0)
    return mask.getbbox()


def fit_subject_to_canvas(png_bytes: bytes, target: float = TARGET_LONG_EDGE):
    """Shrinks an oversized subject and re-centres it on a transparent canvas.

    The model does not obey "70-80% of the canvas": the first run came back at 94%
    with the leaf 28px from the top edge. Rather than spend another call arguing with
    it, the framing rule is enforced here, where it is arithmetic.

    Only ever scales down. Upscaling a subject that is already small would invent
    detail and soften the outline, and a small symbol is a style note, not a defect.
    """
    image = Image.open(BytesIO(png_bytes)).convert("RGBA")
    box = subject_box(image)
    if box is None:
        raise PipelineError("이미지가 전부 투명하다. 피사체를 찾지 못했다.")

    subject = image.crop(box)
    before = max(subject.width, subject.height) / CANVAS
    limit = int(CANVAS * target)
    long_edge = max(subject.width, subject.height)

    scaled = False
    if long_edge > limit:
        ratio = limit / long_edge
        # LANCZOS on straight (non premultiplied) alpha can darken the outermost edge
        # pixel, because fully transparent pixels carry near-black RGB. At this scale
        # that lands inside the 2px anti-aliased edge the style rules already allow.
        subject = subject.resize(
            (max(1, round(subject.width * ratio)), max(1, round(subject.height * ratio))),
            Image.LANCZOS,
        )
        scaled = True

    canvas = Image.new("RGBA", (CANVAS, CANVAS), (0, 0, 0, 0))
    canvas.paste(subject, ((CANVAS - subject.width) // 2, (CANVAS - subject.height) // 2))

    buffer = BytesIO()
    canvas.save(buffer, format="PNG")

    report = {
        "scaled": scaled,
        "before_long_edge": before,
        "after_long_edge": max(subject.width, subject.height) / CANVAS,
        "margin": (CANVAS - max(subject.width, subject.height)) // 2,
    }
    return buffer.getvalue(), report


def trim_to_subject(png_bytes: bytes):
    """Crops a UI panel to its own drawn edge.

    A nine-slice stretches the middle of the sprite and keeps its corners, so a panel
    must end where its ink ends. The canvas margin a symbol needs would be stretched
    into the button instead.
    """
    image = Image.open(BytesIO(png_bytes)).convert("RGBA")
    box = subject_box(image)
    if box is None:
        raise PipelineError("이미지가 전부 투명하다. 피사체를 찾지 못했다.")

    panel = image.crop(box)
    buffer = BytesIO()
    panel.save(buffer, format="PNG")
    report = {
        "scaled": False,
        "before_long_edge": max(panel.width, panel.height) / CANVAS,
        "after_long_edge": max(panel.width, panel.height) / CANVAS,
        "margin": 0,
        "size": panel.size,
    }
    return buffer.getvalue(), report


# ------------------------------------------------------------------------ output


def verify_output(path: Path, framing: str = "70~80%", margin: float = 0.10) -> None:
    """Confirms the file landed and measures it against the framing rules."""
    if not path.exists():
        raise PipelineError(f"저장했다고 했으나 파일이 없다: {path}")

    size = path.stat().st_size
    if size == 0:
        raise PipelineError(f"저장된 파일이 0바이트다: {path}")

    print(f"      경로: {path}")
    print(f"      크기: {size:,} bytes")

    data = path.read_bytes()
    if not data.startswith(PNG_SIGNATURE):
        print("      경고: PNG 시그니처가 아니다. 응답 포맷을 확인해라.", file=sys.stderr)
        return

    width, height = struct.unpack(">II", data[16:24])
    colour_type = data[25]
    names = {0: "Grayscale", 2: "RGB", 3: "Palette", 4: "Gray+Alpha", 6: "RGBA"}
    print(f"      포맷: PNG {width}x{height}, colortype {colour_type} "
          f"({names.get(colour_type, '?')}), 알파 {'있음' if colour_type in (4, 6) else '없음'}")

    image = Image.open(path).convert("RGBA")
    box = subject_box(image)
    if box is None:
        print("      경고: 피사체가 없다 (전부 투명).", file=sys.stderr)
        return

    left, top, right, bottom = box
    bw, bh = right - left, bottom - top
    margins = (left, width - right, top, height - bottom)
    centre_x = abs((left + right) / 2 - width / 2) / width * 100
    centre_y = abs((top + bottom) / 2 - height / 2) / height * 100

    print(f"      피사체: {bw}x{bh}, 긴변 {100 * max(bw, bh) / width:.0f}% (규칙 {framing})")
    print(f"      여백:   좌 {margins[0]} 우 {margins[1]} 상 {margins[2]} 하 {margins[3]} "
          f"px (규칙 {int(width * margin)}px 이상)")
    print(f"      중심:   x {centre_x:.1f}% y {centre_y:.1f}% 이탈 (규칙 3% 이내)")

    alpha = list(image.getchannel("A").getdata())
    total = len(alpha)
    clear = sum(1 for value in alpha if value <= ALPHA_FLOOR)
    semi = sum(1 for value in alpha if ALPHA_FLOOR < value < 248)
    print(f"      알파:   완전투명 {100 * clear / total:.1f}% "
          f"반투명 {100 * semi / total:.1f}% (반투명 8% 미만이어야 글로우가 없다)")

    # --- flatness diagnostics -------------------------------------------------
    #
    # Unique RGB count is reported, never judged. Anti-aliasing along every outline
    # and the alpha edge alone pushes a perfectly flat drawing into the tens of
    # thousands of distinct values, so "30,851 unique colours" says nothing about
    # whether the fills are flat. Using it as a pass/fail gate produced a false
    # failure on the first generated symbol.
    #
    # TODO: replace this with a real flatness measure before it gates anything.
    #   1. Drop pixels with alpha < 250 so anti-aliased edges are excluded.
    #   2. Quantise the remainder (median cut, or 5 bits per channel) into a small
    #      palette and take the dominant clusters.
    #   3. Flat art is a handful of clusters covering most of the opaque area; a
    #      gradient spreads coverage thinly across many neighbouring clusters.
    #      Something like "top 8 clusters cover >= 85% of opaque pixels" is the
    #      shape of the test, with the threshold calibrated on art that has been
    #      accepted by eye first.
    opaque = [px[:3] for px in image.getdata() if px[3] >= 250]
    unique = len(set(opaque))
    print(f"      진단:   불투명 픽셀 고유 RGB {unique:,}개 "
          f"(참고용 - 안티앨리어싱 때문에 flat 판정 근거로 쓰지 않는다)")


# -------------------------------------------------------------------------- call


def generate_png(api_key: str, prompt: str, upload, quality: str) -> bytes:
    """Makes the single API call of this run and returns the decoded PNG bytes."""
    client = OpenAI(api_key=api_key)

    try:
        # output_format is left at its default. A transparent background already
        # implies PNG, and the bytes are checked afterwards, so there is no reason to
        # risk the one call of this run on an extra parameter.
        response = client.images.edit(
            model=MODEL,
            image=[upload],
            prompt=prompt,
            size=SIZE,
            quality=quality,
            background="transparent",
        )
    except Exception as error:
        # The SDK message says which of model, parameters, upload or auth was
        # rejected, and it does not echo the key back.
        raise PipelineError(
            f"이미지 생성 요청이 실패했다.\n"
            f"  model={MODEL} size={SIZE} quality={quality} background=transparent\n"
            f"  {type(error).__name__}: {error}"
        ) from error

    if not response.data:
        raise PipelineError("응답에 이미지가 하나도 없다.")

    encoded = response.data[0].b64_json
    if not encoded:
        raise PipelineError("응답에 b64_json 이 없다. 이 모델이 URL로 돌려줬는지 확인이 필요하다.")

    try:
        return base64.b64decode(encoded, validate=True)
    except (ValueError, TypeError) as error:
        raise PipelineError(f"base64 디코딩에 실패했다: {error}") from error


def parse_args():
    parser = argparse.ArgumentParser(
        description="Generate one slot symbol from the master style reference.")
    parser.add_argument("--subject", default=DEFAULT_SUBJECT,
                        help="English SUBJECT phrase, noun led, 3-12 words.")
    parser.add_argument("--name", default=DEFAULT_NAME,
                        help="Output file stem under ArtPipeline/output/.")
    parser.add_argument("--type", dest="kind", default=DEFAULT_TYPE, choices=sorted(TYPES),
                        help="Generation type: picks the STYLE_RUNTIME section, the reference "
                             "sheet and the quality its policy allows.")
    parser.add_argument("--reference", default="",
                        help="Reference image to attach. Defaults to the symbol sheet; "
                             "never the cabinet shot for a symbol.")
    parser.add_argument("--copy-reference", action="store_true",
                        help="Reproduce the reference closely instead of borrowing only its style.")
    parser.add_argument("--extra", default="",
                        help="Constraint appended to the subject sentence, e.g. a no-face rule.")
    parser.add_argument("--quality", default="",
                        choices=["", "low", "medium", "high"],
                        help="Quality Policy: slot symbols are low; approved low output is final.")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    output_path = OUTPUT_DIR / f"{args.name}.png"
    spec = TYPES[args.kind]
    reference = Path(args.reference) if args.reference else spec["reference"]
    reference_rule = MATCH_REFERENCE_RULE if args.copy_reference else REFERENCE_RULE
    quality = args.quality or spec["quality"]

    try:
        # Everything that can fail for free fails before the key is touched or the
        # single API call is spent.
        require_file(STYLE_RUNTIME, "STYLE_RUNTIME.md")
        require_file(reference, f"레퍼런스 이미지 {reference.name}")

        upload, extension, reference_size = reference_upload(reference)
        print(f"[1/6] 레퍼런스 확인: {reference.name} "
              f"{'(복제 모드)' if args.copy_reference else ''} "
              f"({reference_size:,} bytes, 실제 포맷 {extension.upper()})")

        prompt = build_prompt(args.subject, args.extra, reference_rule, args.kind)
        print(f"[2/6] STYLE_RUNTIME.md 로 프롬프트 조립 ({len(prompt):,} chars).")

        api_key = read_api_key()
        print(f"[3/6] Keychain에서 키를 읽었다 (서비스 {KEYCHAIN_SERVICE}, 계정 {getpass.getuser()}).")

        print(f"[4/6] 생성 요청 1회: type={args.kind}, subject=\"{args.subject}\", quality={quality}.")
        png = generate_png(api_key, prompt, upload, quality)

        if spec["fit"] == "trim":
            png, report = trim_to_subject(png)
        else:
            png, report = fit_subject_to_canvas(
                png, ICON_LONG_EDGE if spec["fit"] == "icon" else TARGET_LONG_EDGE)

        if spec["fit"] == "trim":
            print(f"[5/6] 후처리: 잉크 경계로 잘라 {report['size'][0]}x{report['size'][1]} 패널로 남겼다.")
        elif report["scaled"]:
            print(f"[5/6] 후처리: 피사체 {report['before_long_edge']:.0%} -> "
                  f"{report['after_long_edge']:.0%}, 여백 {report['margin']}px 확보 후 중앙 재배치.")
        else:
            print(f"[5/6] 후처리: 축소 불필요 ({report['before_long_edge']:.0%}), 중앙 재배치만 수행.")

        output_path.parent.mkdir(parents=True, exist_ok=True)
        output_path.write_bytes(png)
        print("[6/6] 저장 완료.")
        # The framing a type is actually held to, so the report does not quote the slot
        # symbol's rule at an icon that was never bound by it.
        if spec["fit"] == "icon":
            verify_output(output_path, framing="75~90%", margin=0.05)
        else:
            verify_output(output_path)
    except PipelineError as error:
        print(f"\n실패: {error}", file=sys.stderr)
        return 1
    except KeyboardInterrupt:
        print("\n중단했다.", file=sys.stderr)
        return 130

    return 0


if __name__ == "__main__":
    sys.exit(main())
