# F1 Style Runtime — Round 99 시험 문서 (대화 10: 대화 9의 얼굴, 대화 7의 머리색과 복장)

이미지를 생성할 때 스크립트(`tools/gen_image.py`)가 읽는 스타일 규칙이다. 스타일 문구는 이 파일에만 둔다.
2026-10-04 그림체 전환(Round 12): 캐릭터·적의 그림체는 사용자가 든 포스터의 손맛(손으로 그린 흔들리는 잉크선, 낙서 획, 고무처럼 부푼 과장, 캐릭터마다 다른 체형과 매력적인 캐리커처 얼굴)에
지금의 평면·낮은 채도 색을 그대로 얹은 것이다. 이전 문서는 `Archive/flat-v1/`. 아이템·배경·UI의 절(§7~§22)도 같은 날 선(손으로 그린 잉크선, 낙서 획)과 기준 그림(확정 셋의 시트)을 새 그림체로 맞췄다. 색·구도·크기 규칙은 그대로다.
방향과 기준 그림은 [`Docs/Design/10_Art_Direction.md`](../Docs/Design/10_Art_Direction.md), 파이프라인의 규칙은
[`Docs/Architecture/13_ART_PIPELINE.md`](../Docs/Architecture/13_ART_PIPELINE.md)가 소유한다.

**조립 순서:** 타입의 Style Rule → Subject → 타입 섹션 → (던전의 것이면) 던전 컨셉 → 타입의 Forbidden → 타입의 Reference Rule
**형식 계약:** 번호 섹션의 제목은 `## <번호>. <제목>`. §1, §2와 Reference Rule 섹션에는 ```` ```text ```` 블록이 하나씩 있고, 타입 섹션은 `- ` 불릿으로 쓴다.
던전 컨셉 섹션의 제목은 `## Dungeon: <DungeonData.Id>`이고, 그 안의 `### Creatures`(적)와 `### Place`(배경) 아래에 `- ` 불릿으로 쓴다.
저장한 체형 섹션의 제목은 `## Body: <이름>`이고 그 아래에 `- ` 불릿으로 쓴다(2026-10-14). 소재의 `Body` 열이 이름을 가리키면 그 문구가 Subject 다음에 "Body type:"으로 붙는다.
체형은 `--style`로 다른 문서를 쓸 때도 이 문서에서 읽는다(저장한 체형은 한 곳에 둔다). `character`, `attack`, `hit`만 쓴다.
저장한 얼굴 기준 섹션의 제목은 `## Face: <이름>`이고, `### Rules`(얼굴, `- ` 불릿), `### Lift`(그 얼굴과 부딪혀 Style Rule·Forbidden에서 빼는 구절, 글자 그대로), `### Avoid`(Forbidden에 더하는 말)를 둔다(2026-10-14).
소재의 `Face` 열이 이름을 가리키면 §4의 "Face:" 규칙 대신 그 Rules가 "Face:"로 붙는다. 얼굴 기준도 이 문서에서만 읽고 `character`만 쓴다(자세는 붙인 대기 그림의 얼굴을 따른다).
번호를 바꾸면 `gen_image.py`의 섹션 번호도 함께 바꾼다.
오려 낸 그림(`character`, `enemy`, `item`)은 §1과 §2를, 배경은 자기 것(§10, §12)을, UI는 자기 것(§14, §17)을 Style Rule과 Forbidden으로 쓴다:
배경은 불투명하고 장면을 그리기 때문이고, UI는 인물이 아니라 정면에서 본 납작한 조각을 그리기 때문이다.

## Generation Types

