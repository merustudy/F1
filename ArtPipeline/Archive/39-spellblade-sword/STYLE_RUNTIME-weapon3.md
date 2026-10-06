# F1 Style Runtime — 같은 인물, 다른 무기, 3차 (Round 39: 날의 끝을 앞 발끝에, 너비를 팔뚝에 묶는다)

시험용 스타일 문서다. 사용자 지시(2026-10-06): "마검사 무기가 빨간색이라 너무 강해 보여. 처음 시작 때 롱소드 같은 기본 검으로 시작하기 때문에 흰색 검으로 바꿔줘(마검사 모션은 마음에 들기 때문에 자세 및 얼굴 유지)",
"기본 무기도 기사와 같은 검으로 하자", "기사와 같은 검 들도록 통일". 확정한 마검사의 그림을 보고 **같은 인물, 같은 자세, 같은 얼굴**로 다시 그리되 손의 검만 **기사의 롱소드**로 바꾼다.
2차의 기준 그림은 한 장에 둘이다(`reference-spellblade-knight.png`): 왼쪽에 마검사의 확정 원본, 오른쪽에 **기사의 확정 원본 그대로**(같은 배율): 검이 손에 들린 크기 그대로 보이게. 1차(아이콘을 붙인 것)는 날이 가늘고 길게 나왔다. §1·§2는 지금 문서의 것(§2 끝에 붉은 검과 다른 자세를 금지로 더함).

## Generation Types

| 타입 | 그리는 것 | 기준 그림 | size | quality | background | 타입 섹션 | Reference Rule | 후처리 |
|---|---|---|---|---|---|---|---|---|
| `character` | 마검사의 전투 준비 자세, 검만 기사의 롱소드로(길이·두께까지) | `reference-spellblade-knight.png`(그대로 붙인다) | `1024x1024` | `medium` | `transparent` | §4 | §3 | `figure` (지금의 전신 그림과 같은 맞추기) |

## 1. Shared Style Rule

Prepend verbatim to every prompt.

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

Paste as the AVOID block of every prompt.

```text
avoid: realistic, photorealistic, 3D render, CGI, painterly, watercolor, soft
shading, gradients, airbrush, textured fills, glossy highlights, neon, bright
saturated colors, pastel, anime face, cute mascot, chibi, grotesque, ugly, bulging
eyes, huge nose, huge jaw, missing teeth, clean vector line, uniform line weight,
eyelashes drawn one by one, sparkles, drop shadow, ground shadow, scenery,
background, light rays, motion lines, frame, text, letters, numbers, logo, title,
watermark, cropped figure, extra characters, two figures, a crowd, elegant fashion
figure, smiling, grinning, laughing, a cheerful face, a small head, a tiny head on a big
body, a redesigned character, a different outfit, a different pose, a red blade, a crimson
blade, a glowing blade, a gem on the sword, a second sword, a thin blade, a narrow blade, a
rapier, a needle-like blade, a blade longer than the knight's, a long blade, a blade
reaching past the front foot, a blade reaching the edge of the picture
```

## 3. Reference Rule

The last part of the prompt.

```text
The attached image shows two characters of our game, already drawn in our style
and approved, at the same scale: on the left, this exact character; on the right,
the knight, whose longsword this character must now hold. Redraw the very same character exactly as he is in the attached image: the
same pose and stance, the same face and its expression, the same gaze toward the
viewer's right, the same hair, body type and proportions, the same head size and
body size, every piece of the outfit and its ornaments, every color, and the same
place he stands in, with the soles of his boots on the floor line where they are
in the attached image. Change one thing only: the sword in his hand is the knight's
longsword from the right of the attached image, the very same sword at the very
same size as it is in the knight's hands: a broad blade of plain grey steel, as
broad and as long as the knight's, with its brass crossguard, round brass pommel
and dark wrapped grip, held exactly where and how the old sword was held, at the
same angle, pointing low toward the viewer's right. This sword is SHORT and BROAD,
not long and thin: from the crossguard to the tip it is about as long as the
character's thigh, so that its tip ends above the toe of his front boot and well
before the edge of the picture, and the blade is about as wide as his forearm. Keep the same hand: the wobbly hand-drawn ink
line, the scribbly marks and the flat coloring with its one hard darker tone. Draw
one figure only, alone, on a transparent background: do not draw the knight, do
not draw the sword a second time beside him, and do not reproduce the background
of the attached image.
```

## 4. Character

- Draw the one character alone, the whole body from the top of the head to the boots, in exactly the pose of the attached character: the same stance, the same lean, the same hands, in a three-quarter view turned toward the viewer's right.
- Keep the proportions, the build, the face, the hair and the outfit of the attached character exactly. Nothing about him changes except the sword.
- The sword: the knight's longsword from the right of the attached image, at the size it has in the knight's hands. It is short and broad: a blade of plain grey steel about as wide as the character's forearm and, from the crossguard to the tip, about as long as his thigh (from the hip to the knee), with a lighter edge as its one second tone, a brass crossguard and a round brass pommel, a dark wrapped grip. Held in the same hand at the same angle as the old sword, low and ready, its tip ends above the toe of his front boot: the blade never reaches past his front foot and never near the edge of the picture. Never a thin or narrow blade, never a long blade. No red, no crimson, no glow, no gem on it.
- Face: the same face as in the attached image, with the same cold, watchful expression. The eyes keep their dark pupils: never dot eyes, never blank eyes without pupils.
- Gaze: the head and the eyes stay turned toward the viewer's right, toward the enemy. Never looking at the viewer, never looking left.
- The boots stand on the floor line where the boots are in the attached image. The whole figure and the whole sword stay inside the canvas with an empty margin on all four sides. Never crop the head, the feet or the sword.
- Draw one character only: no second figure, no second sword, no scenery, no ground line and no shadow under the feet. Never add text, letters, numbers or logos. Output a transparent PNG with all four corner pixels at alpha 0.
- Never a smile. Never a smaller head than in the attached image, never a redesigned character, another outfit or another pose.
