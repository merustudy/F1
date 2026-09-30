# C1 Style Runtime — Cute Clean Cartoon

이미지 생성 시 읽는 경량 규칙이다. 전체 근거와 예외는 [`STYLE_GUIDE.md`](STYLE_GUIDE.md)가 소유하며,
충돌하면 `STYLE_GUIDE.md`가 우선한다. 이 파일만 읽고 프롬프트를 조립한다.

**조립 순서:** §9 Quick Rule → SUBJECT → 타입 섹션(§6 item / §7 character / §10 ui / §11 monster) → §8 AVOID 블록 → 레퍼런스 문구
**모델:** `gpt-image-2`, 레퍼런스는 `images.edit()`의 `image`로 요청당 하나만 첨부한다.

## Generation Types

| 타입 | 레퍼런스 | size | quality | background | 타입 섹션 |
|---|---|---|---|---|---|
| `character_symbol` | `References/Monsters/monster_icon_master.png` | `1024x1024` | `medium` | `transparent` | §7 |
| `item_symbol` | `References/Monsters/monster_icon_master.png` | `1024x1024` | `medium` | `transparent` | §6 |
| `monster_icon` | `References/Monsters/monster_icon_master.png` | `1024x1024` | `medium` | `transparent` | §11 |
| `ui` | `References/UI/panel_master.PNG` (패널·버튼) / `References/UI/layout_master.png` (릴 기둥·배치) | `1024x1024` | `medium` | `transparent` | §10 |

**Quality Policy:** 모든 타입이 `medium`이다(2026-09-15, 심볼이 몬스터 아이콘의 그림체를 따르면서 low를 버렸다). 비용을 아끼려면
`--quality low`로 덮어쓴다. `size`는 코드에 하나(`SIZE = 1024x1024`)이고 타입별 선택지가 없다. 출고된 UI 패널·릴 기둥은 도구가
`1536x1024`/`1024x1536`을 쓰던 시절 산출물이다.

**Style reference:** 심볼 두 타입도 몬스터 시트를 스타일 레퍼런스로 붙인다(2026-09-15 사용자 결정). 구도는 각자 §6/§7을 따르고
그림체(가늘고 흔들리는 잉크선, 삐뚤한 덩어리, 점 눈, 평면 채색)는 몬스터 아이콘과 한 벌이다. 옛 심볼 시트 둘은 더 붙이지 않는다.

**Framing:** 심볼 두 타입만 슬롯 심볼 규격(긴 변 70~80%, 사방 10% 여백)으로 후처리한다. `monster_icon`은 아이콘이므로 긴 변
88%까지 허용하고, `ui`는 자르기만 한다. 심볼 전용 크기·구도 규칙을 `monster_icon`에 적용하지 않는다.

레퍼런스 문구(항상 마지막에 붙인다):

```text
use the attached image only as a style reference for line weight, shape
simplification, color range, shading amount and framing. do not copy any character,
object, the grid, the cell borders, the layout or the white background shown in it.
draw the described subject as a new original standalone illustration on a
transparent background
```

## 1. Core Style

- Draw cute simple 2D cartoon game art with a clean flat vector look. Cheerful and friendly.
- Never realistic, 3D, painterly or anime. Never creepy, grotesque, dusty or worn.

## 2. Shape

- Build every form from circles, ovals, capsules and rounded rectangles. Keep total shapes per asset at 8 or fewer, sharp points at 6 or fewer.
- Use chubby friendly proportions: big heads, small features, short plump objects. Symmetrical front view unless the subject has one natural reason to tilt (15-20 degrees or none).
- Never draw a protrusion thinner than 5% of canvas width. Keep the silhouette one connected mass.

## 3. Outline

- Outline every shape in pure black `#000000`. Exterior 2-3% of the canvas short side, interior lines 50-70% of that. Fully opaque, closed contours, rounded caps.
- Draw it as an inked line, not a vector stroke: the weight varies a little from stroke to stroke and the contour wavers as it goes. Never sketchy, broken, doubled or coloured lines, and never a line that loses its direction. A UI panel keeps its shape exactly (§10).