| 타입 | 그리는 것 | 기준 그림 | size | quality | background | 타입 섹션 | Reference Rule | 후처리 |
|---|---|---|---|---|---|---|---|---|
| `character` | 직업의 전신 그림. 오른쪽을 보고 눈도 오른쪽(적)을 본다. 체형·얼굴·자세는 소재가 캐릭터마다 적는다 | `References/Character/style_ref_roster.png` (확정한 직업 셋의 시트. 그대로 붙인다) | `1024x1024` | `medium` | `transparent` | §4 | §3 | `figure` |
| `enemy` | 적의 전신 그림. 왼쪽을 보고 눈도 왼쪽(파티)을 본다. 던전 컨셉이 붙는다 | `References/Character/style_ref_roster.png` (좌우를 뒤집어 붙인다) | `1024x1024` | `medium` | `transparent` | §5 | §6 | `figure` |
| `background` | 화면의 배경. 불투명, 인물 없음. 던전의 배경이면 던전 컨셉이 붙는다 | `References/Character/style_ref_roster.png` (그대로 붙인다) | `2304x1536` | `medium` | `opaque` | §11 | §13 | `scene` |
| `item` | 아이템의 아이콘. 그 아이템이 차지하는 칸의 모양으로 그린다 | 그 아이템을 든 유닛의 확정된 그림(Roster의 `Reference`). 없으면 `References/Character/style_ref_roster.png` | 칸 수에 따라 `1536x512`(1칸), `1536x1024`(2칸), `1024x1024`(3칸) | `medium` | `transparent` | §7과 칸 수의 섹션(§19, §20, §21) | 든 유닛의 그림이면 §9, 아니면 §8 | `cell` |
| `ui_frame` | UI의 틀(패널, 명패, 칸, 버튼). 늘여 쓰는 빈 사각형 | `References/Character/style_ref_roster.png` (그대로 붙인다) | Roster의 `Size` 비율에 가까운 것 (`1024x1024`, `1536x1024`, `1536x768`, `1536x512`) | `medium` | `transparent` | §15 | §18 | `frame` |
| `ui_icon` | UI의 작은 기호(상태 아이콘) | `References/Character/style_ref_roster.png` (그대로 붙인다) | `1024x1024` | `medium` | `transparent` | §16 | §18 | `glyph` |
| `prop` | 무대 바닥에 서는 소품: 쓰러진 용병의 무덤(2026-10-04 Round 21). 전신 그림처럼 바닥선에 세운다. §23은 지금 하나뿐인 무덤에 맞춰 적었다 | `References/Character/style_ref_roster.png` (그대로 붙인다) | `1024x1024` | `medium` | `transparent` | §23 | §24 | `figure` |
| `attack` | 확정한 용병 그림의 공격 자세: 무기를 휘둘러 돌진하는 순간(Round 23의 규칙). 손에 든 것은 그대로, 무기는 바닥선 아래로 내려가도 된다 | 그 직업의 확정 원본(`output/character/<key>.raw.png`)을 1536x1024에 앉힌 것: 발바닥이 높이의 3/4, 왼쪽 3분의 1에(gen_image.py가 만든다) | `1536x1024` | `medium` | `transparent` | §25 | §26 | `pose` (`tools/fit_pose.py`) |
| `hit` | 확정한 용병 그림의 피격 자세: 맞아서 뒤로 밀리는 순간 | 같은 원본을 가운데에 | `1536x1024` | `medium` | `transparent` | §25 | §26 | `pose` (`tools/fit_pose.py`) |
| `enemy_attack` | 확정한 몬스터 그림의 공격 자세: 무기를 휘둘러 왼쪽(파티)으로 돌진하는 순간(2026-10-05 Round 26~28의 규칙). 눈동자 없는 눈과 장비는 그대로, 무기는 바닥선 아래로 내려가도 된다 | 그 몬스터의 확정 원본(`output/enemy/<key>.raw.png`, 눈동자를 지운 것)을 1536x1024에 앉힌 것: 발바닥이 높이의 3/4, 오른쪽 3분의 1에(gen_image.py가 만든다) | `1536x1024` | `medium` | `transparent` | §27 | §28 | `pose` (`tools/fit_pose.py`) |
| `enemy_hit` | 확정한 몬스터 그림의 피격 자세: 맞아서 오른쪽으로 밀리는 순간 | 같은 원본을 가운데에 | `1536x1024` | `medium` | `transparent` | §27 | §28 | `pose` (`tools/fit_pose.py`) |
| `building` | 로비 마을의 건물 한 채(2026-10-17 Round 78, 30단계): 막사·쉼터·역마차·성문의 레벨 1·2·3과 공용 비계. 마을 그림과 같은 내려다본 시점, 밤, 투명 배경. 레벨 2·3는 그 건물의 레벨 1 결과를 둘째 기준 그림으로 붙인다(`--extra-reference`) | `References/Character/style_ref_roster.png` (그대로 붙인다) + 레벨 2·3는 그 건물의 낮은 레벨 그림 | `1536x1024` | `medium` | `transparent` | §29 | §30 | `trim` (투명 여백만 잘라 낸다) |

## 1. Shared Style Rule

Round 99 시험(대화 6): 올린 일러스트의 결(가는 선, 부드러운 셀 명암)에 가깝게. 지금 그림체와 다르다.

```text
Style: a clean 2D illustration of an attractive young woman for the dialogue box of a
dark fantasy game, in a modern webtoon style.
Linework: clean, fine dark outlines of even weight, closed contours, thin lines for the
hair strands.
Color: flat fills with soft cel shading, natural skin tones, glossy black hair.
Detail: moderate; the face is the focus.
Background: fully transparent. No scenery, no frame, no shadow.
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
figure
```

## 3. Reference Rule

Round 99 시험(대화 10: 대화 9의 얼굴, 대화 7의 머리색과 복장).

```text
Two images are attached. The first image is the woman to draw: keep her face, her
expression, her makeup, the shape of her bob and bangs and the drawing style of the
first image exactly as they are. From the second image take only her hair colour
(plain glossy black with no violet streaks), her outfit (the black ribbon choker,
the strapless black dress with the side slit, the leather belt with the round brass
buckle, the ink pot and the quill case), the leather-bound ledger and the way she
holds it against her chest. Take nothing else from the second image: not its face,
not its expression and not its drawing style. Draw the woman of the first image in
that outfit, front-facing, alone on a transparent background.
```

