# F1 Style Runtime — 머리 크기 시험 (Round 22: 성기사의 머리를 다른 용병과 같은 크기로)

시험용 스타일 문서다. §1·§2는 지금 문서(`ArtPipeline/STYLE_RUNTIME.md`)의 것을 그대로 옮겼고(§2 끝에 작은 머리·영웅 비율·다시 디자인하는 것을 더했다), §3(기준 그림 규칙)과 §4(구도)만 다르다.
기준 그림으로 **성기사의 확정 원본**(`Archive/20-serious-face/approved/character/paladin.raw.png`)을 붙여 얼굴·수염·머리·갑옷·문장·망토·철퇴·방패·색·자세를 그대로 두고 **비율만** 바꾼다: 머리를 키우고 몸통과 다리를 그만큼 줄인다(Round 20의 표정 시험과 같은 방식).
소재는 고친 `Rosters/character.csv`의 `paladin`을 그대로 쓴다. 경위는 `README.md`.

## Generation Types

| 타입 | 그리는 것 | 기준 그림 | size | quality | background | 타입 섹션 | Reference Rule | 후처리 |
|---|---|---|---|---|---|---|---|---|
| `character` | 확정한 성기사 그림의 비율만 바꾼 것(머리를 키움) | `Archive/20-serious-face/approved/character/paladin.raw.png` (그대로 붙인다) | `1024x1024` | `medium` | `transparent` | §4 | §3 | `figure` |

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
figure, a small head, a tiny head on a big body, heroic proportions, long legs, smiling,
grinning, a redesigned character, a different pose, a different outfit, a different weapon
```

## 3. Reference Rule

The last part of the prompt.

```text
The attached image is this exact character of our game, already drawn in our
style and approved, but his head is drawn too small for our game: next to the
other characters it looks tiny on his big body. Redraw the very same character
with a bigger head, as big in proportion to his body as on a stocky caricature
hero about four heads tall, and shorten the torso and the legs to match. Keep
everything else: the face itself (the shape of the head and the jaw, the eyes
with their dark pupils looking to the right, the eyebrows, the nose, the beard
and the short blond hair), the same armor, tabard, emblem, cloak and belt, the
same mace raised the same way, the same shield on the other arm, every color,
the same wide stance turned toward the viewer's right, and the same hand: the
wobbly hand-drawn ink line, the scribbly marks and the flat coloring with its
one hard darker tone. Draw one figure only, alone, on a transparent background,
and do not reproduce the background of the attached image.
```

## 4. Character

- Draw the one character alone, the whole body from the top of the head to the boots, in the pose of the attached image, in a three-quarter view turned toward the viewer's right, where the enemy stands.
- Proportions: about four heads tall with a big head: the head, from the top of the hair to the tip of the beard, is about a quarter of the whole height. The build stays broad and heroic: a huge chest and a square torso on short sturdy legs, big hands and big boots.
- Face: the same face as in the attached image, drawn bigger, with the expression the subject asks for: no smile. The eyes keep their dark pupils: never dot eyes, never blank eyes without pupils.
- Gaze: the head and the eyes stay turned toward the viewer's right, toward the enemy. Never looking at the viewer, never looking left.
- Keep the whole figure, the mace and the shield inside the canvas with a clear empty margin on all four sides. Never crop the head, the feet or the weapon.
- Draw one character only: no second figure, no scenery, no ground line and no shadow under the feet. Never add text, letters, numbers or logos. Output a transparent PNG with all four corner pixels at alpha 0.
