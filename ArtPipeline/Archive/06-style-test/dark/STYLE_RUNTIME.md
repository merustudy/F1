# F1 Style Runtime — 시험: dark (다크 판타지)

시험용 스타일 문서다. 지금의 그림체(`ArtPipeline/STYLE_RUNTIME.md`)는 바꾸지 않는다. 백업한 기준 그림(`../before/style_ref_mercenary.jpg`)의 캐릭터를
그리는 방식(선, 비율, 평면 채색, 얼굴)은 그대로 두고 **어두운 느낌(다크 판타지)**만 입혀 다시 그린다: 어둡고 탁한 색, 넓고 경계가 분명한 그림자,
낡고 그을린 장비, 지친 얼굴. 그 결과가 새 그림체의 기준 그림 후보다. `character` 타입만 있고, 소재는 기준 그림의 캐릭터(`valkyrie`) 하나다.
다른 직업과 타입은 이 방향이 채택되면 그때 쓴다.

돌리는 법 (ArtPipeline 에서):

```text
.venv/bin/python tools/gen_image.py --type character --key valkyrie --name valkyrie_dark \
  --style Archive/06-style-test/dark/STYLE_RUNTIME.md \
  --roster Archive/06-style-test/dark/character.csv \
  --reference Archive/06-style-test/before/style_ref_mercenary.jpg
```

기준 그림은 왼쪽을 본다. `--reference`는 뒤집지 않으므로 그림도 왼쪽을 보고, 원본과 같은 방향으로 견준다(`Flip` false. 게임에 넣을 때는 뒤집는다).

**조립 순서와 형식 계약은 `STYLE_RUNTIME.md`와 같다:** Style Rule(§1) → Subject → Character(§4) → Forbidden(§2) → Reference Rule(§3).
§1, §2, §3에는 ```` ```text ```` 블록이 하나씩 있고, §4는 `- ` 불릿이다. 섹션 번호는 `gen_image.py`의 `character` 타입과 같다.

## Generation Types

| 타입 | 그리는 것 | 기준 그림 | size | quality | background | 타입 섹션 | Reference Rule | 후처리 |
|---|---|---|---|---|---|---|---|---|
| `character` | 기준 그림의 캐릭터를 어두운 느낌으로 다시 그린 것. 기준 그림과 같은 방향(왼쪽)을 본다 | `Archive/06-style-test/before/style_ref_mercenary.jpg` (그대로 붙인다) | `1024x1024` | `medium` | `transparent` | §4 | §3 | `figure` |

## 1. Shared Style Rule

Prepend verbatim to every prompt.

```text
Style: flat 2D cartoon art for a grim dark-fantasy mercenary game. Somber, grounded
and stoic. Hand drawn with bold, simple, rounded shapes. Not chibi and not realistic.
Linework: a bold, even, dark outline around every shape, slightly thinner lines
inside the figure, closed contours, no sketchy, broken or doubled lines.
Color: flat solid fills in dark, desaturated colors: near-black, charcoal, cold grey,
dark brown, deep dried-blood red, and bone or ash where there would be cream or
white. Three to five main colors per figure, all of them dark or dull, with at most
one small accent that is a little brighter. No gradients, no neon, no pastel, no
bright or clean colors.
Shading: flat color with hard-edged darker tones that cover a large part of the
figure, as if it stood in dim light from above: the eye sockets, the underside of the
hair, the inside of the cloak, the lower body and the legs fall into shadow. A few
short dull light strokes on metal. No soft shading.
Mood: worn and weathered. Gear is scuffed, dented and stained, cloth is frayed and
torn at the edges, metal is blackened or rusted, bandages are grimy, and the face is
tired and hard. Grim, never gory: no blood, no wounds, no gore.
Detail: low. Clothes and gear are a few big plain shapes of flat color with at most
three or four simple ornaments in all. No trim along every edge, no engraving, no
small buckles or studs.
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
engraving, many small parts, neon, glow, sparkles, light rays, bright colors, clean
colors, cheerful, cute, smiling, blood, wounds, gore, drop shadow, ground shadow,
scenery, background, frame, text, letters, numbers, watermark, cropped figure, extra
characters
```

## 3. Reference Rule

The last part of a `character` prompt.

```text
The attached image shows a character of this game. Redraw this same character: keep
her face, her hair, her outfit, her cloak, her weapon, her pose and the direction she
faces, and keep the drawing style of the image, its line weight, its body proportions
and its flat coloring. Change only what the style above asks for: the colors, the
shading, the wear of her gear and the mood. Do not reproduce its plain background.
Draw her on a transparent background.
```

## 4. Character

- Draw one mercenary alone, the whole body from the top of the head to the boots, standing upright on both feet.
- Proportions: exactly those of the attached image: an adult figure about six heads tall with a fairly large round head, a long torso and short sturdy legs with the knees set low, thick arms and legs, big hands, big boots and a broad silhouette. Never long-legged, never tall and thin.
- Women: keep the clearly feminine figure of the attached image: a full bust, a defined waist and wide hips, with the clothes fitted to the upper body and belted at the waist so that this shape reads.
- Face: very pale skin and a wide round face with two large blank white oval eyes that have a thick dark outline and no pupils, one thin curved lid line above each eye, a tiny tick for a nose and a very small mouth. No thick eyebrows, no eyelashes, no sharp jaw. A hard-edged shadow lies under the brow and under the chin. A tired, hard, expressionless look.
- Keep the pose of the attached image and the direction it faces: the figure stays turned as it is in the image.
- Keep the weapon of the attached image, held upright, with the whole weapon inside the canvas and its top no higher than the top of the head, so the head is the highest point of the figure.
- Keep the whole figure inside the canvas with a clear empty margin on all four sides. Never crop the head, the feet or the weapon.
- Draw one character only: no second figure, no animal, no scenery, no ground line and no shadow under the feet.
- Never add text, letters, numbers or logos. Output a transparent PNG with all four corner pixels at alpha 0.
