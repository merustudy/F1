# F1 Style Runtime — 붕괴·각성의 초상 (Round 37: 발키리 시험)

시험용 스타일 문서다. §1·§2는 지금 문서(`ArtPipeline/STYLE_RUNTIME.md`)의 것이고(§2 끝에 Round 23의 금지와 초상이 하지 말 것을 더했다), §3(기준 그림 규칙)과 §4(초상의 구도)는
Round 23의 자세 규칙(`../23-motion-six/STYLE_RUNTIME-motion.md`)을 "같은 인물의 가슴 위 초상, 표정과 어깨와 손과 빛의 기분만 바꾼다"로 고친 것이다.
기준 그림은 발키리의 확정 원본(`../20-serious-face/approved/character/valkyrie.raw.png`)을 그대로 붙인다. 소재는 `afflicted.csv`(붕괴: 고통), `virtue.csv`(각성). 경위는 `README.md`.
붕괴의 띠(`BattleFxLayer.Banner`)의 글 왼쪽 그림 자리에 들어갈 그림이다(Round 36 "B"): 다키스트 던전의 고통·각성 알림이 영웅의 초상을 보이는 방식.

## Generation Types

| 타입 | 그리는 것 | 기준 그림 | size | quality | background | 타입 섹션 | Reference Rule | 꼬리(`--tail`) | 후처리 |
|---|---|---|---|---|---|---|---|---|---|
| `character` | 확정한 용병 그림의 붕괴(고통)·각성 초상 | 확정 원본 그대로 | `1024x1024` | `medium` | `transparent` | §4 | §3 | "Draw this one character as a bust, from the top of the head to the chest." | 원본(`*.raw.png`)을 본다. 전신 맞추기(`figure`)는 초상에 맞지 않아 쓰지 않는다 |

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
on a big body, a redesigned character, a different outfit, a different weapon, a full body,
legs, feet, boots, a glow, a halo, light rays, a dark aura, smoke, tears streaming, blood,
a wound, a different hairstyle
```

## 3. Reference Rule

The last part of the prompt.

```text
The attached image is this exact character of our game, already drawn in our
style and approved. Redraw the very same character as a bust, from the top of
the head down to the chest, larger than in the attached image, in the state of
mind the subject describes. Keep everything else exactly as in the attached
image: the face and its features, the hair and its braids, the body type and
proportions (the same big head on the same broad shoulders), every piece of the
outfit and its ornaments that shows above the chest, the weapon where the
subject keeps it in view, and every color, shifted only as far as the subject
says. Keep the same hand: the wobbly hand-drawn ink line, the scribbly marks and
the flat coloring with its one hard darker tone. Change only the expression, the
set of the head and the shoulders, the hands and the mood of the colors the
subject asks for: the same person in the same clothes, not a new design. The
character faces toward the viewer's right, where the enemy stands. Draw one
bust only, alone, on a transparent background, and do not reproduce the
background of the attached image.
```

## 4. Character

- Draw the one character alone as a bust: from the top of the head down to the chest, in a three-quarter view turned toward the viewer's right, where the enemy stands. Nothing below the chest: the bust ends in a clean straight cut across the chest, as a portrait does, with open space under it. No legs, no feet, no boots.
- Keep the face, the hair, the build and the outfit of the attached image exactly: only the expression, the set of the head and the shoulders, the hands and the mood of the colors change, as the subject says. The weapon stays in view only as far as the subject says (the upper haft against the chest, or beside the shoulder), never whole.
- Face: the same face as in the attached image, large on the canvas, with the expression the subject asks for. The eyes keep their dark pupils: never dot eyes, never blank eyes without pupils, never eyes drawn as crosses or spirals.
- Gaze: the head and the eyes stay turned toward the viewer's right, toward the enemy. Never looking at the viewer, never looking left.
- The hair and the cloak may hang or lift as the subject says, but keep their shapes, lengths and colors.
- Mood of the colors: the subject may ask for the whole bust in darker, duller versions of its own colors (deep shadow) or in warmer, brighter versions (a cold or a warm cast). It is still flat color with one hard-edged second tone: no glow, no light rays, no aura, no smoke, no gradient. Nothing is drawn around the character.
- The whole bust stays inside the canvas with an empty margin on all four sides, filling most of the canvas height. Never crop the top of the head or the shoulders.
- Draw one character only: no second figure, no enemy, no effect, no blood, no scenery, no ground line and no shadow. Never add text, letters, numbers or logos. Output a transparent PNG with all four corner pixels at alpha 0.
- Never a smile, a grin or a cheerful face. Never a smaller head than in the attached image, never a redesigned character, another outfit or another weapon.
