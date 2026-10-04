#!/usr/bin/env python3
"""Generates one piece of art per run from the style document, a roster row and the style reference.

The style rules are read from ArtPipeline/STYLE_RUNTIME.md at run time rather than copied
into this file. Editing that document changes what this sends; a rule that lives in two
places is a rule that will disagree with itself.

Exactly one API call is made per run, and everything that can fail for free fails before
the key is read. --dry-run stops before the key; --refit redoes the local post-processing
of an earlier run without any call.
"""

import argparse
import base64
import csv
import datetime
import getpass
import math
import re
import subprocess
import sys
from io import BytesIO
from pathlib import Path

from openai import OpenAI
from PIL import Image, ImageChops, ImageFilter, ImageOps

# tools/ -> ArtPipeline/. Everything is resolved from the file, not the working
# directory, so the script can be run from anywhere.
ROOT = Path(__file__).resolve().parents[1]
STYLE_RUNTIME = ROOT / "STYLE_RUNTIME.md"
ROSTERS = ROOT / "Rosters"
OUTPUT_DIR = ROOT / "output"

# The game's own data. A roster key must be an Id there: the art is wired to the data by it.
GAME_DATA = ROOT.parent / "Assets" / "@Data" / "Source"

# Every paid call is appended here, so the spend of a stage can be added up later.
CALL_LEDGER = ROOT / "Archive" / "calls.csv"
LEDGER_HEADER = ["Time", "Type", "Key", "Name", "Model", "Quality", "Size",
                 "TextInTokens", "ImageInTokens", "OutputTokens", "EstimatedUsd"]

KEYCHAIN_SERVICE = "OPENAI_API_KEY"

# The model must support a transparent background: gpt-image-2 rejects it (HTTP 400).
MODEL = "gpt-image-2.5-sunburst"
SIZE = "1024x1024"
RAW_CANVAS = 1024
REQUEST_TIMEOUT_SECONDS = 240

# The scene fit: every background shares one canvas and one floor line, so a screen can put any
# of them in the same place without per-image offsets. The floor line is where the units' feet
# stand, as a share of the canvas height from its top; the battle screen puts it on the floor of
# its field (UiPrefabSetup.Battle has the same number). The canvas is taller than the 16:9
# screen, so the floor of the UI can move without the scene running out above or below.
SCENE_CANVAS = (2304, 1536)
SCENE_FLOOR = 57
FLOOR_RANGE = (30, 80)

# The UI fit: a piece of the user interface is cut out, scaled to the size its roster row names
# and given one even dark outline, so that every piece carries the same weight of line whatever
# line the model drew. A frame is stretched to its size (it is stretched again on screen, as a
# nine-slice); a glyph keeps its proportions. The sizes a frame may be generated at: the one
# closest in proportion to the row's Size is used.
UI_GENERATION_SIZES = [(1024, 1024), (1536, 1024), (1536, 768), (1536, 512)]
UI_SIZE_RANGE = (16, 1024)
UI_OUTLINE_RANGE = (0, 24)
UI_GLYPH_FILL = 0.84
# The tone of the outline: the dark brown the figures are outlined in, so that the interface and
# the units carry the same line.
UI_LINE = (24, 9, 7)
# A pixel belongs to the fill of a flat frame when its color points the same way as the fill's
# (the cosine of the angle between them) and it is not as dark as the outline.
UI_FILL_LIKENESS = 0.985
UI_FILL_MIN_LENGTH = 48
# The tones of a fill are the peaks of its pixels counted by brightness against the fill, in bins
# this wide. A peak holds at least this share of the pixels and is the highest within this many bins.
UI_TONE_BIN = 0.02
UI_TONE_SHARE = 0.01
UI_TONE_REACH = 4
# The outline is built at this multiple of the final size and scaled down, which smooths its edge.
UI_WORK_SCALE = 3
# Alpha below this is not part of a piece: a faint shadow or glow around it is dropped.
UI_ALPHA_CUT = 128

# The sections of STYLE_RUNTIME.md a cut-out (a figure or an icon on a transparent background)
# starts and closes its prompt with. A type that draws something else names its own.
SHARED_SECTION = 1
FORBIDDEN_SECTION = 2

# One entry per generation type: the sections that describe its composition and how it may
# use the reference, the style references it may borrow from, the data file its roster keys
# are Ids of, its quality and how the result is framed afterwards.
# A type is added when its first piece is drawn.
TYPES = {
    "character": {
        "section": 4,
        "reference_section": 3,
        # The style reference is a sheet of our own confirmed figures (2026-10-04), which face
        # right as a party figure does, so it is attached as it is.
        "references": [ROOT / "References" / "Character" / "style_ref_roster.png"],
        "mirror_references": False,
        "data": "JobData.csv",
        "dungeon_theme": None,
        "quality": "medium",
        "tail": "Draw this one character, full body.",
        "fit": "figure",
    },
    "enemy": {
        "section": 5,
        "reference_section": 6,
        # An enemy faces left, so the roster sheet (facing right) is attached mirrored. The model
        # takes the facing from the reference rather than from the prompt.
        "references": [ROOT / "References" / "Character" / "style_ref_roster.png"],
        "mirror_references": True,
        "data": "EnemyData.csv",
        # An enemy belongs to a dungeon, and what the dungeon says of its creatures is added to its prompt.
        "dungeon_theme": "Creatures",
        "dungeon_required": True,
        "quality": "medium",
        "tail": "Draw this one creature, full body.",
        "fit": "figure",
    },
    "item": {
        "section": 7,
        "reference_section": 8,
        # A roster row may name a unit's figure as its reference: the item is the one that unit
        # holds, and the icon is drawn after it. That prompt ends with this section instead.
        "held_reference_section": 9,
        "references": [ROOT / "References" / "Character" / "style_ref_roster.png"],
        "mirror_references": False,
        "data": "ItemData.csv",
        "dungeon_theme": None,
        "quality": "medium",
        "tail": "Draw this one item as an icon.",
        # An item is drawn for the cells it takes on a board: the data says how many.
        "fit": "cell",
    },
    "background": {
        # A background is an opaque scene: it has its own style rule and its own list of what to avoid.
        "shared_section": 10,
        "section": 11,
        "forbidden_section": 12,
        "reference_section": 13,
        "references": [ROOT / "References" / "Character" / "style_ref_roster.png"],
        "mirror_references": False,
        # A background is not named by one data file: a dungeon's is, the title's is not.
        "data": None,
        # The background of a dungeon takes what the dungeon says of the place.
        "dungeon_theme": "Place",
        "quality": "medium",
        "size": f"{SCENE_CANVAS[0]}x{SCENE_CANVAS[1]}",
        "background": "opaque",
        "tail": "Draw this place as a background.",
        "fit": "scene",
    },
    "ui_frame": {
        # A piece of the user interface is neither a figure nor a scene: its own style rule and list of what to avoid.
        "shared_section": 14,
        "section": 15,
        "forbidden_section": 17,
        "reference_section": 18,
        "references": [ROOT / "References" / "Character" / "style_ref_roster.png"],
        "mirror_references": False,
        # A frame is named by its use on the screens, not by a data id.
        "data": None,
        "dungeon_theme": None,
        "quality": "medium",
        "tail": "Draw this one frame.",
        "fit": "frame",
    },
    "ui_icon": {
        "shared_section": 14,
        "section": 16,
        "forbidden_section": 17,
        "reference_section": 18,
        "references": [ROOT / "References" / "Character" / "style_ref_roster.png"],
        "mirror_references": False,
        "data": None,
        "dungeon_theme": None,
        "quality": "medium",
        "tail": "Draw this one symbol as an icon.",
        "fit": "glyph",
    },
    "ui_piece": {
        "shared_section": 14,
        "section": 22,
        "forbidden_section": 17,
        "reference_section": 18,
        "references": [ROOT / "References" / "Character" / "style_ref_roster.png"],
        "mirror_references": False,
        # A decorative piece shown whole (the storm clock's dial), named by its use on the screens.
        "data": None,
        "dungeon_theme": None,
        "quality": "medium",
        "tail": "Draw this one piece.",
        "fit": "glyph",
    },
}