## 4. Palette

- Use 2-4 hues per asset plus the black outline and white highlights. Main fills at saturation 70-100%, value 60-100%. Never below 60% saturation.
- Keep adjacent fills at least 20% apart in value. Symbols sit on a cream reel, so a white fill is fine but never use the cream `#F8F7EE` itself as a main fill.

| Role | Base | Shadow | Light |
|---|---|---|---|
| Ink Black | `#000000` | — | — |
| Paper Cream (UI fill) | `#F8F7EE` | `#EAE5D3` | `#FFFFFF` |
| Paper White | `#FFFFFF` | `#DCDCDC` | — |
| Sun Yellow | `#F8D018` | `#C9A400` | `#FFE96A` |
| Berry Red | `#D81818` | `#A01010` | `#F05050` |
| Orange | `#E88038` | `#B85A20` | `#F6A868` |
| Grape Purple | `#8030B8` | `#562080` | `#B87AE0` |
| Royal Blue | `#0058C0` | `#003C88` | `#4A8CE8` |
| Gem Cyan | `#18C0F8` | `#0E8CC0` | `#7FE0FF` |
| Leaf Green | `#68A040` | `#46702A` | `#9CD070` |
| Coin Gold | `#F2B824` | `#B8860B` | `#FFD966` |
| Wood Brown | `#984810` | `#602800` | `#C07030` |
| Cocoa Hair | `#603820` | `#3E2312` | `#8A5A3C` |
| Bubblegum Pink | `#F090B8` | `#C86490` | `#F7BAD4` |
| Peach Skin | `#F8E0C8` | `#E2B896` | — |
| Tan Skin | `#E8C090` | `#C69A64` | — |
| Steel Grey | `#C8C8D0` | `#8C8C98` | `#ECECF0` |
| Char Black | `#282830` | `#181820` | `#4A4A58` |

## 5. Shading

- Fill flat. One Base per fill, plus at most one hard-edged Shadow tone (20-35% of the fill area) only where a form would read wrong without it. Never gradient, soft, airbrush or glossy.
- No white highlight anywhere: the flat fill and the ink line carry the form. Where a Shadow tone is used it sits away from the upper left, since the light is from there.
- Never bake a ground shadow, drop shadow, glow, sparkle, vignette or background into an asset.

## 6. Item Symbol Composition

- Attach `References/Monsters/monster_icon_master.png` as the style reference. Borrow its line weight, colour, shading amount and drawing feel only; never copy a monster from it, its grid, its cell borders or its white background, and draw an item, not a creature.
- Draw it loose, wonky and a little silly like a quick doodle, nothing like a polished vector mascot: soft rubbery shapes that lean and sag, no mirror symmetry anywhere, a thin black ink outline that is visibly uneven in weight and wavers as it goes.
- Draw one single game item centered on the canvas, the item alone with nothing else: no hands, no character, no face or eyes on the item, no scene.
- Size the item's long edge to 70-80% of the canvas, leave at least 10% empty margin on all four sides, center it within 3% of the canvas center. Never cropped or cramped.
- Use front view or a shallow three-quarter view for boxy objects. Tilt 15-20 degrees or not at all.
- Draw it by hand rather than by a vector tool: let the shape sag, lean or bulge a little, keep the two sides from matching exactly, and let every contour waver as it goes. Nothing is a perfect circle or a mechanically smooth arc. A thin handle, stem or strap bends rather than running straight.
- Keep the silhouette to the item's basic form plus one big feature (a star on the wand, a bolt on the shield, spots on the mushroom). Cap distinct color regions at 7 and never draw a feature smaller than 6% of the canvas. Draw nothing small inside the form: no rivets, no stitching, no grain, no facet lattice.
- Give the item flat fills behind a black ink outline that is fairly thin for its size and uneven in weight. Keep it flat: no gloss, no gradient, no soft shading, no white highlight, no ground shadow, and a darker tone only where a form would be unreadable without one.
- Never add text, letters, numbers, logos, background, grid or frame. Output a transparent PNG with all four corner pixels at alpha 0.

