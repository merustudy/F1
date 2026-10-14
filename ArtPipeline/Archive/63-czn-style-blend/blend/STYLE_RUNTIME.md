# F1 Style Runtime — 시험: blend (지금 그림체 + 카오스 제로 나이트메어의 특징)

시험용 스타일 문서다. 지금의 그림체(`ArtPipeline/STYLE_RUNTIME.md`)는 바꾸지 않는다(G19: 지금 그림체 유지).
지금 게임의 발키리(`output/character/valkyrie.raw.png`)를 기준 그림으로 붙여 **같은 캐릭터·같은 자세**를 유지하고, 그리는 방식만
카오스 제로 나이트메어(슈퍼크리에이티브, 2025)에서 읽은 특징 쪽으로 섞는다. 그 게임의 그림은 붙이지 않고 특징만 글로 적는다(README "조사").

| 지키는 것 (우리) | 가져오는 것 (카오스 제로 나이트메어) |
|---|---|
| 캐릭터의 정체: 금발의 큰 머리숱과 땋은 가닥, 붉은 털 망토, 크림 상의, 남색 치마, 외날 도끼, 주근깨 | 거의 무채색의 바탕(숯·검정·뼈색·찬 회색)에 **채도 높은 강조색 하나**(여기서는 망토의 진홍) |
| 오른쪽을 보고 적을 노려보는 전투 준비 자세, 웃지 않는 진지한 얼굴 | 경계가 또렷한 **두 단계의 셀 그림자**, 그림자는 찬 청보라 쪽 |
| 굵고 어두운 바깥 외곽선(어두운 무대에서 유닛을 떼어 냄) | 한쪽 가장자리의 **차가운 림 라이트**(그림 안의 밝은 띠. 바깥 빛 번짐은 없음) |
| 탄탄하고 곡선 있는 체형, 과장된 비율 | 더 가늘고 정리된 **안쪽 선**, 애니메풍의 **눈**(또렷한 홍채, 작은 하이라이트 하나, 굵은 윗눈꺼풀 선) |
| 투명 배경, 한 명, 글자 없음 | 끈·버클·하네스·장갑 같은 **전술 장비의 겹침**(중세 판타지 재료로) |

돌리는 법 (저장소 루트에서):

```text
.venv/bin/python ArtPipeline/tools/gen_image.py --type character --key valkyrie --name valkyrie_czn \
  --style ArtPipeline/Archive/63-czn-style-blend/blend/STYLE_RUNTIME.md \
  --roster ArtPipeline/Archive/63-czn-style-blend/blend/character.csv \
  --reference ArtPipeline/output/character/valkyrie.raw.png
```