## 4. Character

Round 99 시험 문서: 리넷의 **정면 일러스트**(머리부터 엉덩이까지, 보는 사람을 본다). 이 문서는 `Archive/99-linette/`에만 있다.

- Draw one woman alone in a front view, from the top of her head down to her hips: her face, shoulders and chest square to the viewer, standing upright, as the subject says.
- Proportions and build: our rubbery cartoon shapes with a slightly big head, as on the reference sheet. A woman keeps a curvy figure (a full bust, a narrow waist, full hips). Rubbery limbs with no muscles drawn inside, big hands.
- Face: attractive, as the subject says, drawn large enough to read clearly, with the expression the subject gives.
- Gaze: the eyes look straight at the viewer.
- The eyes always have their dark pupils: never dot eyes, never blank eyes without pupils.
- Hair: big rubbery clumps with a few scribbly strand strokes.
- Clothes and gear as a few big flat shapes that fit the figure, three or four simple ornaments at most.
- The figure ends at the hips with a clean edge. Keep the head and the hair inside the canvas with a clear empty margin above and at the sides. Never crop the head.
- Draw one character only: no second figure, no animal, no scenery and no shadow. Never add text, letters, numbers or logos. Output a transparent PNG with all four corner pixels at alpha 0.

## 5. Enemy

- Draw one enemy creature alone, the whole body, standing or crouching, with every limb, the tail and the weapon inside the canvas.
- Turn the creature toward the left side of the image: a three-quarter view with the head and the chest facing the viewer's left, where the party stands. Its eyes have no pupils, so the head and the body show where it looks.
- Proportions and build: exaggerated and rubbery, as the subject says. A goblin is a short, hunched humanoid about three heads tall with a big head, long pointed ears, long arms, big hands and big bare feet. A beast keeps its animal build, drawn chunky and simple. Every creature has its own body type.
- Face: caricatured and expressive, full of personality: large blank eyes with no pupils, each filled with one flat colour and nothing dark inside it (one may be bigger than the other), a big nose or snout, and a wide mouth with a few plain crooked teeth or fangs, in a sneer, a grin or a scowl. Menacing in a goofy way, never cute and never gory: no blood, no wounds.
- Draw it in the same hand as the mercenaries: the wobbly ink line, scribbly marks for fur, dirt and stubble, big plain shapes, crude gear drawn as plain shapes, nothing small inside the body beyond the marks.
- Keep the whole figure inside the canvas with a clear empty margin on all four sides. Never crop the head, the feet, the tail or the weapon.
- Draw one creature only: no second figure, no scenery, no ground line and no shadow under the feet.
- Never add text, letters, numbers or logos. Output a transparent PNG with all four corner pixels at alpha 0.

## 6. Enemy Reference Rule

The last part of an `enemy` prompt.

```text
The attached image is a sheet of three mercenaries of our game, already drawn in
our style and mirrored so that they face left. Use it only as a style reference:
the hand-drawn wobbly ink line, the scribbly small marks, the rubbery exaggerated
shapes, the flat muted coloring and the expressive faces. Do not copy any of the
three: do not give the creature their faces, hair, outfits or weapons, and do not
reproduce the white background. Their eyes have dark pupils; the creature's eyes
have none: they are blank, filled with one flat colour. Draw the described
creature as a new, original figure alone, facing the viewer's left, on a
transparent background.
```

## 7. Item

What holds for an item of any size. The shape of the picture and how the item lies in it come from the section of its size (§19, §20, §21).

- Draw one item alone: no hand, no character, no creature and no scene.
- It is an icon shown small, inside a dark slot of the game's interface. Keep to the item's basic silhouette and one or two big features, in two to four flat colors. Nothing thin and nothing small: no engraving, no rivets, no fine string, no tiny gems.
- Draw a long or thin item thicker and shorter than it really is, so that it stays readable when it is small.
- Use mid-tone and light fills that stay readable on a dark background, and keep the darkest tone to the outline.
- Give it the same hand-drawn ink outline as the figures, its weight varying and the contour wobbling a little, a little thicker for its size, with a few scribbly marks for grain or wear.
- Keep the whole item inside the canvas with a clear empty margin on all four sides.
- Never add text, letters, numbers, a frame, a plate, a background, a ground shadow or a glow. Output a transparent PNG with all four corner pixels at alpha 0.

## 8. Item Reference Rule

The last part of an `item` prompt when the reference is the style reference.

```text
The attached image is a sheet of three characters of our game, drawn in our style.
Use it only as a style reference for the hand-drawn wobbly ink line, the scribbly
marks, the shape simplification, the color range and the shading amount. Do not draw
any character, any part of a body or a hand, and do not copy the things they carry.
Draw only the described item, as a new standalone icon in the same drawing style on a
transparent background, and do not reproduce the sheet's white background.
```