## 7. Character Symbol Composition

- Attach `References/Monsters/monster_icon_master.png` as the style reference. Borrow its head shape, dot eyes, line weight, colour and drawing feel only; never copy a monster from it, its grid, its cell borders or its white background, and draw a person, not a creature.
- Draw it loose, wonky and a little silly like a quick doodle, nothing like a polished vector mascot: soft rubbery shapes that lean and sag, no mirror symmetry anywhere, a thin black ink outline that is visibly uneven in weight and wavers as it goes.
- Draw one cute cartoon character head only: face, hair and one headwear or accessory, cut off at the chin. No neck, no shoulders, no body, no hands.
- Make the head one big round or oval mass framed by the hair or hat, drawn by hand rather than by a vector tool: let it lean or bulge a little, keep the two sides from matching exactly, and let every contour waver as it goes. Nothing is a perfect circle or a mechanically smooth arc.
- Two small black dot eyes set wide and low, one a touch bigger or higher than the other, a tiny curved mouth, no nose or a single dot, no blush, no sparkle or white in the eyes, no eyelashes.
- Use a smile or a neutral face by default; change it with one element only (drooping eyes, a mask) when the concept asks.
- Allow at most one headwear or accessory: hat, horns, halo, glasses, goggles, flower, earring or antlers. Never stack two.
- Make the character identifiable by hair color plus accessory silhouette. Never rely on facial features to tell characters apart.
- Front view or a very shallow three-quarter, no tilt. Size the head's long edge to 70-80% of the canvas, leave at least 10% empty margin on all four sides, center it within 3%.
- Flat fills behind a black ink outline that is fairly thin for its size and uneven in weight. Keep it flat: no gloss, no gradient, no soft shading, no white highlight on the hair, and a darker tone only where a form would be unreadable without one. Draw nothing small inside the head: no strand lines in the hair, no inner ear lines, no stitching on a hat. Skin is flat Peach or Tan unless the concept is a green, purple or pale creature.
- Never add text, background, grid, frame or a body. Output a transparent PNG with all four corner pixels at alpha 0.

## 8. Forbidden Elements

- Rendering: realistic, photorealistic, photograph, 3D render, CGI, ray tracing, PBR, clay, painterly, oil painting, watercolor, brush strokes, soft shading, airbrush, gradients, bloom, lens flare.
- Surface: texture, grain, noise, halftone, paper texture, grunge, scratches, worn, dusty, glossy, chrome, glass reflection, metallic shine.
- Color: muted, desaturated, pastel, neon, glowing edges, sparkles, glitter, holographic.
- Form and framing: intricate, ornate, many small parts, multiple objects, cropped subject, off-center, a body or bust on a character symbol, a face on an item, ground shadow, drop shadow, scenery, background, frame, border box, grid.
- Screen junk: text, letters, numbers, logo, watermark, signature, speech bubble.
- IP and quality spam: any title, studio, artist or character name, "in the style of", 8k, highly detailed, masterpiece, award winning, trending on artstation.

Paste this as the AVOID block of every prompt:

```text
avoid: realistic, photorealistic, 3D render, CGI, painterly, watercolor, oil painting,
soft shading, gradients, airbrush, texture, grain, noise, muted colors, pastel, neon
glow, chrome, sparkles, drop shadow, ground shadow, scenery, background, grid, frame,
border box, text, letters, numbers, watermark, cropped subject, extra objects, tiny
details
```

## 9. Quick Shared Style Rule

Prepend verbatim to every prompt.