# The figure fit: every full-body figure stands on the same floor line of the same 3:4
# canvas, so a screen can show any of them in the same place without per-image offsets.
# The canvas is smaller than the generated image on purpose: fitting then only ever
# scales down, and figures of the same Height come out the same size.
FIGURE_CANVAS = (672, 896)
FIGURE_FLOOR_MARGIN = 20
FIGURE_SIDE_MARGIN = 12
HEIGHT_RANGE = (10, 97)
# The dark ring drawn around the whole silhouette of a figure after fitting, in canvas pixels
# (2026-10-04 play feedback: the outline has to be thicker so a unit stands off the background).
# It is the UI line color, so figures and interface share one tone of outline. The side margin
# above is wider than the ring, so the ring never leaves the canvas.
FIGURE_OUTLINE = 8

# The cell fit: an item's icon is drawn for the cells the item takes on a unit's board, stacked
# top to bottom in the panel column under the unit (ItemData.csv, Size; 2026-10-03 mockup V). A
# cell is 180x60 on screen with 2 between cells (BattleItemView; 2026-10-04 mockup B, before it
# 180x50 with 4), so a bigger item is a taller shape; the icon sits inside the slot's rim (8 at
# the sides, 5 above and below), at twice the size on screen. For each count of cells: the
# section of STYLE_RUNTIME.md that says how the item lies in that shape, the size it is
# generated at and the canvas it is fitted to. (The icons on hand were drawn for the 180x50
# cells, on canvases of 328x80, 328x188 and 328x296; the screen fits them by width, so they
# stand the same size with more room above and below, and they are not redrawn.)
ITEM_CELLS = {
    1: {"section": 19, "generate": "1536x512", "canvas": (328, 100)},
    2: {"section": 20, "generate": "1536x1024", "canvas": (328, 224)},
    3: {"section": 21, "generate": "1024x1024", "canvas": (328, 348)},
}
# The ring an icon is given, in the pixels of its canvas, and how much of the canvas it may fill.
ITEM_OUTLINE = 4
ITEM_FILL = 0.98

# Alpha at or below this is treated as empty when measuring the subject.
ALPHA_FLOOR = 8

# USD per million tokens for the model above (pricing page, checked 2026-10-02). Used for
# the estimate printed after a call and written to the ledger, never for a decision.
PRICE_TEXT_IN = 5.00
PRICE_IMAGE_IN = 8.00
PRICE_OUTPUT = 30.00

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
            timeout=60,
        )
    except FileNotFoundError as error:
        raise PipelineError("/usr/bin/security 를 찾지 못했다. macOS가 아닌 환경으로 보인다.") from error
    except subprocess.TimeoutExpired as error:
        raise PipelineError(
            "Keychain이 60초 안에 답하지 않았다. 화면에 접근 허용 창이 떠 있는지 확인해라."
        ) from error

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


def require_data_id(file_name: str, value: str, what: str) -> None:
    """The value must be an Id of the game's data file: a typo here would draw art nothing uses."""
    path = GAME_DATA / file_name
    require_file(path, f"Game Data {file_name}")

    with path.open(encoding="utf-8-sig", newline="") as handle:
        ids = [row.get("Id") for row in csv.DictReader(handle)]

    if value not in ids:
        raise PipelineError(
            f"{what} '{value}' 가 {file_name} 의 Id 가 아니다.\n  파일: {path}\n  있는 Id: {', '.join(i for i in ids if i)}"
        )