## 9. Held Item Reference Rule

The last part of an `item` prompt when the reference is the figure of the unit that holds the item.

```text
The attached image shows a character of this game holding the item. Draw that same
item alone as an icon: keep its shape, its parts and its colors as the character
holds it, simplified to read at a small size. Do not draw the character, any part of
a body or a hand, and do not reproduce the pose. Use the same drawing style: line
weight, flat colors and shading amount. Transparent background.
```

## 10. Background Style Rule

Prepend verbatim to a `background` prompt, in place of §1.

```text
Style: flat 2D cartoon background art for a fantasy mercenary game, drawn in the same
hand as its characters: loose, energetic, exaggerated cartoon shapes, bold and
simplified. Calm and grounded. Not realistic.
Linework: a dark, medium-thick ink outline around the big shapes that is visibly hand
drawn, its weight varying and the contour wobbling a little, thinner lines inside
them, closed contours, with a few scribbly marks for grain, cracks and rubble. No
clean vector line, no sketchy or broken lines.
Color: flat solid fills in muted earthy colors. No gradients, no neon, no pastel.
Shading: flat color with hard-edged darker and lighter tones. Light is drawn as flat
shapes with hard edges, never as a soft glow.
Depth: what is farther away is drawn darker, greyer and plainer.
Tone: darker and lower in contrast than the characters who will stand in front of it,
so that they stand out. The brightest spots are small.
```

## 11. Background

- Draw an empty place seen straight from the side, like a stage. It runs from left to right across the image, not away into the distance. No character, no creature and no figure of any kind.
- A level floor fills the lower half of the image and the back wall of the place rises behind it. The line where the wall meets the floor runs straight and level across the whole width, a little above the middle of the image.
- The characters will stand in a row on the floor, in the middle band of the image. Keep that band calm: no large object and no bright object in it.
- Give the back wall a rhythm: the same structure repeated at even intervals across the width, as a corridor repeats its arches. Put a few small lights on it, each with a small flat pool of light around it.
- Frame the scene with larger, darker things at the left and right edges and in the two lower corners, close to the viewer and drawn as near-silhouettes.
- Keep the floor plain. Panels will cover much of it.
- Make the image darker toward its edges and corners, with flat darker shapes rather than a soft fade.
- The image is opaque and fills the whole canvas from edge to edge. Never add text, letters, numbers, a frame, a border or a logo.

## 12. Background Forbidden

Paste as the AVOID block of a `background` prompt, in place of §2.

```text
avoid: characters, people, creatures, animals, faces, a view down a corridor into
the distance, a vanishing point, realistic, photorealistic, 3D render, CGI, painterly,
watercolor, oil painting, soft shading, gradients, airbrush, texture, grain, glossy
highlights, neon, glow, bloom, lens flare, sparkles, fog, text, letters, numbers,
watermark, frame, border, user interface
```

## 13. Background Reference Rule

The last part of a `background` prompt.

```text
The attached image is a sheet of three characters of our game, drawn in our style;
they will later stand in front of this background. Use it only as a style reference
for the hand-drawn wobbly ink line, the scribbly marks, the flat coloring and the
shading amount. Do not draw them or any other character, creature or figure, and do
not reproduce the sheet's white background. Draw the described place as an empty
scene in the same drawing style, filling the whole canvas.
```

## 14. UI Style Rule

Prepend verbatim to a `ui_frame` or `ui_icon` prompt, in place of §1.

```text
Style: one piece of the user interface of a fantasy mercenary game, drawn in the same
hand as its characters: flat 2D cartoon art with loose, bold, simplified shapes. Calm,
worn and well made, like the gear of a mercenary company: leather, iron and brass.
Linework: a dark, medium-thick ink outline around the piece that is visibly hand
drawn, its weight varying and the contour wobbling a little, thinner lines inside it,
closed contours, with a few scribbly marks for grain, scratches and stitches. No clean
vector line, no sketchy or broken lines.
Color: flat solid fills in muted colors: dark navy, deep red, cream, dull brass,
brown and grey. No gradients, no neon, no pastel.
Shading: flat color, with at most one hard-edged darker tone. No soft shading.
Material: where a material shows (leather grain, hammered iron, aged brass), it is
drawn as a few simple flat shapes in two close tones, even all over. Never a
photo-like texture.
View: seen exactly from the front, like a paper cut-out lying flat. No perspective,
no depth and no thickness.
```

## 15. UI Frame

