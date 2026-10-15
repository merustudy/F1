# F1 Style Runtime — 시험: 대마법사의 모래시계 체형과 더 미형의 얼굴 (지금 그림체)

시험용 스타일 문서다. 지금의 그림체(`ArtPipeline/STYLE_RUNTIME.md`)는 바꾸지 않는다. Round 64의 체형 시험 문서(`Archive/64-valkyrie-body-manga/body/`)를 옮겨
§3만 대마법사에 맞게 고쳤다: 지금 게임의 대마법사를 붙여 그리는 방식·머리·옷·지팡이·자세는 지키고 **몸(소재의 `Body` = 저장한 `hourglass`)과 얼굴(더 미형)만** 바꾼다.
§1·§2·§4는 Round 64 체형 시험 문서 그대로다(§2에 노출을 막는 말 셋, §4의 "Proportions and build"는 몸은 소재대로·머리 크기는 그대로).

돌리는 법 (저장소 루트에서):

```text
.venv/bin/python ArtPipeline/tools/gen_image.py --type character --key archmage --name archmage_hourglass \
  --style ArtPipeline/Archive/66-archmage-hourglass/STYLE_RUNTIME.md \
  --roster ArtPipeline/Archive/66-archmage-hourglass/character.csv \
  --reference ArtPipeline/output/character/archmage.raw.png
```

**조립 순서와 형식 계약은 `STYLE_RUNTIME.md`와 같다:** Style Rule(§1) → Subject → Body type → Character(§4) → Forbidden(§2) → Reference Rule(§3).

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
figure, nudity, see-through clothing, underwear
```

## 3. Reference Rule

The last part of a `character` prompt.

```text
The attached image is the current figure of this exact character in our game,
drawn in our style. Keep how she is drawn: the same hand-drawn ink line, the
same flat muted colors and simple shading, the same long wild grey hair, her
pointed ears and earrings, the same fitted dark purple robe belted at the waist
and open below the belt over dark leggings and heeled boots, the same tall
wooden staff topped with the red crystal in the same hand, the same raised open
casting hand and the same stance, facing and looking toward the viewer's right.
Change only two things. Her body: redraw her figure with the body type the
subject describes, not the lanky, slender body of the attached figure. Her
face: make it more beautiful as the subject says, still drawn in our cartoon
face style with the same serious, focused look. Do not reproduce the
transparent or white background; draw her alone on a transparent background.
```

## 4. Character

- Draw one mercenary alone, the whole body from the top of the head to the boots, standing on both feet in an energetic, characterful stance as the subject says, in a three-quarter view turned toward the viewer's right.
- Proportions and build: exaggerated and stylized, as many heads tall as the subject says, always with a big head: the head stays as big as the head of the attached figure. The body type is exactly the one the subject describes, not the one of the attached figure: a sexy, attractive, clearly curvy woman's figure drawn with simple rubbery shapes. Muscle definition only where the subject asks for it, drawn with two or three simple lines. Big hands and big boots.
- Face: caricatured but attractive, as the subject says: large expressive eyes with dark pupils (one may be a touch larger than the other), well-shaped thick eyebrows, a distinctive but elegant nose (long and straight, small and upturned, or strong and straight), a strong handsome jaw for a man and soft pretty features for a woman, and a serious mouth that does not smile (closed in a firm line, set, or pressed thin): a mercenary about to fight in a dark dungeon looks determined, stern, wary or grim, never cheerful, never grinning. A few freckle or stubble dots are fine. Each character gets its own face shape and its own kind of seriousness, never grotesque.
- Gaze: the eyes look toward the viewer's right, where the enemy stands: the pupils sit toward the right side of the eyes and the head is turned a little that way. Never looking at the viewer, never looking left.
- The eyes always have their dark pupils: never dot eyes, never blank eyes without pupils.
- Hair: big rubbery clumps with a few scribbly strand strokes.
- Clothes and gear as a few big flat shapes that fit the figure, fur drawn with short scribbly strokes, three or four simple ornaments at most. Weapons oversized and chunky, simplified to a few shapes.
- Let the figure hold the weapon as the subject says, with the whole weapon inside the canvas and its top no higher than the top of the head. Keep the whole figure inside the canvas with a clear empty margin on all four sides. Never crop the head, the feet or the weapon.
- Draw one character only: no second figure, no animal, no scenery, no ground line and no shadow under the feet. Never add text, letters, numbers or logos. Output a transparent PNG with all four corner pixels at alpha 0.
