# F1 Style Runtime — 붕괴·각성의 자세 (Round 38: 발키리 시험)

시험용 스타일 문서다. 타입 `hit`의 자리(§1, §2, §25 자세의 구도, §26 기준 그림 규칙)에 **상태의 자세**를 그리는 규칙을 둔다: 공격·피격 자세처럼 확정 원본을 자세의 캔버스(1536x1024, 가운데,
발바닥이 높이의 3/4)에 앉힌 기준 그림을 보고 같은 인물을 다른 자세와 표정으로 다시 그린다(`gen_image.py`의 `pose_reference`가 만든다). 사용자 판정(Round 38 B안): "공격 모션처럼 붕괴·각성 시
생성한 이미지로 교체했다가 원복". §1·§2는 지금 문서의 것(§2 끝에 Round 23의 금지와 땀·눈물·피·빛을 더함), §26은 지금 문서의 것에서 "자세" 옆에 "상태와 색의 기분"을 더한 것. 소재는 `broken.csv`(붕괴), `resolute.csv`(각성).

## Generation Types

| 타입 | 그리는 것 | 기준 그림 | size | quality | background | 타입 섹션 | Reference Rule | 후처리 |
|---|---|---|---|---|---|---|---|---|
| `hit` | 확정한 용병 그림의 붕괴 자세와 각성 자세 (제자리에 선 자세: 피격처럼 가운데에) | `output/hit/valkyrie.reference.png` (확정 원본을 1536x1024 가운데에, 발바닥이 높이의 3/4) | `1536x1024` | `medium` | `transparent` | §25 | §26 | 원본을 본다. 맞추기는 확정 뒤 `tools/fit_pose.py`(목업은 기준 그림의 배율로 대충 세운다) |

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
figure, smiling, grinning, laughing, a cheerful or joyful face, a small head, a tiny head
on a big body, a redesigned character, a different outfit, a different weapon, sweat, tears,
blood, a wound, a glow, a halo, light rays, a dark aura, smoke, an impact effect
```

## 25. Pose

For the pose a mercenary takes at a breakdown or a virtue, drawn after its approved figure. The character stays in place: no lunge, no knock-back.

- Draw the one character alone, the whole body from the top of the head to the boots, standing in place in the posture and the state of mind the subject describes, in a three-quarter view turned toward the viewer's right, where the enemy stands.
- Keep the proportions and the build of the attached image exactly: only the limbs, the torso, the head, the weapon and the expression change. The hands hold the weapon as the subject says.
- Face: the same face as in the attached image, with the expression the subject asks for. The eyes keep their dark pupils: never dot eyes, never blank eyes without pupils, never eyes drawn as crosses or spirals.
- Gaze: the head and the eyes stay turned toward the viewer's right, toward the enemy, even when the head hangs low. Never looking at the viewer, never looking left.
- The hair, the cloak and loose cloth may hang or lift as the subject says, but keep their shapes, lengths and colors.
- Both boots stand on the floor line where the boots are in the attached image; below that line is open space. The weapon may rest on or come down below the floor line as the pose asks: no ground is drawn and nothing hides it, the whole weapon stays visible. The whole figure and the whole weapon stay inside the canvas with an empty margin on all four sides. Never crop the head, the feet or the weapon.
- Mood of the colors: the subject may ask for the whole figure in darker, duller, colder versions of its own colors (a breakdown) or warmer, brighter versions (a virtue). It is still flat color with one hard-edged second tone: no glow, no light rays, no aura, no smoke.
- Draw one character only: no second figure, no enemy, no impact effect, no blood, no scenery, no ground line and no shadow under the feet. Never add text, letters, numbers or logos. Output a transparent PNG with all four corner pixels at alpha 0.
- Never a smile, a grin or a cheerful face. Never a smaller head than in the attached image, never a redesigned character, another outfit or another weapon.

## 26. Pose Reference Rule

The last part of the prompt. The attached image is the mercenary's approved figure, laid on the canvas with open floor under the feet (gen_image.py).

The last part of an `attack` or `hit` prompt. The attached image is the mercenary's approved figure, laid on the canvas with open floor under the feet (gen_image.py).

```text
The attached image is this exact character of our game, already drawn in our
style and approved. Redraw the very same character in the new pose and the state of mind the subject
asks for. Keep everything else exactly as in the attached image: the face and
its features, the hair, the body type and proportions, every piece of the outfit
and its ornaments, the weapon, every color, and the size the character is drawn
at (the same head size and the same body size) and the place it stands in: the
soles of the boots on the floor line where they are in the attached image. Keep
the same hand: the wobbly
hand-drawn ink line, the scribbly marks and the flat coloring with its one hard
darker tone. Change only the pose, the expression and the mood of the colors the subject asks for: the
same person in the same clothes with the same weapon, not a new design. Each
hand that holds a different thing in the attached image keeps it: a weapon in
one hand and a shield on the other arm stay in the same hand and on the same
arm; they never change hands. The
character faces toward the viewer's right, where the enemy stands. Draw one
figure only, alone, on a transparent background, and do not reproduce the
background of the attached image.
```
