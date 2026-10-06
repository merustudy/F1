# F1 Style Runtime — 붕괴·각성의 무대 효과 (Round 38: 시험)

시험용 스타일 문서다. 타입 `ui_piece`의 자리(§14 스타일, §22 구도, §17 금지, §18 기준 그림 규칙)에 **무대 위의 효과 조각**을 그리는 규칙을 둔다:
붕괴의 검은 먹 튐과 각성의 금빛 빛살. 다키스트 던전의 판정 알림이 초상 뒤에 두는 효과를 이 게임의 손(평평한 카툰, 흔들리는 잉크 선)으로 그린다.
§18은 지금 문서의 것 그대로다. 소재는 `fx.csv`. 경위는 `README.md`. 조각은 원본(`*.raw.png`)을 쓴다: 투명 배경의 한 덩어리를 목업과 띠가 크기를 바꿔 쓴다.

## Generation Types

| 타입 | 그리는 것 | 기준 그림 | size | quality | background | 타입 섹션 | Reference Rule | 후처리 |
|---|---|---|---|---|---|---|---|---|
| `ui_piece` | 붕괴·각성의 무대 효과 조각 | 지금의 용병 셋 시트(그대로) | `1024x1024` | `medium` | `transparent` | §22 | §18 | 원본을 본다 |

## 14. UI Style Rule

Prepend verbatim to the prompt, in place of §1.

```text
Style: one effect shape for a fantasy mercenary game, drawn in the same hand as its
characters: flat 2D cartoon art with loose, bold, simplified shapes, like a paper
cut-out. The shape is a single flat graphic element that will be laid behind a
character's portrait on a dark stage.
Linework: a dark, medium-thick ink outline around the shape that is visibly hand
drawn, its weight varying and the contour wobbling a little, closed contours, with a
few scribbly marks inside. No clean vector line.
Color: flat solid fills in two or three colors at most, as the subject names them. No
gradients, no soft glow, no neon, no pastel.
Shading: flat color, with at most one hard-edged second tone. No soft shading.
View: seen exactly from the front, flat. No perspective, no depth and no thickness.
```

## 17. UI Forbidden

Paste as the AVOID block of the prompt, in place of §2.

```text
avoid: perspective, 3D render, bevel, emboss, thickness, drop shadow, cast shadow,
soft glow, bloom, gradients, soft shading, airbrush, glossy highlights, photo-like
or realistic textures, smoke, fog, fire, lightning bolts, stars, sparkles, a face, a
skull, an eye, a hand, a character, a creature, a frame, a border, a circle frame,
text, letters, numbers, watermark, a background, a scene, more than one object
```

## 18. UI Reference Rule

The last part of the prompt.

The last part of a `ui_frame` or `ui_icon` prompt.

```text
The attached image is a sheet of three characters of our game, drawn in our style.
Use it only as a style reference for the hand-drawn wobbly ink line, the scribbly
marks, the flat coloring and the color range. Do not draw them, any part of a body,
their clothes or the things they carry, and do not reproduce the sheet's white
background. Draw only the described piece of the user interface, alone, in the same
drawing style on a transparent background.
```

## 22. UI Piece

For an effect shape laid behind a portrait: the ink burst of a breakdown, the light burst of a virtue.

- Draw one shape alone, whole and centered, seen exactly from the front, flat. Nothing else is in the picture.
- It is roughly round as a whole and fills most of the picture, with an even margin of empty transparent space around it; its edge is irregular as the subject says (ragged tendrils, or tapered rays), never a clean circle.
- The middle of the shape is a plain flat area where a portrait will lie: no detail is drawn there.
- Flat fills in the colors the subject names, with the same bold dark outline as the characters and a few scribbly marks; one hard-edged second tone at most.
- Never add text, letters, numbers, a shadow under it or a soft glow around it. Output a transparent PNG with all four corner pixels at alpha 0.
