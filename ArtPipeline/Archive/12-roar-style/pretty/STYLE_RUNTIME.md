# F1 Style Runtime — 시험: pretty (같은 그림체, 미형의 캐릭터)

시험용 스타일 문서다. 지금의 그림체(`ArtPipeline/STYLE_RUNTIME.md`)는 바꾸지 않는다. 채택한 방향(`../roar/STYLE_RUNTIME.md`: 포스터의 손맛 + 지금의 평면 색)은 그대로 두고,
사용자 지시 "현재의 그림체로 미형의 캐릭터(예쁨, 멋짐, 섹시, 매력적임) 3개"에 따라 **캐릭터를 캐리커처가 아니라 미형으로** 그린다: 고른 큰 눈, 작은 코, 잘생긴 턱, 자신 있는 미소, 매력적인 체형.
선·낙서 획·부푼 과장·색은 `roar`와 같다. 기준 그림은 올린 포스터(저장소 밖). 체형과 얼굴은 소재가 적는다. `Height` 90, `Flip` false.

## Generation Types

| 타입 | 그리는 것 | 기준 그림 | size | quality | background | 타입 섹션 | Reference Rule | 후처리 |
|---|---|---|---|---|---|---|---|---|
| `character` | 세 직업을 같은 그림체의 미형 캐릭터로 | 올린 포스터 (그대로 붙인다. 저장소 밖) | `1024x1024` | `medium` | `transparent` | §4 | §3 | `figure` |

## 1. Shared Style Rule

```text
Style: flat 2D cartoon art for a fantasy mercenary game, drawn in a loose,
energetic, exaggerated TV-cartoon hand: rubbery shapes, bold silhouettes and
expressive faces. The characters are good-looking and charismatic: pretty,
handsome or alluring, with appealing faces and figures, still drawn in the same
loose cartoon hand. Confident and charming, never polished anime, never realistic.
Linework: a dark, medium-thick ink outline that is visibly hand drawn: its weight
varies along the stroke and the contour wobbles a little, closed contours, with a
few small scribbly marks inside the shapes (short strokes for fur, a crease or
two). No clean vector line, no doubled lines.
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
saturated colors, pastel, anime face, cute mascot, chibi, ugly, grotesque,
bulging eyes, mismatched eyes, huge nose, huge jaw, clean vector line, uniform
line weight, eyelashes drawn one by one, sparkles, drop shadow, ground shadow,
scenery, background, light rays, motion lines, frame, text, letters, numbers,
logo, title, watermark, cropped figure, extra characters, two figures, a crowd,
dot eyes, blank eyes without pupils
```

## 3. Reference Rule

```text
The attached image is a poster showing a crowd of cartoon characters under a
title. Use it only as a style reference for the drawing hand: the wobbly
hand-drawn ink line, the scribbly small marks, the rubbery exaggerated shapes and
the bold, expressive faces. Our characters are good-looking where the poster's
are goofy: keep the hand, but give the described subject an attractive face and
figure. Do not copy any character shown in it, do not draw its title or any text,
and do not reproduce its glowing background, its rays or its crowded composition.
Keep our flat, muted coloring instead of its bright colors. Draw the described
subject as one new, original figure alone on a transparent background.
```

## 4. Character

- Draw one mercenary alone, the whole body from the top of the head to the boots, standing on both feet in a confident, characterful stance, in a three-quarter view turned toward the viewer's right.
- Proportions and build: exaggerated but appealing, about five heads tall as the subject says: a man has broad shoulders, a V-shaped torso and a narrow waist; a woman has a curvy figure with a full bust, a narrow waist and full hips, and long strong legs. Rubbery limbs with no muscles drawn inside, big hands and big boots are fine.
- Face: attractive and expressive, as the subject says: large, even expressive eyes with dark pupils, well-shaped thick eyebrows, a small or straight nose, a strong handsome jaw for a man and soft pretty features for a woman, and a confident mouth (a half-smile, a smirk, a calm smile). No caricature: no bulging or mismatched eyes, no huge nose, no huge jaw.
- Hair: big flowing rubbery clumps with a few scribbly strand strokes.
- Clothes and gear as a few big flat shapes that fit the figure, fur drawn with short scribbly strokes, three or four simple ornaments at most. Weapons oversized and chunky, simplified to a few shapes.
- Let the figure hold the weapon with the whole weapon inside the canvas and its top no higher than the top of the head. Keep the whole figure inside the canvas with a clear empty margin on all four sides. Never crop the head, the feet or the weapon.
- Draw one character only: no second figure, no animal, no scenery, no ground line and no shadow under the feet. Never add text, letters, numbers or logos. Output a transparent PNG with all four corner pixels at alpha 0.
