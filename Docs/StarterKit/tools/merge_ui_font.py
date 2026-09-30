#!/usr/bin/env python3
"""Builds the one UI font from two OFL faces: Latin from one, Hangul from the other.

Why this exists: the owner's reference is a heavy geometric Latin, and no Korean font with
full Hangul coverage draws Latin that way. Unity's legacy `Font` does carry a fallback
reference, but it was measured serving Hangul from a *system* font instead (the advance of
`골` at size 24 came back as the OS face's 24, not the bundled face's 22), and a player
without a Korean system font would have drawn boxes. One merged file has neither problem.

    PYBIN=/Users/funitup/Projects/C1/.venv/bin/python
    $PYBIN ArtPipeline/tools/merge_ui_font.py \
        --latin  <Poppins-ExtraBold.ttf> \
        --hangul <NotoSansKR-Black.ttf> \
        --out    Assets/@Fonts/Source/C1UI/C1UI-Black.ttf

Both sources are SIL OFL 1.1 and neither reserves the name this output takes, so the merged
face is released under the same licence under its own name. See the README beside the output.
"""

import argparse
import sys
from pathlib import Path

from fontTools import subset
from fontTools.merge import Merger
from fontTools.ttLib import TTFont

# The Latin face supplies printable ASCII and nothing else. Everything above it - Hangul, the
# middle dot and the arrow the strings use, every symbol - stays with the Hangul face, which is
# the one with the coverage.
LATIN_FIRST, LATIN_LAST = 0x0020, 0x007E

# Legacy Unity `Text` rasterises through FreeType and does no OpenType shaping, so the layout
# tables are dead weight here and merging them is where fontTools fights hardest.
#
# The rest of this list is what the two faces disagree about. A table only one of them carries
# stops the merge outright ("Expected all items to be equal: [0, NotImplemented]"), and none of
# these earn their keep in a UI drawn left to right at 20-44px: STAT describes variable axes a
# static face has none of, vhea/vmtx are for vertical Korean, and the hinting program is a set
# the merged glyph order would no longer match anyway.
DROP_TABLES = [
    "GSUB", "GPOS", "GDEF", "BASE", "JSTF", "morx", "mort", "kern", "DSIG",
    "STAT", "gasp", "vhea", "vmtx", "prep", "fpgm", "cvt ",
]

FAMILY = "C1 UI"
SUBFAMILY = "Black"
POSTSCRIPT = "C1UI-Black"
VERSION = "Version 1.000"

COPYRIGHT = (
    "Merged from Poppins (Copyright 2020 The Poppins Project Authors) and "
    "Noto Sans KR (Copyright 2014-2021 Adobe, with Reserved Font Name 'Source'). "
    "Licensed under the SIL Open Font License, Version 1.1."
)
LICENSE = (
    "This Font Software is licensed under the SIL Open Font License, Version 1.1. "
    "This licence is copied into the folder beside this font."
)
LICENSE_URL = "https://openfontlicense.org"


class MergeError(Exception):
    """Something the operator can act on."""


def codepoints(font: TTFont) -> set:
    return set(font.getBestCmap())


def subset_to(path: Path, unicodes: set, out: Path) -> TTFont:
    """Cuts a face down to the code points asked for, with the layout tables dropped."""
    if not unicodes:
        raise MergeError(f"{path.name}: nothing left to keep after subsetting.")

    font = TTFont(str(path))
    options = subset.Options()
    options.layout_features = []
    options.name_IDs = ["*"]
    options.name_legacy = True
    options.notdef_outline = True
    options.recalc_bounds = True
    options.drop_tables += DROP_TABLES
    options.glyph_names = True

    subsetter = subset.Subsetter(options=options)
    subsetter.populate(unicodes=unicodes)
    subsetter.subset(font)
    font.save(str(out))
    return TTFont(str(out))


def rename(font: TTFont) -> None:
    """Gives the merged face its own identity, so it borrows no reserved name."""
    name = font["name"]
    for platform_id, encoding_id, language_id in ((3, 1, 0x409), (1, 0, 0)):
        for name_id, value in (
            (0, COPYRIGHT),
            (1, FAMILY),
            (2, SUBFAMILY),
            (3, f"{FAMILY} {SUBFAMILY}; {VERSION}"),
            (4, f"{FAMILY} {SUBFAMILY}"),
            (5, VERSION),
            (6, POSTSCRIPT),
            (13, LICENSE),
            (14, LICENSE_URL),
            (16, FAMILY),
            (17, SUBFAMILY),
        ):
            name.setName(value, name_id, platform_id, encoding_id, language_id)


