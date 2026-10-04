# F1 Style Runtime — 모션 시험 (Round 19, 기준 그림 = 그 캐릭터의 확정 원본)

시험용 스타일 문서다. 지금의 그림체(`ArtPipeline/STYLE_RUNTIME.md`)는 바꾸지 않는다. §1·§2는 지금 문서의 것을 그대로 옮겼고(§2 끝에 다시 디자인하는 것만 더했다), §3(기준 그림 규칙)과 §4(구도)만 다르다.
기준 그림으로 **그 캐릭터의 확정 원본**을 붙여 얼굴·머리·체형·옷·무기·색·크기를 그대로 두고 **자세와 표정만** 바꾼다(`Archive/12-roar-style/charm/STYLE_RUNTIME-pose.md`의 방식. 발키리의 준비 자세가 이렇게 나왔다).
호출은 `--size 1536x1024`(자세가 넓어져도 같은 크기로 그릴 자리)로 한다. (얼굴을 더 강하게 지키는 `input_fidelity`는 이 모델이 받지 않는다: 400 `invalid_input_fidelity_model`.) 소재는 자세마다 한 파일이다(`attack.csv`, `hit.csv`). 경위는 `README.md`.

## Generation Types

| 타입 | 그리는 것 | 기준 그림 | size | quality | background | 타입 섹션 | Reference Rule | 후처리 |
|---|---|---|---|---|---|---|---|---|
| `character` | 확정한 직업 그림의 공격·피격 자세 | 그 직업의 확정 원본 `Archive/12-roar-style/approved/character/<key>.raw.png` (그대로 붙인다) | `1536x1024` | `medium` | `transparent` | §4 | §3 | `figure` (시험의 같은 크기 맞추기는 `fit_motion.py`) |

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
at (the same head size and the same body size). Keep the same hand: the wobbly
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
- Keep the whole figure and the whole weapon inside the canvas with a clear empty margin on all four sides. Never crop the head, the feet or the weapon.
- Draw one character only: no second figure, no enemy, no impact effect, no blood, no scenery, no ground line and no shadow under the feet. Never add text, letters, numbers or logos. Output a transparent PNG with all four corner pixels at alpha 0.