**조립 순서와 형식 계약은 `STYLE_RUNTIME.md`와 같다:** Style Rule(§1) → Subject → Character(§4) → Forbidden(§2) → Reference Rule(§3).
§1, §2, §3에는 ```` ```text ```` 블록이 하나씩 있고, §4는 `- ` 불릿이다. 섹션 번호는 `gen_image.py`의 `character` 타입과 같다.

## Generation Types

| 타입 | 그리는 것 | 기준 그림 | size | quality | background | 타입 섹션 | Reference Rule | 후처리 |
|---|---|---|---|---|---|---|---|---|
| `character` | 지금의 발키리를 섞은 그림체로 다시 그린 것. 오른쪽을 본다 | `output/character/valkyrie.raw.png` (그대로 붙인다) | `1024x1024` | `medium` | `transparent` | §4 | §3 | `figure` |

## 1. Shared Style Rule

Prepend verbatim to every prompt.

```text
Style: a 2D game character illustration that blends a bold Western cartoon
figure with the rendering of a modern dark sci-fi horror anime game. Stylized
and exaggerated, not realistic, not a cute mascot, not chibi.
Linework: a dark, bold outer contour that keeps the figure readable on a dark
stage, with clean, thinner interior lines in a dark tone of each area's own
color. Line weight tapers: thick on the outside, fine on folds, straps and hair
strands. Crisp and confident, no sketchy doubled lines.
Color: a desaturated, almost monochrome base of charcoal, black, bone white and
cool grey, with ONE saturated accent color used on a few large areas only. Skin
stays a natural pale tone. No neon, no pastel, no rainbow of colors.
Shading: anime cel shading with hard edges: two flat shadow tones per area (a
shadow and a deeper core shadow), shadows leaning cool blue-violet. A thin, cold
pale-blue rim light painted along one edge of the figure (hair, shoulder, arm,
leg), as a hard-edged band of color inside the figure. Hair gets one hard-edged
highlight band. No airbrush, no soft gradients, no glow outside the figure.
Detail: medium. Layered gear with tactical touches built from fantasy
materials: leather straps, buckles, a chest harness, belts with pouches, metal
fittings and stitched seams. Every detail stays a clear shape that reads at a
small size.
Mood: grim and tense, a soldier of a doomed expedition, never cheerful.
Background: fully transparent. No scenery, no ground, no shadow under the feet,
no frame, no rays, no motion lines, no sparks, no aura.
```

## 2. Forbidden

Paste as the AVOID block of every prompt.

```text
avoid: realistic, photorealistic, 3D render, CGI, painterly, watercolor,
airbrush, soft gradients, textured fills, glossy plastic, glow, bloom, aura,
lens flare, neon, pastel, many saturated colors, cute mascot, chibi, tiny body,
grotesque, ugly, bulging eyes, huge nose, missing teeth, smiling, grinning,
open mouth, eyelashes drawn one by one, sparkles in the eyes, blush stickers,
sci-fi armor, guns, glowing tech, visible skin wounds, blood, drop shadow,
ground shadow, scenery, background, light rays, motion lines, frame, text,
letters, numbers, logo, title, watermark, cropped figure, extra characters, two
figures, a crowd
```

## 3. Reference Rule

The last part of a `character` prompt.

```text
The attached image is the current figure of this exact character in our game.
Keep the same character: the same body type (with the proportions the
composition gives), the same face with
its freckles and serious look, the same long wild wavy blonde hair with its
braids, the same outfit pieces (the fur-trimmed cloak, the laced top, the short
torn skirt, the fur-topped boots and the bracers) and the same single-bladed
battle axe held the same way, in the same braced attack-ready stance, facing
and looking toward the viewer's right. Change only how she is drawn and colored,
as the style above says: the bold outer contour with finer interior lines, the
near-monochrome palette with the crimson cloak as the one accent, the two-tone
cel shading with cool shadows and a cold rim light, the anime-style eyes, and
the added straps and buckles. Do not reproduce the transparent or white
background; draw her alone on a transparent background.
```

## 4. Character

- Draw one mercenary alone, the whole body from the top of the head to the boots, standing on both feet in the stance the subject says, in a three-quarter view turned toward the viewer's right.
- Proportions and build: stylized and a little exaggerated, about five heads tall: the head is a little smaller than in the attached figure and the legs a little longer, but the body type stays as the subject says (brawny and top-heavy yet curvy for this valkyrie). A woman keeps a curvy figure. Hands and boots are sturdy, not oversized.
- Face: an attractive face that mixes our caricature with anime features: large eyes with a clearly drawn iris, a dark pupil, ONE small hard-edged highlight and a bold upper eyelid line drawn as one shape; well-shaped eyebrows lowered in determination; a small nose drawn with a minimal line; a defined jaw and chin. The mouth is closed in a firm, serious line. Freckles may stay as a few small dots. Faint cool shadow under the eyes for fatigue, no wounds.
- Gaze: the eyes look toward the viewer's right, where the enemy stands: the irises sit toward the right side of the eyes and the head is turned a little that way. Never looking at the viewer, never looking left.
- Hair: big clean clumps with a few fine strand lines, one hard-edged highlight band and a thin cold rim light on the outer edge.
- Clothes and gear as the subject says, kept to clear shapes; the added straps and buckles follow the body's form. Weapons are large and solid, drawn with clean planes on the blade: a lighter cold edge and a darker flat side.
- Let the figure hold the weapon as the subject says, with the whole weapon inside the canvas and its top no higher than the top of the head. Keep the whole figure inside the canvas with a clear empty margin on all four sides. Never crop the head, the feet or the weapon.
- Draw one character only: no second figure, no animal, no scenery, no ground line and no shadow under the feet. Never add text, letters, numbers or logos. Output a transparent PNG with all four corner pixels at alpha 0.
