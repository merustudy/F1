# F1 Style Runtime

이미지를 생성할 때 스크립트(`tools/gen_image.py`)가 읽는 스타일 규칙이다. 스타일 문구는 이 파일에만 둔다.
2026-10-04 그림체 전환(Round 12): 캐릭터·적의 그림체는 사용자가 든 포스터의 손맛(손으로 그린 흔들리는 잉크선, 낙서 획, 고무처럼 부푼 과장, 캐릭터마다 다른 체형과 매력적인 캐리커처 얼굴)에
지금의 평면·낮은 채도 색을 그대로 얹은 것이다. 이전 문서는 `Archive/flat-v1/`. 아이템·배경·UI의 절(§7~§22)도 같은 날 선(손으로 그린 잉크선, 낙서 획)과 기준 그림(확정 셋의 시트)을 새 그림체로 맞췄다. 색·구도·크기 규칙은 그대로다.
방향과 기준 그림은 [`Docs/Design/10_Art_Direction.md`](../Docs/Design/10_Art_Direction.md), 파이프라인의 규칙은
[`Docs/Architecture/13_ART_PIPELINE.md`](../Docs/Architecture/13_ART_PIPELINE.md)가 소유한다.

**조립 순서:** 타입의 Style Rule → Subject → 타입 섹션 → (던전의 것이면) 던전 컨셉 → 타입의 Forbidden → 타입의 Reference Rule
**형식 계약:** 번호 섹션의 제목은 `## <번호>. <제목>`. §1, §2와 Reference Rule 섹션에는 ```` ```text ```` 블록이 하나씩 있고, 타입 섹션은 `- ` 불릿으로 쓴다.
던전 컨셉 섹션의 제목은 `## Dungeon: <DungeonData.Id>`이고, 그 안의 `### Creatures`(적)와 `### Place`(배경) 아래에 `- ` 불릿으로 쓴다.
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
figure
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
- Proportions and build: exaggerated and rubbery, between three and five heads tall as the subject says, always with a big head: the head is as big as the heads of the characters on the reference sheet. A tall, broad or heroic build is shown by the body, never by a smaller head. Every character has a strongly different body type (brawny and top-heavy, squat and barrel-shaped, tall and lanky, round and soft, lean and wiry): follow the subject. A woman keeps a curvy figure (a full bust, a narrow waist, full hips) whatever her build. Rubbery limbs with no muscles drawn inside, big hands and big boots.
- Face: caricatured but attractive, as the subject says: large expressive eyes with dark pupils (one may be a touch larger than the other), well-shaped thick eyebrows, a distinctive but elegant nose (long and straight, small and upturned, or strong and straight), a strong handsome jaw for a man and soft pretty features for a woman, and a serious mouth that does not smile (closed in a firm line, set, or pressed thin): a mercenary about to fight in a dark dungeon looks determined, stern, wary or grim, never cheerful, never grinning. A few freckle or stubble dots are fine. Each character gets its own face shape and its own kind of seriousness, never grotesque.
- Gaze: the eyes look toward the viewer's right, where the enemy stands: the pupils sit toward the right side of the eyes and the head is turned a little that way. Never looking at the viewer, never looking left.
- The eyes always have their dark pupils: never dot eyes, never blank eyes without pupils.
- Hair: big rubbery clumps with a few scribbly strand strokes.
- Clothes and gear as a few big flat shapes that fit the figure, fur drawn with short scribbly strokes, three or four simple ornaments at most. Weapons oversized and chunky, simplified to a few shapes.
- Let the figure hold the weapon as the subject says, with the whole weapon inside the canvas and its top no higher than the top of the head. Keep the whole figure inside the canvas with a clear empty margin on all four sides. Never crop the head, the feet or the weapon.
- Draw one character only: no second figure, no animal, no scenery, no ground line and no shadow under the feet. Never add text, letters, numbers or logos. Output a transparent PNG with all four corner pixels at alpha 0.

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

Added to §7 for an item that takes one cell of a board: a wide strip.

- The picture is a wide strip, a little over three times as wide as it is tall. The item fills the strip from its left end to its right end.
- Lay a long item (a sword, an axe, a mace, a staff, a bow) flat and level along the strip: its grip or lower end at the left, its point, blade or head at the right. It is not tilted.
- Lay a compact item (a shield, a flask, a pouch, a charm) on its side, or spread what belongs to it (a cord, a strap, leaves) out to the left and to the right, so that it is clearly wider than it is tall.

## 20. Item Shape: Two Cells

Added to §7 for an item that takes two cells of a board: two cells stacked, a wide rectangle.

- The picture is a wide rectangle, about three units wide to two tall. The item fills it from corner to corner.
- Lay a long item (a bow, a spear) diagonally across it: its grip or lower end at the bottom left, its point or head at the top right. Draw it as long and as slender as it really is.

## 21. Item Shape: Three Cells

Added to §7 for an item that takes three cells of a board: three cells stacked, nearly a square.

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
