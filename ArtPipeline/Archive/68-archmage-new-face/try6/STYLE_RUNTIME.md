# F1 Style Runtime — 시험 6차: 대마법사, 얼굴 크기·비율 재조정과 마법사 로브 (지금 그림체)

시험용 스타일 문서다. 지금의 그림체(`ArtPipeline/STYLE_RUNTIME.md`)는 바꾸지 않는다. 5차(`../try5/`)와 같은 문서다(두 기준 그림: 확정 시트 + 가상 인물, §3·§4 그대로).
사용자 "얼굴 크기 재조정, 전체 비율 재조정, 옷 마법사 로브로 교체": 비율과 옷은 소재(`character.csv`)가 바꾼다. §2의 "cape"는 그대로(로브에 망토·두건 없음).

```text
.venv/bin/python ArtPipeline/tools/gen_image.py --type character --key archmage --name archmage_newface6 \
  --style ArtPipeline/Archive/68-archmage-new-face/try6/STYLE_RUNTIME.md \
  --roster ArtPipeline/Archive/68-archmage-new-face/try6/character.csv \
  --extra-reference ArtPipeline/output/reference/archmage_virtual_face.jpg
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
figure, nudity, see-through clothing, underwear, photographic face, realistic skin
texture, drawn nostrils, nose wings, frown, furrowed brows, dark lipstick, cape
```

## 3. Reference Rule

The last part of a `character` prompt.

```text
Two images are attached. The first is a sheet of three characters of our game,
already drawn in our style: use it only as a style reference for the hand-drawn
wobbly ink line, the scribbly small marks, the rubbery simplified shapes and the
flat muted coloring, and do not copy any of the three characters. The second is
a photo of the virtual woman this character is made from: take from it her face
and her hair - the shape of her face, her eyes, eyelids and brows, her nose, her
lips, the soft fullness of her cheeks, her hairstyle and its black color - so
that she is clearly the same woman, and redraw them in our flat, hand-drawn
cartoon style. Do not copy the photo's realism: not its lighting, its skin
texture, its glossy shine, its clothes, its pose or its background. Draw the
described subject as a figure alone, facing and looking toward the viewer's
right, on a transparent background.
```

## 4. Character

- Draw one mercenary alone, the whole body from the top of the head to the boots, standing on both feet in an energetic, characterful stance as the subject says, in a three-quarter view turned toward the viewer's right.
- Proportions and build: exaggerated and rubbery, between three and five heads tall as the subject says, always with a big head: the head is as big as the heads of the characters on the reference sheet. A tall, broad or heroic build is shown by the body, never by a smaller head. Every character has a strongly different body type (brawny and top-heavy, squat and barrel-shaped, tall and lanky, round and soft, lean and wiry): follow the subject and its body type. A woman keeps a curvy figure (a full bust, a narrow waist, full hips) whatever her build. Rubbery limbs with no muscles drawn inside, big hands and big boots.
- Face: the face of the woman in the second image, kept recognisable and drawn in our style with our ink line and flat color: simplified, not caricatured, her features neither exaggerated nor changed. Her soft, round oval face with full cheeks and a small rounded chin; her large, gentle, dark brown eyes with soft inner-fold double eyelids and a little fullness under the eyes; her soft, straight, natural dark brows, level and relaxed; her small straight nose with a soft rounded tip, drawn only as a short line and the tip, never the nostrils or the wings of the nose; her full, soft lips in a light natural pink; her fair skin with a light pink blush. A mercenary about to fight in a dark dungeon does not smile: her lips are gently closed and her look calm, soft and quietly confident.
- Gaze: the eyes look toward the viewer's right, where the enemy stands: the pupils sit toward the right side of the eyes and the head is turned a little that way. Never looking at the viewer, never looking left.
- The eyes always have their dark pupils: never dot eyes, never blank eyes without pupils.
- Hair: big rubbery clumps with a few scribbly strand strokes.
- Clothes and gear as a few big flat shapes that fit the figure, fur drawn with short scribbly strokes, three or four simple ornaments at most. Weapons oversized and chunky, simplified to a few shapes.
- Let the figure hold the weapon as the subject says, with the whole weapon inside the canvas and its top no higher than the top of the head. Keep the whole figure inside the canvas with a clear empty margin on all four sides. Never crop the head, the feet or the weapon.
- Draw one character only: no second figure, no animal, no scenery, no ground line and no shadow under the feet. Never add text, letters, numbers or logos. Output a transparent PNG with all four corner pixels at alpha 0.
