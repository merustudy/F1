# F1 Style Runtime — 모션 시험 v2 (Round 19, 공격 2: 발 아래에 바닥 여백)

시험용 스타일 문서다. v1(`STYLE_RUNTIME-motion.md`, 공격 1·피격)에서 §3과 §4의 한 절만 바꿨다. 사용자 판정(2026-10-04): 공격 1은 자세가 좋지만 도끼가 바닥에서 꺾였다 —
기준 그림의 발이 캔버스 맨 아래에 붙어 있고 §4가 네 변에 여백을 요구해서, 모델이 도끼를 발 높이 위로 접어 넣었다.
v2: 기준 그림은 확정 원본을 그릴 캔버스(1536x1024)에 0.76배로 앉힌 것(`reference-floor.png`, `make_reference.py`: 발바닥이 높이의 3/4, 그 아래는 빈 바닥, 오른쪽에 돌진할 자리)이고,
§3은 "붙인 그림의 크기와 자리를 그대로", §4는 "발은 붙인 그림의 바닥선에, 무기는 바닥선 아래(발 앞)로 내려가도 된다. 땅은 그리지 않고 무기는 전부 보인다. 캔버스 안에는 둔다". 경위는 `README.md`.

## Generation Types

| 타입 | 그리는 것 | 기준 그림 | size | quality | background | 타입 섹션 | Reference Rule | 후처리 |
|---|---|---|---|---|---|---|---|---|
| `character` | 확정한 직업 그림의 공격 자세 (바닥선 아래로 내려가는 무기) | `reference-floor.png` (확정 원본을 1536x1024에 0.76배로, 발바닥이 높이의 3/4. 그대로 붙인다) | `1536x1024` | `medium` | `transparent` | §4 | §3 | `figure` (시험의 같은 크기 맞추기는 `fit_motion.py`) |

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
figure, a redesigned character, a different face, a different outfit, a different
weapon
```

## 3. Reference Rule

The last part of the prompt.

```text
The attached image is this exact character of our game, already drawn in our
style and approved. Redraw the very same character in the new pose the subject
asks for. Keep everything else exactly as in the attached image: the face and
its features, the hair, the body type and proportions, every piece of the outfit
and its ornaments, the weapon, every color, and the size the character is drawn
at (the same head size and the same body size) and the place it stands in: the
soles of the boots on the floor line where they are in the attached image. Keep
the same hand: the wobbly
hand-drawn ink line, the scribbly marks and the flat coloring with its one hard
darker tone. Change only the pose and the expression the subject asks for: the
same person in the same clothes with the same weapon, not a new design. The
character faces toward the viewer's right, where the enemy stands. Draw one
figure only, alone, on a transparent background, and do not reproduce the
background of the attached image.
```

## 4. Character

- Draw the one character alone, the whole body from the top of the head to the boots, in the action pose the subject describes, in a three-quarter view turned toward the viewer's right, where the enemy stands.
- Keep the proportions and the build of the attached image exactly: only the limbs, the torso, the head and the weapon move. The hands grip the weapon as the subject says.
- Face: the same face as in the attached image, with the expression the subject asks for. The eyes keep their dark pupils unless the subject closes an eye: never dot eyes, never blank eyes without pupils.
- Gaze: the head and the eyes stay turned toward the viewer's right, toward the enemy. Never looking at the viewer, never looking left.
- The hair, the cloak and loose cloth may swing with the motion as the subject says, but keep their shapes, lengths and colors.
- The boots stand on the floor line where the boots are in the attached image; below that line is open space in front of the character. The weapon may come down below the floor line, in front of the feet, as far as the pose asks: no ground is drawn and nothing hides it, the whole weapon stays visible. The whole figure and the whole weapon stay inside the canvas with an empty margin on all four sides. Never crop the head, the feet or the weapon.
- Draw one character only: no second figure, no enemy, no impact effect, no blood, no scenery, no ground line and no shadow under the feet. Never add text, letters, numbers or logos. Output a transparent PNG with all four corner pixels at alpha 0.