def carry_vertical_metrics(merged: TTFont, source: TTFont) -> None:
    """Keeps the Hangul face's line box: it is the taller of the two and sets the line height."""
    merged["hhea"].ascent = source["hhea"].ascent
    merged["hhea"].descent = source["hhea"].descent
    merged["hhea"].lineGap = source["hhea"].lineGap
    for field in ("sTypoAscender", "sTypoDescender", "sTypoLineGap", "usWinAscent", "usWinDescent"):
        setattr(merged["OS/2"], field, getattr(source["OS/2"], field))


def verify(path: Path, latin: set, hangul: set) -> None:
    """Fails the build rather than shipping a font with a hole in it."""
    font = TTFont(str(path))
    have = codepoints(font)

    missing_latin = sorted(latin - have)
    if missing_latin:
        raise MergeError(f"{len(missing_latin)} Latin code points are missing, first: {missing_latin[:8]}")

    missing_hangul = sorted(hangul - have)
    if missing_hangul:
        raise MergeError(f"{len(missing_hangul)} Hangul syllables are missing, first: {missing_hangul[:8]}")

    for extra in (0x00B7, 0x2192):
        if extra not in have:
            raise MergeError(f"U+{extra:04X}, which the game's strings use, is missing.")

    print(f"      code points: {len(have):,}")
    print(f"      Latin {LATIN_FIRST:#06x}-{LATIN_LAST:#06x}: all {len(latin)} present")
    print(f"      Hangul AC00-D7A3: all {len(hangul)} present")
    print(f"      glyphs: {len(font.getGlyphOrder()):,}")
    print(f"      size: {path.stat().st_size:,} bytes")


def main() -> int:
    parser = argparse.ArgumentParser(description="Merge a Latin face and a Hangul face into one UI font.")
    parser.add_argument("--latin", required=True, help="TTF the printable ASCII comes from.")
    parser.add_argument("--hangul", required=True, help="TTF everything else comes from.")
    parser.add_argument("--out", required=True, help="Where to write the merged TTF.")
    args = parser.parse_args()

    latin_path, hangul_path, out_path = Path(args.latin), Path(args.hangul), Path(args.out)

    try:
        for path in (latin_path, hangul_path):
            if not path.is_file():
                raise MergeError(f"Missing input: {path}")

        latin_font, hangul_font = TTFont(str(latin_path)), TTFont(str(hangul_path))
        if latin_font["head"].unitsPerEm != hangul_font["head"].unitsPerEm:
            raise MergeError(
                "The two faces disagree on units per em "
                f"({latin_font['head'].unitsPerEm} vs {hangul_font['head'].unitsPerEm}); "
                "one would have to be scaled before merging."
            )

        wanted_latin = {c for c in range(LATIN_FIRST, LATIN_LAST + 1)} & codepoints(latin_font)
        wanted_hangul = codepoints(hangul_font) - wanted_latin
        print(f"[1/5] Latin face keeps {len(wanted_latin)} code points, Hangul face keeps {len(wanted_hangul):,}.")

        out_path.parent.mkdir(parents=True, exist_ok=True)
        latin_cut = out_path.with_suffix(".latin.tmp.ttf")
        hangul_cut = out_path.with_suffix(".hangul.tmp.ttf")
        subset_to(latin_path, wanted_latin, latin_cut)
        subset_to(hangul_path, wanted_hangul, hangul_cut)
        print("[2/5] Both faces cut to disjoint code points.")

        # The Hangul face goes first so the merged font inherits its tables and glyph order.
        merged = Merger().merge([str(hangul_cut), str(latin_cut)])
        print("[3/5] Merged.")

        carry_vertical_metrics(merged, TTFont(str(hangul_cut)))
        rename(merged)
        merged.save(str(out_path))
        print(f"[4/5] Saved {out_path}.")

        for temp in (latin_cut, hangul_cut):
            temp.unlink(missing_ok=True)

        full_hangul = {c for c in range(0xAC00, 0xD7A4)}
        verify(out_path, wanted_latin, full_hangul & codepoints(hangul_font))
        print("[5/5] Verified.")
    except MergeError as error:
        print(f"\n실패: {error}", file=sys.stderr)
        return 1

    return 0


if __name__ == "__main__":
    sys.exit(main())