def read_roster_row(kind: str, key: str, roster: Path = None) -> dict:
    """The roster row of a key: what to draw, and how the type frames it or what it is drawn after.

    roster: another roster file than the type's own, for a style test (--roster).
    """
    path = roster or ROSTERS / f"{kind}.csv"
    require_file(path, f"Roster {path.name}")

    with path.open(encoding="utf-8", newline="") as handle:
        rows = list(csv.DictReader(handle))

    matches = [row for row in rows if row.get("Key") == key]
    if len(matches) != 1:
        known = ", ".join(row.get("Key", "?") for row in rows)
        raise PipelineError(
            f"Roster에서 Key '{key}' 를 {len(matches)}개 찾았다 (하나여야 한다).\n"
            f"  파일: {path}\n  있는 Key: {known}"
        )

    row = matches[0]
    subject = (row.get("Subject") or "").strip()
    if not subject:
        raise PipelineError(f"Roster의 '{key}' 에 Subject 가 없다.\n  파일: {path}")

    spec = TYPES[kind]
    if spec["data"]:
        require_data_id(spec["data"], key, "Roster 의 Key")

    # Height and Flip place a figure on its canvas. An icon has neither.
    height, flip = 0, "false"
    if spec["fit"] == "figure":
        try:
            height = int(row.get("Height") or "")
        except ValueError as error:
            raise PipelineError(f"Roster의 '{key}' 의 Height 가 정수가 아니다.\n  파일: {path}") from error
        if not HEIGHT_RANGE[0] <= height <= HEIGHT_RANGE[1]:
            raise PipelineError(
                f"Roster의 '{key}' 의 Height {height} 가 범위 {HEIGHT_RANGE[0]}~{HEIGHT_RANGE[1]} 밖이다."
            )

        flip = (row.get("Flip") or "").strip().lower()
        if flip not in ("true", "false"):
            raise PipelineError(f"Roster의 '{key}' 의 Flip 은 true 나 false 여야 한다.\n  파일: {path}")

    # FloorLine says where the units' feet stand in the drawn scene, as a share of its height.
    floor_line = 0
    if spec["fit"] == "scene":
        try:
            floor_line = int(row.get("FloorLine") or "")
        except ValueError as error:
            raise PipelineError(f"Roster의 '{key}' 의 FloorLine 이 정수가 아니다.\n  파일: {path}") from error
        if not FLOOR_RANGE[0] <= floor_line <= FLOOR_RANGE[1]:
            raise PipelineError(
                f"Roster의 '{key}' 의 FloorLine {floor_line} 이 범위 {FLOOR_RANGE[0]}~{FLOOR_RANGE[1]} 밖이다."
            )

    # An item is drawn for as many cells as the data gives it.
    cells = 0
    if spec["fit"] == "cell":
        cells = read_item_cells(key)

    # A piece of the user interface names the size it is fitted to and the outline it is given.
    ui = None
    if spec["fit"] in ("frame", "glyph"):
        ui = read_ui_columns(row, key, path)

    # A row may name its own reference, as a path from the repository root: the figure of the
    # unit that holds the item. Only a type that says how to use one takes it.
    reference = None
    named = (row.get("Reference") or "").strip()
    if named:
        if "held_reference_section" not in spec:
            raise PipelineError(f"Roster의 '{key}' 에 Reference 가 있지만 타입 {kind} 은 그것을 쓰지 않는다.\n  파일: {path}")
        reference = ROOT.parent / named
        require_file(reference, f"Roster 의 Reference {named}")

    dungeon = (row.get("Dungeon") or "").strip()
    if dungeon:
        if not spec["dungeon_theme"]:
            raise PipelineError(f"Roster의 '{key}' 에 Dungeon 이 있지만 타입 {kind} 은 던전 컨셉을 쓰지 않는다.\n  파일: {path}")
        require_data_id("DungeonData.csv", dungeon, "Roster 의 Dungeon")
    elif spec.get("dungeon_required"):
        raise PipelineError(f"Roster의 '{key}' 에 Dungeon 이 없다.\n  파일: {path}")

    return {"subject": subject, "height": height, "flip": flip == "true", "floor_line": floor_line,
            "dungeon": dungeon, "reference": reference, "ui": ui, "cells": cells}


def read_item_cells(key: str) -> int:
    """How many cells of a board the item takes: the Size of its row in the game's data."""
    path = GAME_DATA / "ItemData.csv"
    require_file(path, "Game Data ItemData.csv")
    with path.open(encoding="utf-8-sig", newline="") as handle:
        sizes = {row.get("Id"): row.get("Size") for row in csv.DictReader(handle)}

    try:
        cells = int(sizes.get(key) or "")
    except ValueError as error:
        raise PipelineError(f"ItemData.csv 의 '{key}' 의 Size 가 정수가 아니다.\n  파일: {path}") from error
    if cells not in ITEM_CELLS:
        raise PipelineError(
            f"ItemData.csv 의 '{key}' 의 Size {cells} 에 맞는 칸 모양이 없다 (있는 것: {sorted(ITEM_CELLS)})."
        )
    return cells


def read_ui_columns(row: dict, key: str, path: Path) -> dict:
    """Size (WxH) and Outline of a user interface piece, checked before anything is spent."""
    def number(column: str, low: int, high: int) -> int:
        try:
            value = int(row.get(column) or "")
        except ValueError as error:
            raise PipelineError(f"Roster의 '{key}' 의 {column} 이 정수가 아니다.\n  파일: {path}") from error
        if not low <= value <= high:
            raise PipelineError(f"Roster의 '{key}' 의 {column} {value} 이 범위 {low}~{high} 밖이다.\n  파일: {path}")
        return value

    match = re.fullmatch(r"(\d+)x(\d+)", (row.get("Size") or "").strip())
    if not match:
        raise PipelineError(f"Roster의 '{key}' 의 Size 는 '너비x높이' 여야 한다.\n  파일: {path}")
    width, height = int(match.group(1)), int(match.group(2))

    # A frame's fill is flattened to one color unless the row keeps its drawn grain (Flat = no).
    flat = (row.get("Flat") or "yes").strip().lower()
    if flat not in ("yes", "no"):
        raise PipelineError(f"Roster의 '{key}' 의 Flat 은 yes 나 no 여야 한다.\n  파일: {path}")
    if not all(UI_SIZE_RANGE[0] <= side <= UI_SIZE_RANGE[1] for side in (width, height)):
        raise PipelineError(f"Roster의 '{key}' 의 Size {width}x{height} 가 범위 {UI_SIZE_RANGE[0]}~{UI_SIZE_RANGE[1]} 밖이다.")

    return {"size": (width, height), "outline": number("Outline", *UI_OUTLINE_RANGE), "flat": flat == "yes"}


def generation_size(width: int, height: int) -> str:
    """The size a frame is generated at: the allowed one whose proportions are closest to the fitted size."""
    def distance(candidate):
        return abs(math.log((candidate[0] / candidate[1]) / (width / height)))

    best = min(UI_GENERATION_SIZES, key=distance)
    return f"{best[0]}x{best[1]}"


def style_sections(text: str) -> dict:
    """Splits the runtime document on its numbered '## N. Title' headings."""
    sections, current, buffer = {}, None, []

    for line in text.split("\n"):
        heading = re.match(r"^##\s+(\d+)\.\s", line)
        if heading:
            if current is not None:
                sections[current] = "\n".join(buffer)
            current, buffer = int(heading.group(1)), []
        elif line.startswith("## "):
            # An unnumbered heading ends the numbered section before it.
            if current is not None:
                sections[current] = "\n".join(buffer)
            current, buffer = None, []
        elif current is not None:
            buffer.append(line)

    if current is not None:
        sections[current] = "\n".join(buffer)

    return sections


def dungeon_themes(text: str) -> dict:
    """The '## Dungeon: <id>' sections of the runtime document.

    A dungeon says one thing of its creatures and another of the place, each under a
    '### <Part>' heading: dungeon id -> part -> its rules in one run.
    """
    themes, dungeon, part, buffer = {}, None, None, []

    def close():
        if dungeon is not None and part is not None:
            themes.setdefault(dungeon, {})[part] = bullet_rules("\n".join(buffer))

    for line in text.split("\n"):
        heading = re.match(r"^##\s+Dungeon:\s*(\S+)\s*$", line)
        sub = re.match(r"^###\s+(\S+)\s*$", line)
        if heading or line.startswith("## "):
            close()
            dungeon, part, buffer = (heading.group(1) if heading else None), None, []
        elif sub and dungeon is not None:
            close()
            part, buffer = sub.group(1), []
        elif part is not None:
            buffer.append(line)

    close()
    return themes


