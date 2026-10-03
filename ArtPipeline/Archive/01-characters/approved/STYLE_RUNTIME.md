# F1 Style Runtime

이미지를 생성할 때 스크립트(`tools/gen_image.py`)가 읽는 스타일 규칙이다. 스타일 문구는 이 파일에만 둔다.
방향과 기준 그림은 [`Docs/Design/10_Art_Direction.md`](../Docs/Design/10_Art_Direction.md), 파이프라인의 규칙은
[`Docs/Architecture/13_ART_PIPELINE.md`](../Docs/Architecture/13_ART_PIPELINE.md)가 소유한다.

**조립 순서:** §1 Shared Style Rule → Subject → 타입 섹션 → §2 Forbidden → §3 Reference Rule
**형식 계약:** 섹션 제목은 `## <번호>. <제목>`. §1, §2, §3에는 ```` ```text ```` 블록이 하나씩 있고, 타입 섹션은 `- ` 불릿으로 쓴다.
번호를 바꾸면 `gen_image.py`의 섹션 번호도 함께 바꾼다.

## Generation Types

| 타입 | 그리는 것 | 기준 그림 | size | quality | background | 타입 섹션 | 후처리 |
|---|---|---|---|---|---|---|---|
| `character` | 직업의 전신 그림 | `References/Character/style_ref_mercenary.jpg` (좌우를 뒤집어 붙인다) | `1024x1024` | `medium` | `transparent` | §4 | `figure` |

## 1. Shared Style Rule

Prepend verbatim to every prompt.

```text
Style: flat 2D cartoon character art for a fantasy mercenary game. Calm, grounded and
a little stoic. Hand drawn with bold, simple, rounded shapes. Not chibi and not
realistic.
Proportions: an adult figure about six heads tall with a fairly large round head, a
long torso and short sturdy legs with the knees set low. By default the build is
heavy: thick arms and legs, big hands, big boots, a wide stance and a broad
silhouette. When the subject asks for a lighter build, slim the body down to the
build of the woman in the reference and keep everything else. Never long-legged,
never tall and thin.
Women: a female character has a clearly feminine figure like the woman in the
reference: a full bust, a defined waist and wide hips. Her clothes fit the upper
body and are belted at the waist so that this shape reads; a robe or a cloak never
hides it.
Linework: a bold, even, dark outline around every shape, slightly thinner lines
inside the figure, closed contours, no sketchy, broken or doubled lines.
Color: flat solid fills in muted earthy colors, three to five main colors per
character, very pale skin. No gradients, no neon, no pastel.
Shading: flat color with at most one hard-edged darker tone in a few places, such as
the folds of a cloak. A few short light strokes on metal are allowed. No soft shading.
Face: a wide round face with two large blank white oval eyes that have a thick dark
outline and no pupils, one thin curved lid line above each eye, a tiny tick for a
nose and a very small mouth. No thick eyebrows, no eyelashes, no sharp jaw. A calm,
expressionless look.
Detail: low. Clothes and gear are a few big plain shapes of flat color with at most
three or four simple ornaments in all, such as one brooch, one emblem and one belt
with a pouch. No trim along every edge, no engraving, no small buckles or studs.
Background: fully transparent. No scenery, no ground, no shadow under the feet, no
frame.
```

## 2. Forbidden

Paste as the AVOID block of every prompt.

```text
avoid: chibi, super deformed, tall and thin, long legs, elegant fashion figure,
sharp jaw, thick eyebrows, eyelashes, pupils, irises, anime face, realistic,
photorealistic, 3D render, CGI, painterly, watercolor, oil painting, soft shading,
gradients, airbrush, texture, grain, glossy highlights, intricate detail, ornate trim,
engraving, many small parts, neon, glow, sparkles, drop shadow, ground shadow,
scenery, background, frame, text, letters, numbers, watermark, cropped figure, extra
characters
```

## 3. Reference Rule

Always the last part of the prompt.

```text
Use the attached image only as a style reference for line weight, body proportions
and build, face style, color range, shading amount and level of detail. Do not copy the
character shown in it: not the face, hair, outfit, colors or weapon. Do not reproduce
its plain background. Draw the described subject as a new, original character in the
same drawing style on a transparent background.
```

## 4. Character

- Draw one mercenary alone, the whole body from the top of the head to the boots, standing upright on both feet.
- Turn the figure toward the right side of the image, as the attached reference is turned: a three-quarter front view with the head and the chest facing the viewer's right.
- Let the figure hold the weapon the subject names, upright or at rest at its side, with the whole weapon inside the canvas. Keep the top of the weapon no higher than the top of the head, so the head is the highest point of the figure.
- Make the job readable from the silhouette: hair or headwear, the main garment and the weapon.
- Keep the whole figure inside the canvas with a clear empty margin on all four sides. Never crop the head, the feet or the weapon.
- Draw one character only: no second figure, no animal, no scenery, no ground line and no shadow under the feet.
- Never add text, letters, numbers or logos. Output a transparent PNG with all four corner pixels at alpha 0.
