# F1 Style Runtime — 무덤 시험 (Round 21: 쓰러진 용병의 무덤)

시험용 스타일 문서다. 지금의 그림체(`ArtPipeline/STYLE_RUNTIME.md`)는 바꾸지 않는다. §1·§2는 지금 문서의 것을 그대로 옮겼고, 새 타입 `prop`(무대 바닥에 서는 소품)의 구도(§23)와 기준 그림 규칙(§24)을 더했다.
채택되면 §23·§24를 지금 문서로 옮긴다. 소재는 `grave.csv`(안마다 한 줄). 경위는 `README.md`.

## Generation Types

| 타입 | 그리는 것 | 기준 그림 | size | quality | background | 타입 섹션 | Reference Rule | 후처리 |
|---|---|---|---|---|---|---|---|---|
| `prop` | 무대 바닥에 서는 소품(쓰러진 용병의 무덤). 전신 그림처럼 바닥선에 세운다 | `References/Character/style_ref_roster.png` (그대로 붙인다) | `1024x1024` | `medium` | `transparent` | §23 | §24 | `figure` |

## 1. Shared Style Rule

Prepend verbatim to every prompt.

```text
Style: flat 2D cartoon art for a fantasy mercenary game, drawn in a loose,
energetic, exaggerated TV-cartoon hand: rubbery, bold, simplified shapes with
personality. Not realistic, not polished anime, not a cute mascot.
Linework: a dark, medium-thick ink outline that is visibly hand drawn: its weight
varies along the stroke and the contour wobbles a little, closed contours, with a
few small scribbly marks inside the shapes (short strokes for fur or grain, a few
dots, a crease or two). No clean vector line, no doubled lines.
Color: flat solid fills in muted earthy colors, three to five main colors per
figure. No gradients, no neon, no pastel, no bright saturated colors.
Shading: flat color with at most one hard-edged darker tone in a few places, such
as the folds of a cloak. No soft shading, no highlights.
Detail: low. Clothes and gear are a few big plain shapes with three or four simple
ornaments at most; the scribbly marks are the only small detail.
Background: fully transparent. No scenery, no ground, no shadow under the feet, no
frame, no rays, no motion lines, no sparks.
```

## 2. Forbidden

Paste as the AVOID block of every prompt.

```text
avoid: realistic, photorealistic, 3D render, CGI, painterly, watercolor, soft
shading, gradients, airbrush, textured fills, glossy highlights, neon, bright
saturated colors, pastel, anime face, cute mascot, chibi, grotesque, ugly, bulging
eyes, huge nose, huge jaw, missing teeth, clean vector line, uniform line weight,
eyelashes drawn one by one, sparkles, drop shadow, ground shadow, scenery,
background, light rays, motion lines, frame, text, letters, numbers, logo, title,
watermark, cropped figure, extra characters, two figures, a crowd, elegant fashion
figure
```

## 23. Prop

- Draw one prop alone, standing on the floor of the battle stage: the whole prop from its top down to the ground it stands in, seen from the side in a slight three-quarter view like the figures of our game.
- It is the grave marker of a fallen mercenary of the party: a plain, solemn thing in a dark dungeon. Never cute, never comic, no skull, no bones, no gore, no blood.
- Keep it simple and readable when small: one strong silhouette, a few big shapes and two to four flat muted colors. A small low mound of earth or stones at its foot is part of it.
- Keep the whole prop inside the canvas with a clear empty margin on all four sides. Never crop it.
- Draw no character, no creature, no hand, no scenery beyond the mound at its foot, no shadow under it, no glow, no candles and no flowers.
- Never add text, letters, numbers, names, R.I.P. or logos. Output a transparent PNG with all four corner pixels at alpha 0.

## 24. Prop Reference Rule

The last part of a `prop` prompt.

```text
The attached image is a sheet of three characters of our game, drawn in our style.
Use it only as a style reference for the hand-drawn wobbly ink line, the scribbly
marks, the shape simplification, the flat muted color range and the shading
amount. Do not draw any character or any part of one. Draw only the described
prop, as a new standalone picture in the same drawing style on a transparent
background, and do not reproduce the sheet's white background.
```