def fenced_block(body: str) -> str:
    """First ```text fence in a section - the blocks meant to be pasted verbatim."""
    match = re.search(r"```text\n(.*?)\n```", body, re.S)
    return match.group(1).strip() if match else ""


def bullet_rules(body: str) -> str:
    """The '- ' rules of a section, joined into one sentence run."""
    rules = [line[2:].strip() for line in body.split("\n") if line.startswith("- ")]
    return " ".join(rules)


def build_prompt(kind: str, subject: str, extra: str = "", dungeon: str = "", held: bool = False, cells: int = 0,
                 style: Path = None) -> str:
    """Assembles the prompt in the order the style document prescribes.

    held: the attached reference is the figure of the unit that holds the subject, so the prompt
    ends with the type's rule for that instead of its rule for a plain style reference.
    cells: for an item, how many cells it takes; the rules of that shape follow the item's own.
    style: another style document than STYLE_RUNTIME.md, for a style test (--style). Same format.
    """
    style = style or STYLE_RUNTIME
    text = style.read_text(encoding="utf-8")
    sections = style_sections(text)
    spec = dict(TYPES[kind])
    if held:
        spec["reference_section"] = spec["held_reference_section"]

    shared_section = spec.get("shared_section", SHARED_SECTION)
    forbidden_section = spec.get("forbidden_section", FORBIDDEN_SECTION)
    shared = fenced_block(sections.get(shared_section, ""))
    composition = bullet_rules(sections.get(spec["section"], ""))
    shape_section = ITEM_CELLS[cells]["section"] if cells else None
    shape = bullet_rules(sections.get(shape_section, "")) if shape_section else ""
    forbidden = fenced_block(sections.get(forbidden_section, ""))
    reference_rule = fenced_block(sections.get(spec["reference_section"], ""))
    theme = dungeon_themes(text).get(dungeon, {}).get(spec["dungeon_theme"], "") if dungeon else ""

    required = [
        (f"{shared_section}. Style Rule (```text 블록)", shared),
        (f"{spec['section']}. {kind} composition (- 규칙)", composition),
        (f"{forbidden_section}. Forbidden (```text 블록)", forbidden),
        (f"{spec['reference_section']}. {kind} Reference Rule (```text 블록)", reference_rule),
    ]
    if dungeon:
        required.append((f"Dungeon: {dungeon} / {spec['dungeon_theme']} (- 규칙)", theme))
    if shape_section:
        required.append((f"{shape_section}. {kind} shape of {cells} cell(s) (- 규칙)", shape))

    missing = [name for name, value in required if not value]
    if missing:
        raise PipelineError(
            f"{style.name} 에서 필요한 섹션을 읽지 못했다.\n"
            + "".join(f"  누락: {name}\n" for name in missing)
            + f"  파일: {style}"
        )

    parts = [
        shared,
        f"Subject: {subject}. {extra + ' ' if extra else ''}{spec['tail']}",
        f"Composition: {composition}{' ' + shape if shape else ''}",
    ]
    if theme:
        parts.append(f"Dungeon: {theme}")
    parts += [forbidden, reference_rule]
    return "\n\n".join(parts)


