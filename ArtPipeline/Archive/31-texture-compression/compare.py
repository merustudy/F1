#!/usr/bin/env python3
"""Round 31: the variants read back from the GPU (run_probe.sh) against the art as it was, and compare-sheet.png. No API call.

  .venv/bin/python ArtPipeline/Archive/31-texture-compression/compare.py <readback dir>

Each picture is laid on the stage's dark colour; the measure is over the pixels the picture covers in either. PSNR in dB
(higher is nearer; 40 and up is not seen at this size), the largest difference of a channel, and the share of pixels off by
more than 24 of 255. The controls part the two causes of a difference: pose_potraw is the 2048x1024 file uncompressed (the
64/63 scaling alone), icon_nomipsraw the icon without mip maps uncompressed.
"""
import math
import sys
from pathlib import Path
from PIL import Image, ImageChops, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
FONT = HERE.parents[2] / "Assets/@Fonts/Source/Pretendard/Pretendard-Medium.ttf"
STAGE = (52, 44, 38)
PAIRS = [
    ("pose_orig@675x338", "pose_potraw@675x338", "자세 1080p: 64/63배 크기 조정만"),
    ("pose_potraw@675x338", "pose_pot@675x338", "자세 1080p: BC7 압축만"),
    ("pose_orig@675x338", "pose_pot@675x338", "자세 1080p: 권장안 전체 (2048x1024 BC7)"),
    ("pose_orig@1013x506", "pose_pot@1013x506", "자세 보스 150%: 권장안 전체"),
    ("pose_orig@1350x675", "pose_pot@1350x675", "자세 4K: 권장안 전체"),
    ("pose_orig@675x338", "pose_nomips@675x338", "자세 1080p: 밉맵 끔"),
    ("fig_orig@225x300", "fig_nomips@225x300", "전신 1080p: 밉맵 끔"),
    ("icon_orig@164x94", "icon_nomipsraw@164x94", "아이콘 1080p(절반 크기): 밉맵 끔만"),
    ("icon_nomipsraw@164x94", "icon_nomips@164x94", "아이콘 1080p: BC7 압축만"),
]


def on_stage(path):
    image = Image.open(path).convert("RGBA")
    stage = Image.new("RGBA", image.size, STAGE + (255,))
    stage.alpha_composite(image)
    return stage.convert("RGB"), image.getchannel("A")


def measure(folder, a, b):
    (ia, aa), (ib, ab) = on_stage(folder / f"{a}.png"), on_stage(folder / f"{b}.png")
    mask = ImageChops.lighter(aa, ab)
    diff = [d for d, m in zip(ImageChops.difference(ia, ib).getdata(), mask.getdata()) if m > 0]
    se = sum(c * c for d in diff for c in d) / (3 * len(diff))
    psnr = 99.0 if se == 0 else 10 * math.log10(255 * 255 / se)
    return psnr, max(max(d) for d in diff), sum(1 for d in diff if max(d) > 24) / len(diff)


def sheet(folder, out):
    font = ImageFont.truetype(str(FONT), 24)
    zoom = 2
    rows = [
        [("자세 1080p — 지금 (2016x1008, 무압축)", "pose_orig@675x338", (200, 40, 470, 338)),
         ("권장: 2048x1024 BC7", "pose_pot@675x338", (200, 40, 470, 338)),
         ("밉맵 끔 BC7 (그림이 달라짐)", "pose_nomips@675x338", (200, 40, 470, 338))],
        [("전신 1080p — 지금 (672x896, 무압축)", "fig_orig@225x300", (20, 40, 205, 300)),
         ("밉맵 끔 BC7 (그림이 달라짐)", "fig_nomips@225x300", (20, 40, 205, 300))],
    ]
    strips = []
    for row in rows:
        cells = []
        for label, name, box in row:
            crop = on_stage(folder / f"{name}.png")[0].crop(box)
            cells.append((label, crop.resize((crop.width * zoom, crop.height * zoom), Image.NEAREST)))
        width = 24 + sum(c.width + 24 for _, c in cells)
        strip = Image.new("RGB", (width, max(c.height for _, c in cells) + 70), (24, 22, 20))
        draw, x = ImageDraw.Draw(strip), 24
        for label, cell in cells:
            strip.paste(cell, (x, 56))
            draw.text((x, 14), label, fill=(235, 225, 210), font=font)
            x += cell.width + 24
        strips.append(strip)
    result = Image.new("RGB", (max(s.width for s in strips), sum(s.height for s in strips)), (24, 22, 20))
    y = 0
    for strip in strips:
        result.paste(strip, (0, y))
        y += strip.height
    result.save(out)
    return result.size


def main():
    folder = Path(sys.argv[1])
    for a, b, label in PAIRS:
        if (folder / f"{a}.png").is_file() and (folder / f"{b}.png").is_file():
            psnr, worst, share = measure(folder, a, b)
            print(f"{label:34s} PSNR {psnr:5.1f} dB, 최대 차이 {worst:3d}, 24 넘게 다른 픽셀 {100 * share:5.2f}%")
    size = sheet(folder, HERE / "compare-sheet.png")
    print(f"비교 시트: {HERE / 'compare-sheet.png'} {size[0]}x{size[1]}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
