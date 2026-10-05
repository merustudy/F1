# F1 Style Runtime — 몬스터 모션 (Round 26: 몬스터의 공격·피격 자세 시험)

시험용 스타일 문서다. 용병의 자세 규칙(Round 23 v2 `../23-motion-six/STYLE_RUNTIME-motion2.md`, 지금 문서의 §25·§26)을 몬스터에 옮겼다.
§1은 지금 문서(`ArtPipeline/STYLE_RUNTIME.md`) 그대로, §2는 지금 문서에 자세 그림의 금지(다시 디자인, 작은 머리, 눈동자, 피와 상처)를 더했다.
§5(자세의 구도)와 §6(기준 그림 규칙)은 §25·§26을 몬스터의 것으로 바꿨다: 왼쪽(파티)을 본다, 눈동자 없는 노란 눈은 그대로, 맨발이 바닥선에, "우스꽝스럽게 위협적인" 얼굴(Design/10 §3).
던전 절은 지금 문서의 `abandoned_mine` / `Creatures`에서 장비 문장만 "붙인 그림의 장비를 그대로"로 바꿨다(지금 문장 "채굴 장비 하나를 걸친다"가 장비를 바꾸게 할 수 있다).
기준 그림은 몬스터의 확정 원본을 1536x1024에 앉힌 것(`make_references.py`: 발바닥이 높이의 3/4, 공격은 오른쪽 3분의 1·피격은 가운데)이다. 소재는 `attack.csv`, `hit.csv`. 경위는 `README.md`.

## Generation Types

| 타입 | 그리는 것 | 기준 그림 | size | quality | background | 타입 섹션 | Reference Rule | 후처리 |
|---|---|---|---|---|---|---|---|---|
| `enemy` | 확정한 몬스터 그림의 공격 자세와 피격 자세 | `references/<자세>-<key>.png` (확정 원본을 1536x1024에, 발바닥이 높이의 3/4. 그대로 붙인다) | `1536x1024` | `medium` | `transparent` | §5 | §6 | `figure` (같은 크기 맞추기는 `fit_monster_poses.py`) |

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
figure, a cute or friendly face, a small head, a tiny head on a big body, a
redesigned creature, different gear, a different outfit, a different weapon,
pupils, irises, dark dots in the eyes, blood, wounds, gore
```

## 5. Enemy

- Draw the one creature alone, the whole body from the top of the head and its gear to the feet, in the action pose the subject describes, in a three-quarter view turned toward the viewer's left, where the party stands.
- Keep the proportions and the build of the attached image exactly: only the limbs, the torso, the head and the weapon move. The hands grip the weapon as the subject says.
- Face: the same face as in the attached image, with the expression the subject asks for: menacing in a goofy way, never cute and never gory. The eyes stay blank as in the attached image: each is filled with one flat dull lantern yellow, with no pupil and nothing dark inside it, unless the subject squeezes an eye shut.
- Facing: the head and the chest stay turned toward the viewer's left, toward the party. The eyes have no pupils, so the head and the body show where it looks. Never turned to the viewer, never turned right.
- The ears, the hair, loose leather, rope and hanging gear may swing with the motion as the subject says, but keep their shapes, lengths and colors.
- The bare feet stand on the floor line where the feet are in the attached image; below that line is open space in front of the creature. The weapon may come down below the floor line, in front of the feet, as far as the pose asks: no ground is drawn and nothing hides it, the whole weapon stays visible. The whole figure and the whole weapon stay inside the canvas with an empty margin on all four sides. Never crop the head, the feet or the weapon.
- Draw one creature only: no second figure, no mercenary, no impact effect, no blood, no wound, no scenery, no ground line and no shadow under the feet. Never add text, letters, numbers or logos. Output a transparent PNG with all four corner pixels at alpha 0.
- Never a smaller head than in the attached image, never a redesigned creature, other gear or another weapon.

## 6. Enemy Reference Rule

The last part of the prompt.

```text
The attached image is this exact creature of our game, already drawn in our
style and approved. Redraw the very same creature in the new pose the subject
asks for. Keep everything else exactly as in the attached image: the face and
its features, the ears, the body type and proportions, every piece of its gear
and clothing, the weapon, every color, and the size the creature is drawn at
(the same head size and the same body size) and the place it stands in: the
soles of its feet on the floor line where they are in the attached image. Its
eyes stay blank as in the attached image, filled with one flat dull lantern
yellow, with no pupils. Keep the same hand: the wobbly hand-drawn ink line, the
scribbly marks and the flat coloring with its one hard darker tone. Change only
the pose and the expression the subject asks for: the same creature with the
same gear and the same weapon, not a new design. The creature faces toward the
viewer's left, where the party stands. Draw one figure only, alone, on a
transparent background, and do not reproduce the background of the attached
image.
```

## Dungeon: abandoned_mine

### Creatures

- This creature lives in an abandoned mine that goblins have taken over.
- Its eyes are filled with one flat dull lantern yellow, with no pupils.
- It is dusted with soot and grey rock dust.
- It keeps the scavenged mining gear of the attached image as the subject lists it, and nothing more.
- Gear and accents use the colors of the mine: rust orange, soot grey and dull lantern yellow.