- Draw one empty frame alone: a flat rectangle with slightly rounded corners, a plate of worked material (leather, iron or brass-bound wood) as the subject says. Nothing else is in the picture.
- It fills the picture: its sides run parallel to the sides of the picture, with a small even margin of empty space around it. It is not tilted and not seen from an angle.
- All four sides are straight and all four corners are alike: the left half mirrors the right half and the top half mirrors the bottom half.
- The border is the same on all four sides and narrow: a band of the material with its small fittings (rivets, studs, stitches or a brass fillet) spaced evenly, and one small matching ornament in each corner (a rivet, a stud or a corner cap) that stays inside the corner. The inside takes up most of the frame.
- The inside is one flat color. It may carry a faint, even, flat two-tone grain of the material (leather or hammered metal as simple shapes), the same everywhere, because the frame is stretched on screen. Nothing is written or drawn in it and no picture or emblem is in it.
- Never add text, letters, numbers, icons, symbols, a shadow under it or a glow around it. Output a transparent PNG with all four corner pixels at alpha 0.

## 16. UI Icon

- Draw one simple symbol alone, large and centered: no frame, no plate, no badge and no scene behind it.
- It is shown about 24 pixels high. Keep to one clear silhouette with one or two big features, in two or three flat colors. Nothing thin and nothing small.
- Use mid-tone and light fills that stay readable on a dark panel, and keep the darkest tone to the outline.
- Give it the same hand-drawn ink outline as the figures, its weight varying and the contour wobbling a little, a little thicker for its size, with a few scribbly marks for grain or wear.
- Keep the whole symbol inside the canvas with a clear empty margin on all four sides.
- Never add text, letters, numbers, a ground shadow or a glow. Output a transparent PNG with all four corner pixels at alpha 0.

## 17. UI Forbidden

Paste as the AVOID block of a `ui_frame` or `ui_icon` prompt, in place of §2.

```text
avoid: perspective, a tilted or angled view, 3D render, bevel, emboss, thickness,
drop shadow, cast shadow, glow, bloom, gradients, soft shading, airbrush, glossy
highlights, photo-like or realistic textures, cracks, scratches, scrollwork, filigree,
gems, an emblem or a picture inside the frame, text, letters, numbers, watermark,
a background, a scene, a character, a hand, more than one object
```

## 18. UI Reference Rule

The last part of a `ui_frame` or `ui_icon` prompt.

```text
The attached image is a sheet of three characters of our game, drawn in our style.
Use it only as a style reference for the hand-drawn wobbly ink line, the scribbly
marks, the flat coloring and the color range. Do not draw them, any part of a body,
their clothes or the things they carry, and do not reproduce the sheet's white
background. Draw only the described piece of the user interface, alone, in the same
drawing style on a transparent background.
```

## 19. Item Shape: One Cell

Added to §7 for an item whose piece on the board is a wide strip (three squares or two side by side).

- The picture is a wide strip, a little over three times as wide as it is tall. The item fills the strip from its left end to its right end.
- Lay a long item (a sword, an axe, a mace, a staff, a bow) flat and level along the strip: its grip or lower end at the left, its point, blade or head at the right. It is not tilted.
- Lay a compact item (a shield, a flask, a pouch, a charm) on its side, or spread what belongs to it (a cord, a strap, leaves) out to the left and to the right, so that it is clearly wider than it is tall.

## 20. Item Shape: Two Cells

Added to §7 for an item whose piece on the board is a wide rectangle (three squares wide, two tall).

- The picture is a wide rectangle, about three units wide to two tall. The item fills it from corner to corner.
- Lay a long item (a bow, a spear) diagonally across it: its grip or lower end at the bottom left, its point or head at the top right. Draw it as long and as slender as it really is.

## 21. Item Shape: Three Cells

Added to §7 for an item whose piece on the board is nearly a square (one square, two by two, or two wide and three tall).

- The picture is nearly square, a little taller than it is wide (about fifteen to sixteen). The item fills it from corner to corner.
- Lay a long item (a halberd, a pike) diagonally across it: its grip or lower end at the bottom left, its point or head at the top right. Draw it as long and as slender as it really is.

## 22. UI Piece

For a decorative piece of the interface that is shown whole, never stretched: the dial of the storm clock.

- Draw one piece alone, whole and centered, seen exactly from the front like a paper cut-out lying flat. Nothing else is in the picture.
- It fills the picture with a small even margin of empty space around it.
- It is made of the same materials as the frames (brass, iron, leather, dark navy) in a few flat shapes and two or three colors, with the same bold dark outline, and its fittings (rivets, ticks, a rim) are even and symmetrical.
- Its face is empty where the subject says so: numbers are written on it by the game.
- Never add text, letters, numbers, a shadow under it or a glow around it. Output a transparent PNG with all four corner pixels at alpha 0.

## 23. Prop

