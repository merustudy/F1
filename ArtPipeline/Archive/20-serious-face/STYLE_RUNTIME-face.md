# F1 Style Runtime — 표정 시험 (Round 20: 전투 준비 자세에서 웃지 않는 얼굴)

시험용 스타일 문서다. 지금의 그림체(`ArtPipeline/STYLE_RUNTIME.md`)는 바꾸지 않는다. §1·§2는 지금 문서의 것을 그대로 옮겼고(§2 끝에 웃는 얼굴과 다시 디자인하는 것을 더했다), §3(기준 그림 규칙)과 §4(구도)만 다르다.
기준 그림으로 **그 캐릭터의 확정 원본**을 붙여 자세·크기·자리·머리·체형·옷·무기·색과 얼굴 생김새를 그대로 두고 **표정만** 바꾼다(Round 19의 자세 시험과 같은 방식). 소재는 표정마다 한 파일이다(`serious.csv`, `grim.csv`). 경위는 `README.md`.

## Generation Types

| 타입 | 그리는 것 | 기준 그림 | size | quality | background | 타입 섹션 | Reference Rule | 후처리 |
|---|---|---|---|---|---|---|---|---|
| `character` | 확정한 직업 그림의 표정만 바꾼 것 | 그 직업의 확정 원본 `Archive/12-roar-style/approved/character/<key>.raw.png` (그대로 붙인다) | `1024x1024` | `medium` | `transparent` | §4 | §3 | `figure` |

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
figure, smiling, grinning, laughing, a cheerful or happy face, a redesigned character,
a different pose, a different outfit, a different weapon
```

## 3. Reference Rule

The last part of the prompt.

```text
The attached image is this exact character of our game, already drawn in our
style and approved. Redraw the very same picture: the same pose and stance, the
same size and place on the canvas, the same hair, body, outfit and ornaments,
the same weapon held the same way, every color, and the same hand: the wobbly
hand-drawn ink line, the scribbly marks and the flat coloring with its one hard
darker tone. Keep the face itself: the shape of the head and the jaw, the eyes
with their dark pupils looking to the right, the shape of the eyebrows, the
nose, the freckles. Change only the expression the subject asks for. The
character faces toward the viewer's right, where the enemy stands. Draw one
figure only, alone, on a transparent background, and do not reproduce the
background of the attached image.
```

## 4. Character

- Draw the one character alone, the whole body from the top of the head to the boots, exactly in the pose of the attached image, in a three-quarter view turned toward the viewer's right, where the enemy stands.
- Keep the proportions, the build, the pose, the weapon and the place on the canvas exactly as in the attached image.
- Face: the same face as in the attached image with the expression the subject asks for: no smile. The eyes keep their dark pupils: never dot eyes, never blank eyes without pupils.
- Gaze: the head and the eyes stay turned toward the viewer's right, toward the enemy. Never looking at the viewer, never looking left.
- Keep the whole figure and the whole weapon inside the canvas as in the attached image. Never crop the head, the feet or the weapon.
- Draw one character only: no second figure, no scenery, no ground line and no shadow under the feet. Never add text, letters, numbers or logos. Output a transparent PNG with all four corner pixels at alpha 0.
