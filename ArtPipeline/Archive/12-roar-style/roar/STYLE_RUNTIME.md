# F1 Style Runtime — 시험: roar (올린 포스터의 그림체 + 지금의 평면 색)

시험용 스타일 문서다. 지금의 그림체(`ArtPipeline/STYLE_RUNTIME.md`)는 바꾸지 않는다. 사용자 지시: "색상은 지금처럼 플랫한 느낌의 색상에서, 그림체만 업로드 이미지 스타일을 반영(다양한 얼굴, 몸 형태, 개성 있고 매력 있음)".
기준 그림은 사용자가 올린 "썬더캣츠 로어" 포스터다(저작권이 있는 그림이라 저장소에 복사하지 않는다. 이 세션의 올린 파일을 `--reference`로 그대로 붙였다).
포스터에서 읽은 그림체(README "스타일 분석")를 §1·§4에 글로 풀었고, 작품 이름은 프롬프트에 넣지 않는다. 색 규칙(§1의 Color, Shading)은 지금 스타일 문서의 것을 그대로 썼다.
체형과 얼굴은 소재(`character.csv`)가 캐릭터마다 다르게 적는다(이 그림체의 핵심). 등신도 소재가 정한다(3~5). `Height` 90, `Flip` false.
시선 규칙(§4 Gaze: 오른쪽, 적이 서는 쪽)은 사용자 지시로 뒤에 더했다. 이 세트의 세 장(`*_roar`)은 그 전에 그린 것이다.

## Generation Types

| 타입 | 그리는 것 | 기준 그림 | size | quality | background | 타입 섹션 | Reference Rule | 후처리 |
|---|---|---|---|---|---|---|---|---|
| `character` | 발키리를 포스터의 그림체와 지금의 평면 색으로 | 올린 포스터 (그대로 붙인다. 저장소 밖) | `1024x1024` | `medium` | `transparent` | §4 | §3 | `figure` |

## 1. Shared Style Rule

```text
Style: flat 2D cartoon art for a fantasy mercenary game, drawn in a loose, goofy,
exaggerated TV-cartoon hand: rubbery, bulbous shapes, boldly varied body types and
caricatured faces full of personality. Energetic and funny, never pretty, never
polished, never cute-mascot.
Linework: a dark, medium-thick ink outline that is visibly hand drawn: its weight
varies along the stroke and the contour wobbles a little, closed contours, with a
few small scribbly marks inside the shapes (short strokes for fur, a few stubble
or freckle dots, a crease or two). No clean vector line, no doubled lines.
Color: flat solid fills in muted earthy colors, three to five main colors per
figure, as in our current look. No gradients, no neon, no pastel, no bright
saturated colors.
Shading: flat color with at most one hard-edged darker tone in a few places, such
as the folds of a cloak. No soft shading, no highlights.
Detail: low. Clothes and gear are a few big plain shapes with three or four simple
ornaments at most; the scribbly marks are the only small detail.
Background: fully transparent. No scenery, no ground, no shadow under the feet, no
frame, no rays, no motion lines, no sparks.
```

## 2. Forbidden

```text
avoid: realistic, photorealistic, 3D render, CGI, painterly, watercolor, soft
shading, gradients, airbrush, textured fills, glossy highlights, neon, bright
saturated colors, pastel, anime face, pretty face, cute mascot, chibi, clean vector
line, uniform line weight, eyelashes, sparkles, drop shadow, ground shadow,
scenery, background, light rays, motion lines, frame, text, letters, numbers,
logo, title, watermark, cropped figure, extra characters, two figures, a crowd,
dot eyes, blank eyes without pupils, elegant fashion figure
```

## 3. Reference Rule

```text
The attached image is a poster showing a crowd of cartoon characters under a
title. Use it only as a style reference for the drawing hand: the wobbly
hand-drawn ink line, the scribbly small marks, the rubbery exaggerated shapes, the
boldly varied body types and the caricatured, expressive faces with big toothy
grins and bulging eyes. Do not copy any character shown in it, do not draw its
title or any text, and do not reproduce its glowing background, its rays or its
crowded composition. Keep our flat, muted coloring instead of its bright colors.
Draw the described subject as one new, original figure alone on a transparent
background.
```

## 4. Character

- Draw one mercenary alone, the whole body from the top of the head to the boots, standing on both feet in an energetic, characterful stance, in a three-quarter view turned toward the viewer's right.
- Proportions and build: exaggerated and rubbery, between three and five heads tall as the subject says. Every character has a strongly different body type (brawny and top-heavy, squat and barrel-shaped, tall and lanky, round and soft): follow the subject. Rubbery limbs with no muscles drawn inside, just big bulging or thin bendy shapes, big hands and big boots.
- Face: caricatured and expressive, as the subject says: large white eyes with small dark pupils, often of two different sizes, thick eyebrows, a distinctive nose (round, long, hooked or button), a strong chin or a weak one, and a wide expressive mouth (a toothy grin, a clenched frown, a thin smirk). A few freckle or stubble dots are fine. Each character gets its own face shape and expression.
- Gaze: the eyes look toward the viewer's right, where the enemy stands: the pupils sit toward the right side of the eyes and the head is turned a little that way. Never looking at the viewer, never looking left.
- Hair: big rubbery clumps with a few scribbly strand strokes.
- Clothes and gear as a few big flat shapes, fur drawn with short scribbly strokes, three or four simple ornaments at most. Weapons oversized and chunky, simplified to a few shapes.
- Let the figure hold the weapon with the whole weapon inside the canvas and its top no higher than the top of the head. Keep the whole figure inside the canvas with a clear empty margin on all four sides. Never crop the head, the feet or the weapon.
- Draw one character only: no second figure, no animal, no scenery, no ground line and no shadow under the feet. Never add text, letters, numbers or logos. Output a transparent PNG with all four corner pixels at alpha 0.