- Draw one prop alone, standing on the floor of the battle stage: the whole prop from its top down to the ground it stands in, seen from the side in a slight three-quarter view like the figures of our game.
- It is the grave marker of a fallen mercenary of the party: a plain, solemn thing in a dark dungeon. Never cute, never comic, no skull, no bones, no gore, no blood.
- Keep it simple and readable when small: one strong silhouette, a few big shapes and two to four flat muted colors. A small low mound of earth or stones at its foot is part of it.
- Keep the whole prop inside the canvas with a clear empty margin on all four sides. Never crop it.
- Draw no character, no creature, no hand, no scenery beyond the mound at its foot, no shadow under it, no glow, no candles and no flowers.
- Never add text, letters, numbers, names, R.I.P. or logos. Output a transparent PNG with all four corner pixels at alpha 0.

## 24. Prop Reference Rule

The last part of a `prop` prompt.

```text
The attached image is a sheet of three characters of our game, drawn in our style.
Use it only as a style reference for the hand-drawn wobbly ink line, the scribbly
marks, the shape simplification, the flat muted color range and the shading
amount. Do not draw any character or any part of one. Draw only the described
prop, as a new standalone picture in the same drawing style on a transparent
background, and do not reproduce the sheet's white background.
```

## 25. Pose

For an attack or a hit pose of an approved mercenary, drawn after its battle-ready figure (Docs/Design/10 §2, §5).

- Draw the one character alone, the whole body from the top of the head to the boots, in the action pose the subject describes, in a three-quarter view turned toward the viewer's right, where the enemy stands.
- Keep the proportions and the build of the attached image exactly: only the limbs, the torso, the head and the weapon move. The hands grip the weapon as the subject says.
- Hands: when the attached figure holds a different thing in each hand (a weapon and a shield), each stays where it is whatever the pose; the subject names them. The arm that holds the weapon makes the move, the other arm keeps its own thing.
- Face: the same face as in the attached image, with the expression the subject asks for. The eyes keep their dark pupils unless the subject closes an eye: never dot eyes, never blank eyes without pupils.
- Gaze: the head and the eyes stay turned toward the viewer's right, toward the enemy. Never looking at the viewer, never looking left.
- The hair, the cloak and loose cloth may swing with the motion as the subject says, but keep their shapes, lengths and colors.
- The boots stand on the floor line where the boots are in the attached image; below that line is open space in front of the character. The weapon may come down below the floor line, in front of the feet, as far as the pose asks: no ground is drawn and nothing hides it, the whole weapon stays visible. The whole figure and the whole weapon stay inside the canvas with an empty margin on all four sides. Never crop the head, the feet or the weapon.
- Draw one character only: no second figure, no enemy, no impact effect, no blood, no scenery, no ground line and no shadow under the feet. Never add text, letters, numbers or logos. Output a transparent PNG with all four corner pixels at alpha 0.
- Never a smile, a grin or a cheerful face: the attack is a fierce war cry and the hit a pained grimace. Never a smaller head than in the attached image, never a redesigned character, another outfit or another weapon.

## 26. Pose Reference Rule

The last part of an `attack` or `hit` prompt. The attached image is the mercenary's approved figure, laid on the canvas with open floor under the feet (gen_image.py).

```text
The attached image is this exact character of our game, already drawn in our
style and approved. Redraw the very same character in the new pose the subject
asks for. Keep everything else exactly as in the attached image: the face and
its features, the hair, the body type and proportions, every piece of the outfit
and its ornaments, the weapon, every color, and the size the character is drawn
at (the same head size and the same body size) and the place it stands in: the
soles of the boots on the floor line where they are in the attached image. Keep
the same hand: the wobbly
hand-drawn ink line, the scribbly marks and the flat coloring with its one hard
darker tone. Change only the pose and the expression the subject asks for: the
same person in the same clothes with the same weapon, not a new design. Each
hand that holds a different thing in the attached image keeps it: a weapon in
one hand and a shield on the other arm stay in the same hand and on the same
arm; they never change hands. The
character faces toward the viewer's right, where the enemy stands. Draw one
figure only, alone, on a transparent background, and do not reproduce the
background of the attached image.
```

## 27. Enemy Pose

For an attack or a hit pose of an approved monster, drawn after its figure (Docs/Design/10 §5; rounds 26 to 28). The dungeon's concept is not added to these prompts: the approved figure carries it, and the pose keeps that figure's gear.