```text
Style: cute simple 2D cartoon game art, cheerful and friendly, hand drawn and a
little wonky rather than a clean vector look.
Shapes: big simple rounded shapes, low detail, chubby friendly proportions, front view,
drawn loose, leaning or sagging a little and never mirror-perfect.
Linework: thin pure black outline on every shape, inked by hand so its weight varies
visibly and the contour wavers as it goes, closed contours, no sketchy or broken lines.
Color: flat solid fills in bright clear saturated colors, two to four colors per
subject. No gradients, no muted or pastel tones, no neon.
Shading: flat color, no highlight at all, and at most one hard-edged darker tone where
a form would be unreadable without it. Light from the upper left.
Readability: one subject, centered, big and simple enough to stay clear at 128x128
pixels.
Background: fully transparent. No scenery, no frame, no grid, no drop shadow, no glow.
Never: realistic, 3D render, painterly, soft brush shading, texture, grain, text,
watermark.
```

## 10. UI Composition

- Attach `References/UI/panel_master.PNG` for a panel or button and `References/UI/layout_master.png` for a reel column. Borrow the cream fill, the ink outline weight and the corner radius only; never copy the five-column layout into one asset.
- Draw one flat cream colored `#F8F7EE` rounded rectangle with a thick black ink outline at 2.5-3% of its short side, corner radius 10-12% of the short side. The waver the shared rule asks for is limited here to line weight alone: the rectangle stays a true rectangle with equal corners and parallel sides, because a nine-slice stretches it. Never lean, sag or bulge a panel.
- Keep the whole interior one flat color for nine-slice: no decoration, no icons, no text, no numbers, no gradient, no paper texture, no inner shadow.
- Panel or button: canvas `1536x1024`, the panel centered and 90% of the canvas width, cropped to content afterwards. Reel column: canvas `1024x1536`, one tall column centered at 70-80% of the canvas height. Never draw the full screen layout.
- Pressed and Disabled are made locally, not generated: Pressed fill `#EAE5D3`, Disabled outline `#8C8C98`.
- Output a transparent PNG; the screen background is painted by Unity.

## 11. Monster Icon Composition

- Attach `References/Monsters/monster_icon_master.png` as the style reference. Borrow its line weight, flat colour, shading amount and framing only; never copy a monster from it, its grid, its cell borders or its white background.
- Draw one cute fantasy monster: its head and face. A neck or a little of the shoulders is allowed where the creature needs it, and nothing below that. This is a standalone icon, not a slot symbol, and the reel-cell sizing of the symbol types does not apply.
- Use a front view or a shallow three-quarter. Keep the whole creature one simple readable mass: one big head shape plus at most two features that name it, such as horns, ears, a single eye, fangs, a cap or a fin.
- Draw it loose, wonky and a little silly, nothing like a polished vector mascot. Build the creature from soft rubbery shapes that bend and sag, let the whole head lean, tilt or bulge to one side, and keep any horn, ear, arm or antenna a thin bendy noodle rather than a solid tapered cone. Every line should look drawn by hand in one pass: no mirror symmetry anywhere, no mechanically smooth curve, and a contour that wavers slightly as it goes.
- Make the head a plain simple blob or bean that reads in one glance, and keep the features small on it. Do not add fur tufts, scale rows, feather layers, inner ear lines, cheek blush or any other small detail: what names the creature is its outer shape and at most two features, nothing drawn inside the body.
- Keep eyes and mouth as simple as a doodle: two small solid black dots set wide apart and low on the head, one a touch bigger or higher than the other, with no white catchlight, no eyelashes and no eyelids. One plain mouth that is a short curved line, or an open shape with nothing in it but a flat tongue. One pair of small fangs is allowed where the creature calls for it. Friendly, curious or a bit dopey. Never frightening or gory: no blood, no wounds, no rot, no realistic teeth, no empty staring sockets.
- Fill flat in bright clear colour behind a black ink outline that is fairly thin for its size and visibly uneven in weight, thicker on one stroke than the next. Keep it completely flat: no gloss, no gradient, no soft shading, no white highlight, and no darker tone at all unless a form is unreadable without one. At most one simple pattern of a few large spots or stripes, drawn as loosely as the rest.
- Size the monster's long edge to 75-90% of the canvas, centred, with clear margin on all four sides so nothing is cropped.
- Never add a background, scenery, a ground shadow, a frame, a grid, text or a second creature. Output a transparent PNG with all four corner pixels at alpha 0.
