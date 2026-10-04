# F1 Style Runtime — 모션 v2 (Round 23: 용병 여섯의 공격·피격 자세, 손을 지킴)

시험용 스타일 문서다. Round 19의 v2(`../19-motion-test/STYLE_RUNTIME-motion2.md`: 공격 2차에서 확정한 규칙)와 같고, §1·§2만 지금 문서(`ArtPipeline/STYLE_RUNTIME.md`)의 것으로 바꿨다.
§2 끝에는 웃는 얼굴(Round 20), 작은 머리(Round 22), 다시 디자인하는 것을 금지로 더했다. §3(기준 그림 규칙)과 §4(자세의 구도)는 v2 그대로다.
기준 그림은 직업마다 확정 원본을 1536x1024에 앉힌 것(`make_references.py`: 발바닥이 높이의 3/4, 그 아래는 빈 바닥)이다. 소재는 `attack.csv`, `hit.csv`. 경위는 `README.md`.

v1(`STYLE_RUNTIME-motion.md`, 첫 열두 장)에서 손의 규칙만 더했다: 성기사의 공격이 철퇴와 방패를 든 손을 바꿔 그렸다(사용자 판정 2026-10-04 "무기를 든 손이 반대로 바꼈어").
§2에 방패를 다른 팔로 옮긴 것을 금지로, §3에 "양손에 각각 다른 것을 들면 각 손은 붙인 그림에서 든 것을 그대로 든다", §4에 손의 절을 더했다. 소재는 성기사의 손을 적은 `attack2.csv`·`hit2.csv`.
규칙의 범위는 사용자 판정(같은 날)으로 양손에 각각 장비를 든 경우로 좁혔다: "마검사처럼 한손으로 무기 들고 있는 경우는 이상하지 않으니 일단 현행 유지".

## Generation Types

| 타입 | 그리는 것 | 기준 그림 | size | quality | background | 타입 섹션 | Reference Rule | 후처리 |
|---|---|---|---|---|---|---|---|---|
| `character` | 확정한 직업 그림의 공격 자세와 피격 자세 | `references/<자세>-<key>.png` (확정 원본을 1536x1024에, 발바닥이 높이의 3/4. 그대로 붙인다) | `1536x1024` | `medium` | `transparent` | §4 | §3 | `figure` (같은 크기 맞추기는 `fit_poses.py`) |

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
figure, smiling, grinning, laughing, a cheerful or joyful face, a small head, a tiny head
on a big body, a redesigned character, a different outfit, a different weapon,
a weapon and a shield that change hands
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
same person in the same clothes with the same weapon, not a new design. Each
hand that holds a different thing in the attached image keeps it: a weapon in
one hand and a shield on the other arm stay in the same hand and on the same
arm; they never change hands. The
character faces toward the viewer's right, where the enemy stands. Draw one
figure only, alone, on a transparent background, and do not reproduce the
background of the attached image.
```

## 4. Character

- Draw the one character alone, the whole body from the top of the head to the boots, in the action pose the subject describes, in a three-quarter view turned toward the viewer's right, where the enemy stands.
- Keep the proportions and the build of the attached image exactly: only the limbs, the torso, the head and the weapon move. The hands grip the weapon as the subject says.
- Hands: when the attached figure holds a different thing in each hand (a weapon and a shield), each stays where it is whatever the pose; the subject names them. The arm that holds the weapon makes the move, the other arm keeps its own thing.
- Face: the same face as in the attached image, with the expression the subject asks for. The eyes keep their dark pupils unless the subject closes an eye: never dot eyes, never blank eyes without pupils.
- Gaze: the head and the eyes stay turned toward the viewer's right, toward the enemy. Never looking at the viewer, never looking left.
- The hair, the cloak and loose cloth may swing with the motion as the subject says, but keep their shapes, lengths and colors.
- The boots stand on the floor line where the boots are in the attached image; below that line is open space in front of the character. The weapon may come down below the floor line, in front of the feet, as far as the pose asks: no ground is drawn and nothing hides it, the whole weapon stays visible. The whole figure and the whole weapon stay inside the canvas with an empty margin on all four sides. Never crop the head, the feet or the weapon.
- Draw one character only: no second figure, no enemy, no impact effect, no blood, no scenery, no ground line and no shadow under the feet. Never add text, letters, numbers or logos. Output a transparent PNG with all four corner pixels at alpha 0.