- Draw the one creature alone, the whole body from the top of the head and its gear to the feet and the tail, in the action pose the subject describes, in a three-quarter view turned toward the viewer's left, where the party stands.
- Keep the proportions and the build of the attached image exactly: only the limbs, the torso, the head, the tail and the weapon move. If it holds a weapon, its hands grip it as the subject says.
- Face: the same face as in the attached image, with the expression the subject asks for: menacing in a goofy way, never cute and never gory. The eyes stay blank as in the attached image: each is filled with the same one flat colour, with no pupil and nothing dark inside it, unless the subject squeezes an eye shut.
- Facing: the head and the chest stay turned toward the viewer's left, toward the party. The eyes have no pupils, so the head and the body show where it looks. Never turned to the viewer, never turned right.
- The ears, the hair, the tail, loose leather, cloth, rope and hanging gear may swing with the motion as the subject says, but keep their shapes, lengths and colors. The creature keeps the gear of the attached image as the subject lists it, and nothing more.
- The feet, the paws or the boots stand on the floor line where they are in the attached image; below that line is open space in front of the creature. A weapon may come down below the floor line, in front of the feet, as far as the pose asks: no ground is drawn and nothing hides it, the whole weapon stays visible. The whole figure and the whole weapon stay inside the canvas with an empty margin on all four sides. Never crop the head, the feet, the tail or the weapon.
- Draw one creature only: no second figure, no mercenary, no impact effect, no arrow in flight, no blood, no wound, no scenery, no ground line and no shadow under the feet. Never add text, letters, numbers or logos. Output a transparent PNG with all four corner pixels at alpha 0.
- Never a smaller head than in the attached image, never a redesigned creature, other gear or another weapon, never pupils, irises or dark dots in the eyes.

## 28. Enemy Pose Reference Rule

The last part of an `enemy_attack` or `enemy_hit` prompt. The attached image is the monster's approved figure, laid on the canvas with open floor under the feet (gen_image.py).

```text
The attached image is this exact creature of our game, already drawn in our
style and approved. Redraw the very same creature in the new pose the subject
asks for. Keep everything else exactly as in the attached image: the face and
its features, the ears, the body type and proportions, every piece of its gear
and clothing, the weapon, every color, and the size the creature is drawn at
(the same head size and the same body size) and the place it stands in: the
feet, paws or boots on the floor line where they are in the attached image. Its
eyes stay blank as in the attached image, filled with one flat colour, with no
pupils. Keep the same hand: the wobbly hand-drawn ink line, the scribbly marks
and the flat coloring with its one hard darker tone. Change only the pose and
the expression the subject asks for: the same creature with the same gear and
the same weapon, not a new design. The creature faces toward the viewer's left,
where the party stands. Draw one figure only, alone, on a transparent
background, and do not reproduce the background of the attached image.
```

## 29. Building

For one building of the company's town (Round 78, 2026-10-17): the barracks, the inn, the stagecoach and the gate, each at three levels, and the shared scaffold. The town background is drawn separately and the buildings stand on it as cut-out pictures, so every building is drawn alone on a transparent background, from the same viewpoint and under the same night light as the town painting.

- Draw one building alone, the whole of it from the top of its roof, chimney, flag or banner down to the ground it stands on, seen from a raised three-quarter view a little from above, as if from a balcony: the viewer looks down at an angle of about thirty degrees, so the roof and the front wall are both visible. The front of the building faces the lower left of the image.
- It is night: the walls and the roof are dark muted colours, and the building has a few small warm window lights and one or two lanterns, each with a small flat pool of light around it. These are the brightest spots and they are small. Nothing glows.
- The building stands on a small flat patch of trodden earth of its own that is part of the picture: a little ground around the foot of the walls, a step, a fence post or a barrel may stand on it. Nothing else: no road, no hills, no sky, no neighbour, no tree beyond a bush at a wall.
- Draw it in the same hand as the town: the wobbly hand-drawn ink line around the big shapes, thinner lines inside them, a few scribbly marks for grain and cracks, flat muted fills with hard-edged darker and lighter tones. Low detail: a few big plain shapes and three or four ornaments at most.
- The level is the size and the richness of the building, as the subject says: a low level is small and plain, a high level is bigger, taller and richer in parts. The same building keeps its kind, its roof colour, its banner or sign and its door from one level to the next; it grows, it is not replaced.
- Keep the whole building inside the canvas with a clear empty margin on all four sides. Never crop the roof, the flag or the ground at its foot.
- Draw no character, no creature, no animal unless the subject names one, no shadow cast on the ground beyond the building's own foot, no glow, no fog, no text, no letters, no numbers, no name on a sign (every sign is blank) and no logo. Output a transparent PNG with all four corner pixels at alpha 0.

## 30. Building Reference Rule

The last part of a `building` prompt. For level 2 and 3 the building's own lower level (`output/building/<id>_<level-1>.png`) is attached after the style sheet (`--extra-reference`); the rule says what each image is for.

```text
The first attached image is a sheet of three characters of our game, drawn in our
style. Use it only as a style reference for the hand-drawn wobbly ink line, the
scribbly marks, the shape simplification, the flat muted color range and the
shading amount. Do not draw any character or any part of one, and do not
reproduce the sheet's white background. If a second image is attached, it is
this same building of our town at the level just below the one to draw, already
approved: keep its kind, its viewpoint, its roof colour, its door, its banner or
sign and its colours, and draw the same building grown to the new level as the
subject says, bigger and richer, not a new design. Draw only the described
building, as a new standalone picture in the same drawing style on a transparent
background.
```

## 31. Building Forbidden

Paste as the AVOID block of a `building` prompt, in place of §2 (a building has its own patch of ground and is not a figure).