def reference_upload(path: Path, mirror: bool = False):
    """Packs a reference for upload, labelled by what its bytes actually are.

    The extension is not trusted. A reference named .png has been seen holding JPEG
    data, and the API validates uploads by content, so a wrong content type is
    rejected with an error that points nowhere near the real cause.

    A mirrored reference is flipped left to right in memory and sent as PNG; the file
    on disk stays as it is.
    """
    data = path.read_bytes()

    if mirror:
        try:
            flipped = ImageOps.mirror(Image.open(BytesIO(data)).convert("RGB"))
        except Exception as error:
            raise PipelineError(f"레퍼런스 이미지를 열지 못했다.\n  경로: {path}\n  {error}") from error
        buffer = BytesIO()
        flipped.save(buffer, format="PNG")
        data = buffer.getvalue()
        return (f"{path.stem}_mirrored.png", data, "image/png"), "png", len(data)

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
    generated art carries near-black RGB under fully transparent pixels, so it
    reports the whole canvas every time.
    """
    mask = image.getchannel("A").point(lambda value: 255 if value > ALPHA_FLOOR else 0)
    return mask.getbbox()


def fit_figure(png_bytes: bytes, height_percent: int, flip: bool):
    """Stands a full-body figure on the floor line of the figure canvas.

    The model does not obey a framing rule, so the rule is enforced here, where it is
    arithmetic: the subject is cut out on its alpha, scaled to the roster's Height (a
    share of the canvas height) or to the canvas width, whichever is tighter, and put
    bottom-centre with its feet on the floor line.

    Only ever scales down. A subject smaller than its target is left as it is and
    reported, because scaling it up would soften the outline; the verdict decides.
    """
    image = Image.open(BytesIO(png_bytes)).convert("RGBA")
    box = subject_box(image)
    if box is None:
        raise PipelineError("이미지가 전부 투명하다. 피사체를 찾지 못했다.")

    left, top, right, bottom = box
    touches_edge = left <= 1 or top <= 1 or right >= image.width - 1 or bottom >= image.height - 1

    subject = image.crop(box)
    if flip:
        subject = ImageOps.mirror(subject)

    canvas_width, canvas_height = FIGURE_CANVAS
    target_height = round(canvas_height * height_percent / 100)
    max_width = canvas_width - 2 * FIGURE_SIDE_MARGIN
    ratio = min(target_height / subject.height, max_width / subject.width)

    raw_size = subject.size
    if ratio < 1:
        # LANCZOS on straight (non premultiplied) alpha can darken the outermost edge
        # pixel, because fully transparent pixels carry near-black RGB. At this scale
        # that lands inside the anti-aliased edge of the outline.
        subject = subject.resize(
            (max(1, round(subject.width * ratio)), max(1, round(subject.height * ratio))),
            Image.LANCZOS,
        )

    canvas = Image.new("RGBA", FIGURE_CANVAS, (0, 0, 0, 0))
    x = (canvas_width - subject.width) // 2
    y = canvas_height - FIGURE_FLOOR_MARGIN - subject.height
    canvas.paste(subject, (x, y))

    if FIGURE_OUTLINE > 0:
        # One even dark ring around the silhouette, under the figure: its outer edge is softened
        # by a pixel so that it does not alias at the size the screen shows it.
        body = canvas.getchannel("A").point(lambda value: 255 if value > ALPHA_FLOOR else 0)
        ring = Image.new("RGBA", FIGURE_CANVAS, UI_LINE + (0,))
        ring.putalpha(grow(body, FIGURE_OUTLINE).filter(ImageFilter.GaussianBlur(0.8)))
        ring.alpha_composite(canvas)
        canvas = ring

    buffer = BytesIO()
    canvas.save(buffer, format="PNG")

    report = {
        "raw_size": raw_size,
        "raw_fill": max(raw_size[0] / image.width, raw_size[1] / image.height),
        "ratio": min(ratio, 1.0),
        "size": subject.size,
        "target_height": target_height,
        "short_by": max(0, target_height - subject.height) if ratio >= 1 else 0,
        "width_bound": ratio < 1 and subject.height < target_height - 1,
        "touches_edge": touches_edge,
        "flipped": flip,
    }
    return buffer.getvalue(), report


def fit_scene(png_bytes: bytes, floor_line: int):
    """Puts a background's floor on the floor line every background shares.

    The roster says where the units' feet stand in the drawn picture. A window of the canvas's
    proportions is cut from it so that this height lands on the shared floor line, and the
    window is scaled to the canvas. A picture whose floor is already there is kept as drawn.
    """
    image = Image.open(BytesIO(png_bytes)).convert("RGB")
    width, height = image.size
    if width * SCENE_CANVAS[1] != height * SCENE_CANVAS[0]:
        raise PipelineError(
            f"배경 원본 {width}x{height} 의 비율이 캔버스 {SCENE_CANVAS[0]}x{SCENE_CANVAS[1]} 와 다르다."
        )

    # The tallest window whose floor share is the shared one: it loses a strip above or below.
    floor = height * floor_line / 100
    share = SCENE_FLOOR / 100
    window_height = min(height, floor / share, (height - floor) / (1 - share))
    window_width = window_height * width / height
    top = floor - share * window_height
    left = (width - window_width) / 2
    cut = window_height < height - 0.5
    if cut:
        image = image.crop((round(left), round(top), round(left + window_width), round(top + window_height)))
    if image.size != SCENE_CANVAS:
        image = image.resize(SCENE_CANVAS, Image.LANCZOS)

    buffer = BytesIO()
    image.save(buffer, format="PNG")
    report = {
        "scene": True,
        "size": image.size,
        "floor_line": floor_line,
        "cut": cut,
        "window": (round(window_width), round(window_height)),
    }
    return buffer.getvalue(), report


def grow(mask: Image.Image, radius: int) -> Image.Image:
    """Widens the white of a mask by a radius, with rounded corners.

    One step widens by a pixel. Steps over the four neighbours alternate with steps over all
    eight, which grows an octagon: close enough to a disc at the few pixels an outline takes.
    """
    for step in range(radius):
        if step % 2 == 0:
            grown = mask
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                grown = ImageChops.lighter(grown, ImageChops.offset(mask, dx, dy))
            mask = grown
        else:
            mask = mask.filter(ImageFilter.MaxFilter(3))
    return mask


def fill_of(image: Image.Image):
    """The color most of a flat piece is filled with: its commonest opaque color, coarsely binned."""
    counts = {}
    for red, green, blue, alpha in image.resize((48, 48), Image.BOX).getdata():
        if alpha > 200:
            key = (red // 8, green // 8, blue // 8)
            counts[key] = counts.get(key, 0) + 1
    if not counts:
        raise PipelineError("피사체에 불투명한 부분이 없다.")
    red, green, blue = max(counts, key=counts.get)
    return (red * 8 + 4, green * 8 + 4, blue * 8 + 4)


def flatten_fill(image: Image.Image, target=None) -> Image.Image:
    """Gives the body of a flat piece one flat color: its own, or another.

    Every pixel of the fill's own hue becomes the target color, which takes out the faint
    mottling a generated fill carries. A band of a clearly darker or lighter tone of the same
    hue (the lower edge of a button, the rim of a slot) is made flat too and keeps its step
    against the fill: the tones the piece really uses are found as the peaks of how bright its
    pixels are, and each pixel takes the nearest of them. The outline and the lines of another
    hue are left as they are.
    """
    fill = fill_of(image)
    target = target or fill
    norm = sum(channel * channel for channel in fill) ** 0.5
    result = image.copy()
    pixels = result.load()

    # Every pixel of the fill's hue, with how bright it is against the fill.
    steps = {}
    for y in range(result.height):
        for x in range(result.width):
            red, green, blue, alpha = pixels[x, y]
            length = (red * red + green * green + blue * blue) ** 0.5
            # The darkest pixels are the outline, whatever their hue.
            if alpha == 0 or length <= UI_FILL_MIN_LENGTH:
                continue
            likeness = (red * fill[0] + green * fill[1] + blue * fill[2]) / (length * norm)
            if likeness > UI_FILL_LIKENESS:
                steps[(x, y)] = length / norm

    tones = fill_tones(steps.values())
    for (x, y), step in steps.items():
        tone = min(tones, key=lambda candidate: abs(candidate - step))
        pixels[x, y] = tuple(min(255, round(channel * tone)) for channel in target) + (pixels[x, y][3],)
    return result


def tint_fill(image: Image.Image, target) -> Image.Image:
    """Gives the body of a piece another color and keeps its drawn grain.

    Every pixel of the fill's own hue is scaled, channel by channel, by the ratio of the target
    color to the fill color, so the two tones of a grain stay two tones in the new color. The
    outline and the lines of another hue are left as they are. For a frame whose fill was not
    flattened (Flat = no in its roster row).
    """
    fill = fill_of(image)
    norm = sum(channel * channel for channel in fill) ** 0.5
    scale = tuple(target[i] / max(1, fill[i]) for i in range(3))
    result = image.copy()
    pixels = result.load()
    for y in range(result.height):
        for x in range(result.width):
            red, green, blue, alpha = pixels[x, y]
            length = (red * red + green * green + blue * blue) ** 0.5
            if alpha == 0 or length <= UI_FILL_MIN_LENGTH:
                continue
            likeness = (red * fill[0] + green * fill[1] + blue * fill[2]) / (length * norm)
            if likeness > UI_FILL_LIKENESS:
                pixels[x, y] = (min(255, round(red * scale[0])), min(255, round(green * scale[1])), min(255, round(blue * scale[2])), alpha)
    return result


def fill_tones(steps) -> list:
    """The flat tones of a piece's fill, as brightness against the fill: 1.0 and the tone of each band.

    A tone is a peak in the count of pixels by brightness that holds a real share of them.
    The pixels on the soft edge between two tones do not make a peak of their own.
    """
    counts = {}
    total = 0
    for step in steps:
        bin_index = round(step / UI_TONE_BIN)
        counts[bin_index] = counts.get(bin_index, 0) + 1
        total += 1

    def around(index, reach):
        return sum(counts.get(index + offset, 0) for offset in range(-reach, reach + 1))

    peaks = []
    for index in sorted(counts):
        here = around(index, 1)
        if here < total * UI_TONE_SHARE:
            continue
        if all(here >= around(index + offset, 1) for offset in range(-UI_TONE_REACH, UI_TONE_REACH + 1)):
            if not peaks or index - peaks[-1] > UI_TONE_REACH:
                peaks.append(index)

    tones = [index * UI_TONE_BIN for index in peaks]
    # The fill itself is exactly the target color.
    nearest = min(tones, key=lambda tone: abs(tone - 1.0)) if tones else None
    return [1.0 if tone == nearest else tone for tone in tones] or [1.0]


def fit_ui(png_bytes: bytes, size, outline: int, stretch: bool, fill: float = UI_GLYPH_FILL, flatten: bool = True):
    """Cuts a piece of the user interface, or an item's icon, out and gives it its size and its outline.

    stretch: a frame fills its size exactly, whatever its drawn proportions were (it is
    stretched again on screen), and its fill is made one flat color. Otherwise the piece keeps
    its proportions and is centred, as large as fits within the given share of the canvas.
    The outline is one even dark ring around the piece, in the tone the figures are outlined
    in, so every piece of the interface has the same weight and color of line.
    """
    image = Image.open(BytesIO(png_bytes)).convert("RGBA")
    solid = image.getchannel("A").point(lambda value: 255 if value >= UI_ALPHA_CUT else 0)
    box = solid.getbbox()
    if box is None:
        raise PipelineError("이미지가 전부 투명하다. 피사체를 찾지 못했다.")

    left, top, right, bottom = box
    touches_edge = left <= 1 or top <= 1 or right >= image.width - 1 or bottom >= image.height - 1
    piece = image.crop(box)
    piece.putalpha(solid.crop(box))
    raw_size = piece.size

    # Work larger than the result: the ring's edge is hard here and smooth after scaling down.
    width, height = size
    work = (width * UI_WORK_SCALE, height * UI_WORK_SCALE)
    ring = outline * UI_WORK_SCALE
    room = (work[0] - 2 * ring, work[1] - 2 * ring)
    if stretch:
        inner = room
    else:
        ratio = min(room[0] * fill / piece.width, room[1] * fill / piece.height)
        inner = (max(1, round(piece.width * ratio)), max(1, round(piece.height * ratio)))
    piece = piece.resize(inner, Image.LANCZOS)

    canvas = Image.new("RGBA", work, (0, 0, 0, 0))
    canvas.paste(piece, ((work[0] - inner[0]) // 2, (work[1] - inner[1]) // 2))
    body = canvas.getchannel("A").point(lambda value: 255 if value >= UI_ALPHA_CUT else 0)

    result = Image.new("RGBA", work, UI_LINE + (0,))
    if ring > 0:
        result.putalpha(grow(body, ring))
    result.alpha_composite(canvas)
    result = result.resize(size, Image.LANCZOS)
    if stretch and flatten:
        result = flatten_fill(result)

    buffer = BytesIO()
    result.save(buffer, format="PNG")
    centre = image.getpixel(((left + right) // 2, (top + bottom) // 2))
    report = {
        "ui": True,
        "stretch": stretch,
        "raw_size": raw_size,
        "raw_fill": max(raw_size[0] / image.width, raw_size[1] / image.height),
        "size": size,
        "outline": outline,
        "hollow": centre[3] < 200,
        "inner": inner if stretch else (round(inner[0] / UI_WORK_SCALE), round(inner[1] / UI_WORK_SCALE)),
        "drawn_ratio": raw_size[0] / raw_size[1],
        "fitted_ratio": inner[0] / inner[1],
        "touches_edge": touches_edge,
    }
    return buffer.getvalue(), report


def fit(kind: str, png_bytes: bytes, row: dict):
    """The post-processing of a type: a figure stands on its floor line, an item fits its cells, a scene gets the shared floor line."""
    if TYPES[kind]["fit"] == "scene":
        return fit_scene(png_bytes, row["floor_line"])
    if TYPES[kind]["fit"] in ("frame", "glyph"):
        return fit_ui(png_bytes, row["ui"]["size"], row["ui"]["outline"], stretch=TYPES[kind]["fit"] == "frame", flatten=row["ui"]["flat"])
    if TYPES[kind]["fit"] == "cell":
        png, report = fit_ui(png_bytes, ITEM_CELLS[row["cells"]]["canvas"], ITEM_OUTLINE, stretch=False, fill=ITEM_FILL)
        report["cells"] = row["cells"]
        return png, report
    return fit_figure(png_bytes, row["height"], row["flip"])


def print_fit(report: dict) -> None:
    if report.get("scene"):
        print(f"      장면: {report['size'][0]}x{report['size'][1]}, 불투명, 바닥선은 높이의 {SCENE_FLOOR}%")
        if report["cut"]:
            print(f"      발 높이 {report['floor_line']}% 를 바닥선에 맞추려고 원본에서 "
                  f"{report['window'][0]}x{report['window'][1]} 창을 잘라 캔버스 크기로 맞췄다.")
        else:
            print(f"      발 높이 {report['floor_line']}% 가 바닥선과 같다. 그린 대로 뒀다.")
        return

    if report.get("ui"):
        width, height = report["size"]
        print(f"      조각: 원본 {report['raw_size'][0]}x{report['raw_size'][1]} (생성 캔버스의 {report['raw_fill']:.0%}) "
              f"-> {width}x{height}, 외곽선 {report['outline']}px")
        if report["stretch"]:
            drift = report["fitted_ratio"] / report["drawn_ratio"]
            print(f"      비율: 그린 것 {report['drawn_ratio']:.2f} -> 맞춘 것 {report['fitted_ratio']:.2f} (가로로 {drift:.2f}배)")
            if not 0.85 <= drift <= 1.18:
                print("      경고: 그린 비율과 맞춘 비율이 15% 넘게 다르다. 모서리가 눌려 보일 수 있다.", file=sys.stderr)
            if report["hollow"]:
                print("      경고: 틀의 가운데가 비어 있다(투명). 채워진 틀이 아니다.", file=sys.stderr)
        if report.get("cells"):
            # How much of its cells the icon takes: a long item should reach across the width.
            inner_width, inner_height = report["inner"]
            print(f"      칸 {report['cells']}개의 캔버스 {width}x{height} 안에 {inner_width}x{inner_height} "
                  f"(폭의 {inner_width / width:.0%}, 높이의 {inner_height / height:.0%})")
        if report["touches_edge"]:
            print("      경고: 피사체가 생성 캔버스의 가장자리에 닿았다. 잘렸을 수 있다.", file=sys.stderr)
        return

    width, height = report["size"]
    print(f"      피사체: 원본 {report['raw_size'][0]}x{report['raw_size'][1]} "
          f"(생성 캔버스의 {report['raw_fill']:.0%}) -> {width}x{height} "
          f"(배율 {report['ratio']:.2f}{', 좌우 뒤집음' if report['flipped'] else ''})")
    print(f"      키:     {height}px / 목표 {report['target_height']}px "
          f"(캔버스 {FIGURE_CANVAS[0]}x{FIGURE_CANVAS[1]}, 바닥 여백 {FIGURE_FLOOR_MARGIN}px)")
    if report["touches_edge"]:
        print("      경고: 피사체가 생성 캔버스의 가장자리에 닿았다. 잘렸을 수 있다.", file=sys.stderr)
    if report["short_by"] > 0:
        print(f"      경고: 피사체가 목표보다 {report['short_by']}px 작다. 키우지 않고 그대로 뒀다.", file=sys.stderr)
    if report["width_bound"]:
        print("      참고: 폭이 캔버스를 넘어 폭에 맞춰 줄였다. 키가 목표보다 작다.")


def verify_output(path: Path) -> None:
    """Confirms the file landed and reports what the fit left on the canvas."""
    if not path.exists():
        raise PipelineError(f"저장했다고 했으나 파일이 없다: {path}")
    if path.stat().st_size == 0:
        raise PipelineError(f"저장된 파일이 0바이트다: {path}")

    if Image.open(path).mode == "RGB":
        # An opaque scene: there is no subject to measure.
        with Image.open(path) as scene:
            print(f"      경로:   {path}")
            print(f"      크기:   {scene.width}x{scene.height}, {path.stat().st_size:,} bytes, 불투명")
        return

    image = Image.open(path).convert("RGBA")
    box = subject_box(image)
    if box is None:
        raise PipelineError(f"저장된 그림이 전부 투명하다: {path}")

    left, top, right, bottom = box
    histogram = image.getchannel("A").histogram()
    total = image.width * image.height
    clear = sum(histogram[: ALPHA_FLOOR + 1])
    semi = sum(histogram[ALPHA_FLOOR + 1: 248])

    print(f"      경로:   {path}")
    print(f"      크기:   {image.width}x{image.height}, {path.stat().st_size:,} bytes")
    print(f"      여백:   좌 {left} 우 {image.width - right} 상 {top} 하 {image.height - bottom} px")
    print(f"      알파:   완전투명 {100 * clear / total:.1f}% 반투명 {100 * semi / total:.1f}% "
          f"(반투명이 8%를 넘으면 글로우나 부드러운 그림자가 있다)")


# -------------------------------------------------------------------------- call


def generate_png(api_key: str, prompt: str, uploads: list, quality: str, size: str, background: str):
    """Makes the single API call of this run. Returns the decoded PNG bytes and the usage."""
    client = OpenAI(api_key=api_key, timeout=REQUEST_TIMEOUT_SECONDS, max_retries=0)

    try:
        # A transparent background needs an output format that carries alpha.
        response = client.images.edit(
            model=MODEL,
            image=uploads,
            prompt=prompt,
            size=size,
            quality=quality,
            background=background,
            output_format="png",
        )
    except Exception as error:
        # The SDK message says which of model, parameters, upload or auth was
        # rejected, and it does not echo the key back.
        raise PipelineError(
            f"이미지 생성 요청이 실패했다.\n"
            f"  model={MODEL} size={size} quality={quality} background={background} output_format=png\n"
            f"  {type(error).__name__}: {error}"
        ) from error

    if not response.data:
        raise PipelineError("응답에 이미지가 하나도 없다.")

    encoded = response.data[0].b64_json
    if not encoded:
        raise PipelineError("응답에 b64_json 이 없다. 이 모델이 URL로 돌려줬는지 확인이 필요하다.")

    try:
        return base64.b64decode(encoded, validate=True), response.usage
    except (ValueError, TypeError) as error:
        raise PipelineError(f"base64 디코딩에 실패했다: {error}") from error


def usage_numbers(usage) -> dict:
    """Token counts of a call and what they cost at the prices above. Zeroes when the API gave none."""
    if usage is None:
        return {"text_in": 0, "image_in": 0, "output": 0, "usd": 0.0, "known": False}

    details = getattr(usage, "input_tokens_details", None)
    text_in = getattr(details, "text_tokens", 0) or 0
    image_in = getattr(details, "image_tokens", 0) or 0
    if details is None:
        text_in = getattr(usage, "input_tokens", 0) or 0
    output = getattr(usage, "output_tokens", 0) or 0

    usd = (text_in * PRICE_TEXT_IN + image_in * PRICE_IMAGE_IN + output * PRICE_OUTPUT) / 1_000_000
    return {"text_in": text_in, "image_in": image_in, "output": output, "usd": usd, "known": True}


def append_ledger(kind: str, key: str, name: str, quality: str, size: str, numbers: dict) -> float:
    """Records the call and returns the estimated total of every call recorded so far."""
    CALL_LEDGER.parent.mkdir(parents=True, exist_ok=True)
    is_new = not CALL_LEDGER.exists()

    with CALL_LEDGER.open("a", encoding="utf-8", newline="") as handle:
        writer = csv.writer(handle, lineterminator="\n")
        if is_new:
            writer.writerow(LEDGER_HEADER)
        writer.writerow([
            datetime.datetime.now().astimezone().isoformat(timespec="seconds"),
            kind, key, name, MODEL, quality, size,
            numbers["text_in"], numbers["image_in"], numbers["output"], f"{numbers['usd']:.4f}",
        ])

    with CALL_LEDGER.open(encoding="utf-8", newline="") as handle:
        return sum(float(row["EstimatedUsd"]) for row in csv.DictReader(handle))


# -------------------------------------------------------------------------- main


def parse_args():
    parser = argparse.ArgumentParser(
        description="Generate one piece of art from STYLE_RUNTIME.md, a roster row and the style reference.")
    parser.add_argument("--type", dest="kind", required=True, choices=sorted(TYPES),
                        help="Generation type: picks the STYLE_RUNTIME section, the references, the quality and the fit.")
    parser.add_argument("--key", required=True,
                        help="Key of the roster row to draw (Rosters/<type>.csv).")
    parser.add_argument("--name", default="",
                        help="Output file stem under output/<type>/. Defaults to the key; give another for a second candidate.")
    parser.add_argument("--extra", default="",
                        help="Constraint appended to the subject sentence for this run, e.g. a direction from a verdict.")
    parser.add_argument("--quality", default="", choices=["", "low", "medium", "high"],
                        help="Overrides the type's quality.")
    # A style test: its document, roster and reference live together under Archive/<round>/<style>/
    # and are named here. The files of the current style are not touched.
    parser.add_argument("--style", default="",
                        help="Style document for this run instead of STYLE_RUNTIME.md (same format).")
    parser.add_argument("--roster", default="",
                        help="Roster file for this run instead of Rosters/<type>.csv.")
    parser.add_argument("--reference", default="",
                        help="Style reference for this run instead of the type's. Attached as it is, never mirrored: "
                             "give one that already faces the way the subject should.")
    parser.add_argument("--dry-run", action="store_true",
                        help="Check the files and print the prompt. Reads no key and makes no call.")
    parser.add_argument("--refit", action="store_true",
                        help="Redo the post-processing from the saved raw image. Makes no call.")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    spec = TYPES[args.kind]
    name = args.name or args.key
    quality = args.quality or spec["quality"]
    background = spec.get("background", "transparent")
    output_path = OUTPUT_DIR / args.kind / f"{name}.png"
    raw_path = OUTPUT_DIR / args.kind / f"{name}.raw.png"
    style = Path(args.style).resolve() if args.style else STYLE_RUNTIME
    roster = Path(args.roster).resolve() if args.roster else None
    custom_reference = Path(args.reference).resolve() if args.reference else None

    try:
        if args.refit:
            row = read_roster_row(args.kind, args.key, roster)
            require_file(raw_path, f"원본 {raw_path.name}")
            png, report = fit(args.kind, raw_path.read_bytes(), row)
            output_path.write_bytes(png)
            print(f"[refit] {raw_path.name} -> {output_path.name} (호출 없음).")
            print_fit(report)
            verify_output(output_path)
            return 0

        # Everything that can fail for free fails before the key is touched or the
        # single API call is spent.
        require_file(style, f"스타일 문서 {style.name}")
        row = read_roster_row(args.kind, args.key, roster)

        # A frame and an item are generated in the proportions they will have; everything else at the type's size.
        if spec["fit"] == "frame":
            size = generation_size(*row["ui"]["size"])
        elif spec["fit"] == "cell":
            size = ITEM_CELLS[row["cells"]]["generate"]
        else:
            size = spec.get("size", SIZE)

        # The row's own reference (the figure that holds the item) replaces the type's and is sent as it is.
        # So is a reference given for the run (--reference): whoever gives one chooses its facing.
        held = row["reference"] is not None
        if held and custom_reference is not None:
            raise PipelineError("Roster 의 Reference 와 --reference 를 함께 쓸 수 없다. 하나만 남겨라.")
        if custom_reference is not None:
            references, mirror = [custom_reference], False
        else:
            references = [row["reference"]] if held else spec["references"]
            mirror = spec.get("mirror_references", False) and not held

        uploads = []
        for reference in references:
            require_file(reference, f"레퍼런스 이미지 {reference.name}")
            upload, extension, reference_size = reference_upload(reference, mirror)
            uploads.append(upload)
            print(f"[1/6] 레퍼런스 확인: {reference.name} ({reference_size:,} bytes, 보내는 포맷 {extension.upper()}"
                  f"{', 좌우를 뒤집어 붙인다' if mirror else ''}{', 이 아이템을 든 유닛' if held else ''}"
                  f"{', --reference 로 받은 것' if custom_reference is not None else ''})")

        prompt = build_prompt(args.kind, row["subject"], args.extra, row["dungeon"], held, row["cells"], style)
        print(f"[2/6] {style if args.style else style.name} 로 프롬프트 조립 ({len(prompt):,} chars"
              f"{f', 소재는 {roster}' if roster else ''}).")

        if not args.dry_run and (output_path.exists() or raw_path.exists()):
            raise PipelineError(
                f"산출물이 이미 있다. 덮어쓰지 않는다.\n  경로: {output_path}\n"
                f"  다른 후보는 --name 으로 이름을 주고, 다시 그리려면 먼저 파일을 치워라."
            )

        if args.dry_run:
            print("[dry-run] 키를 읽지 않고 호출하지 않는다. 보낼 프롬프트:\n")
            print(prompt)
            if spec["fit"] == "figure":
                placement = f"Height={row['height']} Flip={row['flip']}"
            elif spec["fit"] == "scene":
                placement = f"fit=scene FloorLine={row['floor_line']}"
            elif spec["fit"] == "cell":
                canvas = ITEM_CELLS[row["cells"]]["canvas"]
                placement = f"fit=cell Cells={row['cells']} Canvas={canvas[0]}x{canvas[1]} Outline={ITEM_OUTLINE}"
            elif spec["fit"] in ("frame", "glyph"):
                ui = row["ui"]
                placement = f"fit={spec['fit']} Size={ui['size'][0]}x{ui['size'][1]} Outline={ui['outline']}"
            else:
                placement = f"fit={spec['fit']}"
            print(f"\n[dry-run] type={args.kind} key={args.key} name={name} quality={quality} "
                  f"model={MODEL} size={size} {placement}")
            return 0

        api_key = read_api_key()
        print(f"[3/6] Keychain에서 키를 읽었다 (서비스 {KEYCHAIN_SERVICE}, 계정 {getpass.getuser()}).")

        print(f"[4/6] 생성 요청 1회: type={args.kind}, key={args.key}, quality={quality}.")
        raw, usage = generate_png(api_key, prompt, uploads, quality, size, background)

        # The raw image is saved before anything else can fail: it is what the call paid for.
        output_path.parent.mkdir(parents=True, exist_ok=True)
        raw_path.write_bytes(raw)

        numbers = usage_numbers(usage)
        total = append_ledger(args.kind, args.key, name, quality, size, numbers)
        if numbers["known"]:
            print(f"      사용량: 입력 text {numbers['text_in']:,} + image {numbers['image_in']:,}, "
                  f"출력 {numbers['output']:,} tokens -> 약 ${numbers['usd']:.3f} (누계 약 ${total:.2f})")
        else:
            print(f"      사용량: 응답에 없다 (누계 약 ${total:.2f}, 이 호출은 0으로 적었다).")

        png, report = fit(args.kind, raw, row)
        output_path.write_bytes(png)
        print(f"[5/6] 후처리: 원본은 {raw_path.name}, 맞춘 그림은 {output_path.name}.")
        print_fit(report)

        print("[6/6] 저장 완료.")
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
