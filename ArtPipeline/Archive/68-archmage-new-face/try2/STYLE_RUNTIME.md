# F1 Style Runtime — 시험 2차: 대마법사의 새 얼굴 다듬기 + 조금 더 가린 옷 (지금 그림체)

시험용 스타일 문서다. 지금의 그림체(`ArtPipeline/STYLE_RUNTIME.md`)는 바꾸지 않는다. 1차(`../STYLE_RUNTIME.md`)에서 §4 "Face"의 셋을 세게(눈썹은 내리지 않음, 입술은 옅은 장밋빛, 찡그리지 않음),
§2에 짙은/빨간 입술·찡그림·좁은 곱슬·망토를 더했다. 옷과 머리는 소재(`character.csv`). 실제 인물의 사진은 붙이지 않는다.

```text
.venv/bin/python ArtPipeline/tools/gen_image.py --type character --key archmage --name archmage_newface2 \
  --style ArtPipeline/Archive/68-archmage-new-face/try2/STYLE_RUNTIME.md \
  --roster ArtPipeline/Archive/68-archmage-new-face/try2/character.csv
```

**조립 순서와 형식 계약은 `STYLE_RUNTIME.md`와 같다:** Style Rule(§1) → Subject → Character(§4) → Forbidden(§2) → Reference Rule(§3).

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
figure, nudity, see-through clothing, underwear, drawn nostrils, nose wings, wide
nose, hooked nose, heavy jaw, dark lipstick, red lips, frown, furrowed brows, tight curls, cape
```

## 3. Reference Rule

The last part of a `character` prompt.

```text
The attached image is a sheet of three characters of our game, already drawn in
our style. Use it only as a style reference: the hand-drawn wobbly ink line, the
scribbly small marks, the rubbery exaggerated shapes, the flat muted coloring and
the way each character has its own body type and an expressive, attractive face.
Do not copy any of the three characters: not their faces, hair, outfits or
weapons. Do not reproduce the white background. Draw the described subject as a
new, original figure alone, facing and looking toward the viewer's right, on a
transparent background.
```

## 4. Character

- Draw one mercenary alone, the whole body from the top of the head to the boots, standing on both feet in an energetic, characterful stance as the subject says, in a three-quarter view turned toward the viewer's right.
- Proportions and build: exaggerated and rubbery, between three and five heads tall as the subject says, always with a big head: the head is as big as the heads of the characters on the reference sheet. A tall, broad or heroic build is shown by the body, never by a smaller head. Every character has a strongly different body type (brawny and top-heavy, squat and barrel-shaped, tall and lanky, round and soft, lean and wiry): follow the subject and its body type. A woman keeps a curvy figure (a full bust, a narrow waist, full hips) whatever her build. Rubbery limbs with no muscles drawn inside, big hands and big boots.
- Face: caricatured but attractive in our style: a pretty young woman's face with common beauty traits, not a portrait of any real person. A slim oval face with a smooth, gently tapering jawline and a small chin; large, long almond eyes with double eyelids, dark brown irises and dark pupils, a thin eyeliner drawn a little past the outer corners; straight, soft, natural brows that sit level and relaxed, never lowered, angled or drawn together; a straight, high nose bridge drawn as one clean line ending in a small refined tip, never the nostrils or the wings of the nose; soft full lips in a light, natural rose, only a shade darker than the skin, never dark or red; a flat rosy blush high across the cheeks and over the bridge of the nose; small silver hoop earrings. A mercenary about to fight in a dark dungeon does not smile, but she does not frown either: the mouth is closed and the look calm, cool and a little aloof.
- Gaze: the eyes look toward the viewer's right, where the enemy stands: the pupils sit toward the right side of the eyes and the head is turned a little that way. Never looking at the viewer, never looking left.
- The eyes always have their dark pupils: never dot eyes, never blank eyes without pupils.
- Hair: big rubbery clumps with a few scribbly strand strokes.
- Clothes and gear as a few big flat shapes that fit the figure, fur drawn with short scribbly strokes, three or four simple ornaments at most. Weapons oversized and chunky, simplified to a few shapes.
- Let the figure hold the weapon as the subject says, with the whole weapon inside the canvas and its top no higher than the top of the head. Keep the whole figure inside the canvas with a clear empty margin on all four sides. Never crop the head, the feet or the weapon.
- Draw one character only: no second figure, no animal, no scenery, no ground line and no shadow under the feet. Never add text, letters, numbers or logos. Output a transparent PNG with all four corner pixels at alpha 0.