```text
avoid: realistic, photorealistic, 3D render, CGI, painterly, watercolor, oil painting,
soft shading, gradients, airbrush, textured fills, glossy highlights, neon, glow,
bloom, lens flare, bright saturated colors, pastel, clean vector line, uniform line
weight, sparkles, fog, drop shadow, a road, hills, sky, a horizon, neighbouring
buildings, a whole town, people, characters, creatures, faces, text, letters,
numbers, a name on a sign, logo, title, watermark, frame, border, user interface,
cropped roof, cropped ground
```

## Dungeon: abandoned_mine

### Creatures

- This creature lives in an abandoned mine that goblins have taken over.
- Its eyes are filled with one flat dull lantern yellow, with no pupils.
- It is dusted with soot and grey rock dust.
- A goblin wears one piece of scavenged mining gear, such as a dented miner's cap with a candle stub, a small tin lantern on the belt, a coil of rope or a leather apron. A beast carries no gear.
- Gear and accents use the colors of the mine: rust orange, soot grey and dull lantern yellow.

### Place

- The place is an abandoned mine that goblins have taken over: a rough rock tunnel.
- Old timber frames, two posts and a beam, shore the back wall at even intervals. A tin lantern with a candle stub hangs on some of the posts.
- A rusted rail track runs along the floor near the back wall. An ore cart, crates, coils of rope, a pickaxe and loose rock are what is left lying around.
- The colors of the mine: dark brown and soot grey rock, rust orange and dull lantern yellow.

## Dungeon: abyss

### Creatures

- This creature belongs to the abyss, a black pit under the dead woods where the dead were thrown and something below woke them.
- Its eyes are filled with one flat pale sickly green, with no pupils.
- It is bone-white and ash-grey, dusted with grave dirt, with cracks and chips drawn as a few scribbly marks.
- An undead soldier wears one piece of ancient grave gear, such as a rusted iron skullcap, a cracked round shield, a tattered burial shroud or an iron collar. The lord wears more: a horned iron crown and a rotting cloak.
- Gear and accents use the colors of the abyss: bone white, ash grey, rust brown and sickly pale green.

### Place

- The place is the abyss: the bottom of a black pit under dead woods, a wide cavern of dark stone where the dead were thrown.
- Bones and skulls are heaped along the back wall in piles, drawn as plain shapes. Rusted iron chains hang from the dark above, and a few broken grave markers lean in the rubble.
- A faint sickly green glow seeps from cracks in the floor near the back wall; the rest is lit by nothing. Black roots hang down from the edge of the pit far above.
- The colors of the abyss: black and ash-grey stone, bone white, rust brown and sickly pale green.

## Body: hourglass

- A voluptuous hourglass figure about four and a half heads tall, still with the big head the composition asks for.
- A full, round bust, a very narrow cinched waist, wide curvy hips and thick, shapely thighs.
- Softer, rounded shoulders and slimmer arms: never top-heavy, never a brawler's bulk.

## Body: athletic

- A toned, athletic and curvy figure about four and a half heads tall, still with the big head the composition asks for.
- A full bust, a narrow waist, round hips, strong shapely thighs and calves, and defined but feminine shoulders and arms.
- Where the outfit bares the midriff, a flat toned stomach drawn with a few simple lines for the abs.

## Face: subculture_60

사용자 이름 "서브컬쳐_60"(2026-10-14, Round 67): 중국·일본 서브컬처 여캐 얼굴을 60%, 우리 예쁜 얼굴 기준(Round 66)을 40%. 기록은 `Archive/67-archmage-subculture-face/README.md`.

### Rules

- A blend of a little more than half a modern Chinese and Japanese anime and game heroine face and a little less than half our pretty face: a heart-shaped anime face with soft round cheeks narrowing to a small pointed chin and a short lower face, the eyes set at about the middle of the head.
- Very large eyes: tall oval irises in three flat tones of the eye colour the subject gives (otherwise a deep colour that suits her), a dark pupil and two white highlights, a bold dark upper lash line flicking out at the corner and a few lower lashes.
- Thin high brows, partly under the bangs when she has bangs.
- The nose only as a tiny tick or a small shadow at the tip, never the nostrils or the wings of the nose.
- A small mouth drawn as a short line with a hint of rose lip colour, and a flat pink blush under the eyes.
- Clean, thin lines on the face; the rest of the figure stays in our hand-drawn ink line and flat color.
- A mercenary about to fight in a dark dungeon does not smile: the mouth is closed and the look is calm, cool and focused, the brows relaxed, not frowning. The only highlights in the whole figure are the small white ones in the eyes.

### Lift

- not polished anime
- anime face

### Avoid

- sparkling star eyes
- drawn nostrils
- nose wings
- wide nose
- long nose
- hooked nose
- heavy jaw
- dark lipstick
- realistic nose
- realistic lips
