# F1 Style Runtime — 시험: 대마법사의 새 모습 2 (예쁜 얼굴 기준, 지금 무기, 게임 안의 키)

시험용 스타일 문서다. 지금의 그림체(`ArtPipeline/STYLE_RUNTIME.md`)는 바꾸지 않는다(G19). `../newlook/`과 같은 세트(지금 §1~§4 + §2 노출 금지 셋, 기준 그림은 확정 시트)에서 셋만 바꿨다:

1. §4의 "Face" 불릿을 **예쁜 여성 얼굴의 시험 기준**으로: 사용자 "얼굴의 예쁨 기준도 너무 서양 카툰 기준 … 일반적인 동서양 기준을 반영 … 콧볼은 안그리는 걸로, 예쁜 스타일 기준".
   작은 갸름한 달걀형 얼굴과 부드러운 V라인·작은 턱, 크고 맑은 눈(쌍꺼풀, 굵은 윗속눈썹 선 하나), 자연스러운 눈썹, **콧대의 짧은 선과 작은 코끝만(콧구멍·콧볼을 그리지 않음)**, 작고 도톰한 자연스러운 장밋빛 입술, 맑은 피부, 젊고 우아함. 웃지 않는 진지한 눈빛(지금 규칙)은 그대로.
2. §2: "anime face"를 "big shiny anime eyes"로 좁힘(동양의 예쁜 얼굴이 막히지 않게, 번쩍이는 큰 애니 눈만 막음). 끝에 그린 콧구멍·콧볼·넓은 코·긴 코·매부리코·각진 턱·짙은 립스틱을 더함.
3. 무기는 지금 대마법사의 것(갈래진 끝에 붉은 수정을 문 옹이진 나무 지팡이). 소재 `character.csv`.

키: 게임은 그림을 소재의 `Height`(캔버스 높이의 %)까지 키우거나 줄여 세운다(폭에 걸리면 더 작게). 그래서 그린 등신은 머리 크기만 바꾸고, 게임 안의 키는 `Height`가 정한다.
"보통(바이킹)"은 **게임 안에서 발키리와 같은 머리 크기·키**로 읽었다: 생성 뒤 머리를 발키리의 게임 그림과 같은 크기로 맞추는 `Height`를 재서 `--refit`한다(README 66 "새 모습 2").

돌리는 법 (저장소 루트에서. `--reference` 없음 = 타입의 기준 그림):

```text
.venv/bin/python ArtPipeline/tools/gen_image.py --type character --key archmage --name archmage_newlook2 \
  --style ArtPipeline/Archive/66-archmage-hourglass/newlook2/STYLE_RUNTIME.md \
  --roster ArtPipeline/Archive/66-archmage-hourglass/newlook2/character.csv
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
saturated colors, pastel, big shiny anime eyes, cute mascot, chibi, grotesque, ugly, bulging
eyes, huge nose, huge jaw, missing teeth, clean vector line, uniform line weight,
eyelashes drawn one by one, sparkles, drop shadow, ground shadow, scenery,
background, light rays, motion lines, frame, text, letters, numbers, logo, title,
watermark, cropped figure, extra characters, two figures, a crowd, elegant fashion
figure, nudity, see-through clothing, underwear, drawn nostrils, nose wings, wide
nose, long nose, hooked nose, heavy jaw, dark lipstick
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
- Face (a pretty woman, 2026-10-14 test standard): a face that reads as beautiful by both East Asian and Western standards, still drawn in our hand-drawn ink line and flat color: a small, slim oval face with a soft V-shaped jawline and a small, slightly pointed chin; large, clear, bright eyes with dark pupils, double eyelids and one thick upper lash line; soft, natural, gently arched eyebrows; a small, slim, straight nose drawn only as a short line for the bridge and a tiny tip, never with the nostrils or the wings of the nose drawn; small, soft, full lips in a natural rose color; smooth clear skin; a youthful, elegant and pretty look. A mercenary about to fight in a dark dungeon does not smile: the mouth is closed, the look cool, focused and determined. Not a caricature, not a big shiny anime face.
- Gaze: the eyes look toward the viewer's right, where the enemy stands: the pupils sit toward the right side of the eyes and the head is turned a little that way. Never looking at the viewer, never looking left.
- The eyes always have their dark pupils: never dot eyes, never blank eyes without pupils.
- Hair: big rubbery clumps with a few scribbly strand strokes.
- Clothes and gear as a few big flat shapes that fit the figure, fur drawn with short scribbly strokes, three or four simple ornaments at most. Weapons oversized and chunky, simplified to a few shapes.
- Let the figure hold the weapon as the subject says, with the whole weapon inside the canvas and its top no higher than the top of the head. Keep the whole figure inside the canvas with a clear empty margin on all four sides. Never crop the head, the feet or the weapon.
- Draw one character only: no second figure, no animal, no scenery, no ground line and no shadow under the feet. Never add text, letters, numbers or logos. Output a transparent PNG with all four corner pixels at alpha 0.
